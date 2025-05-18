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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 取消令牌
///
/// 与<see cref="Promise{T}"/>的实现差异：
/// 1.监听器列表为双向链表，通过锁互斥。
/// 2.监听器的通知未做栈深度优化，因为与监听器列表的删除冲突。
/// </summary>
public sealed class CancelTokenSource : ICancelTokenSource
{
    /// <summary>
    /// 默认的延迟调度器
    /// </summary>
    private static readonly IScheduledExecutorService _delayer = GlobalEventLoop.Inst;

    private volatile int code;
    private int _lock; // 维护双向链表
    private Completion? _head;
    private Completion? _tail;

    public CancelTokenSource() {
    }

    public CancelTokenSource(int code) {
        if (code != 0) {
            this.code = CancelCodes.CheckCode(code);
        }
    }

    public bool CanBeCancelled => true;

    ICancelTokenSource ICancelTokenSource.NewInstance(bool copyCode) => NewInstance(copyCode);

    public CancelTokenSource NewInstance(bool copyCode = false) {
        return new CancelTokenSource(copyCode ? code : 0);
    }

    #region tokenSource

    public bool Cancel(int cancelCode = CancelCodes.REASON_DEFAULT) {
        CancelCodes.CheckCode(cancelCode);
        int preCode = InternalCancel(cancelCode);
        if (preCode != 0) {
            return false;
        }
        PostComplete(this);
        return true;
    }

    /// <summary>
    /// 在一段时间后发送取消命令
    /// </summary>
    /// <param name="cancelCode">取消码</param>
    /// <param name="millisecondsDelay">延迟时间(毫秒)</param>
    public void CancelAfter(int cancelCode, long millisecondsDelay) {
        CancelAfter(cancelCode, TimeSpan.FromMilliseconds(millisecondsDelay), _delayer);
    }

    /// <summary>
    /// 在一段时间后发送取消命令
    /// </summary>
    /// <param name="cancelCode">取消码</param>
    /// <param name="timeSpan">延迟时间</param>
    public void CancelAfter(int cancelCode, TimeSpan timeSpan) {
        CancelAfter(cancelCode, timeSpan, _delayer);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancelCode">取消码</param>
    /// <param name="timeSpan">延迟时间</param>
    /// <param name="delayer">调度器</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void CancelAfter(int cancelCode, TimeSpan timeSpan, IScheduledExecutorService delayer) {
        if (delayer == null) throw new ArgumentNullException(nameof(delayer));
        ScheduledTaskBuilder<int> builder = ScheduledTaskBuilder.NewTask(new Canceller(this, cancelCode));
        builder.SetOnlyOnce(timeSpan.Ticks, new TimeSpan(1));
        delayer.Schedule(in builder);
    }

    private class Canceller : ITask
    {
        private readonly CancelTokenSource source;
        private readonly int cancelCode;

        public Canceller(CancelTokenSource source, int cancelCode) {
            this.source = source;
            this.cancelCode = cancelCode;
        }

        public int Options => 0;

        public void Run() {
            source.Cancel(cancelCode);
        }
    }

    #endregion

    #region code

    public int CancelCode => code;

    public bool IsCancelRequested => code != 0;

    public int Reason => CancelCodes.GetReason(code);

    public void CheckCancel() {
        int code = this.code;
        if (code != 0) {
            throw new BetterCancellationException(code);
        }
    }

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

    #region uni-transfer

    public Registration ThenTransferTo(ICancelTokenSource child, int options = 0) {
        return PushUniTransfer(null, child, options);
    }

    public Registration ThenTransferToAsync(IExecutor executor, ICancelTokenSource child, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniTransfer(executor, child, options);
    }

    private Registration PushUniTransfer(IExecutor? executor, ICancelTokenSource child, int options) {
        if (child == null) throw new ArgumentNullException(nameof(child));
        if (IsCancelRequested && executor == null) {
            Completion.FireNow(this, TYPE_TRANSFER, child, null);
            return Registration.Closed;
        }
        Completion completion = GetCompletion(executor, options, this, TYPE_TRANSFER, child, null);
        return PushCompletion(completion);
    }

    #endregion

    #endregion

    #region core

    private const int SYNC = AbstractPromise.SYNC;
    private const int ASYNC = AbstractPromise.ASYNC;
    private const int NESTED = AbstractPromise.NESTED;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int InternalCancel(int cancelCode) {
        Debug.Assert(cancelCode != 0);
        return Interlocked.CompareExchange(ref code, cancelCode, 0);
    }

    private void EnterLock() {
        SpinWait spinWait = default;
        if (Interlocked.CompareExchange(ref _lock, 1, 0) != 0) {
            spinWait.SpinOnce();
        }
    }

    private void ExitLock() {
        Debug.Assert(_lock == 1);
        Volatile.Write(ref _lock, 0);
    }

    private Registration PushCompletion(Completion newHead) {
        var cancelToken = ExecutorCoreUtil.GetCancelToken(newHead.ctx, newHead.options);
        if (cancelToken.IsCancelRequested) {
            return default;
        }
        if (IsCancelRequested) {
            newHead.TryFire(SYNC);
            return default;
        }
        Registration registration;
        EnterLock();
        try {
            if (IsCancelRequested) {
                registration = default;
                goto outer;
            }
            // 双向绑定
            if (_head != null) {
                newHead.next = _head;
                _head.prev = newHead;
            }
            _head = newHead;
            if (_tail == null) {
                _tail = newHead;
            }
            registration = new Registration(newHead, newHead._rid);
        }
        finally {
            ExitLock();
        }
        outer:
        if (!registration.HasResource) {
            newHead.TryFire(SYNC);
        } else {
            // 如果目标令牌是池化的对象，这里可能添加到目标CTS新的生命周期，导致内存泄漏...真想处理这个问题，需要使用可重入锁
            if (cancelToken.CanBeCancelled
                && TaskOptions.IsEnabled(newHead.options, TaskOptions.STAGE_LISTEN_CANCEL_TOKEN)) {
                cancelToken.ThenRun(INVOKER, registration, TaskOptions.STAGE_UNCANCELLABLE_CTX);
            }
        }
        return registration;
    }

    /// <summary>
    /// 弹出一个回调
    /// </summary>
    /// <returns></returns>
    private Completion? PopCompletion() {
        EnterLock();
        try {
            Completion head = _head;
            if (head == null) {
                return null;
            }
            _head = head.next;
            if (_tail == head) {
                _tail = null;
            }
            // 解除双向绑定
            if (_head != null) {
                head.next = null;
                _head.prev = null;
            }
            Debug.Assert(head.prev == null);
            return head;
        }
        finally {
            ExitLock();
        }
    }

    /// <summary>
    /// 如果返回true，则应该由调用方触发回收
    ///
    /// 注意：虽然node的前后置是由当前锁维护可见性的，但source不是，因此应当在Node的锁内调用。
    /// </summary>
    /// <param name="node"></param>
    /// <returns>是否删除成功</returns>
    private bool RemoveCompletion(Completion node) {
        EnterLock();
        try {
            Completion prev = node.prev;
            Completion next = node.next;
            bool contains;
            if (prev == null) {
                contains = node == _head;
            } else if (next == null) {
                contains = node == _tail;
            } else {
                contains = true;
            }
            if (!contains) {
                return false;
            }
            // fixPointers
            if (_head == _tail) {
                _head = _tail = null;
            } else if (node == _head) {
                _head = next;
                next!.prev = null;
            } else if (node == _tail) {
                _tail = prev;
                prev!.next = null;
            } else {
                prev!.next = next;
                next!.prev = prev;
            }
            // help gc
            node.prev = null;
            node.next = null;
            return true;
        }
        finally {
            ExitLock();
        }
    }

    private static void PostComplete(CancelTokenSource source) {
        Completion next;
        while ((next = source.PopCompletion()) != null) {
            next.TryFire(SYNC);
        }
    }

    #endregion

    #region completion

    private const int TYPE_ACCEPT = 0;
    private const int TYPE_ACCEPT_CTX = 1;
    private const int TYPE_RUN = 2;
    private const int TYPE_RUN_CTX = 3;
    private const int TYPE_NOTIFY = 4;
    private const int TYPE_TRANSFER = 5;

    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    private const int MASK_TASK_TYPE = 0x0F;
    /** 已加入异步队列 -- 不能被立即销毁；必须由TryFire销毁 */
    private const int MASK_ASYNC_FIRING = 0x10;

    private static readonly ConcurrentObjectPool<Completion> POOL = new(
        () => new Completion(), c => c.Reset(), TaskPoolConfig.GetPoolSize<int>(TaskPoolType.CtsCompletion));


    private static Completion GetCompletion(IExecutor? executor, int options, CancelTokenSource source,
                                            int type, object action, object? ctx) {
        // 去除用户的低位，记录type
        options &= (~TaskOptions.MASK_CTL_RESERVED);
        options |= type;

        Completion completion = POOL.Acquire();
        completion._rid++; // 从池中取出时也加1
        completion.executor = executor;
        completion.options = options;
        completion.source = source;
        completion.action = action;
        completion.ctx = ctx;
        return completion;
    }

    /// <summary>
    /// 用于关闭监听器
    /// </summary>
    private static readonly Action<object> INVOKER = (ctx => {
        Registration registration = (Registration)ctx;
        registration.Dispose();
    });

    /// <summary>
    /// 回收的时机：
    /// 1.如果已进入异步派发阶段，则由{@code TryFire(ASYNC)}负责回收
    /// 2.如果尚未进入异步派发阶段，则由<see cref="CancelTokenSource.RemoveCompletion"/>成功的一方负责回收。
    /// </summary>
    private sealed class Completion : ITask, IPooledDisposable
    {
        /** 前后置的可见性由<see cref="CancelTokenSource._lock"/>保护 */
        internal Completion? prev;
        internal Completion? next;

        /** 重入id -- 只增不减；需要在锁的保护下更新，即归还到池需要在锁的保护下 */
        internal int _rid;
        /** 派发和回收的通知锁 */
        internal int _lock;

#nullable disable
        /// <summary>
        /// 关联的取消令牌--从取消令牌上删除时仍需要保留
        /// </summary>
        internal CancelTokenSource source;
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
#nullable enable

        internal void Reset() {
            Debug.Assert(prev == null && next == null);
            Debug.Assert(_lock == 1);
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

        internal void TryFire(int mode) {
            // 如果走到这里，当前Completion一定未被回收，但action可能已被清理，即已收到取消信号
            CancelTokenSource source;
            IExecutor executor;
            int options;
            object? action;
            object? ctx;

            EnterLock();
            bool fire;
            try {
                options = this.options;
                action = this.action;
                ctx = this.ctx;
                // 如果已收到取消信号，则直接回收
                if (action == null || ExecutorCoreUtil.IsCancelRequested(ctx, options)) {
                    POOL.Release(this);
                    return;
                }
                source = this.source;
                executor = this.executor;

                // 如果是同步模式，需要claim -- 与Future的实现不同，我们在锁外提交任务以避免阻塞，但需要先标记
                if (mode <= 0 && !ExecutorCoreUtil.IsInlinable(executor, options)) {
                    this.options |= MASK_ASYNC_FIRING;
                    this.executor = null;
                    fire = false;
                } else {
                    // 数据已拷贝到临时变量；在锁内回收，锁外执行
                    POOL.Release(this);
                    fire = true;
                }
            }
            finally {
                ExitLock();
            }
            // 在锁外执行回调和提交任务
            if (fire) {
                FireNow(source, options, action, ctx);
                return;
            }
            try {
                executor.Execute(this);
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex);
            }
        }

        internal static void FireNow(CancelTokenSource source,
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
                    case TYPE_TRANSFER: {
                        // 这里本来有一个递归优化，为简化逻辑删除了
                        ICancelTokenSource action = (ICancelTokenSource)rawAction;
                        action.Cancel(source.code);
                        break;
                    }
                    default: {
                        throw new IllegalStateException();
                    }
                }
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "Action caught an exception");
            }
        }

        public bool IsDisposed(long reentryId) {
            EnterLock();
            try {
                return reentryId != _rid || this.action == null;
            }
            finally {
                ExitLock();
            }
        }

        public void Dispose(long reentryId) {
            EnterLock();
            try {
                // 只有rid匹配时，才能保证数据有效性 -- action为null表示已执行回调
                if (_rid != reentryId || this.action == null) {
                    return;
                }
                this.action = null;
                this.ctx = null;
                // 如果当前未进入异步执行，尝试立即回收 -- 可能正处于TryFire等待锁的状态，因此删除可能会失败
                if ((options & MASK_ASYNC_FIRING) == 0 && source.RemoveCompletion(this)) {
                    POOL.Release(this);
                }
            }
            finally {
                ExitLock();
            }
        }

        internal void EnterLock() {
            SpinWait spinWait = default;
            if (Interlocked.CompareExchange(ref _lock, 1, 0) != 0) {
                spinWait.SpinOnce();
            }
        }

        internal void ExitLock() {
            Debug.Assert(_lock == 1);
            Volatile.Write(ref _lock, 0);
        }
    }

    #endregion
}
}