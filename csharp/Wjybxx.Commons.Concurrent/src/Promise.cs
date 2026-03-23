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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// Promise不会实现两份（泛型和非泛型），那会导致大量的重复代码，有非常高的维护成本。
/// 在不需要结果的情况下，可以选择将泛型参数定义为byte或int，尽可能减少开销即可 -- 推荐使用int。
/// 
/// PS：重复编码不仅仅是指Promise，与Promise相关的各个体系都需要双份...
/// </summary>
/// <typeparam name="T"></typeparam>
public class Promise<T> : AbstractPromise, IPromise<T>
{
    /// <summary>
    /// 已完成的Promise常量实例
    /// </summary>
    public static readonly Promise<T> COMPLETED = new Promise<T>(null, default, null);
    /// <summary>
    /// 已被取消的Promise常量实例
    /// </summary>
    [Obsolete("C#异常不适合共享")]
    public static readonly Promise<T> CANCELLED = new Promise<T>(null, default, StacklessCancellationException.Default);

    /** 任务成功执行时的结果 -- 可见性由<see cref="_ex"/>保证 */
    private T _result;
    /// <summary>
    /// 任务失败完成时的结果，也包含了任务的状态。
    /// 
    /// 1. 如果为null，表示尚未开始。
    /// 2. 如果为<see cref="AbstractPromise.EX_COMPUTING"/>，表示正在计算。
    /// 3. 如果为<see cref="AbstractPromise.EX_PUBLISHING"/>，表示成功，但正在发布成功结果。
    /// 4. 如果为<see cref="AbstractPromise.EX_SUCCESS"/>，表示成功，且结果已可见。
    /// 5. 如果为<see cref="OperationCanceledException"/>，表示取消 -- 避免捕获堆栈。
    /// 6. 如果为<see cref="ExceptionDispatchInfo"/>，表示失败。
    /// </summary>
    private volatile object? _ex;

    /** 任务绑定的线程 -- 其实不一定是执行线程 */
    private IExecutor? _executor;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executor">任务关联的线程，死锁检测等</param>
    public Promise(IExecutor? executor = null) {
        _executor = executor;
    }

    protected Promise(IExecutor? executor, T result, object? ex) {
        this._executor = executor;
        if (ex == null) {
            this._result = result;
            this._ex = EX_SUCCESS;
        } else {
            this._result = default;
            this._ex = AbstractPromise.WrapException(ex);
        }
    }

    public static Promise<T> FromResult(T? result, IExecutor? executor = null) {
        return new Promise<T>(executor, result, null);
    }

    public static Promise<T> FromException(Exception ex, IExecutor? executor = null) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new Promise<T>(executor, default, ex);
    }

    public static Promise<T> FromException(ExceptionDispatchInfo ex, IExecutor? executor = null) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new Promise<T>(executor, default, ex);
    }

    public static Promise<T> FromCancelled(int cancelCode, IExecutor? executor = null) {
        Exception ex = StacklessCancellationException.InstOf(cancelCode);
        return new Promise<T>(executor, default, ex);
    }

    #region internal

    /// <summary>
    /// 
    /// </summary>
    internal void Reset() {
        stack = null;
        _executor = null;
        _result = default;
        _ex = null; // store fence
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetExecutor(IExecutor e) {
        _executor = e;
    }

    /// <summary>
    /// Promise进入执行状态
    ///
    /// 注意：该方法不一定会被执行，取决于用户是否调用<see cref="TrySetComputing"/>方法，只在特定场景下有用。
    /// </summary>
    protected virtual void OnComputing() {
    }

    /// <summary>
    /// Promise进入了完成状态，子类可清理不再需要的数据，不可执行其它逻辑
    /// </summary>
    protected virtual void OnCompleted() {
    }

    private bool InternalSetResult(T? result) {
        // 先测试Pending状态 -- 如果大多数任务都是先更新为Computing状态，则先测试Computing有优势，暂不优化
        object preEx = Interlocked.CompareExchange(ref _ex, EX_PUBLISHING, null);
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
        object preEx = Interlocked.CompareExchange(ref _ex, result, null);
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
    public bool IsFailedOrCancelled => PeekState(_ex) > ST_SUCCESS;

    internal sealed override bool IsRelaxedCompleted => PeekState(_ex, false) >= ST_SUCCESS;
    internal sealed override bool IsStrictlyCompleted => PeekState(_ex) >= ST_SUCCESS;

    #endregion

    #region 状态更新

    public bool TrySetComputing() {
        object preState = Interlocked.CompareExchange(ref _ex, EX_COMPUTING, null);
        if (preState == null) {
            OnComputing();
            return true;
        }
        return false;
    }

    public TaskStatus TrySetComputing2() {
        object preState = Interlocked.CompareExchange(ref _ex, EX_COMPUTING, null);
        if (preState == null) {
            OnComputing();
            return TaskStatus.Pending;
        }
        return (TaskStatus)PeekState(preState);
    }

    public void SetComputing() {
        if (!TrySetComputing()) {
            throw new IllegalStateException("Already computing");
        }
    }

    public bool TrySetResult(T? result) {
        if (InternalSetResult(result)) {
            OnCompleted();
            PostComplete(this);
            return true;
        }
        return false;
    }

    public void SetResult(T? result) {
        if (!TrySetResult(result)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public bool TrySetException(Exception cause) {
        if (cause == null) throw new ArgumentNullException(nameof(cause));
        if (InternalSetException(cause)) {
            if (cause is not OperationCanceledException) {
                FutureLogger.LogCause(cause); // 记录日志
            }
            OnCompleted();
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

    public bool TrySetException(ExceptionDispatchInfo dispatchInfo) {
        if (dispatchInfo == null) throw new ArgumentNullException(nameof(dispatchInfo));
        if (InternalSetException(dispatchInfo)) {
            OnCompleted();
            PostComplete(this);
            return true;
        }
        return false;
    }

    public void SetException(ExceptionDispatchInfo dispatchInfo) {
        if (!TrySetException(dispatchInfo)) {
            throw new IllegalStateException("Already complete");
        }
    }

    public bool TrySetCancelled(int cancelCode) {
        if (InternalSetException(StacklessCancellationException.InstOf(cancelCode))) {
            OnCompleted();
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
        if (_ex == EX_SUCCESS) {
            return _result;
        }
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

    public object ExceptionOrDispatchInfoNow() {
        return ExceptionOrDispatchInfoNow(PollState(), _ex);
    }

    /** 上报future的执行结果 -- 取消以外的异常都将被包装为<see cref="CompletionException"/> */
    private T ReportJoin(int state) {
        Debug.Assert(state > 0);
        if (state == ST_SUCCESS) {
            return _result;
        }
        if (state == ST_CANCELLED) {
            throw BetterCancellationException.Capture((OperationCanceledException)_ex!);
        }
        ExceptionDispatchInfo dispatchInfo = (ExceptionDispatchInfo)_ex!;
        if (dispatchInfo.SourceException is CompletionException) {
            dispatchInfo.Throw();
        }
        throw new CompletionException(null, ExceptionUtil.RestoreStackTrace(dispatchInfo));
    }

    #endregion

    #region 阻塞结果查询

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
        Await();
        return ReportJoin(PollState());
    }

    public T Get(TimeSpan timeout) {
        int state = PollState();
        if (IsDone0(state)) {
            return ReportJoin(state);
        }
        if (Await(timeout)) {
            return ReportJoin(PollState());
        }
        throw new TimeoutException();
    }

    public T Join() {
        int state = PollState();
        if (IsDone0(state)) {
            return ReportJoin(state);
        }
        AwaitUninterruptibly();
        return ReportJoin(PollState());
    }

    private Awaiter? TryPushAwaiter() {
        Completion head = stack;
        if (head is Awaiter awaiter) {
            return awaiter; // 阻塞操作不多，而且通常集中在调用链的首尾
        }
        awaiter = new Awaiter(this);
        return PushCompletion(awaiter) ? awaiter : null;
    }

    public virtual IFuture<T> Await() {
        if (IsCompleted) {
            return this;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            awaiter.Await();
        }
        return this;
    }

    public virtual IFuture<T> AwaitUninterruptibly() {
        if (IsCompleted) {
            return this;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            awaiter.AwaitUninterruptibly();
        }
        return this;
    }

    public virtual bool Await(TimeSpan timeout) {
        if (IsCompleted) {
            return true;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            return awaiter.Await(timeout);
        }
        return true;
    }

    public virtual bool AwaitUninterruptibly(TimeSpan timeout) {
        if (IsCompleted) {
            return true;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            return awaiter.AwaitUninterruptibly(timeout);
        }
        return true;
    }

    public FutureAwaiter<T> GetAwaiter() {
        return new FutureAwaiter<T>(this);
    }

    public FutureAwaitable<T> GetAwaitable(IExecutor executor, int options = 0) {
        return new FutureAwaitable<T>(this, executor, options);
    }

    #endregion

    #region OnCompleted

    public void OnCompleted(Action<IFuture<T>> continuation, int options = 0) {
        PushUniOnCompleted(null, continuation, null, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>> continuation, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        PushUniOnCompleted(executor, continuation, null, options);
    }

    public void OnCompleted(Action<IFuture<T>, object?> continuation, object? state, int options = 0) {
        PushUniOnCompleted(null, continuation, state, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>, object?> continuation, object? state, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        PushUniOnCompleted(executor, continuation, state, options);
    }

    private void PushUniOnCompleted(IExecutor? executor, object continuation, object? state, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsCompleted && executor == null) {
            UniOnCompleted.FireNow(this, continuation, state, null);
        } else {
            PushCompletion(new UniOnCompleted(executor, options, this, continuation, state));
        }
    }

    #endregion

    #region fsm

    public void OnCompleted(Action<object?> continuation, object? state, int options = 0) {
        PushMoveNextCompletion(null, continuation, state, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<object?> continuation, object? state, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        PushMoveNextCompletion(executor, continuation, state, options);
    }

    /** 状态机特殊优化 */
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

    protected virtual Promise<U> NewIncompletePromise<U>(IExecutor? exe) {
        return new Promise<U>(exe);
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
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
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
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
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
        Promise<T> promise = NewIncompletePromise<T>(executor == null ? this.Executor : executor);
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
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
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
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
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
        Promise<int> promise = NewIncompletePromise<int>(executor == null ? this.Executor : executor);
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
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
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
        Promise<int> promise = NewIncompletePromise<int>(executor == null ? this.Executor : executor);
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
        Promise<T> promise = NewIncompletePromise<T>(executor == null ? this.Executor : executor);
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
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
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
        Promise<T> promise = NewIncompletePromise<T>(executor == null ? this.Executor : executor);
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

    private bool CompleteValue(T? value) {
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
        if (x is not OperationCanceledException) {
            FutureLogger.LogCause(x);
        }
        // 统一封装为CompletionException
        if (x is not CompletionException) {
            x = new CompletionException(null, x);
        }
        return InternalSetException(x);
    }

    /**
     * 使用依赖项的结果进入完成状态，通常表示当前{@link Completion}只是一个简单的中继。
     */
    private bool CompleteRelay(T? r, object ex) {
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
        protected Promise<V> input;
        protected Promise<U> output;

        protected UniCompletion(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<U> output) {
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
            if (!ExecutorUtil.IsInlinable(e, options)) {
                e.Execute(this);
                return false;
            }
            return true;
        }
    }

    #region compose-x

    private static bool TryTransferTo<U>(IFuture<U> input, Promise<U> output) {
        if (input is Promise<U> promise) {
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

        public UniComposeApply(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<U> output,
                               Func<object, V, IFuture<U>> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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
                        ExecutorUtil.SetPromise(output, relay);
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

        public UniComposeCall(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<U> output,
                              Func<object, IFuture<U>> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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
                        ExecutorUtil.SetPromise(output, relay);
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

        public UniComposeCatching(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<V> output,
                                  Func<object, X, IFuture<V>> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<V> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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
                        ExecutorUtil.SetPromise(output, relay);
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

        public UniComposeHandle(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<U> output,
                                Func<object, V, Exception, IFuture<U>> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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
                        ExecutorUtil.SetPromise(output, relay);
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

        public UniApply(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<U> output,
                        Func<object, V, U> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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

        public UniAccept(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<int> output,
                         Action<object, V> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<int> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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

        public UniCall(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<U> output,
                       Func<object, U> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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

        public UniRun(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<int> output,
                      Action<object> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<int> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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

        public UniCatching(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<V> output,
                           Func<object, X, V> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<V> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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

        public UniHandle(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<U> output,
                         Func<object, V, Exception, U> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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

        public UniWhenComplete(IExecutor? executor, object? ctx, int options, Promise<V> input, Promise<V> output,
                               Action<object, V, Exception> fn)
            : base(executor, ctx, options, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<V> output = this.output;
            object ctx = this.ctx;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                ICancelToken cancelToken = ExecutorUtil.GetCancelToken(ctx, options);
                if (cancelToken.IsRequested) {
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

    private class UniOnCompleted : Completion
    {
#nullable disable
        private IExecutor executor;
        private int options;
        private Promise<T> input;
        private object action;
        private object ctx;
#nullable restore

        internal UniOnCompleted(IExecutor? executor, int options, Promise<T> input,
                                object action, object? ctx) {
            this.executor = executor;
            this.options = options;
            this.input = input;
            this.action = action;
            this.ctx = ctx;
        }

        public override int Options {
            get => options;
            set => options = value;
        }

        protected bool Claim() {
            IExecutor? e = this.executor;
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

        public override AbstractPromise? TryFire(int mode) {
            Promise<T>? input = this.input;
            {
                if (ExecutorUtil.IsCancelRequested(ctx, options)) {
                    goto outer;
                }
                // 异步模式下已经claim
                if (!FireNow(input, action, ctx, mode > 0 ? null : this)) {
                    return null;
                }
            }
            outer:
            // help gc
            this.executor = null;
            this.input = null;
            this.action = null;
            this.ctx = null;
            return null;
        }

        public static bool FireNow(Promise<T> input,
                                   object rawAction, object? ctx,
                                   UniOnCompleted? c) {
            try {
                if (c != null && !c.Claim()) {
                    return false;
                }
                if (rawAction is Action<IFuture<T>> action1) {
                    action1.Invoke(input);
                } else {
                    Action<IFuture<T>, object?> action2 = (Action<IFuture<T>, object?>)rawAction;
                    action2.Invoke(input, ctx);
                }
            }
            catch (Exception e) {
                FutureLogger.LogCause(e, "UniOnCompleted caught an exception");
            }
            return true;
        }
    }

    #endregion
}
}