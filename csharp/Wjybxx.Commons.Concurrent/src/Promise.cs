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
using System.Diagnostics.CodeAnalysis;
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
            this._ex = WrapException(ex);
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

    public static Promise<T> FromCancelled(IExecutor? executor = null) {
        Exception ex = new OperationCanceledException();
        return new Promise<T>(executor, default, ex);
    }

    public static Promise<T> FromCancelled(CancellationToken cts, IExecutor? executor = null) {
        Exception ex = new OperationCanceledException(cts);
        return new Promise<T>(executor, default, ex);
    }

    #region internal

    internal void Reset() {
#pragma warning disable CS0420        
        stack = null;
        _executor = null;
        _result = default;
        ref object? exRef = ref _ex; // 去除volatile内存屏障，由对象池保证可见性
        exRef = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetExecutor(IExecutor e) {
        _executor = e;
    }

    /// <summary>
    /// Promise进入了完成状态，子类可清理不再需要的数据，不可执行其它逻辑
    /// </summary>
    protected virtual void OnCompleted() {
    }

#pragma warning disable CS0420
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
        object result = WrapException(ex);
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

    #endregion

    #region 上下文

    /// <summary>
    /// 允许重写，Executor可能存储在其它地方
    /// </summary>
    public virtual IExecutor? Executor => _executor;

    #endregion

    #region 状态查询

    /** 是否表示完成状态 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDone0(int state) {
        return state >= ST_SUCCESS;
    }

    /** 是否表示完成状态 -- 不包含发布状态 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDone0([NotNullWhen(true)] object? ex) {
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

    internal sealed override bool IsRelaxedCompleted => PeekState(_ex, strict: false) >= ST_SUCCESS;
    internal sealed override bool IsStrictlyCompleted => PeekState(_ex) >= ST_SUCCESS;

    #endregion

    #region 状态更新

    public bool TrySetComputing() {
        object preState = Interlocked.CompareExchange(ref _ex, EX_COMPUTING, null);
        if (preState == null) {
            return true;
        }
        return false;
    }

    public TaskStatus TrySetComputing2() {
        object preState = Interlocked.CompareExchange(ref _ex, EX_COMPUTING, null);
        if (preState == null) {
            return TaskStatus.Pending;
        }
        return (TaskStatus)PeekState(preState);
    }

    public void SetComputing() {
        if (!TrySetComputing()) {
            throw new InvalidOperationException("Already computing");
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
            throw new InvalidOperationException("Already complete");
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
            throw new InvalidOperationException("Already complete");
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
            throw new InvalidOperationException("Already complete");
        }
    }

    public bool TrySetCancelled(CancellationToken cts = default) {
        if (PeekState(_ex) > ST_COMPUTING) return false; // 避免创建不必要的异常
        if (InternalSetException(new OperationCanceledException(cts))) {
            OnCompleted();
            PostComplete(this);
            return true;
        }
        return false;
    }

    public void SetCancelled(CancellationToken cts = default) {
        if (!TrySetCancelled(cts)) {
            throw new InvalidOperationException("Already complete");
        }
    }

    #endregion

    #region 非阻塞结果查询

    public T ResultNow() {
        if (_ex == EX_SUCCESS) {
            return _result;
        }
        int state = PollState(ref _ex);
        return state switch
        {
            ST_SUCCESS => _result,
            ST_FAILED => throw new InvalidOperationException("Task completed with exception"),
            ST_CANCELLED => throw new InvalidOperationException("Task was cancelled"),
            _ => throw new InvalidOperationException("Task has not completed")
        };
    }

    public Exception ExceptionNow(bool throwIfCancelled = true) {
        return ExceptionNow(ref _ex, throwIfCancelled);
    }

    public object ExceptionOrDispatchInfoNow() {
        return ExceptionOrDispatchInfoNow(ref _ex);
    }

    private T ReportJoin(int state) {
        Debug.Assert(state > 0);
        if (state == ST_SUCCESS) {
            return _result;
        }
        if (state == ST_CANCELLED) {
            throw (OperationCanceledException)_ex!;
        }
        ExceptionDispatchInfo dispatchInfo = (ExceptionDispatchInfo)_ex!;
        dispatchInfo.Throw(); // 不再封装异常
        return default;
    }

    #endregion

    #region 阻塞结果查询

    protected void CheckDeadlock() {
        if (Executor is ISingleThreadExecutor se && se.InEventLoop()) {
            throw new BlockingOperationException();
        }
    }

    public T Get() {
        int state = PollState(ref _ex);
        if (IsDone0(state)) {
            return ReportJoin(state);
        }
        Await();
        return ReportJoin(PollState(ref _ex));
    }

    public T Get(TimeSpan timeout) {
        int state = PollState(ref _ex);
        if (IsDone0(state)) {
            return ReportJoin(state);
        }
        if (Await(timeout)) {
            return ReportJoin(PollState(ref _ex));
        }
        throw new TimeoutException();
    }

    public T Join() {
        int state = PollState(ref _ex);
        if (IsDone0(state)) {
            return ReportJoin(state);
        }
        AwaitUninterruptibly();
        return ReportJoin(PollState(ref _ex));
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

    #endregion

    #region OnCompleted

    public void OnCompleted(Action<IFuture<T>, object?> continuation, object? state,
                            int options = 0, CancellationToken cancelToken = default) {
        PushUniOnCompleted(null, options, cancelToken, continuation, state);
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>, object?> continuation, object? state,
                                 int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        PushUniOnCompleted(executor, options, cancelToken, continuation, state);
    }

    private void PushUniOnCompleted(IExecutor? executor, int options, CancellationToken cancelToken,
                                    Action<IFuture<T>, object?> continuation, object? state) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsCompleted && executor == null) {
            UniOnCompleted.FireNow(this, continuation, state, null);
        } else {
            PushCompletion(new UniOnCompleted(executor, options, cancelToken, this, continuation, state));
        }
    }

    #endregion

    #region OnCompleted-fsm

    public void OnCompleted(Action<object?> continuation, object? state,
                            int options = 0, CancellationToken cancelToken = default) {
        PushUniOnCompletedFsm(null, options, cancelToken, continuation, state);
    }

    public void OnCompletedAsync(IExecutor executor, Action<object?> continuation, object? state,
                                 int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        PushUniOnCompletedFsm(executor, options, cancelToken, continuation, state);
    }

    /** 状态机特殊优化 */
    private void PushUniOnCompletedFsm(IExecutor? executor, int options, CancellationToken cancelToken,
                                        Action<object?> continuation, object? state) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsCompleted && executor == null) {
            // 需检查取消令牌，行为一致性
            if (!cancelToken.IsCancellationRequested) {
                UniOnCompletedFsm.FireNow(continuation, state, null);
            }
        } else {
            UniOnCompletedFsm completion = new(executor, options, cancelToken, continuation, state);
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

    public IFuture<U> ComposeApply<U>(Func<T, IFuture<U>> fn,
                                      int options = 0, CancellationToken cancelToken = default) {
        return PushUniComposeApply(null, options, cancelToken, fn);
    }

    public IFuture<U> ComposeApplyAsync<U>(IExecutor executor, Func<T, IFuture<U>> fn,
                                           int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniComposeApply(executor, options, cancelToken, fn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IFuture<U> PushUniComposeApply<U>(IExecutor? executor, int options, CancellationToken cancelToken,
                                              Func<T, IFuture<U>> fn) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniComposeApply<T, U>(executor, options, cancelToken, this, promise, fn));
        return promise;
    }

    #endregion

    #region compose-call

    public IFuture<U> ComposeCall<U>(Func<IFuture<U>> fn,
                                     int options = 0, CancellationToken cancelToken = default) {
        return PushComposeCall(null, options, cancelToken, fn);
    }

    public IFuture<U> ComposeCallAsync<U>(IExecutor executor, Func<IFuture<U>> fn,
                                          int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushComposeCall(executor, options, cancelToken, fn);
    }

    private IFuture<U> PushComposeCall<U>(IExecutor? executor, int options, CancellationToken cancelToken,
                                          Func<IFuture<U>> fn) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniComposeCall<T, U>(executor, options, cancelToken, this, promise, fn));
        return promise;
    }

    #endregion

    #region ComposeCatching

    public IFuture<T> ComposeCatching<X>(Func<X, IFuture<T>> fallback,
                                         int options = 0, CancellationToken cancelToken = default) where X : Exception {
        return PushComposeCatching(null, options, cancelToken, fallback);
    }

    public IFuture<T> ComposeCatchingAsync<X>(IExecutor executor, Func<X, IFuture<T>> fallback,
                                              int options = 0, CancellationToken cancelToken = default) where X : Exception {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushComposeCatching(executor, options, cancelToken, fallback);
    }

    private IFuture<T> PushComposeCatching<X>(IExecutor? executor, int options, CancellationToken cancelToken,
                                              Func<X, IFuture<T>> fallback) where X : Exception {
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        Promise<T> promise = NewIncompletePromise<T>(executor == null ? this.Executor : executor);
        PushCompletion(new UniComposeCatching<X, T>(executor, options, cancelToken, this, promise, fallback));
        return promise;
    }

    #endregion

    #region ComposeHandle

    public IFuture<U> ComposeHandle<U>(Func<T, Exception, IFuture<U>> fn,
                                       int options = 0, CancellationToken cancelToken = default) {
        return PushComposeHandle(null, options, cancelToken, fn);
    }

    public IFuture<U> ComposeHandleAsync<U>(IExecutor executor, Func<T, Exception, IFuture<U>> fn,
                                            int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushComposeHandle(executor, options, cancelToken, fn);
    }

    private IFuture<U> PushComposeHandle<U>(IExecutor? executor, int options, CancellationToken cancelToken,
                                            Func<T, Exception, IFuture<U>> fn) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniComposeHandle<T, U>(executor, options, cancelToken, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-apply

    public IFuture<U> ThenApply<U>(Func<T, U> fn,
                                   int options = 0, CancellationToken cancelToken = default) {
        return PushUniApply(null, options, cancelToken, fn);
    }

    public IFuture<U> ThenApplyAsync<U>(IExecutor executor, Func<T, U> fn,
                                        int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniApply(executor, options, cancelToken, fn);
    }


    private IFuture<U> PushUniApply<U>(IExecutor? executor, int options, CancellationToken cancelToken,
                                       Func<T, U> fn) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniApply<T, U>(executor, options, cancelToken, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-accpt

    public IFuture ThenAccept(Action<T> fn,
                              int options = 0, CancellationToken cancelToken = default) {
        return PushUniAccept(null, options, cancelToken, fn);
    }

    public IFuture ThenAcceptAsync(IExecutor executor, Action<T> fn,
                                   int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniAccept(executor, options, cancelToken, fn);
    }

    private IFuture PushUniAccept(IExecutor? executor, int options, CancellationToken cancelToken,
                                  Action<T> fn) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        Promise<int> promise = NewIncompletePromise<int>(executor == null ? this.Executor : executor);
        PushCompletion(new UniAccept<T>(executor, options, cancelToken, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-call

    public IFuture<U> ThenCall<U>(Func<U> fn,
                                  int options = 0, CancellationToken cancelToken = default) {
        return PushUniCall(null, options, cancelToken, fn);
    }

    public IFuture<U> ThenCallAsync<U>(IExecutor executor, Func<U> fn,
                                       int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniCall(executor, options, cancelToken, fn);
    }

    private IFuture<U> PushUniCall<U>(IExecutor? executor, int options, CancellationToken cancelToken,
                                      Func<U> fn) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniCall<T, U>(executor, options, cancelToken, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-run

    public IFuture ThenRun(Action fn,
                           int options = 0, CancellationToken cancelToken = default) {
        return PushUniRun(null, options, cancelToken, fn);
    }

    public IFuture ThenRunAsync(IExecutor executor, Action fn,
                                int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniRun(executor, options, cancelToken, fn);
    }

    private IFuture PushUniRun(IExecutor? executor, int options, CancellationToken cancelToken,
                               Action fn) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        Promise<int> promise = NewIncompletePromise<int>(executor == null ? this.Executor : executor);
        PushCompletion(new UniRun<T>(executor, options, cancelToken, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-catch

    public IFuture<T> Catching<X>(Func<X, T> fallback,
                                  int options = 0, CancellationToken cancelToken = default) where X : Exception {
        return PushUniCatching(null, options, cancelToken, fallback);
    }

    public IFuture<T> CatchingAsync<X>(IExecutor executor, Func<X, T> fallback,
                                       int options = 0, CancellationToken cancelToken = default) where X : Exception {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniCatching(executor, options, cancelToken, fallback);
    }

    private IFuture<T> PushUniCatching<X>(IExecutor? executor, int options, CancellationToken cancelToken,
                                          Func<X, T> fallback) where X : Exception {
        if (fallback == null) throw new ArgumentNullException(nameof(fallback));
        Promise<T> promise = NewIncompletePromise<T>(executor == null ? this.Executor : executor);
        PushCompletion(new UniCatching<X, T>(executor, options, cancelToken, this, promise, fallback));
        return promise;
    }

    #endregion

    #region uni-handle

    public IFuture<U> Handle<U>(Func<T, Exception, U> fn,
                                int options = 0, CancellationToken cancelToken = default) {
        return PushUniHandle(null, options, cancelToken, fn);
    }

    public IFuture<U> HandleAsync<U>(IExecutor executor, Func<T, Exception, U> fn,
                                     int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniHandle(executor, options, cancelToken, fn);
    }

    private IFuture<U> PushUniHandle<U>(IExecutor? executor, int options, CancellationToken cancelToken,
                                        Func<T, Exception, U> fn) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        Promise<U> promise = NewIncompletePromise<U>(executor == null ? this.Executor : executor);
        PushCompletion(new UniHandle<T, U>(executor, options, cancelToken, this, promise, fn));
        return promise;
    }

    #endregion

    #region uni-when-complete

    public IFuture<T> WhenComplete(Action<T, Exception> fn,
                                   int options = 0, CancellationToken cancelToken = default) {
        return PushUniWhenComplete(null, options, cancelToken, fn);
    }

    public IFuture<T> WhenCompleteAsync(IExecutor executor, Action<T, Exception> fn,
                                        int options = 0, CancellationToken cancelToken = default) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return PushUniWhenComplete(executor, options, cancelToken, fn);
    }

    private IFuture<T> PushUniWhenComplete(IExecutor? executor, int options, CancellationToken cancelToken,
                                           Action<T, Exception> fn) {
        if (fn == null) throw new ArgumentNullException(nameof(fn));
        Promise<T> promise = NewIncompletePromise<T>(executor == null ? this.Executor : executor);
        PushCompletion(new UniWhenComplete<T>(executor, options, cancelToken, this, promise, fn));
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

    private bool CompleteCancelled(CancellationToken cancelToken) {
        Debug.Assert(cancelToken.IsCancellationRequested);
        return InternalSetException(new OperationCanceledException(cancelToken));
    }

    /// <summary>
    /// 如果一个<c>Completion</c>在计算中出现异常，则使用该方法使目标进入完成状态。
    /// (出现新的异常)
    /// </summary>
    private bool CompleteThrowable(Exception x) {
        if (x is not OperationCanceledException) {
            FutureLogger.LogCause(x);
        }
        // C#不再封装异常，保留原始异常类型和堆栈
        return InternalSetException(x);
    }

    /// <summary>
    /// 使用依赖项的结果进入完成状态，通常表示当前<c>Completion</c>只是一个简单的中继。
    /// </summary>
    private bool CompleteRelay(T? r, object ex) {
        if (ex == EX_SUCCESS) {
            return InternalSetResult(r);
        } else {
            return InternalSetException(ex);
        }
    }

    /// <summary>
    /// 使用依赖项的异常结果进入完成状态，通常表示当前<c>Completion</c>只是一个简单的中继。
    /// 在已知依赖项异常完成的时候可以调用该方法，减少开销。
    /// 这里实现和Task不同，这里保留原始结果，不强制将异常转换为<see cref="CompletionException"/>。
    /// 这样有助与用户捕获正确的异常类型，而不是一个奇怪的CompletionException
    /// </summary>
    private bool CompleteRelayThrowable(object r) {
        return InternalSetException(r);
    }

    #endregion

    private abstract class UniCompletion<V, U> : Completion
    {
        protected IExecutor? executor;
        protected CancellationToken cancelToken;
        protected int options;
        protected Promise<V> input;
        protected Promise<U> output;

        protected UniCompletion(IExecutor? executor, int options, CancellationToken cancelToken,
                                Promise<V> input, Promise<U> output) {
            this.executor = executor;
            this.cancelToken = cancelToken;
            this.options = options;
            this.input = input;
            this.output = output;
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
            // 不应该存在其它竞争任务更新output，用户取消应该总是通过令牌实现
            // if (!output.TrySetComputing()) {
            //     throw new OperationCanceledException();
            // }
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
        private Func<V, IFuture<U>> fn;

        public UniComposeApply(IExecutor? executor, int options, CancellationToken cancelToken,
                               Promise<V> input, Promise<U> output,
                               Func<V, IFuture<U>> fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                if (cancelToken.IsCancellationRequested) {
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
                    IFuture<U> relay = fn(input._result);
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
            this.cancelToken = default;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniComposeCall<V, U> : UniCompletion<V, U>
    {
        private Func<IFuture<U>> fn;

        public UniComposeCall(IExecutor? executor, int options, CancellationToken cancelToken,
                              Promise<V> input, Promise<U> output,
                              Func<IFuture<U>> fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                if (cancelToken.IsCancellationRequested) {
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
                    IFuture<U> relay = fn();
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
            this.cancelToken = default;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniComposeCatching<X, V> : UniCompletion<V, V> where X : Exception
    {
        private Func<X, IFuture<V>> fn;

        public UniComposeCatching(IExecutor? executor, int options, CancellationToken cancelToken,
                                  Promise<V> input, Promise<V> output,
                                  Func<X, IFuture<V>> fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<V> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                if (cancelToken.IsCancellationRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                object rawEx = input._ex!;
                X ex; // 暂不恢复堆栈
                if (IsSucceed(rawEx) || (ex = UnwrapException(rawEx, restore: false) as X) == null) {
                    setCompleted = output.CompleteRelay(input._result, rawEx);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    IFuture<V> relay = fn(ex);
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
            this.cancelToken = default;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniComposeHandle<V, U> : UniCompletion<V, U>
    {
        private Func<V, Exception, IFuture<U>> fn;

        public UniComposeHandle(IExecutor? executor, int options, CancellationToken cancelToken,
                                Promise<V> input, Promise<U> output,
                                Func<V, Exception, IFuture<U>> fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                if (cancelToken.IsCancellationRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    object rawEx = input._ex!;
                    Exception ex = IsSucceed(rawEx) ? null : UnwrapException(rawEx, restore: false);
                    IFuture<U> relay = fn(input._result, ex);
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
            this.cancelToken = default;
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
        private Func<V, U> fn;

        public UniApply(IExecutor? executor, int options, CancellationToken cancelToken,
                        Promise<V> input, Promise<U> output,
                        Func<V, U> fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                if (cancelToken.IsCancellationRequested) {
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
                    setCompleted = output.CompleteValue(fn(input._result));
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.cancelToken = default;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniAccept<V> : UniCompletion<V, int>
    {
        private Action<V> fn;

        public UniAccept(IExecutor? executor, int options, CancellationToken cancelToken,
                         Promise<V> input, Promise<int> output,
                         Action<V> fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<int> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                if (cancelToken.IsCancellationRequested) {
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
                    fn(input._result);
                    setCompleted = output.CompleteNull();
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.cancelToken = default;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniCall<V, U> : UniCompletion<V, U>
    {
        private Func<U> fn;

        public UniCall(IExecutor? executor, int options, CancellationToken cancelToken,
                       Promise<V> input, Promise<U> output,
                       Func<U> fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                if (cancelToken.IsCancellationRequested) {
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
                    setCompleted = output.CompleteValue(fn());
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.cancelToken = default;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniRun<V> : UniCompletion<V, int>
    {
        private Action fn;

        public UniRun(IExecutor? executor, int options, CancellationToken cancelToken,
                      Promise<V> input, Promise<int> output,
                      Action fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<int> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                if (cancelToken.IsCancellationRequested) {
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
                    fn();
                    setCompleted = output.CompleteNull();
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.cancelToken = default;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniCatching<X, V> : UniCompletion<V, V> where X : Exception
    {
        private Func<X, V> fn;

        public UniCatching(IExecutor? executor, int options, CancellationToken cancelToken,
                           Promise<V> input, Promise<V> output,
                           Func<X, V> fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<V> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                if (cancelToken.IsCancellationRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                object rawEx = input._ex!;
                X ex; // 暂不恢复堆栈
                if (IsSucceed(rawEx) || (ex = UnwrapException(rawEx, restore: false) as X) == null) {
                    setCompleted = output.CompleteRelay(input._result, rawEx);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    setCompleted = output.CompleteValue(fn(ex));
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.cancelToken = default;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniHandle<V, U> : UniCompletion<V, U>
    {
        private Func<V, Exception, U> fn;

        public UniHandle(IExecutor? executor, int options, CancellationToken cancelToken,
                         Promise<V> input, Promise<U> output,
                         Func<V, Exception, U> fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<U> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                if (cancelToken.IsCancellationRequested) {
                    setCompleted = output.CompleteCancelled(cancelToken);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    object rawEx = input._ex!;
                    Exception ex = IsSucceed(rawEx) ? null : UnwrapException(rawEx);
                    setCompleted = output.CompleteValue(fn(input._result, ex));
                }
                catch (Exception e) {
                    setCompleted = output.CompleteThrowable(e);
                }
            }
            outer:
            // help gc
            this.cancelToken = default;
            this.input = null!;
            this.output = null!;
            this.fn = null!;
            return PostFire(output, mode, setCompleted);
        }
    }

    private class UniWhenComplete<V> : UniCompletion<V, V>
    {
        private Action<V, Exception> fn;

        public UniWhenComplete(IExecutor? executor, int options, CancellationToken cancelToken,
                               Promise<V> input, Promise<V> output,
                               Action<V, Exception> fn)
            : base(executor, options, cancelToken, input, output) {
            this.fn = fn;
        }

        public override AbstractPromise? TryFire(int mode) {
            Promise<V> input = this.input;
            Promise<V> output = this.output;
            bool setCompleted;
            {
                if (output.IsCompleted) {
                    setCompleted = false;
                    goto outer;
                }
                // 下游始终保持为上游结果，不进入取消状态
                if (cancelToken.IsCancellationRequested) {
                    setCompleted = output.CompleteRelay(input._result, input._ex!);
                    goto outer;
                }
                try {
                    if (mode <= 0 && !Claim()) {
                        return null; // 等待下次执行
                    }
                    object rawEx = input._ex!;
                    Exception ex = IsSucceed(rawEx) ? null : UnwrapException(rawEx);
                    fn(input._result, ex);
                    setCompleted = output.CompleteRelay(input._result, rawEx);
                }
                catch (Exception e) {
                    FutureLogger.LogCause(e, "UniWhenComplete caught an exception");
                    setCompleted = output.CompleteRelay(input._result, input._ex!);
                }
            }
            outer:
            // help gc
            this.cancelToken = default;
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
        private CancellationToken cancelToken;
        private int options;
        private Promise<T> input;
        private Action<IFuture<T>, object?> action;
        private object state;
#nullable restore

        internal UniOnCompleted(IExecutor? executor, int options, CancellationToken cancelToken,
                                Promise<T> input,
                                Action<IFuture<T>, object?> action, object? state) {
            this.executor = executor;
            this.cancelToken = cancelToken;
            this.options = options;
            this.input = input;
            this.action = action;
            this.state = state;
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
                if (cancelToken.IsCancellationRequested) {
                    goto outer;
                }
                // 异步模式下已经claim
                if (!FireNow(input, action, state, mode > 0 ? null : this)) {
                    return null;
                }
            }
            outer:
            // help gc
            this.cancelToken = default;
            this.input = null;
            this.action = null;
            this.state = null;
            return null;
        }

        public static bool FireNow(Promise<T> input,
                                   Action<IFuture<T>, object?> action, object? state,
                                   UniOnCompleted? c) {
            try {
                if (c != null && !c.Claim()) {
                    return false;
                }
                action.Invoke(input, state);
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