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
using Wjybxx.Commons.Concurrent;

namespace Wjybxx.Commons.Sequential
{
/// <summary>
/// 单线程版本的<see cref="IPromise{T}"/>
///
/// <h3>单线程化做的变动</h3>
/// 1.去除{@link #result}等的volatile操作，变更为普通字段。
/// 2.去除了阻塞操作Awaiter的支持。
/// 3.去除了state的中间状态 -- 可对比<see cref="UniPromise{T}"/>
///
/// <h3>Async的含义</h3>
/// 既然是单线程的，又何来异步一说？这里的异步是指不立即执行给定的行为，而是提交到Executor等待调度。
/// 这有什么作用？有几个作用：
/// 1.让出CPU，避免过多的任务集中处理。
/// 2.延迟到特定阶段执行 -- 通过<see cref="TaskOptions"/>指定。
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class UniPromise<T> : AbstractUniPromise, IPromise<T>
{
    /// <summary>
    /// 已完成的Promise常量实例
    /// </summary>
    public static readonly UniPromise<T> COMPLETED = new UniPromise<T>(null, default, null);
    /// <summary>
    /// 已被取消的Promise常量实例
    /// </summary>
    public static readonly UniPromise<T> CANCELLED = new UniPromise<T>(null, default, StacklessCancellationException.Default);

    /** 任务成功执行时的结果 -- 可见性由<see cref="_ex"/>保证 */
    private T _result;
    /// <summary>
    /// 任务失败完成时的结果，也包含了任务的状态。
    /// 
    /// 1. 如果为null，表示尚未开始。
    /// 2. 如果为<see cref="AbstractUniPromise.EX_COMPUTING"/>，表示正在计算。
    /// 3. 如果为<see cref="AbstractUniPromise.EX_SUCCESS"/>，表示成功，且结果已可见。
    /// 4. 如果为<see cref="OperationCanceledException"/>，表示取消。
    /// 5. 如果为<see cref="ExceptionDispatchInfo"/>，表示失败。
    /// </summary>
    private object? _ex;

    /** 任务绑定的线程 -- 其实不一定是执行线程 */
    private IExecutor? _executor;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executor">任务关联的线程，死锁检测等</param>
    public UniPromise(IExecutor? executor = null) {
        _executor = executor;
    }

    private UniPromise(IExecutor? executor, T result, object? ex) {
        this._executor = executor;
        if (ex == null) {
            this._result = result;
            this._ex = EX_SUCCESS;
        } else {
            this._result = default;
            this._ex = WrapException(ex);
        }
    }

    public static UniPromise<T> FromResult(T result, IExecutor? executor = null) {
        return new UniPromise<T>(executor, result, null);
    }

    public static UniPromise<T> FromException(Exception ex, IExecutor? executor = null) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new UniPromise<T>(executor, default, ex);
    }

    public static UniPromise<T> FromException(ExceptionDispatchInfo ex, IExecutor? executor = null) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new UniPromise<T>(executor, default, ex);
    }

    public static UniPromise<T> FromCancelled(int code, IExecutor? executor = null) {
        Exception ex = StacklessCancellationException.InstOf(code);
        return new UniPromise<T>(executor, default, ex);
    }

    #region internal

    internal void Reset() {
        stack = null;
        _executor = null;
        _result = default;
        _ex = null;
    }

    private bool InternalSetResult(T result) {
        object preEx = this._ex;
        if (preEx == null || preEx == EX_COMPUTING) {
            this._result = result;
            this._ex = EX_SUCCESS;
            return true;
        }
        return false;
    }

    private bool InternalSetException(object ex) {
        object result = ex is ExceptionDispatchInfo ? ex : WrapException(ex);
        object preEx = this._ex;
        if (preEx == null || preEx == EX_COMPUTING) {
            this._ex = result;
            return true;
        }
        return false;
    }

    /** 获取当前状态，如果处于发布中状态，则等待目标线程发布完毕 */
    private int PollState() {
        object? ex = _ex;
        if (ex == null) {
            return ST_PENDING;
        }
        if (ex == EX_COMPUTING) {
            return ST_COMPUTING;
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
    private static int PeekState(object? ex, bool strict = true) {
        if (ex == null) {
            return ST_PENDING;
        }
        if (ex == EX_COMPUTING) {
            return ST_COMPUTING;
        }
        if (ex == EX_SUCCESS) {
            return ST_SUCCESS;
        }
        return ex is OperationCanceledException ? ST_CANCELLED : ST_FAILED;
    }

    private ExceptionDispatchInfo DispatchInfo => (ExceptionDispatchInfo)_ex!;

    #endregion

    #region 上下文

    /// <summary>
    /// 允许重写，Executor可能存储在其它地方
    /// </summary>
    public virtual IExecutor? Executor => _executor;

    public IFuture<T> AsReadonly() => new ForwardFuture<T>(this);

    #endregion

    #region 状态查询

    /** 是否表示完成状态 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDone0(int state) {
        return state >= ST_SUCCESS;
    }

    /** 是否表示完成状态 -- 不包含发布状态 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDone0(object? ex) {
        return ex != null
               && ex != EX_COMPUTING
               && ex != EX_PUBLISHING;
    }

    /** 是否表示成功完成状态 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsSucceed(object? ex) {
        return ex == EX_SUCCESS;
    }

    public TaskStatus Status => (TaskStatus)PeekState(_ex);

    public bool IsPending => _ex == null;
    public bool IsComputing => _ex == EX_COMPUTING;
    public bool IsSucceeded => PeekState(_ex) == ST_SUCCESS;
    public bool IsFailed => PeekState(_ex) == ST_FAILED;
    public bool IsCancelled => PeekState(_ex) == ST_CANCELLED;

    public bool IsCompleted => PeekState(_ex) >= ST_SUCCESS;
    public bool IsFailedOrCancelled => PeekState(_ex) >= ST_FAILED;

    protected sealed override bool IsRelaxedCompleted => PeekState(_ex, false) >= ST_SUCCESS;
    protected sealed override bool IsStrictlyCompleted => PeekState(_ex) >= ST_SUCCESS;

    #endregion

    #region 状态更新

    public bool TrySetComputing() {
        object preEx = this._ex;
        if (preEx == null) {
            this._ex = EX_COMPUTING;
            return true;
        }
        return false;
    }

    public TaskStatus TrySetComputing2() {
        object preEx = this._ex;
        if (preEx == null) {
            this._ex = EX_COMPUTING;
            return ST_PENDING;
        }
        return (TaskStatus)PeekState(preEx);
    }

    public void SetComputing() {
        if (!TrySetComputing()) {
            throw new IllegalStateException("Already computing");
        }
    }

    public bool TrySetResult(T result) {
        if (InternalSetResult(result)) {
            PostComplete(this);
            return true;
        }
        return false;
    }

    public void SetResult(T result) {
        if (!TrySetResult(result)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public bool TrySetException(Exception cause) {
        if (cause == null) throw new ArgumentNullException(nameof(cause));
        if (InternalSetException(cause)) {
            FutureLogger.LogCause(cause); // 记录日志
            PostComplete(this);
            return true;
        }
        return false;
    }

    public void SetException(Exception cause) {
        if (!TrySetException(cause)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public bool TrySetCancelled(int cancelCode) {
        if (InternalSetException(StacklessCancellationException.InstOf(cancelCode))) {
            PostComplete(this);
            return true;
        }
        return false;
    }

    public void SetCancelled(int cancelCode) {
        if (!TrySetCancelled(cancelCode)) {
            throw new IllegalStateException("Already complete");
        }
    }

    #endregion

    #region 非阻塞结果查询

    public T ResultNow() {
        int state = PollState();
        return state switch
        {
            ST_SUCCESS => _result,
            ST_FAILED => throw new IllegalStateException("Task completed with exception"),
            ST_CANCELLED => throw new IllegalStateException("Task was cancelled"),
            _ => throw new IllegalStateException("Task has not completed")
        };
    }

    public Exception ExceptionNow(bool throwIfCancelled = true) {
        return ExceptionNow(PollState(), _ex, throwIfCancelled);
    }

    public void ThrowIfFailedOrCancelled() {
        IFuture.ThrowIfFailedOrCancelled(this);
    }

    /** 上报future的执行结果 -- 取消以外的异常都将被包装为<see cref="CompletionException"/> */
    private T ReportJoin(int state) {
        Debug.Assert(state > 0);
        if (state == ST_SUCCESS) {
            return _result;
        }
        if (state == ST_CANCELLED) {
            throw BetterCancellationException.Capture((Exception)_ex!);
        }
        ExceptionDispatchInfo dispatchInfo = (ExceptionDispatchInfo)_ex!;
        if (dispatchInfo.SourceException is CompletionException) {
            dispatchInfo.Throw();
        }
        throw new CompletionException(null, ExceptionUtil.RestoreStackTrace(dispatchInfo));
    }

    #endregion

    #region 阻塞结果查询

    // virtual 以支持重写
    protected void CheckDeadlock() {
        if (Executor is ISingleThreadExecutor se && se.InEventLoop()) {
            throw new BlockingOperationException();
        }
    }

    public T Get() {
        int state = PollState();
        if (IsDone0(state)) {
            return ReportJoin(state);
        }
        throw new BlockingOperationException("Get");
    }

    public T Join() {
        int state = PollState();
        if (IsDone0(state)) {
            return ReportJoin(state);
        }
        throw new BlockingOperationException("Join");
    }

    public IFuture<T> Await() {
        if (IsCompleted) {
            return this;
        }
        throw new BlockingOperationException("Await");
    }

    public IFuture<T> AwaitUninterruptibly() {
        if (IsCompleted) {
            return this;
        }
        throw new BlockingOperationException("AwaitUninterruptibly");
    }

    public bool Await(TimeSpan timeout) {
        if (IsCompleted) {
            return true;
        }
        throw new BlockingOperationException("Await");
    }

    public bool AwaitUninterruptibly(TimeSpan timeout) {
        if (IsCompleted) {
            return true;
        }
        throw new BlockingOperationException("AwaitUninterruptibly");
    }

    public FutureAwaiter<T> GetAwaiter() {
        return new FutureAwaiter<T>(this);
    }

    public FutureAwaitable<T> GetAwaitable(IExecutor executor, int options = 0) {
        return new FutureAwaitable<T>(this, executor, options);
    }

    #endregion

    #region async

    public void OnCompleted(Action<IFuture<T>> continuation, int options = 0) {
        PushUniOnCompleted1(null, continuation, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>> continuation, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        PushUniOnCompleted1(executor, continuation, options);
    }

    public void OnCompleted(Action<IFuture<T>, object> continuation, object state, int options = 0) {
        PushUniOnCompleted2(null, continuation, state, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>, object> continuation, object state, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        PushUniOnCompleted2(executor, continuation, state, options);
    }

    public void OnCompleted(Action<object?> continuation, object? state, int options = 0) {
        PushMoveNextCompletion(null, continuation, state, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<object?> continuation, object? state, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        PushMoveNextCompletion(executor, continuation, state, options);
    }

    private void PushUniOnCompleted1(IExecutor? executor, Action<IFuture<T>> continuation, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsCompleted && executor == null) {
            UniOnCompleted1.FireNow(this, continuation, null);
        } else {
            PushCompletion(new UniOnCompleted1(executor, options, this, continuation));
        }
    }

    private void PushUniOnCompleted2(IExecutor? executor, Action<IFuture<T>, object> continuation, object? state, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsCompleted && executor == null) {
            UniOnCompleted2.FireNow(this, continuation, state, null);
        } else {
            PushCompletion(new UniOnCompleted2(executor, options, this, continuation, state));
        }
    }

    private void PushMoveNextCompletion(IExecutor? executor, Action<object?> continuation, object? state, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsCompleted && executor == null) {
            MoveNextCompletion.FireNow(continuation, state, null);
        } else {
            MoveNextCompletion completion = MoveNextCompletion.POOL.Acquire();
            completion.Init(executor, options, continuation, state);
            PushCompletion(completion);
        }
    }

    #endregion

    protected virtual UniPromise<U> NewIncompletePromise<U>(IExecutor? exe) {
        return new UniPromise<U>(exe);
    }

    #region 链式调用

    // 暂不做已完成情况下的优化--降低代码复杂度；另外向已完成的Future添加监听器的情况不常见(至少比例是低的)

    #region ComposeApply

    public IFuture<U> ComposeApply<U>(Func<object, T, IFuture<U>> fn, object? ctx, int options = 0) {
        return PushUniComposeApply(null, fn, ctx, options);
    }

    public IFuture<U> ComposeApplyAsync<U>(IExecutor executor, Func<object, T, IFuture<U>> fn, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniComposeApply(executor, fn, ctx, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IFuture<U> PushUniComposeApply<U>(IExecutor? executor,
                                              Func<object, T, IFuture<U>> fn, object? ctx, int options) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        UniPromise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniComposeApply<T, U>(executor, ctx, options, this, promise, fn));
        return promise;
    }

    #endregion

    #region compose-call

    public IFuture<U> ComposeCall<U>(Func<object, IFuture<U>> fn, object? ctx, int options = 0) {
        return PushComposeCall(null, fn, ctx, options);
    }

    public IFuture<U> ComposeCallAsync<U>(IExecutor executor, Func<object, IFuture<U>> fn, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushComposeCall(executor, fn, ctx, options);
    }

    private IFuture<U> PushComposeCall<U>(IExecutor? executor,
                                          Func<object, IFuture<U>> fn, object? ctx, int options) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        UniPromise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniComposeCall<T, U>(executor, ctx, options, this, promise, fn));
        return promise;
    }

    #endregion

    #region ComposeCatching

    public IFuture<T> ComposeCatching<X>(Func<object, X, IFuture<T>> fallback, object? ctx, int options = 0) where X : Exception {
        return PushComposeCatching(null, fallback, ctx, options);
    }

    public IFuture<T> ComposeCatchingAsync<X>(IExecutor executor, Func<object, X, IFuture<T>> fallback, object? ctx, int options = 0) where X : Exception {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushComposeCatching(executor, fallback, ctx, options);
    }

    private IFuture<T> PushComposeCatching<X>(IExecutor? executor,
                                              Func<object, X, IFuture<T>> fallback, object? ctx, int options) where X : Exception {
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        UniPromise<T> promise = NewIncompletePromise<T>(executor == null ? this.Executor : executor);
        PushCompletion(new UniComposeCatching<X, T>(executor, ctx, options, this, promise, fallback));
        return promise;
    }

    #endregion

    #region ComposeHandle

    public IFuture<U> ComposeHandle<U>(Func<object, T, Exception, IFuture<U>> fn, object? ctx, int options = 0) {
        return PushComposeHandle(null, fn, ctx, options);
    }

    public IFuture<U> ComposeHandleAsync<U>(IExecutor executor, Func<object, T, Exception, IFuture<U>> fn, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushComposeHandle(executor, fn, ctx, options);
    }

    private IFuture<U> PushComposeHandle<U>(IExecutor? executor,
                                            Func<object, T, Exception, IFuture<U>> fn, object? ctx, int options) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        UniPromise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniComposeHandle<T, U>(executor, ctx, options, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-apply

    public IFuture<U> ThenApply<U>(Func<object, T, U> fn, object? ctx, int options = 0) {
        return PushUniApply(null, fn, ctx, options);
    }

    public IFuture<U> ThenApplyAsync<U>(IExecutor executor, Func<object, T, U> fn, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniApply(executor, fn, ctx, options);
    }


    private IFuture<U> PushUniApply<U>(IExecutor? executor, Func<object, T, U> fn, object? ctx, int options) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        UniPromise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniApply<T, U>(executor, ctx, options, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-accpt

    public IFuture ThenAccept(Action<object, T> fn, object? ctx, int options = 0) {
        return PushUniAccept(null, fn, ctx, options);
    }

    public IFuture ThenAcceptAsync(IExecutor executor, Action<object, T> fn, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniAccept(executor, fn, ctx, options);
    }

    private IFuture PushUniAccept(IExecutor? executor, Action<object, T> fn, object? ctx, int options) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        UniPromise<int> promise = NewIncompletePromise<int>(executor == null ? this.Executor : executor);
        PushCompletion(new UniAccept<T>(executor, ctx, options, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-call

    public IFuture<U> ThenCall<U>(Func<object, U> fn, object? ctx, int options = 0) {
        return PushUniCall(null, fn, ctx, options);
    }

    public IFuture<U> ThenCallAsync<U>(IExecutor executor, Func<object, U> fn, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniCall(executor, fn, ctx, options);
    }

    private IFuture<U> PushUniCall<U>(IExecutor? executor, Func<object, U> fn, object? ctx, int options) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        UniPromise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniCall<T, U>(executor, ctx, options, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-run

    public IFuture ThenRun(Action<object> fn, object? ctx, int options = 0) {
        return PushUniRun(null, fn, ctx, options);
    }

    public IFuture ThenRunAsync(IExecutor executor, Action<object> fn, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniRun(executor, fn, ctx, options);
    }

    private IFuture PushUniRun(IExecutor? executor, Action<object> fn, object? ctx, int options) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        UniPromise<int> promise = NewIncompletePromise<int>(executor == null ? this.Executor : executor);
        PushCompletion(new UniRun<T>(executor, ctx, options, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-catch

    public IFuture<T> Catching<X>(Func<object, X, T> fallback, object? ctx, int options = 0) where X : Exception {
        return PushUniCatching(null, fallback, ctx, options);
    }

    public IFuture<T> CatchingAsync<X>(IExecutor executor, Func<object, X, T> fallback, object? ctx, int options = 0) where X : Exception {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniCatching(executor, fallback, ctx, options);
    }

    private IFuture<T> PushUniCatching<X>(IExecutor? executor, Func<object, X, T> fallback, object? ctx, int options) where X : Exception {
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        UniPromise<T> promise = NewIncompletePromise<T>(executor == null ? this.Executor : executor);
        PushCompletion(new UniCatching<X, T>(executor, ctx, options, this, promise, fallback));
        return promise;
    }

    #endregion

    #region uni-handle

    public IFuture<U> Handle<U>(Func<object, T, Exception, U> fn, object? ctx, int options = 0) {
        return PushUniHandle(null, fn, ctx, options);
    }

    public IFuture<U> HandleAsync<U>(IExecutor executor, Func<object, T, Exception, U> fn, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniHandle(executor, fn, ctx, options);
    }

    private IFuture<U> PushUniHandle<U>(IExecutor? executor, Func<object, T, Exception, U> fn, object? ctx, int options) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        UniPromise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniHandle<T, U>(executor, ctx, options, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-when-complete

    public IFuture<T> WhenComplete(Action<object, T, Exception> fn, object? ctx, int options = 0) {
        return PushUniWhenComplete(null, fn, ctx, options);
    }

    public IFuture<T> WhenComplete(IExecutor executor, Action<object, T, Exception> fn, object? ctx, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniWhenComplete(executor, fn, ctx, options);
    }

    private IFuture<T> PushUniWhenComplete(IExecutor? executor, Action<object, T, Exception> fn, object? ctx, int options) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        UniPromise<T> promise = NewIncompletePromise<T>(executor == null ? this.Executor : executor);
        PushCompletion(new UniWhenComplete<T>(executor, ctx, options, this, promise, fn));
        return promise;
    }

    #endregion

    #endregion

    #region 开放给completion的方法

    // 开放给Completion的方法

    private bool CompleteNull() {
        return InternalSetResult(default);
    }

    private bool CompleteValue(T value) {
        return InternalSetResult(value);
    }

    private bool CompleteCancelled(ICancelToken cancelToken) {
        int cancelCode = cancelToken.CancelCode;
        Debug.Assert(cancelCode > 0);
        return InternalSetException(StacklessCancellationException.InstOf(cancelCode));
    }

    /**
     * 如果一个{@link Completion}在计算中出现异常，则使用该方法使目标进入完成状态。
     * (出现新的异常)
     */
    private bool CompleteThrowable(Exception x) {
        FutureLogger.LogCause(x);
        // 统一封装为CompletionException
        if (x is not CompletionException) {
            x = new CompletionException(null, x);
        }
        return InternalSetException(x);
    }

    /**
     * 使用依赖项的结果进入完成状态，通常表示当前{@link Completion}只是一个简单的中继。
     */
    private bool CompleteRelay(T r, object ex) {
        if (ex == EX_SUCCESS) {
            return InternalSetResult(r);
        } else {
            return InternalSetException(ex);
        }
    }

    /**
     * 使用依赖项的异常结果进入完成状态，通常表示当前{@link Completion}只是一个简单的中继。
     * 在已知依赖项异常完成的时候可以调用该方法，减少开销。
     * 这里实现和{@link CompletableFuture}不同，这里保留原始结果，不强制将异常转换为{@link CompletionException}。
     * 这样有助与用户捕获正确的异常类型，而不是一个奇怪的CompletionException
     */
    private bool CompleteRelayThrowable(object r) {
        return InternalSetException(r);
    }

    #endregion

    private abstract class UniCompletion<V, U> : Completion
    {
        protected IExecutor? executor;
        protected object? ctx;
        protected int options;
        protected UniPromise<V> input;
        protected UniPromise<U> output;

        protected UniCompletion(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<U> output) {
            this.executor = executor;
            this.ctx = ctx;
            this.options = options;
            this.input = input;
            this.output = output;
        }

        public override int Options {
            get => options;
            set => options = value;
        }

        public bool Claim() {
            IExecutor? e = this.executor;
            if (e == CLAIMED) {
                return true;
            }
            if (!output.TrySetComputing()) { // 被用户取消
                throw StacklessCancellationException.Default;
            }
            this.executor = CLAIMED;
            if (e != null) {
                return TryInline(this, e, options);
            }
            return true;
        }
    }

    #region compose-x

    private static bool TryTransferTo<U>(IFuture<U> input, UniPromise<U> output) {
        if (input is UniPromise<U> promise) {
            object ex = promise._ex;
            if (IsDone0(ex)) {
                return output.CompleteRelay(promise._result, ex!);
            }
            return false;
        }
        // 有可能是Readonly或其它实现
        TaskStatus state = input.Status;
        switch (state) {
            case TaskStatus.Pending:
            case TaskStatus.Computing: {
                return false;
            }
            case TaskStatus.Success: {
                return output.CompleteValue(input.ResultNow());
            }
            case TaskStatus.Failed:
            case TaskStatus.Cancelled: {
                Exception ex = input.ExceptionNow(false);
                return output.CompleteRelayThrowable(ex);
            }
            default: {
                throw new AssertionError();
            }
        }
    }

    private class UniComposeApply<V, U> : UniCompletion<V, U>
    {
        Func<object, V, IFuture<U>> fn;

        public UniComposeApply(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<U> output,
                               Func<object, V, IFuture<U>> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                object rawEx = input._ex!;
                if (!IsSucceed(rawEx)) {
                    setCompleted = output.CompleteRelayThrowable(rawEx);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    IFuture<U> relay = fn(ctx, input._result);
                    setCompleted = TryTransferTo(relay, output);
                    if (!setCompleted) { // 添加监听
                        Executors.SetPromise(output, relay);
                    }
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniComposeCall<V, U> : UniCompletion<V, U>
    {
        Func<object, IFuture<U>> fn;

        public UniComposeCall(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<U> output,
                              Func<object, IFuture<U>> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                object rawEx = input._ex!;
                if (!IsSucceed(rawEx)) {
                    setCompleted = output.CompleteRelayThrowable(rawEx);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    IFuture<U> relay = fn(ctx);
                    setCompleted = TryTransferTo(relay, output);
                    if (!setCompleted) { // 添加监听
                        Executors.SetPromise(output, relay);
                    }
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniComposeCatching<X, V> : UniCompletion<V, V> where X : Exception
    {
        Func<object, X, IFuture<V>> fn;

        public UniComposeCatching(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<V> output,
                                  Func<object, X, IFuture<V>> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<V> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                object rawEx = input._ex!;
                X ex; // 这里暂不恢复堆栈
                if (IsSucceed(rawEx) || (ex = UnwrapException(rawEx) as X) == null) {
                    setCompleted = output.CompleteRelay(input._result, rawEx);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    IFuture<V> relay = fn(ctx, ex);
                    setCompleted = TryTransferTo(relay, output);
                    if (!setCompleted) { // 添加监听
                        Executors.SetPromise(output, relay);
                    }
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniComposeHandle<V, U> : UniCompletion<V, U>
    {
        Func<object, V, Exception, IFuture<U>> fn;

        public UniComposeHandle(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<U> output,
                                Func<object, V, Exception, IFuture<U>> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    object rawEx = input._ex!;
                    Exception ex = IsSucceed(rawEx) ? null : UnwrapException(rawEx);
                    IFuture<U> relay = fn(ctx, input._result, ex);
                    setCompleted = TryTransferTo(relay, output);
                    if (!setCompleted) { // 添加监听
                        Executors.SetPromise(output, relay);
                    }
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    #endregion

    #region uni-x

    private class UniApply<V, U> : UniCompletion<V, U>
    {
        Func<object, V, U> fn;

        public UniApply(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<U> output,
                        Func<object, V, U> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                object rawEx = input._ex!;
                if (!IsSucceed(rawEx)) {
                    setCompleted = output.CompleteRelayThrowable(rawEx);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    setCompleted = output.CompleteValue(fn(ctx, input._result));
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniAccept<V> : UniCompletion<V, int>
    {
        Action<object, V> fn;

        public UniAccept(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<int> output,
                         Action<object, V> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<int> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                object rawEx = input._ex!;
                if (!IsSucceed(rawEx)) {
                    setCompleted = output.CompleteRelayThrowable(rawEx);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    fn(ctx, input._result);
                    setCompleted = output.CompleteNull();
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniCall<V, U> : UniCompletion<V, U>
    {
        Func<object, U> fn;

        public UniCall(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<U> output,
                       Func<object, U> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                object rawEx = input._ex!;
                if (!IsSucceed(rawEx)) {
                    setCompleted = output.CompleteRelayThrowable(rawEx);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    setCompleted = output.CompleteValue(fn(ctx));
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniRun<V> : UniCompletion<V, int>
    {
        Action<object> fn;

        public UniRun(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<int> output,
                      Action<object> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<int> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                object rawEx = input._ex!;
                if (!IsSucceed(rawEx)) {
                    setCompleted = output.CompleteRelayThrowable(rawEx);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    fn(ctx);
                    setCompleted = output.CompleteNull();
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniCatching<X, V> : UniCompletion<V, V> where X : Exception
    {
        Func<object, X, V> fn;

        public UniCatching(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<V> output,
                           Func<object, X, V> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<V> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                object rawEx = input._ex!;
                X ex; // 这里暂不恢复堆栈
                if (IsSucceed(rawEx) || (ex = UnwrapException(rawEx) as X) == null) {
                    setCompleted = output.CompleteRelay(input._result, rawEx);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    setCompleted = output.CompleteValue(fn(ctx, ex));
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniHandle<V, U> : UniCompletion<V, U>
    {
        Func<object, V, Exception, U> fn;

        public UniHandle(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<U> output,
                         Func<object, V, Exception, U> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    object rawEx = input._ex!;
                    Exception ex = IsSucceed(rawEx) ? null : UnwrapException(rawEx);
                    setCompleted = output.CompleteValue(fn(ctx, input._result, ex));
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniWhenComplete<V> : UniCompletion<V, V>
    {
        Action<object, V, Exception> fn;

        public UniWhenComplete(IExecutor? executor, object? ctx, int options, UniPromise<V> input, UniPromise<V> output,
                               Action<object, V, Exception> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<V> input = this.input;
            UniPromise<V> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = Executors.GetCancelToken(ctx, options);
                if (cancelToken.IsCancelRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    object rawEx = input._ex!;
                    Exception ex = IsSucceed(rawEx) ? null : UnwrapException(rawEx);
                    fn(ctx, input._result, ex);
                    setCompleted = output.CompleteRelay(input._result, rawEx);
                }
                catch (Exception e) {
                    FutureLogger.LogCause(e, "UniWhenComplete caught an exception");
                    setCompleted = output.CompleteRelay(input._result, input._ex!);
                }
            }
            outer:
            // help gc
            this.ctx = null;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    #endregion

    #region on-complete

    private abstract class UniOnCompleted : Completion
    {
#nullable disable
        protected IExecutor executor;
        protected int options;
        protected UniPromise<T> input;
#nullable enable

        protected UniOnCompleted(IExecutor? executor, int options, UniPromise<T> input) {
            this.executor = executor;
            this.options = options;
            this.input = input;
        }

        public override int Options {
            get => options;
            set => options = value;
        }

        protected bool Claim() {
            IExecutor e = this.executor;
            if (e == CLAIMED) {
                return true;
            }
            this.executor = CLAIMED;
            if (e != null) {
                return TryInline(this, e, options);
            }
            return true;
        }
    }

    private class UniOnCompleted1 : UniOnCompleted
    {
#nullable disable
        private Action<IFuture<T>> action;
#nullable enable

        public UniOnCompleted1(IExecutor? executor, int options, UniPromise<T> input, Action<IFuture<T>> action)
            : base(executor, options, input) {
            this.action = action;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<T>? input = this.input;
            {
                // 异步模式下已经claim
                if (!FireNow(input, action, mode > 0 ? null : this)) {
                    return null;
                }
            }
            // help gc
            this.executor = null;
            this.input = null;
            this.action = null;
            return null;
        }

        public static bool FireNow(UniPromise<T> input, Action<IFuture<T>> action,
                                   UniOnCompleted1? c) {
            try {
                if (c != null && !c.Claim()) {
                    return false;
                }
                action(input);
            }
            catch (Exception e) {
                FutureLogger.LogCause(e, "UniOnCompleted1 caught an exception");
            }
            return true;
        }
    }

    private class UniOnCompleted2 : UniOnCompleted
    {
#nullable disable
        private Action<IFuture<T>, object> action;
        private object state;
#nullable enable
        public UniOnCompleted2(IExecutor? executor, int options, UniPromise<T> input,
                               Action<IFuture<T>, object> action, object? state) :
            base(executor, options, input) {
            this.action = action;
            this.state = state;
        }

        public override AbstractUniPromise? TryFire(int mode) {
            UniPromise<T>? input = this.input;
            {
                if (Executors.IsCancelRequested(state, options)) {
                    goto outer;
                }
                // 异步模式下已经claim
                if (!FireNow(input, action, state, mode > 0 ? null : this)) {
                    return null;
                }
            }
            outer:
            // help gc
            this.executor = null;
            this.input = null;
            this.action = null;
            this.state = null;
            return null;
        }

        public static bool FireNow(UniPromise<T> input,
                                   Action<IFuture<T>, object?> action, object? state,
                                   UniOnCompleted2? c) {
            try {
                if (c != null && !c.Claim()) {
                    return false;
                }
                action(input, state);
            }
            catch (Exception e) {
                FutureLogger.LogCause(e, "UniOnCompleted2 caught an exception");
            }
            return true;
        }
    }

    #endregion
}
}