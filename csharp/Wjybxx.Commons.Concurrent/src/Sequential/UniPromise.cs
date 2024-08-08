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
using Wjybxx.Commons.Concurrent;
using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.Sequential
{
/// <summary>
/// 单线程版本的<see cref="IPromise{T}"/>
///
/// <h3>单线程化做的变动</h3>
/// 1.去除{@link #result}等的volatile操作，变更为普通字段。
/// 2.去除了阻塞操作Awaiter的支持。
/// 3.去除了state的中间状态 -- 可对比<see cref="UniPromise{T}"/>
/// 4.<see cref="TryInline"/>对executor的检测调整
///
/// <h3>Async的含义</h3>
/// 既然是单线程的，又何来异步一说？这里的异步是指不立即执行给定的行为，而是提交到Executor等待调度。
/// 这有什么作用？有几个作用：
/// 1.让出CPU，避免过多的任务集中处理。
/// 2.延迟到特定阶段执行 -- 通过<see cref="TaskOption"/>指定。
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
    public static readonly UniPromise<T> CANCELLED = new UniPromise<T>(null, default, StacklessCancellationException.INST1);

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
    /// 2. 如果为<see cref="AbstractUniPromise.EX_COMPUTING"/>，表示正在计算。
    /// 3. 如果为<see cref="AbstractUniPromise.EX_SUCCESS"/>，表示成功，且结果已可见。
    /// 4. 如果为其它异常，表示任务失败或取消。
    /// </summary>
    private object? _ex;

    /** 任务绑定的线程 -- 其实不一定是执行线程 */
    private readonly IExecutor? _executor;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executor">任务关联的线程，死锁检测等</param>
    public UniPromise(IExecutor? executor = null) {
        _executor = executor;
    }

    private UniPromise(IExecutor? executor, T result, Exception? ex) {
        this._executor = executor;
        if (ex == null) {
            this._result = result;
            this._ex = EX_SUCCESS;
        } else {
            this._result = default;
            this._ex = ex;
        }
    }

    public static UniPromise<T> FromResult(T result, IExecutor? executor = null) {
        return new UniPromise<T>(executor, result, null);
    }

    public static UniPromise<T> FromException(Exception ex, IExecutor? executor = null) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new UniPromise<T>(executor, default, ex);
    }

    public static UniPromise<T> FromCancelled(int code, IExecutor? executor = null) {
        Exception ex = StacklessCancellationException.InstOf(code);
        return new UniPromise<T>(executor, default, ex);
    }

    #region internal

    private bool InternalSetResult(T result) {
        object preEx = this._ex;
        if (preEx == null || preEx == EX_COMPUTING) {
            this._result = result;
            this._ex = EX_SUCCESS;
            return true;
        }
        return false;
    }

    private bool InternalSetException(Exception exception) {
        Debug.Assert(exception != null);
        object preEx = this._ex;
        if (preEx == null || preEx == EX_COMPUTING) {
            this._ex = exception;
            return true;
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

    public bool IsCompleted => PeekState(_ex) >= ST_SUCCESS;
    public bool IsFailedOrCancelled => PeekState(_ex) >= ST_FAILED;

    protected sealed override bool IsRelaxedCompleted => PeekState(_ex) >= ST_SUCCESS;
    protected sealed override bool IsStrictlyCompleted => PeekState(_ex, true) >= ST_SUCCESS;

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

    public void OnCompleted(Action<object?> continuation, object? state, int options = 0) {
        PushUniOnCompleted4(null, continuation, state, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<object?> continuation, object? state, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        PushUniOnCompleted4(executor, continuation, state, options);
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
            UniOnCompleted2 completion = UniOnCompleted2.POOL.Acquire();
            completion.Init(executor, options, this, continuation, state);
            PushCompletion(completion);
        }
    }

    private void PushUniOnCompleted3(IExecutor? executor, Action<IFuture<T>, IContext> continuation, IContext context, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsCompleted && executor == null) {
            UniOnCompleted3.FireNow(this, continuation, context, null);
        } else {
            PushCompletion(new UniOnCompleted3(executor, options, this, continuation, context));
        }
    }

    private void PushUniOnCompleted4(IExecutor? executor, Action<object?> continuation, object? state, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (IsCompleted && executor == null) {
            UniOnCompleted4.FireNow(continuation, state, null);
        } else {
            UniOnCompleted4 completion = UniOnCompleted4.POOL.Acquire();
            completion.Init(executor, options, continuation, state);
            PushCompletion(completion);
        }
    }

    #endregion

    #region completion

    private static bool TryInline(Completion completion, IExecutor e, int options) {
        // 尝试内联
        if (TaskOption.IsEnabled(options, TaskOption.STAGE_TRY_INLINE)) {
            if (e is IUniExecutorService) { // uni-executor支持
                return true;
            }
            if (e is ISingleThreadExecutor eventLoop
                && eventLoop.InEventLoop()) {
                return true;
            }
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

    private static bool IsCancelling(object? ctx, int options) {
        return TaskOption.IsEnabled(options, TaskOption.STAGE_CHECK_OBJECT_CTX)
               && ctx is IContext ctx2
               && ctx2.CancelToken.IsCancelling;
    }

    private abstract class UniOnCompleted : Completion
    {
#nullable disable
        protected IExecutor executor;
        protected int options;
        protected UniPromise<T> input;
#nullable enable
        protected UniOnCompleted() {
        }

        protected UniOnCompleted(IExecutor? executor, int options, UniPromise<T> input) {
            this.executor = executor;
            this.options = options;
            this.input = input;
        }

        protected void Init(IExecutor? executor, int options, UniPromise<T> input) {
            this.executor = executor;
            this.options = options;
            this.input = input;
        }

        public override void Reset() {
            next = null; // 不调用base
            executor = null;
            options = 0;
            input = null;
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

        protected internal override AbstractUniPromise? TryFire(int mode) {
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
        public UniOnCompleted2() {
        }

        public void Init(IExecutor? executor, int options, UniPromise<T> input,
                         Action<IFuture<T>, object> action, object? state) {
            base.Init(executor, options, input);
            this.action = action;
            this.state = state;
        }

        public override void Reset() {
            base.Reset();
            action = null;
            state = null;
        }

        protected internal override AbstractUniPromise? TryFire(int mode) {
            UniPromise<T>? input = this.input;
            {
                if (IsCancelling(state, options)) {
                    goto outer;
                }
                // 异步模式下已经claim
                if (!FireNow(input, action, state, mode > 0 ? null : this)) {
                    return null;
                }
            }
            outer:
            POOL.Release(this);
            // help gc
            // this.executor = null;
            // this.input = null;
            // this.action = null;
            // this.state = null;
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

        /// <summary>
        /// 放在类内部，避免随着Promise就初始化
        /// </summary>
        internal static readonly IObjectPool<UniOnCompleted2> POOL = new ConcurrentObjectPool<UniOnCompleted2>(
            () => new UniOnCompleted2(), task => task.Reset(), TaskPoolConfig.GetPoolSize<T>());
    }

    private class UniOnCompleted3 : UniOnCompleted
    {
#nullable disable
        private Action<IFuture<T>, IContext> action;
        private IContext context;
#nullable enable

        public UniOnCompleted3(IExecutor? executor, int options, UniPromise<T> input,
                               Action<IFuture<T>, IContext> action, IContext context)
            : base(executor, options, input) {
            this.action = action;
            this.context = context;
        }

        protected internal override AbstractUniPromise? TryFire(int mode) {
            UniPromise<T>? input = this.input;
            {
                if (context.CancelToken.IsCancelling) {
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
            this.input = null;
            this.action = null;
            this.context = null;
            return null;
        }

        public static bool FireNow(UniPromise<T> input,
                                   Action<IFuture<T>, IContext> action, IContext context,
                                   UniOnCompleted3? c) {
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

    /// <summary>
    /// 直接继承Completion，特殊优化
    /// </summary>
    private class UniOnCompleted4 : Completion
    {
#nullable disable
        protected IExecutor executor;
        protected int options;
        private Action<object> action;
        private object state;
#nullable enable

        private UniOnCompleted4() {
        }

        public void Init(IExecutor? executor, int options, Action<object?> action, object? state) {
            this.executor = executor;
            this.options = options;
            this.action = action;
            this.state = state;
        }

        public override void Reset() {
            next = null; // 减少调用
            executor = null;
            options = 0;
            action = null;
            state = null;
        }

        public override int Options {
            get => options;
            set => options = value;
        }

        private bool Claim() {
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

        protected internal override AbstractUniPromise? TryFire(int mode) {
            {
                if (IsCancelling(state, options)) {
                    goto outer;
                }
                // 异步模式下已经claim
                if (!FireNow(action, state, mode > 0 ? null : this)) {
                    return null;
                }
            }
            outer:
            POOL.Release(this);
            // help gc
            // this.executor = null;
            // this.input = null!;
            // this.action = null!;
            // this.state = null;
            return null;
        }

        public static bool FireNow(Action<object?> action, object? state,
                                   UniOnCompleted4? c) {
            try {
                if (c != null && !c.Claim()) {
                    return false;
                }
                action(state);
            }
            catch (Exception e) {
                FutureLogger.LogCause(e, "UniOnCompleted4 caught an exception");
            }
            return true;
        }

        /// <summary>
        /// 放在类内部，避免随着Promise就初始化
        /// </summary>
        internal static readonly IObjectPool<UniOnCompleted4> POOL = new ConcurrentObjectPool<UniOnCompleted4>(
            () => new UniOnCompleted4(), task => task.Reset(), TaskPoolConfig.GetPoolSize<T>());
    }

    #endregion
}
}