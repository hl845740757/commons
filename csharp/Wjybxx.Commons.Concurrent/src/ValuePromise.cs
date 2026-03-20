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
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using Wjybxx.Commons.Pool;
using static Wjybxx.Commons.Concurrent.AbstractPromise;

namespace Wjybxx.Commons.Concurrent
{
#nullable restore

/// <summary>
///
/// 1.该类型由于要复用，不能继承Promise，否则可能导致用户使用到错误的接口，也可能导致类型测试时的混乱。
/// 2.统一在用户获得结果后触发回收。
/// 3.该实现并不是严格线程安全的，但在使用<see cref="ValueFuture{T}"/>的情况下是安全的。
/// </summary>
/// <typeparam name="T"></typeparam>
public class ValuePromise<T> : IValuePromise<T>
{
#nullable disable
    /// <summary>
    /// 任务的结果
    /// </summary>
    private T _result;
#nullable restore
    ///<summary>
    /// 任务失败完成时的结果，也包含了任务的状态。
    /// 
    /// 1. 如果为null，表示尚未开始。
    /// 2. 如果为<see cref="EX_COMPUTING"/>，表示正在计算。
    /// 3. 如果为<see cref="EX_PUBLISHING"/>，表示成功，但正在发布成功结果。
    /// 4. 如果为<see cref="EX_SUCCESS"/>，表示成功，且结果已可见。
    /// 5. 如果为<see cref="OperationCanceledException"/>，表示取消 -- 避免捕获堆栈。
    /// 6. 如果为<see cref="ExceptionDispatchInfo"/>，表示失败。
    /// </summary>
    private volatile object? _ex;
    /// <summary>
    /// 重入id（归还到池和从池中取出时都加1）
    /// </summary>
    private int _reentryId;
    /// <summary>
    /// 回调
    /// 
    /// Q：为什么不能平铺放在Promise中？
    /// A：由于我们的框架支持异步回调，因此需要实现<see cref="ITask"/>接口；而Promise不适合直接实现<see cref="ITask"/>，这会限制子类的扩展。
    /// （回调实现为伴生对象）
    /// </summary>
    private readonly Completion _completion = new Completion();
    /// <summary>
    /// 任务绑定的Executor，用于检测死锁
    /// </summary>
    private IExecutor? _executor;

    protected ValuePromise() {
    }

    /// <summary>
    /// 测试对象是否已被回收
    /// </summary>
    /// <param name="rid"></param>
    /// <returns></returns>
    public bool IsRecycled(int rid) {
        return rid != _reentryId;
    }

    /// <summary>
    /// 当前重入id
    /// </summary>
    internal int ReentryId => _reentryId;

    /// <summary>
    /// 增加重入id(重用对象时调用)
    /// </summary>
    /// <returns>增加后的值</returns>
    internal int IncReentryId() {
        return ++_reentryId;
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    protected virtual void Reset() {
        _reentryId++;
        _result = default!;
        _completion.Reset();
        _executor = null;
        _ex = null; // store fence
    }

    /// <summary>
    /// 用户已正常获取结果信息，可以尝试回收
    /// </summary>
    protected virtual void PrepareToRecycle() {
        if (GetType() == typeof(ValuePromise<T>)) {
            POOL.Release(this);
        }
    }

    #region internal

    private bool InternalSetResult(T? result) {
        // 先测试Pending状态 -- 如果大多数任务都是先更新为Computing状态，则先测试Computing有优势，暂不优化
        object? preEx = Interlocked.CompareExchange(ref _ex, EX_PUBLISHING, null);
        if (preEx == null) {
            _result = result;
            _ex = EX_SUCCESS;
            return true;
        }
        if (preEx == EX_COMPUTING) {
            // 任务可能处于Computing状态，重试
            preEx = Interlocked.CompareExchange(ref _ex, EX_PUBLISHING, EX_COMPUTING);
            if (preEx == EX_COMPUTING) {
                _result = result;
                _ex = EX_SUCCESS;
                return true;
            }
        }
        return false;
    }

    private bool InternalSetException(object ex) {
        object result = AbstractPromise.WrapException(ex);
        // Debug.Assert(exception != null);
        // 先测试Pending状态 -- 如果大多数任务都是先更新为Computing状态，则先测试Computing有优势，暂不优化
        object? preEx = Interlocked.CompareExchange(ref _ex, result, null);
        if (preEx == null) {
            return true;
        }
        if (preEx == EX_COMPUTING) {
            // 任务可能处于Computing状态，重试
            preEx = Interlocked.CompareExchange(ref _ex, result, EX_COMPUTING);
            if (preEx == EX_COMPUTING) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 获取当前状态，如果处于发布中状态，则等待目标线程发布完毕
    /// </summary>
    /// <returns></returns>
    private int PollState() {
        object? ex = _ex;
        if (ex == null) {
            return ST_PENDING;
        }
        if (ex == EX_COMPUTING) {
            return ST_COMPUTING;
        }
        if (ex == EX_PUBLISHING) {
            // busy spin -- 该过程通常很快，因此自旋等待即可
            while ((ex = _ex) == EX_PUBLISHING) {
                Thread.SpinWait(1);
            }
            return ST_SUCCESS;
        }
        if (ex == EX_SUCCESS) {
            return ST_SUCCESS;
        }
        return ex is OperationCanceledException ? ST_CANCELLED : ST_FAILED;
    }

    #endregion

    #region promise

    private TaskStatus Status => (TaskStatus)PeekState(_ex);

    private T ResultNow() {
        int state = PollState();
        return state switch
        {
            ST_SUCCESS => _result,
            ST_FAILED => throw new IllegalStateException("Task completed with exception"),
            ST_CANCELLED => throw new IllegalStateException("Task was cancelled"),
            _ => throw new IllegalStateException("Task has not completed")
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Exception ExceptionNow(bool throwIfCancelled) {
        return AbstractPromise.ExceptionNow(PollState(), _ex, throwIfCancelled);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Internal_TrySetComputing() {
        object? preState = Interlocked.CompareExchange(ref _ex, EX_COMPUTING, null);
        return preState == null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal TaskStatus Internal_TrySetComputing2() {
        object? preState = Interlocked.CompareExchange(ref _ex, EX_COMPUTING, null);
        return (TaskStatus)PeekState(preState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Internal_TrySetResult(T? result) {
        if (InternalSetResult(result)) {
            PostComplete();
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Internal_TrySetException(Exception cause) {
        if (cause == null) throw new ArgumentNullException(nameof(cause));
        if (InternalSetException(cause)) {
            FutureLogger.LogCause(cause); // 记录日志
            PostComplete();
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Internal_TrySetException(ExceptionDispatchInfo dispatchInfo) {
        if (dispatchInfo == null) throw new ArgumentNullException(nameof(dispatchInfo));
        if (InternalSetException(dispatchInfo)) {
            PostComplete();
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Internal_TrySetCancelled(int cancelCode) {
        if (InternalSetException(StacklessCancellationException.InstOf(cancelCode))) {
            PostComplete();
            return true;
        }
        return false;
    }

    #endregion

    #region api-future

    // 这里我们不再进行特殊的优化，以允许ValueFuture获取装箱的结果；而且Promise一创建就完成的概率是比较低的
    public ValueFuture VoidFuture => new ValueFuture(this, _reentryId);

    public ValueFuture<T> Future => new ValueFuture<T>(this, _reentryId);

    #region core

    public TaskStatus GetStatus(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        return Status;
    }

    public Exception GetException(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        Exception ex = ExceptionNow(false);
        // GetResult以后归还到池
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }
        return ex;
    }

    public object GetExceptionOrDispatchInfo(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        object ex = AbstractPromise.ExceptionOrDispatchInfoNow(PollState(), _ex);
        // GetResult以后归还到池
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }
        return ex;
    }

    public void GetVoidResult(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        TaskStatus status = Status;
        if (!status.IsCompleted()) {
            throw new IllegalStateException("Task has not completed");
        }

        Exception? ex = null;
        if (status != TaskStatus.Success) {
            ex = ExceptionNow(false);
        }
        // GetResult以后归还到池
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }

        if (ex != null) {
            throw status == TaskStatus.Cancelled ? ex : new CompletionException(null, ex);
        }
    }

    public T GetResult(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        TaskStatus status = Status;
        if (!status.IsCompleted()) {
            throw new IllegalStateException("Task has not completed");
        }

        T r = default!;
        Exception? ex = null;
        if (status == TaskStatus.Success) {
            r = ResultNow();
        } else {
            ex = ExceptionNow(false);
        }
        // GetResult以后归还到池
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }

        if (ex != null) {
            throw status == TaskStatus.Cancelled ? ex : new CompletionException(null, ex);
        }
        return r;
    }

    public IFuture<U> AsFuture<U>(int reentryId) {
        // 当前的T可能是超类型，如object，因此无法简单检测类型转换的安全性
        ValidateReentryId(reentryId);
        TaskStatus status = Status;
        switch (status) {
            case TaskStatus.Success: {
                T result = GetResult(reentryId); // 触发回收
                U? castR = (U?)(object?)result;
                return Promise<U>.FromResult(castR);
            }
            case TaskStatus.Cancelled: {
                Exception ex = GetException(reentryId); // 触发回收
                return Promise<U>.FromException(ex);
            }
            case TaskStatus.Failed: {
                object ex = GetExceptionOrDispatchInfo(reentryId);
                return Promise<U>.FromException((ExceptionDispatchInfo)ex);
            }
            default: {
                // 添加回调
                Promise<U> promise = new Promise<U>(_executor);
                if (status == TaskStatus.Computing) {
                    promise.TrySetComputing();
                }
                SetCompletion(TYPE_SET_PROMISE_U, promise, null, null, 0);
                return promise;
            }
        }
    }

    public IFuture<T> AsFuture(int reentryId) {
        ValidateReentryId(reentryId);
        TaskStatus status = Status;
        switch (status) {
            case TaskStatus.Success: {
                T result = GetResult(reentryId); // 触发回收
                return Promise<T>.FromResult(result);
            }
            case TaskStatus.Cancelled: {
                Exception ex = GetException(reentryId); // 触发回收-可能是子类异常
                return Promise<T>.FromException(ex);
            }
            case TaskStatus.Failed: {
                object ex = GetExceptionOrDispatchInfo(reentryId);
                return Promise<T>.FromException((ExceptionDispatchInfo)ex);
            }
            default: {
                // 添加回调
                Promise<T> promise = new Promise<T>(_executor);
                if (status == TaskStatus.Computing) {
                    promise.TrySetComputing();
                }
                SetCompletion(TYPE_SET_PROMISE_T, promise, null, null, 0);
                return promise;
            }
        }
    }

    public void Forget(int reentryId) {
        ValidateReentryId(reentryId);
        SetCompletion(TYPE_FORGET, "", null, null, 0);
    }

    #endregion

    #region 回调

    public void OnCompleted(int reentryId, Action<object?> continuation, object? state, int options = 0) {
        ValidateReentryId(reentryId);
        SetCompletion(TYPE_RUN_CTX, continuation, state, null, options);
    }

    public void OnCompletedAsync(int reentryId, IExecutor executor, Action<object?> continuation, object? state, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        ValidateReentryId(reentryId);
        SetCompletion(TYPE_RUN_CTX, continuation, state, executor, options);
    }

    #endregion

    private void SetCompletion(int type, object action, object? state, IExecutor? executor, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        // 去除用户的低位，记录type
        options &= (~TaskOptions.MASK_CTL_RESERVED);
        options |= type;

        // 先尝试锁定为发布状态，PostComplete会等待发布
        Completion completion = _completion;
        int oldCtl = Interlocked.CompareExchange(ref completion.ctl, MASK_PUBLISHING, 0);
        if (oldCtl == 0) {
            completion.input = this;
            completion.executor = executor;
            completion.options = options;
            completion.action = action;
            completion.state = state;
            Volatile.Write(ref completion.ctl, MASK_PUBLISHED);
            return;
        }
        // 检测重复添加监听器(包含发布标记)
        if (oldCtl != MASK_FIRED) {
            throw new InvalidOperationException("Already continuation registered, can not await twice or get result after await.");
        }
        // Future已完成
        completion.input = this;
        completion.executor = executor;
        completion.options = options;
        completion.action = action;
        completion.state = state;
        completion.TryFire(SYNC);
    }

    private void PostComplete() {
        while (true) {
            int ctl = Volatile.Read(ref _completion.ctl);
            if (ctl == MASK_PUBLISHING) {
                Thread.SpinWait(1);
                continue;
            }
            // ctl == 0 || ctl == MASK_PUBLISHED
            // 需要保留原始ctl，以识是否重复添加监听器
            int nextCtl = (ctl | MASK_FIRED);
            if (Interlocked.CompareExchange(ref _completion.ctl, nextCtl, ctl) != ctl) {
                Thread.SpinWait(1);
                continue;
            }
            if (ctl != 0) {
                _completion.TryFire(SYNC);
            }
            return;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateReentryId(int reentryId, bool ignoreReentrant = false) {
        if (ignoreReentrant || reentryId == this._reentryId) {
            return;
        }
        throw new IllegalStateException("promise has been reused");
    }

    #endregion

    #region api-promise

    public bool TrySetComputing(int reentryId) {
        ValidateReentryId(reentryId);
        return Internal_TrySetComputing();
    }

    public TaskStatus TrySetComputing2(int reentryId) {
        ValidateReentryId(reentryId);
        return Internal_TrySetComputing2();
    }

    public void SetComputing(int reentryId) {
        ValidateReentryId(reentryId);
        if (!Internal_TrySetComputing()) {
            throw new IllegalStateException("Already computing");
        }
    }

    public bool TrySetResult(int reentryId, T result) {
        ValidateReentryId(reentryId);
        return Internal_TrySetResult(result);
    }

    public void SetResult(int reentryId, T result) {
        ValidateReentryId(reentryId);
        if (!Internal_TrySetResult(result)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public bool TrySetException(int reentryId, Exception cause) {
        ValidateReentryId(reentryId);
        return Internal_TrySetException(cause);
    }

    public void SetException(int reentryId, Exception cause) {
        ValidateReentryId(reentryId);
        if (!Internal_TrySetException(cause)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public bool TrySetException(int reentryId, ExceptionDispatchInfo dispatchInfo) {
        ValidateReentryId(reentryId);
        return Internal_TrySetException(dispatchInfo);
    }

    public void SetException(int reentryId, ExceptionDispatchInfo dispatchInfo) {
        ValidateReentryId(reentryId);
        if (!Internal_TrySetException(dispatchInfo)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public bool TrySetCancelled(int reentryId, int cancelCode) {
        ValidateReentryId(reentryId);
        return Internal_TrySetCancelled(cancelCode);
    }

    public void SetCancelled(int reentryId, int cancelCode) {
        ValidateReentryId(reentryId);
        if (!Internal_TrySetCancelled(cancelCode)) {
            throw new IllegalStateException("Already complete");
        }
    }

    #endregion

    #region completion

    private const int TYPE_RUN = 0;
    private const int TYPE_RUN_CTX = 1;
    private const int TYPE_SET_PROMISE_U = 2;
    private const int TYPE_SET_PROMISE_T = 3;
    private const int TYPE_FORGET = 4;

    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    private const int MASK_TASK_TYPE = 0x0F;

    /** 正发布回调 -- future需要等待发布完成 */
    private const int MASK_PUBLISHING = 0x10;
    /** 已发布回调 */
    private const int MASK_PUBLISHED = 0x20;
    /** 已通知回调 */
    private const int MASK_FIRED = 0x40;

    private class Completion : ITask
    {
#nullable disable
        /// <summary>
        /// 部分回调依赖Promise的数据
        /// </summary>
        internal ValuePromise<T> input;
        /// <summary>
        /// 控制标识，用于保证可见性
        /// 1.如果为0表示尚未发布action。
        /// 2.如果等于<see cref="ValuePromise{T}.MASK_PUBLISHING"/>表示正在发布回调。
        /// 3.如果包含<see cref="ValuePromise{T}.MASK_PUBLISHED"/>表示已发布回调。
        /// 4.如果等于<see cref="ValuePromise{T}.MASK_FIRED"/>表示已通知回调。
        ///
        /// （不和options共享字段，避免额外的复杂度）
        /// </summary>
        internal int ctl;

        /// <summary>
        /// 回调线程
        /// </summary>
        internal IExecutor executor;
        /// <summary>
        /// 回调任务选项
        /// PS：低8位存储任务类型和其它控制标记。
        /// </summary>
        internal int options;
        /// <summary>
        /// 回调
        /// </summary>
        internal object action;
        /// <summary>
        /// 回调参数
        /// </summary>
        internal object state;
#nullable restore

        public void Reset() {
            input = null;
            ctl = 0;
            executor = null;
            options = 0;
            action = null;
            state = null;
        }

        public int Options => options;

        public void Run() {
            TryFire(ASYNC);
        }

        private bool Claim() {
            IExecutor e = this.executor;
            if (e == CLAIMED) {
                return true;
            }
            this.executor = CLAIMED;
            if (!ExecutorUtil.IsInlinable(e, options)) {
                e.Execute(this);
                return false;
            }
            return true;
        }

        public void TryFire(int mode) {
            if (ExecutorUtil.IsCancelRequested(state, options)) {
                return;
            }
            // 异步模式下已经claim
            if (mode <= 0 && !Claim()) {
                return;
            }
            FireNow();
            // 这里不能清理数据，由用户的Action调用GetResult触发回收时清理
        }

        private void FireNow() {
            int taskType = (options & MASK_TASK_TYPE);
            try {
                switch (taskType) {
                    case TYPE_RUN: {
                        Action action = (Action)this.action;
                        action();
                        break;
                    }
                    case TYPE_RUN_CTX: {
                        Action<object> action = (Action<object>)this.action;
                        action(state);
                        break;
                    }
                    case TYPE_SET_PROMISE_U: {
                        // 装箱
                        IPromise output = (IPromise)this.action;
                        if (input.Status == TaskStatus.Success) {
                            output.TrySetResult(input.ResultNow());
                        } else {
                            output.TrySetException(input.ExceptionNow(false));
                        }
                        // 用户已获取结果
                        input.PrepareToRecycle();
                        break;
                    }
                    case TYPE_SET_PROMISE_T: {
                        // 非装箱
                        IPromise<T> output = (IPromise<T>)this.action;
                        if (input.Status == TaskStatus.Success) {
                            output.TrySetResult(input.ResultNow());
                        } else {
                            output.TrySetException(input.ExceptionNow(false));
                        }
                        // 用户已获取结果
                        input.PrepareToRecycle();
                        break;
                    }
                    case TYPE_FORGET: {
                        // 用户不需要结果
                        input.PrepareToRecycle();
                        break;
                    }
                    default: {
                        throw new IllegalStateException();
                    }
                }
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "Value promise fire caught exception");
            }
        }
    }

    #endregion

    #region factory

    private static readonly ConcurrentObjectPool<ValuePromise<T>> POOL =
        new(() => new ValuePromise<T>(), e => e.Reset(),
            TaskPoolConfig.GetPoolSize<T>(TaskPoolType.ValuePromise));

    /// <summary>
    /// 申请一个Promise对象
    ///
    /// 1.如果没有回调添加，可使用<see cref="Forget"/>触发回收。
    /// 2.如果Promise不会发布给其它对象，则可以使用该方法申请对象。
    /// </summary>
    /// <param name="executor">任务关联的线程</param>
    /// <returns></returns>
    public static ValuePromise<T> Acquire(IExecutor? executor = null) {
        ValuePromise<T> promise = POOL.Acquire();
        promise.IncReentryId();
        promise._executor = executor;
        return promise;
    }

    /// <summary>
    /// 该接口用于外部库申请ValuePromise
    ///
    /// 1.如果没有回调添加，可使用<see cref="Forget"/>触发回收。
    /// 2.如果Promise可能有多个持有者，则需要持有rid。
    /// </summary>
    /// <param name="rid">接收Promise的重入版本id</param>
    /// <param name="executor">任务关联的线程</param>
    /// <returns></returns>
    public static ValuePromise<T> Acquire(out int rid, IExecutor? executor = null) {
        ValuePromise<T> promise = POOL.Acquire();
        rid = promise.IncReentryId();
        promise._executor = executor;
        return promise;
    }

    #endregion
}
}