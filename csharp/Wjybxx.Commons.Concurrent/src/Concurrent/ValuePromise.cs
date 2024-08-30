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
using static Wjybxx.Commons.Concurrent.AbstractPromise;

namespace Wjybxx.Commons.Concurrent
{
#nullable enable

/// <summary>
///
/// 1.该类型由于要复用，不能继承Promise，否则可能导致用户使用到错误的接口，也可能导致类型测试时的混乱。
/// 2.统一在用户获得结果后触发回收。
/// 3.该实现并不是严格线程安全的，但在使用<see cref="ValueFuture{T}"/>的情况下是安全的。
/// </summary>
/// <typeparam name="T"></typeparam>
internal class ValuePromise<T> : IValuePromise<T>
{
    /// <summary>
    /// 任务的结果
    /// </summary>
    private T _result;
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
    /// 当前重入id
    /// </summary>
    public int ReentryId => _reentryId;

    /// <summary>
    /// 增加重入id(重用对象时调用)
    /// </summary>
    /// <returns>增加后的值</returns>
    public int IncReentryId() {
        return ++_reentryId;
    }

    /// <summary>
    /// 重置数据
    /// </summary>
    public virtual void Reset() {
        _reentryId++;
        _result = default!;
        _ex = null;
        _completion.Reset();
    }

    /// <summary>
    /// 用户已正常获取结果信息，可以尝试回收
    /// </summary>
    protected virtual void PrepareToRecycle() {
    }

    #region internal

    private bool InternalSetResult(T result) {
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

    private bool InternalSetException(Exception exception) {
        object result = WrapException(exception);
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

    /// <summary>
    /// 获取当前状态
    /// </summary>
    /// <param name="ex">当前的状态信息</param>
    /// <param name="strict">如果为true，则即将完成的情况也返回计算中</param>
    /// <returns></returns>
    private static int PeekState(object? ex, bool strict = false) {
        if (ex == null) {
            return ST_PENDING;
        }
        if (ex == EX_COMPUTING) {
            return ST_COMPUTING;
        }
        if (ex == EX_PUBLISHING) {
            return strict ? ST_COMPUTING : ST_SUCCESS;
        }
        if (ex == EX_SUCCESS) {
            return ST_SUCCESS;
        }
        return ex is OperationCanceledException ? ST_CANCELLED : ST_FAILED;
    }

    #endregion

    #region promise

    protected internal TaskStatus Status => (TaskStatus)PeekState(_ex);
    protected internal bool IsCompleted => PeekState(_ex) >= ST_SUCCESS;

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
    private Exception ExceptionNow(bool throwIfCancelled = true) {
        return AbstractPromise.ExceptionNow(PollState(), _ex, throwIfCancelled);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected internal bool Internal_TrySetComputing() {
        object? preState = Interlocked.CompareExchange(ref _ex, EX_COMPUTING, null);
        return preState == null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TaskStatus Internal_TrySetComputing2() {
        object? preState = Interlocked.CompareExchange(ref _ex, EX_COMPUTING, null);
        return (TaskStatus)PeekState(preState);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Internal_SetComputing() {
        if (!Internal_TrySetComputing()) {
            throw new IllegalStateException("Already computing");
        }
    }

    protected internal bool Internal_TrySetResult(T result) {
        if (InternalSetResult(result)) {
            PostComplete();
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Internal_SetResult(T result) {
        if (!Internal_TrySetResult(result)) {
            throw new IllegalStateException("Already complete");
        }
    }

    protected internal bool Internal_TrySetException(Exception cause) {
        if (cause == null) throw new ArgumentNullException(nameof(cause));
        if (InternalSetException(cause)) {
            FutureLogger.LogCause(cause); // 记录日志
            PostComplete();
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Internal_SetException(Exception cause) {
        if (!Internal_TrySetException(cause)) {
            throw new IllegalStateException("Already complete");
        }
    }

    protected internal bool Internal_TrySetCancelled(int cancelCode) {
        if (InternalSetException(StacklessCancellationException.InstOf(cancelCode))) {
            PostComplete();
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Internal_SetCancelled(int cancelCode) {
        if (!Internal_TrySetCancelled(cancelCode)) {
            throw new IllegalStateException("Already complete");
        }
    }

    #endregion

    #region api-future

    public ValueFuture VoidFuture {
        get {
            TaskStatus status = Status;
            switch (status) {
                case TaskStatus.Success: {
                    return new ValueFuture();
                }
                case TaskStatus.Cancelled:
                case TaskStatus.Failed: {
                    return new ValueFuture(ExceptionNow(false));
                }
                default: {
                    return new ValueFuture(this, _reentryId);
                }
            }
        }
    }

    public ValueFuture<T> Future {
        get {
            TaskStatus status = Status;
            switch (status) {
                case TaskStatus.Success: {
                    return new ValueFuture<T>(ResultNow(), null);
                }
                case TaskStatus.Cancelled:
                case TaskStatus.Failed: {
                    return new ValueFuture<T>(default, ExceptionNow(false));
                }
                default: {
                    return new ValueFuture<T>(this, _reentryId);
                }
            }
        }
    }

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

        T r = default;
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

    public void OnCompleted(int reentryId, Action<object?> continuation, object? state, int options = 0) {
        ValidateReentryId(reentryId);
        SetCompletion(TYPE_ACTION_STATE, continuation, state, null, options);
    }

    public void OnCompletedAsync(int reentryId, IExecutor executor, Action<object?> continuation, object? state, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        ValidateReentryId(reentryId);
        SetCompletion(TYPE_ACTION_STATE, continuation, state, executor, options);
    }

    public void SetVoidPromiseWhenCompleted(int reentryId, IPromise<int> promise) {
        ValidateReentryId(reentryId);
        SetCompletion(TYPE_SET_VOID_PROMISE, promise, null, null, 0);
    }

    public void SetPromiseWhenCompleted(int reentryId, IPromise<T> promise) {
        ValidateReentryId(reentryId);
        SetCompletion(TYPE_SET_PROMISE, promise, null, null, 0);
    }

    private void SetCompletion(int type, object action, object? state, IExecutor? executor, int options) {
        if (action == null) throw new ArgumentNullException(nameof(action));

        // 先尝试锁定为发布状态，PostComplete会等待发布
        Completion completion = _completion;
        int oldOptions = Interlocked.CompareExchange(ref completion.options, MASK_PUBLISHING, 0);
        if (oldOptions != 0) {
            if (oldOptions != MASK_COMPLETED) {
                throw new InvalidOperationException("Already continuation registered, can not await twice or get result after await.");
            }
            const int lockOptions = MASK_PUBLISHING | MASK_COMPLETED;
            // 需要再次竞争completion的使用权，多线程添加监听器之间的竞争
            oldOptions = Interlocked.CompareExchange(ref completion.options, lockOptions, MASK_COMPLETED);
            if (oldOptions != MASK_COMPLETED) {
                throw new InvalidOperationException("Already continuation registered, can not await twice or get result after await.");
            }
        }

        options &= (~TaskOptions.MASK_PRIORITY_AND_SCHEDULE_PHASE); // 去重用户的低位
        options |= type;
        options |= (MASK_PUBLISHED | MASK_COMPLETED); // COMPLETED(总是设置不会导致错误)

        completion.input = this;
        completion.action = action;
        completion.state = state;
        completion.executor = executor;
        Volatile.Write(ref completion.options, options);

        // future已进入完成状态
        if (oldOptions == MASK_COMPLETED) {
            completion.TryFire(SYNC);
        }
    }

    private void PostComplete() {
        int options = Volatile.Read(ref _completion.options);
        if (options == 0) {
            options = Interlocked.CompareExchange(ref _completion.options, MASK_COMPLETED, 0);
            if (options == 0) {
                return; // 竞争成功，添加监听器的时候同步通知
            }
        }
        // 如果正在发布，则进行等待
        while ((options & MASK_PUBLISHING) != 0) {
            options = Volatile.Read(ref _completion.options);
        }
        _completion.TryFire(SYNC);
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
        Internal_SetComputing();
    }

    public bool TrySetResult(int reentryId, T result) {
        ValidateReentryId(reentryId);
        return Internal_TrySetResult(result);
    }

    public void SetResult(int reentryId, T result) {
        ValidateReentryId(reentryId);
        Internal_SetResult(result);
    }

    public bool TrySetException(int reentryId, Exception cause) {
        ValidateReentryId(reentryId);
        return Internal_TrySetException(cause);
    }

    public void SetException(int reentryId, Exception cause) {
        ValidateReentryId(reentryId);
        Internal_SetException(cause);
    }

    public bool TrySetCancelled(int reentryId, int cancelCode) {
        ValidateReentryId(reentryId);
        return Internal_TrySetCancelled(cancelCode);
    }

    public void SetCancelled(int reentryId, int cancelCode) {
        ValidateReentryId(reentryId);
        Internal_SetCancelled(cancelCode);
    }

    #endregion

    #region completion

    /** 回调为无参的普通的Action */
    private const int TYPE_ACTION = 0;
    /** 回调为Action + State */
    private const int TYPE_ACTION_STATE = 1;
    /** 回调为设置VoidPromise */
    private const int TYPE_SET_VOID_PROMISE = 2;
    /** 回调为设置Promise */
    private const int TYPE_SET_PROMISE = 3;

    /** 任务类型的掩码 -- 4bit，最大16种，可省去大量的instanceof测试 */
    private const int MASK_TASK_TYPE = 0x0F;

    /** 正发布回调 -- future需要等待发布完成 */
    private const int MASK_PUBLISHING = 0x10;
    /** 已发布回调 */
    private const int MASK_PUBLISHED = 0x20;
    /** future已完成 */
    private const int MASK_COMPLETED = 0x40;

    private class Completion : ITask
    {
#nullable disable
        /// <summary>
        /// 部分回调依赖Promise的数据
        /// </summary>
        internal volatile ValuePromise<T> input;
        /// <summary>
        /// 回调线程
        /// </summary>
        internal IExecutor executor;
        /// <summary>
        /// 回调任务选项
        /// 
        /// 1.如果为0表示尚未发布action。
        /// 2.如果等于<see cref="ValuePromise{T}.MASK_PUBLISHING"/>表示正在发布。
        /// 3.如果包含<see cref="ValuePromise{T}.MASK_PUBLISHED"/>表示已发布。
        /// 4.如果等于<see cref="ValuePromise{T}.MASK_COMPLETED"/>表示Future已完成，但此时没有回调。
        /// 
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
#nullable enable

        public void Reset() {
            input = null;
            action = null;
            state = null;
            executor = null;
            options = 0;
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
            if (e != null) {
                // TryInline
                if (IsInlinable(e, options)) {
                    return true;
                }
                e.Execute(this);
                return false;
            }
            return true;
        }

        public void TryFire(int mode) {
            if (IsCancelling(state, options)) {
                return;
            }
            // 异步模式下已经claim
            if (mode <= 0 && !Claim()) {
                return;
            }
            try {
                RunAction();
            }
            catch (Exception ex) {
                FutureLogger.LogCause(ex, "Value promise fire caught exception");
            }
            // 与Promise中的实现不同，这里不能清理数据，因为用户的回调可能触发对象回收再复用
        }

        private void RunAction() {
            int taskType = (options & MASK_TASK_TYPE);
            switch (taskType) {
                case TYPE_ACTION: {
                    Action action = (Action)this.action;
                    action();
                    break;
                }
                case TYPE_ACTION_STATE: {
                    Action<object> action = (Action<object>)this.action;
                    action(state);
                    break;
                }
                case TYPE_SET_VOID_PROMISE: {
                    IPromise<int> output = (IPromise<int>)this.action;
                    if (input.Status == TaskStatus.Success) {
                        output.TrySetResult(0);
                    } else {
                        output.TrySetException(input.ExceptionNow(false));
                    }
                    // 用户已获取结果
                    input.PrepareToRecycle();
                    break;
                }
                case TYPE_SET_PROMISE: {
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
                default: {
                    throw new AssertionError();
                }
            }
        }
    }

    #endregion
}
}