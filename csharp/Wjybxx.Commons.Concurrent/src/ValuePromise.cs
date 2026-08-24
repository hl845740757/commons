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
using System.Runtime.ExceptionServices;
using System.Threading;
using Wjybxx.Commons.Pool;
using static Wjybxx.Commons.Concurrent.AbstractPromise;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
///
/// 1.该类型由于要复用，不能继承Promise，否则可能导致用户使用到错误的接口，也可能导致类型测试时的混乱。
/// 2.统一在用户获得结果后触发回收。
/// 3.该实现并不是严格线程安全的，但在使用<see cref="ValueFuture{T}"/>的情况下是安全的。
/// </summary>
/// <typeparam name="T"></typeparam>
public class ValuePromise<T> : IValuePromise<T>, ITask
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
    /// 回调数据
    /// PS：ValuePromise自身实现<see cref="ITask"/>，因此回调数据可使用值类型管理。
    /// </summary>
    private Completion _completion;
    /// <summary>
    /// 任务绑定的Executor，用于检测死锁
    /// </summary>
    private IExecutor? _executor;

    protected ValuePromise() {
    }

    /// <summary>
    /// 测试对象是否已被回收
    /// </summary>
    public bool IsRecycled(int rid) {
        return rid != _reentryId;
    }

    /// <summary>
    /// Promise是否已回收或已完成
    /// </summary>
    public bool IsRecycledOrCompleted(int rid) {
        return rid != _reentryId || PeekState(_ex) >= ST_SUCCESS;
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
#pragma warning disable CS0420
        _reentryId++;
        _result = default!;
        _completion = default;
        _executor = null;
        ref object? exRef = ref _ex; // 去除volatile内存屏障，由对象池保证可见性
        exRef = null;
    }

    /// <summary>
    /// 用户已正常获取结果信息，可以尝试回收
    /// </summary>
    protected virtual void PrepareToRecycle() {
        if (GetType() == typeof(ValuePromise<T>)) {
            POOL?.Release(this);
        }
    }

    #region internal

#pragma warning disable CS0420
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
        object result = WrapException(ex);
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

    #endregion

    #region promise

    private TaskStatus Status => (TaskStatus)PeekState(_ex);

    private T ResultNow() {
        int state = PollState(ref _ex);
        return state switch
        {
            ST_SUCCESS => _result,
            ST_FAILED => throw new InvalidOperationException("Task completed with exception"),
            ST_CANCELLED => throw new InvalidOperationException("Task was cancelled"),
            _ => throw new InvalidOperationException("Task has not completed")
        };
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
            if (cause is not OperationCanceledException) {
                FutureLogger.LogCause(cause); // 记录日志
            }
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
    internal bool Internal_TrySetCancelled(CancellationToken cts) {
        if (PeekState(_ex) > ST_COMPUTING) return false; // 避免创建不必要的异常
        if (InternalSetException(new OperationCanceledException(cts))) {
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
        return (TaskStatus)PeekState(_ex);
    }

    public Exception GetException(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        Exception ex = ExceptionNow(ref _ex);
        // GetResult以后归还到池
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }
        return ex;
    }

    public object GetExceptionOrDispatchInfo(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        object ex = ExceptionOrDispatchInfoNow(ref _ex);
        // GetResult以后归还到池
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }
        return ex;
    }

    public void GetVoidResult(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        TaskStatus status = (TaskStatus)PollState(ref _ex);
        if (!status.IsCompleted()) {
            throw new InvalidOperationException("Task has not completed");
        }

        object? ex = null;
        if (status != TaskStatus.Success) {
            ex = ExceptionOrDispatchInfoNow(ref _ex);
        }
        // GetResult以后归还到池
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }

        if (ex != null) {
            if (ex is ExceptionDispatchInfo dispatchInfo) {
                dispatchInfo.Throw();
            } else {
                throw (OperationCanceledException)ex;
            }
        }
    }

    public T GetResult(int reentryId, bool ignoreReentrant = false) {
        ValidateReentryId(reentryId, ignoreReentrant);
        TaskStatus status = (TaskStatus)PollState(ref _ex);
        if (!status.IsCompleted()) {
            throw new InvalidOperationException("Task has not completed");
        }

        T r = default!;
        object? ex = null;
        if (status == TaskStatus.Success) {
            r = ResultNow();
        } else {
            ex = ExceptionOrDispatchInfoNow(ref _ex);
        }
        // GetResult以后归还到池
        if (!ignoreReentrant) {
            PrepareToRecycle();
        }

        if (ex != null) {
            if (ex is ExceptionDispatchInfo dispatchInfo) {
                dispatchInfo.Throw();
            } else {
                throw (OperationCanceledException)ex;
            }
        }
        return r;
    }

    public IFuture<U> AsFuture<U>(int reentryId) {
        // 当前的T可能是超类型，如object，因此无法简单检测类型转换的安全性
        ValidateReentryId(reentryId);
        TaskStatus status = (TaskStatus)PollState(ref _ex);
        switch (status) {
            case TaskStatus.Success: {
                T result = GetResult(reentryId); // 触发回收
                U castR = (U)(object)result; // 类型转换
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
                SetCompletion(TYPE_SET_PROMISE_U, null, promise, null, default, 0);
                return promise;
            }
        }
    }

    public IFuture<T> AsFuture(int reentryId) {
        ValidateReentryId(reentryId);
        TaskStatus status = (TaskStatus)PollState(ref _ex);
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
                SetCompletion(TYPE_SET_PROMISE_T, null, promise, null, default, 0);
                return promise;
            }
        }
    }

    public void Forget(int reentryId) {
        ValidateReentryId(reentryId);
        SetCompletion(TYPE_FORGET, null, null, null, default, 0);
    }

    #endregion

    #region 回调

    public void OnCompleted(int reentryId, Action<object?> continuation, object? state,
                            CancellationToken cancelToken = default, int options = 0) {
        ValidateReentryId(reentryId);
        SetCompletion(TYPE_RUN_CTX, continuation, state, null, cancelToken, options);
    }

    public void OnCompletedAsync(int reentryId, IExecutor executor, Action<object?> continuation, object? state,
                                 CancellationToken cancelToken = default, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        ValidateReentryId(reentryId);
        SetCompletion(TYPE_RUN_CTX, continuation, state, executor, cancelToken, options);
    }
    
    private void SetCompletion(int type, object? action, object? state,
                               IExecutor? executor, CancellationToken cancelToken, int options) {
        // if (action == null) throw new ArgumentNullException(nameof(action));
        // 去除用户的低位，记录type
        options &= (~TaskOptions.MASK_CTL_RESERVED);
        options |= type;

        // 注意：_completion为值类型，必须直接通过字段访问，不可赋值给局部变量（否则将操作副本）
        int oldCtl = Interlocked.CompareExchange(ref _completion.ctl, MASK_PUBLISHING, 0);
        if ((oldCtl & MASK_REGISTERED) != 0) {
            throw new InvalidOperationException("Continuation registered, can not await twice or get result after await.");
        }
        _completion.executor = executor;
        _completion.cancelToken = cancelToken;
        _completion.options = options;
        _completion.action = action;
        _completion.state = state;
        if (oldCtl == 0) {
            // Future未完成或正在通知，会等待监听器发布完成
            Volatile.Write(ref _completion.ctl, MASK_PUBLISHED);
        } else {
            // Future在注册监听器前已完成通知，立即补偿触发
            Debug.Assert(oldCtl == MASK_FIRED);
            TryFire(SYNC);
        }
    }

    // 用户不会在添加回调以后，通知之前还主动查询结果，因此不存在回收竞争
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
                TryFire(SYNC);
            }
            return;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateReentryId(int reentryId, bool ignoreReentrant = false) {
        if (reentryId != this._reentryId && !ignoreReentrant) {
            throw new InvalidOperationException("promise has been reused");
        }
    }

    #endregion
    
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
            throw new InvalidOperationException("Already computing");
        }
    }

    public bool TrySetResult(int reentryId, T result) {
        ValidateReentryId(reentryId);
        return Internal_TrySetResult(result);
    }

    public void SetResult(int reentryId, T result) {
        ValidateReentryId(reentryId);
        if (!Internal_TrySetResult(result)) {
            throw new InvalidOperationException("Already complete");
        }
    }

    public bool TrySetException(int reentryId, Exception cause) {
        ValidateReentryId(reentryId);
        return Internal_TrySetException(cause);
    }

    public void SetException(int reentryId, Exception cause) {
        ValidateReentryId(reentryId);
        if (!Internal_TrySetException(cause)) {
            throw new InvalidOperationException("Already complete");
        }
    }

    public bool TrySetException(int reentryId, ExceptionDispatchInfo dispatchInfo) {
        ValidateReentryId(reentryId);
        return Internal_TrySetException(dispatchInfo);
    }

    public void SetException(int reentryId, ExceptionDispatchInfo dispatchInfo) {
        ValidateReentryId(reentryId);
        if (!Internal_TrySetException(dispatchInfo)) {
            throw new InvalidOperationException("Already complete");
        }
    }

    public bool TrySetCancelled(int reentryId, CancellationToken cts = default) {
        ValidateReentryId(reentryId);
        return Internal_TrySetCancelled(cts);
    }

    public void SetCancelled(int reentryId, CancellationToken cts = default) {
        ValidateReentryId(reentryId);
        if (!Internal_TrySetCancelled(cts)) {
            throw new InvalidOperationException("Already complete");
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

    private const int MASK_REGISTERED = MASK_PUBLISHING | MASK_PUBLISHED;

    int ITask.Options => _completion.options;

    void ITask.Run() {
        TryFire(ASYNC);
    }

    private bool Claim() {
        IExecutor? e = _completion.executor;
        if (e == CLAIMED) {
            return true;
        }
        _completion.executor = CLAIMED;
        if (!ExecutorUtil.IsInlinable(e, _completion.options)) {
            e.Execute(this); // ValuePromise自身即ITask，无需装箱
            return false;
        }
        return true;
    }

    private void TryFire(int mode) {
        if (_completion.cancelToken.IsCancellationRequested) {
            if (_completion.state is IPromise output) { // 需要使下游Promise进入取消状态
                output.TrySetCancelled(_completion.cancelToken);
            }
            PrepareToRecycle(); // 手动触发回收
            return;
        }
        // 异步模式下已经claim
        if (mode <= 0 && !Claim()) {
            return;
        }
        try {
            FireNow();
        }
        catch (Exception ex) {
            FutureLogger.LogCause(ex, "Value promise fire caught exception");
        }
        // 由用户的Action调用GetResult触发回收时清理，否则可能清理到复用后的对象
    }

    private void FireNow() {
        int taskType = (_completion.options & MASK_TASK_TYPE);
        switch (taskType) {
            case TYPE_RUN: {
                Action action = (Action)_completion.action;
                action();
                break;
            }
            case TYPE_RUN_CTX: {
                Action<object> action = (Action<object>)_completion.action;
                action(_completion.state);
                break;
            }
            case TYPE_SET_PROMISE_U: {
                // 装箱
                IPromise output = (IPromise)_completion.state;
                if (Status == TaskStatus.Success) {
                    output.TrySetResult(ResultNow());
                } else {
                    object ex = ExceptionOrDispatchInfoNow(ref _ex);
                    if (ex is ExceptionDispatchInfo dispatchInfo) {
                        output.TrySetException(dispatchInfo);
                    } else {
                        output.TrySetException((Exception)ex);
                    }
                }
                // 用户已获取结果
                PrepareToRecycle();
                break;
            }
            case TYPE_SET_PROMISE_T: {
                // 非装箱
                IPromise<T> output = (IPromise<T>)_completion.state;
                if (Status == TaskStatus.Success) {
                    output.TrySetResult(ResultNow());
                } else {
                    object ex = ExceptionOrDispatchInfoNow(ref _ex);
                    if (ex is ExceptionDispatchInfo dispatchInfo) {
                        output.TrySetException(dispatchInfo);
                    } else {
                        output.TrySetException((Exception)ex);
                    }
                }
                // 用户已获取结果
                PrepareToRecycle();
                break;
            }
            case TYPE_FORGET: {
                // 用户不需要结果
                PrepareToRecycle();
                break;
            }
            default: {
                throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// 回调数据 -- 值类型，内联在<see cref="ValuePromise{T}"/>中，避免额外的对象分配。
    /// </summary>
    private struct Completion
    {
#nullable disable
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
        /// 取消令牌
        /// </summary>
        internal CancellationToken cancelToken;
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
    }

    #endregion

    #region factory

    // 池化成本还是蛮高的，或许也可以考虑链表化
    private static readonly ConcurrentObjectPool<ValuePromise<T>>? POOL;

    static ValuePromise() {
        int poolSize = TaskPoolConfig.GetPoolSize<T>(TaskPoolType.ValuePromise);
        if (poolSize > 0) {
            POOL = new ConcurrentObjectPool<ValuePromise<T>>(() => new ValuePromise<T>(), e => e.Reset(), poolSize);
        }
    }

    /// <summary>
    /// 申请一个Promise对象
    ///
    /// 1.如果没有回调添加，可使用<see cref="Forget"/>触发回收。
    /// 2.如果Promise不会发布给其它对象，则可以使用该方法申请对象。
    /// </summary>
    /// <param name="executor">任务关联的线程</param>
    /// <returns></returns>
    public static ValuePromise<T> Acquire(IExecutor? executor = null) {
        ValuePromise<T> promise = POOL != null ? POOL.Acquire() : new ValuePromise<T>();
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
        ValuePromise<T> promise = POOL != null ? POOL.Acquire() : new ValuePromise<T>();
        rid = promise.IncReentryId();
        promise._executor = executor;
        return promise;
    }

    #endregion
}
}