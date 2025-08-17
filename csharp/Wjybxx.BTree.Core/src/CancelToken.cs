#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.BTree
{
/// <summary>
/// 行为树模块使用的取消令牌
///
/// 1.行为树模块需要的功能不多，且需要进行一些特殊的优化，因此去除对Concurrent模块的依赖。
/// 2.关于取消码的设计，可查看<see cref="CancelCodes"/>类。
/// 3.继承<see cref="ICancelTokenListener"/>是为了方便通知子Token。
/// 4.在行为树模块，Task在运行期间最多只应该添加一次监听。
/// 5.Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
/// </summary>
[NotThreadSafe]
public class CancelToken : ICancelTokenSource, ICancelTokenListener
{
    /** 取消码 -- 0表示未收到信号 */
    private int code;
    /** 监听器列表 -- 通知期间可能会被重用 */
    private readonly SmallDynamicArray<ICancelTokenListener> listeners = new(4);
    /** 用于检测复用 -- short应当足够 */
    private short reentryId;

    public CancelToken() {
    }

    public CancelToken(int code) {
        if (code != 0) {
            CancelCodes.CheckCode(code);
        }
        this.code = code;
    }

    void ICancelTokenListener.OnCancelRequested(ICancelToken cancelToken, object ctx) {
        Cancel(cancelToken.CancelCode);
    }

    ICancelTokenSource ICancelTokenSource.NewInstance(bool copyCode) {
        return NewInstance(copyCode);
    }

    /// <summary>
    /// 创建一个同类型实例(默认只拷贝环境数据)
    /// </summary>
    /// <param name="copyCode">是否拷贝当前取消码</param>
    public virtual CancelToken NewInstance(bool copyCode = false) {
        return new CancelToken(copyCode ? code : 0);
    }

    /// <summary>
    /// 重置状态(行为树模块取消令牌需要复用)
    /// 注意：该方法会静默删除监听器，可能导致监听器丢失信号。
    /// </summary>
    public virtual void Reset() {
        reentryId++;
        code = 0;
        if (listeners.ElementCount == 0) {
            return;
        }
        // 需要将监听器归还到池
        listeners.BeginItr();
        try {
            for (int idx = listeners.IndexOf(null), len = listeners.Length; idx < len; idx++) {
                var listener = listeners.Set(idx, null);
                if (listener is Completion completion) {
                    ReleaseCompletion(completion);
                }
            }
        }
        finally {
            listeners.EndItr();
        }
    }

    /// <summary>
    /// 重入id，允许外部捕获
    /// </summary>
    public int ReentryId => reentryId;

    /// <summary>
    /// 当前是否正在进行通知
    /// </summary>
    protected bool IsFiring => listeners.IsIterating;

    #region query

    public bool CanBeCancelled => true;

    /// <summary>
    /// 取消码
    /// </summary>
    public int CancelCode {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => code;
    }

    /// <summary>
    /// 当前是否收到了取消信号
    /// </summary>
    public bool IsCancelRequested {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => code != 0;
    }

    /// <summary>
    /// 取消任务的原因
    /// </summary>
    public int Reason => CancelCodes.GetReason(code);

    /// <summary>
    /// 取消的紧急程度
    /// </summary>
    public int Degree => CancelCodes.GetDegree(code);

    /// <summary>
    /// 检查当前是否收到了取消信号
    /// </summary>
    /// <exception cref="BetterCancellationException"></exception>
    public void CheckCancel() {
        if (code != 0) {
            throw new BetterCancellationException(code);
        }
    }

    #endregion

    #region cancel

    public bool Cancel(int cancelCode = CancelCodes.REASON_DEFAULT) {
        CancelCodes.CheckCode(cancelCode);
        int r = this.code;
        if (r == 0) {
            this.code = cancelCode;
            PostComplete(this);
            return true;
        }
        return false;
    }

    private static void PostComplete(CancelToken cancelToken) {
        SmallDynamicArray<ICancelTokenListener> listeners = cancelToken.listeners;
        if (listeners.Length == 0) {
            return;
        }
        int reentryId = cancelToken.reentryId;
        listeners.BeginItr();
        try {
            for (int idx = 0, len = listeners.Length; idx < len; idx++) {
                var listener = listeners.Set(idx, null);
                if (listener == null) {
                    continue;
                }
                try {
                    listener.OnCancelRequested(cancelToken, null);
                }
                catch (Exception e) {
                    TaskLogger.Info(e, "listener caught exception");
                }
                // 在通知期间被Reset
                if (reentryId != cancelToken.reentryId) {
                    return;
                }
            }
        }
        finally {
            listeners.EndItr();
        }
    }

    #endregion

    #region 监听器

    /// <summary>
    /// 添加监听器
    /// </summary>
    public void AddListener(ICancelTokenListener listener) {
        if (listener == null) throw new ArgumentNullException(nameof(listener));
        if (listener == this) throw new ArgumentException("add self");
        if (code != 0) {
            try {
                listener.OnCancelRequested(this, null);
            }
            catch (Exception e) {
                TaskLogger.Info(e, "listener caught exception");
            }
        } else {
            listeners.Add(listener);
        }
    }

    /// <summary>
    /// 删除监听器
    /// 注意：Task在处理取消信号时不需要调用该方法来删除自己，令牌会先删除Listener再通知。
    /// </summary>
    /// <param name="listener">要删除的监听器</param>
    /// <param name="firstOccurrence">是否强制正向查找删除</param>
    /// <returns>存在匹配的监听器则返回true</returns>
    public bool RemListener(ICancelTokenListener listener, bool firstOccurrence = false) {
        int index = firstOccurrence
            ? listeners.IndexOfRef(listener)
            : listeners.LastIndexOfRef(listener);
        if (index < 0) {
            return false;
        }
        listeners.Set(index, null);
        return true;
    }

    /// <summary>
    /// 查询是否存在给定的监听器
    /// </summary>
    /// <param name="listener">要查询的监听器</param>
    /// <returns>如果存在则返回true，否则返回false</returns>
    public bool HasListener(ICancelTokenListener listener) {
        return listeners.ContainsRef(listener);
    }

    /// <summary>
    /// 监听器数量
    /// </summary>
    public int ListenerCount => listeners.ElementCount;

    #endregion

    #region 监听器

    #region uni-accept

    public Registration ThenAccept(Action<ICancelToken> action, int options = 0) {
        return PushUniAccept(null, action, options);
    }

    public Registration ThenAcceptAsync(IExecutor executor, Action<ICancelToken> action, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniAccept(executor, action, options);
    }

    private Registration PushUniAccept(IExecutor? executor, Action<ICancelToken> action, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_ACCEPT, action, null);
            return Registration.Closed;
        }
        Completion completion = GetCompletion(executor, options, this, TYPE_ACCEPT, action, null);
        return PushCompletion(completion);
    }

    #endregion

    #region uni-accept-ctx

    public Registration ThenAccept(Action<ICancelToken, object> action, object? ctx, int options = 0) {
        return PushUniAcceptCtx(null, action, ctx, options);
    }

    public Registration ThenAcceptAsync(IExecutor executor, Action<ICancelToken, object> action, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniAcceptCtx(executor, action, ctx, options);
    }

    private Registration PushUniAcceptCtx(IExecutor? executor, Action<ICancelToken, object> action, object? state, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_ACCEPT_CTX, action, state);
            return Registration.Closed;
        }
        Completion completion = GetCompletion(executor, options, this, TYPE_ACCEPT_CTX, action, state);
        return PushCompletion(completion);
    }

    #endregion

    #region uni-run

    public Registration ThenRun(Action action, int options = 0) {
        return PushUniRun(null, action, options);
    }

    public Registration ThenRunAsync(IExecutor executor, Action action, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniRun(executor, action, options);
    }

    private Registration PushUniRun(IExecutor? executor, Action action, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_RUN, action, null);
            return Registration.Closed;
        }
        Completion completion = GetCompletion(executor, options, this, TYPE_RUN, action, null);
        return PushCompletion(completion);
    }

    #endregion

    #region uni-run-ctx

    public Registration ThenRun(Action<object> action, object? ctx, int options = 0) {
        return PushUniRunCtx(null, action, ctx, options);
    }

    public Registration ThenRunAsync(IExecutor executor, Action<object> action, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniRunCtx(executor, action, ctx, options);
    }

    private Registration PushUniRunCtx(IExecutor? executor, Action<object> action, object? state, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_RUN_CTX, action, state);
            return Registration.Closed;
        }
        Completion completion = GetCompletion(executor, options, this, TYPE_RUN_CTX, action, state);
        return PushCompletion(completion);
    }

    #endregion

    #region uni-notify

    public Registration ThenNotify(ICancelTokenListener action, object? ctx, int options = 0) {
        return PushUniNotify(null, action, ctx, options);
    }

    public Registration ThenNotifyAsync(IExecutor executor, ICancelTokenListener action, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniNotify(executor, action, ctx, options);
    }

    private Registration PushUniNotify(IExecutor? executor, ICancelTokenListener listener, object? ctx, int options) {
        if (listener == null) throw new ArgumentNullException(nameof(listener));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_NOTIFY, listener, ctx);
            return Registration.Closed;
        }
        Completion completion = GetCompletion(executor, options, this, TYPE_NOTIFY, listener, ctx);
        return PushCompletion(completion);
    }

    #endregion

    #endregion

    #region core

    private const int SYNC = 0;
    private const int ASYNC = 1;
    private const int NESTED = -1;

    private Registration PushCompletion(Completion newHead) {
        var cancelToken = ExecutorUtil.GetCancelToken(newHead.ctx, newHead.options);
        if (cancelToken.IsCancelRequested) {
            return default;
        }
        if (IsCancelRequested) {
            newHead.TryFire(SYNC);
            return default;
        }
        Registration registration = new Registration(newHead, newHead._rid);
        listeners.Add(newHead);

        if (cancelToken.CanBeCancelled
            && TaskOptions.IsEnabled(newHead.options, TaskOptions.STAGE_LISTEN_CANCEL_TOKEN)) {
            cancelToken.ThenRun(INVOKER, registration, TaskOptions.STAGE_UNCANCELLABLE_CTX);
        }
        return registration;
    }

    #endregion

    #region completion

    private const int TYPE_ACCEPT = 0;
    private const int TYPE_ACCEPT_CTX = 1;
    private const int TYPE_RUN = 2;
    private const int TYPE_RUN_CTX = 3;
    private const int TYPE_NOTIFY = 4;

    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    private const int MASK_TASK_TYPE = 0x0F;
    /** 已加入异步队列 -- 不能被立即关闭 */
    private const int MASK_ASYNC_FIRING = 0x10;
    /** 已收到Dispose信号 */
    private const int MASK_DISPOSED = 0x20;

    /** 行为树通常只在业务线程调用，避免多线程开销 */
    private static readonly ThreadLocal<Stack<Completion>> POOL = new(() => new Stack<Completion>());

    /**  申请一个<see cref="Completion"/>实例 */
    private static Completion GetCompletion(IExecutor? executor, int options, CancelToken source,
                                            int type, object action, object? ctx) {
        // 去除用户的低位，记录type
        options &= (~TaskOptions.MASK_CTL_RESERVED);
        options |= type;

        if (!POOL.Value!.TryPop(out Completion completion)) {
            completion = new Completion();
        }
        int rid = completion._rid + 1;
        completion._rid = rid;

        completion.executor = executor;
        completion.options = options;
        completion.source = source;
        completion.action = action;
        completion.ctx = ctx;
        return completion;
    }

    private static void ReleaseCompletion(Completion completion) {
        completion.Reset();
        POOL.Value!.Push(completion);
    }

    /// <summary>
    /// 用于关闭监听器
    /// </summary>
    private static readonly Action<object> INVOKER = (ctx => {
        Registration registration = (Registration)ctx;
        registration.Dispose();
    });

    private sealed class Completion : ITask, ICancelTokenListener, IPooledDisposable
    {
        /** 重入id -- 只增不减 */
        internal int _rid;

#nullable disable
        /// <summary>
        /// 关联的取消令牌
        /// </summary>
        internal CancelToken source;
        /// <summary>
        /// 绑定的线程
        /// </summary>
        internal IExecutor executor;
        /// <summary>
        /// 任务的调度选项，包含任务的类型
        /// </summary>
        internal int options;
        /// <summary>
        /// 用户回调
        /// </summary>
        internal object? action;
        /// <summary>
        /// 回调关联的参数
        /// </summary>
        internal object? ctx;
#nullable restore

        internal void Reset() {
            _rid++; // 池化时+1
            source = null;
            executor = null;
            options = 0;
            action = null;
            ctx = null;
        }

        public int Options => options;

        public void Run() {
            TryFire(ASYNC);
        }

        public void OnCancelRequested(ICancelToken cancelToken, object ctx) {
            TryFire(SYNC);
        }

        internal void TryFire(int mode) {
            // 如果走到这里，当前Completion一定未被回收，但action可能已被清理，即已收到取消信号
            CancelToken source;
            IExecutor executor;
            int options;
            object? action;
            object? ctx;
            // 代码可参考并发库中的实现
            bool fire;
            {
                options = this.options;
                action = this.action;
                ctx = this.ctx;
                // 如果已收到取消信号，则直接回收
                if (action == null || ExecutorUtil.IsCancelRequested(ctx, options)) {
                    ReleaseCompletion(this);
                    return;
                }
                source = this.source;
                executor = this.executor;

                // 如果是同步模式，需要claim=
                if (mode <= 0 && !ExecutorUtil.IsInlinable(executor, options)) {
                    this.options |= MASK_ASYNC_FIRING;
                    this.executor = null;
                    fire = false;
                } else {
                    // 数据已拷贝到临时变量
                    ReleaseCompletion(this);
                    fire = true;
                }
            }
            if (fire) {
                FireNow(source, options, action, ctx);
                return;
            }
            try {
                executor.Execute(this);
            }
            catch (Exception ex) {
                TaskLogger.Info(ex, "claim caught exception");
            }
        }

        internal static void FireNow(CancelToken source,
                                     int options, object rawAction, object? ctx) {
            int taskType = (options & MASK_TASK_TYPE);
            try {
                switch (taskType) {
                    case TYPE_ACCEPT: {
                        Action<ICancelToken> action = (Action<ICancelToken>)rawAction;
                        action(source);
                        break;
                    }
                    case TYPE_ACCEPT_CTX: {
                        Action<ICancelToken, object?> action = (Action<ICancelToken, object?>)rawAction;
                        action(source, ctx);
                        break;
                    }
                    case TYPE_RUN: {
                        Action action = (Action)rawAction;
                        action();
                        break;
                    }
                    case TYPE_RUN_CTX: {
                        Action<object?> action = (Action<object?>)rawAction;
                        action(ctx);
                        break;
                    }
                    case TYPE_NOTIFY: {
                        ICancelTokenListener action = (ICancelTokenListener)rawAction;
                        action.OnCancelRequested(source, ctx);
                        break;
                    }
                    default: {
                        throw new IllegalStateException();
                    }
                }
            }
            catch (Exception ex) {
                TaskLogger.Info(ex, "Action caught an exception");
            }
        }

        public bool IsDisposed(long reentryId) {
            return reentryId != _rid || this.action == null;
        }

        public void Dispose(long reentryId) {
            if (reentryId != _rid || this.action == null) {
                return;
            }
            action = null;
            ctx = null;
            // 如果当前未进入异步执行，尝试立即回收
            if ((options & MASK_ASYNC_FIRING) == 0 && source.RemListener(this)) {
                ReleaseCompletion(this);
            }
        }
    }

    #endregion
}
}