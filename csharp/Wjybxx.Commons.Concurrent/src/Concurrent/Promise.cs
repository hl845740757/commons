﻿#region LICENSE

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
using System.Threading;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// Promise不会实现两份（泛型和非泛型），那会导致大量的重复代码，有非常高的维护成本。
/// 在不需要结果的情况下，可以选择将泛型参数定义为byte或int，尽可能减少开销即可。
/// 
/// PS：重复编码不仅仅是指Promise，与Promise相关的各个体系都需要双份...
/// </summary>
/// <typeparam name="T"></typeparam>
public class Promise<T> : AbstractPromise, IPromise<T>
{
    private const int ST_PENDING = (int)TaskStatus.Pending;
    private const int ST_COMPUTING = (int)TaskStatus.Computing;
    private const int ST_SUCCESS = (int)TaskStatus.Success;
    private const int ST_FAILED = (int)TaskStatus.Failed;
    private const int ST_CANCELLED = (int)TaskStatus.Cancelled;

    /** 任务成功执行时的结果 -- 可见性由<see cref="_ex"/>保证 */
    private T _result;
    /// <summary>
    /// 任务失败完成时的结果，也包含了任务的状态。
    /// 
    /// 1. 如果为null，表示尚未开始。
    /// 2. 如果为<see cref="AbstractPromise.EX_COMPUTING"/>，表示正在计算。
    /// 3. 如果为<see cref="AbstractPromise.EX_PUBLISHING"/>，表示成功，但正在发布成功结果。
    /// 4. 如果为<see cref="AbstractPromise.EX_SUCCESS"/>，表示成功，且结果已可见。
    /// 5. 如果为其它异常，表示任务失败或取消。
    /// </summary>
    private volatile object? _ex;

    /** 任务绑定的线程 -- 其实不一定是执行线程 */
    private readonly IExecutor? _executor;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executor">任务关联的线程，死锁检测等</param>
    public Promise(IExecutor? executor = null) {
        _executor = executor;
    }

    private Promise(IExecutor? executor, T result, Exception? ex) {
        this._executor = executor;
        if (ex == null) {
            this._result = result;
            this._ex = EX_SUCCESS;
        } else {
            this._result = default;
            this._ex = ex;
        }
    }

    public static Promise<T> FromResult(T result, IExecutor? executor = null) {
        return new Promise<T>(executor, result, null);
    }

    public static Promise<T> FromException(Exception ex, IExecutor? executor = null) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new Promise<T>(executor, default, ex);
    }

    public static Promise<T> FromCancelled(int code, IExecutor? executor = null) {
        Exception ex = StacklessCancellationException.InstOf(code);
        return new Promise<T>(executor, default, ex);
    }

    #region internal

    private bool InternalSetResult(T result) {
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

    private bool InternalSetException(Exception exception) {
        Debug.Assert(exception != null);
        // 先测试Pending状态 -- 如果大多数任务都是先更新为Computing状态，则先测试Computing有优势，暂不优化
        object preEx = Interlocked.CompareExchange(ref _ex, exception, null);
        if (preEx == null) {
            return true;
        }
        if (preEx == EX_COMPUTING) {
            // 任务可能处于Computing状态，重试
            preEx = Interlocked.CompareExchange(ref _ex, exception, EX_COMPUTING);
            if (preEx == EX_COMPUTING) {
                return true;
            }
        }
        return false;
    }

    /** 获取当前状态，如果处于发布中状态，则等待目标线程发布完毕 */
    private int PollState() {
        object ex = _ex;
        if (ex == null) {
            return ST_PENDING;
        }
        if (ex == EX_COMPUTING) {
            return ST_COMPUTING;
        }
        if (ex == EX_PUBLISHING) {
            // busy spin -- 该过程通常很快，因此自旋等待即可
            while ((ex = _ex) == EX_PUBLISHING) {
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

    #region 上下文

    /// <summary>
    /// 允许重写，Executor可能存储在其它地方
    /// </summary>
    public virtual IExecutor? Executor => _executor;

    public IFuture<T> AsReadonly() => new ForwardFuture<T>(this);

    #endregion

    #region 状态查询

    /** 是否表示完成状态 */
    private static bool IsDone0(int state) {
        return state >= ST_SUCCESS;
    }

    public TaskStatus Status => (TaskStatus)PeekState(_ex);
    public bool IsPending => _ex == null;
    public bool IsComputing => _ex == EX_COMPUTING;
    public bool IsSucceeded => PeekState(_ex) == ST_SUCCESS;
    public bool IsFailed => PeekState(_ex) == ST_FAILED;
    public bool IsCancelled => _ex is OperationCanceledException;

    public bool IsDone => PeekState(_ex) >= ST_SUCCESS;
    public bool IsFailedOrCancelled => PeekState(_ex) >= ST_FAILED;

    protected sealed override bool IsRelaxedCompleted => PeekState(_ex) >= ST_SUCCESS;
    protected sealed override bool IsStrictlyCompleted => PeekState(_ex, true) >= ST_SUCCESS;

    #endregion

    #region 状态更新

    public bool TrySetComputing() {
        object preState = Interlocked.CompareExchange(ref _ex, EX_COMPUTING, null);
        return preState == null;
    }

    public TaskStatus TrySetComputing2() {
        object preState = Interlocked.CompareExchange(ref _ex, EX_COMPUTING, null);
        return (TaskStatus)PeekState(preState, false);
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

    public bool TrySetCancelled(int code) {
        if (InternalSetException(StacklessCancellationException.InstOf(code))) {
            PostComplete(this);
            return true;
        }
        return false;
    }

    public void SetCancelled(int code) {
        if (!TrySetCancelled(code)) {
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
        int state = PollState();
        return state switch
        {
            ST_FAILED => (Exception)_ex!,
            ST_CANCELLED when throwIfCancelled => throw (Exception)_ex!,
            ST_CANCELLED => (Exception)_ex!,
            ST_SUCCESS => throw new IllegalStateException("Task completed with a result"),
            _ => throw new IllegalStateException("Task has not completed")
        };
    }

    /** 上报future的执行结果 -- 取消以外的异常都将被包装为<see cref="CompletionException"/> */
    private T ReportJoin(int state) {
        Debug.Assert(state > 0);
        if (state == ST_SUCCESS) {
            return _result;
        }
        if (state == ST_CANCELLED) {
            throw (Exception)_ex!;
        }
        throw new CompletionException(null, (Exception)_ex);
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
        Await();
        return ReportJoin(PollState());
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

    public IFuture<T> Await() {
        if (IsDone) {
            return this;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            awaiter.Await();
        }
        return this;
    }

    public IFuture<T> AwaitUninterruptibly() {
        if (IsDone) {
            return this;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            awaiter.AwaitUninterruptibly();
        }
        return this;
    }

    public bool Await(TimeSpan timeout) {
        if (IsDone) {
            return true;
        }
        CheckDeadlock();
        Awaiter awaiter = TryPushAwaiter();
        if (awaiter != null) {
            return awaiter.Await(timeout);
        }
        return true;
    }

    public bool AwaitUninterruptibly(TimeSpan timeout) {
        if (IsDone) {
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

    public void OnCompleted(Action<IFuture<T>, IContext> continuation, IContext context, int options = 0) {
        PushUniOnCompleted3(null, continuation, context, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>, IContext> continuation, IContext context, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        PushUniOnCompleted3(executor, continuation, context, options);
    }

    private void PushUniOnCompleted1(IExecutor? executor, Action<IFuture<T>> continuation, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsDone && executor == null) {
            UniOnCompleted1<T>.FireNow(this, continuation, null);
        } else {
            PushCompletion(new UniOnCompleted1<T>(executor, options, this, continuation));
        }
    }

    private void PushUniOnCompleted2(IExecutor? executor, Action<IFuture<T>, object> continuation, object? state, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsDone && executor == null) {
            UniOnCompleted2<T>.FireNow(this, continuation, state, null);
        } else {
            PushCompletion(new UniOnCompleted2<T>(executor, options, this, continuation, state));
        }
    }

    private void PushUniOnCompleted3(IExecutor? executor, Action<IFuture<T>, IContext> continuation, IContext context, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsDone && executor == null) {
            UniOnCompleted3<T>.FireNow(this, continuation, context, null);
        } else {
            PushCompletion(new UniOnCompleted3<T>(executor, options, this, continuation, context));
        }
    }

    #endregion

    #region completion

    private static bool Submit(Completion completion, IExecutor e, int options) {
        // 尝试内联
        if (TaskOption.IsEnabled(options, TaskOption.STAGE_TRY_INLINE)
            && e is ISingleThreadExecutor eventLoop
            && eventLoop.InEventLoop()) {
            return true;
        }
        // 判断是否需要传递选项
        if (options != 0
            && !TaskOption.IsEnabled(options, TaskOption.STAGE_NON_TRANSITIVE)) {
            e.Execute(completion);
        } else {
            completion.Options = 0;
            e.Execute(completion);
        }
        return false;
    }

    private abstract class UniOnCompleted<V> : Completion
    {
        protected IExecutor? executor;
        protected int options;
        protected Promise<V> input;

        protected UniOnCompleted(IExecutor? executor, int options, Promise<V> input) {
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
                return Submit(this, e, options);
            }
            return true;
        }
    }

    private class UniOnCompleted1<V> : UniOnCompleted<V>
    {
        private Action<IFuture<V>> action;

        public UniOnCompleted1(IExecutor? executor, int options, Promise<V> input, Action<IFuture<V>> action)
            : base(executor, options, input) {
            this.action = action;
        }

        protected internal override AbstractPromise? TryFire(int mode) {
            Promise<V>? input = this.input;
            {
                // 异步模式下已经claim
                if (!FireNow(input, action, mode > 0 ? null : this)) {
                    return null;
                }
            }
            // help gc
            this.executor = null;
            this.input = null!;
            this.action = null!;
            return null;
        }

        public static bool FireNow(Promise<V> input, Action<IFuture<V>> action,
                                   UniOnCompleted1<V>? c) {
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

    private class UniOnCompleted2<V> : UniOnCompleted<V>
    {
        private Action<IFuture<V>, object> action;
        private object? state;

        public UniOnCompleted2(IExecutor? executor, int options, Promise<V> input,
                               Action<IFuture<V>, object> action, object? state)
            : base(executor, options, input) {
            this.action = action;
            this.state = state;
        }

        protected internal override AbstractPromise? TryFire(int mode) {
            Promise<V>? input = this.input;
            {
                // 异步模式下已经claim
                if (!FireNow(input, action, state, mode > 0 ? null : this)) {
                    return null;
                }
            }
            // help gc
            this.executor = null;
            this.input = null!;
            this.action = null!;
            this.state = null;
            return null;
        }

        public static bool FireNow(Promise<V> input,
                                   Action<IFuture<V>, object> action, object? state,
                                   UniOnCompleted2<V>? c) {
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

    private class UniOnCompleted3<V> : UniOnCompleted<V>
    {
        private Action<IFuture<V>, IContext> action;
        private IContext context;

        public UniOnCompleted3(IExecutor? executor, int options, Promise<V> input,
                               Action<IFuture<V>, IContext> action, in IContext context)
            : base(executor, options, input) {
            this.action = action;
            this.context = context;
        }

        protected internal override AbstractPromise? TryFire(int mode) {
            Promise<V>? input = this.input;
            {
                if (context.CancelToken.IsCancelling()) {
                    goto outer;
                }
                // 异步模式下已经claim
                if (!FireNow(input, action, context, mode > 0 ? null : this)) {
                    return null;
                }
            }
            outer:
            // help gc
            this.executor = null;
            this.input = null!;
            this.action = null!;
            return null;
        }

        public static bool FireNow(Promise<V> input,
                                   Action<IFuture<V>, IContext> action, in IContext context,
                                   UniOnCompleted3<V>? c) {
            try {
                if (c != null && !c.Claim()) {
                    return false;
                }
                action(input, context);
            }
            catch (Exception e) {
                FutureLogger.LogCause(e, "UniOnCompleted2 caught an exception");
            }
            return true;
        }
    }

    #endregion
}