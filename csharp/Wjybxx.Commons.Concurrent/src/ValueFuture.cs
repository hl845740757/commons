#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 1.框架会默认缓存await创建的状态机，可通过<see cref="TaskPoolConfig"/>调整池大小。
/// 2.状态机默认会在用户获取执行结果的时候回收，因此不可在await返回之后继续使用该Future对象 。
/// 3.如果用户不需要任务的执行结果，需调用<see cref="Forget"/>告知Promise在任务完成后自动回收。
/// 4.如果需要返回装箱的结果，可通过带有<see cref="SuppressedTypes"/>的GetAwaiter方法进行等待和获取结果。
/// </summary>
[AsyncMethodBuilder(typeof(AsyncValueFutureMethodBuilder))]
public readonly struct ValueFuture
{
    private readonly object? _future;
    private readonly int _reentryId;
    private readonly long _taskId;

    private readonly object? _result;
    private readonly object? _ex;

    /** 用工厂方法构建，避免歧义 */
    private ValueFuture(object? r, object? ex) {
        _future = null;
        _reentryId = 0;
        _taskId = 0;
        _result = r;
        _ex = ex != null ? AbstractPromise.WrapException(ex) : null;
    }

    public ValueFuture(IFuture future) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = 0;
        _taskId = 0;
        _result = null;
        _ex = null;
    }

    public ValueFuture(IValuePromise future, int reentryId) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = reentryId;
        _taskId = 0;
        _result = null;
        _ex = null;
    }

    private ValueFuture(in ValueFuture src, long taskId) {
        _future = src._future;
        _reentryId = src._reentryId;
        _taskId = taskId;
        _result = src._result;
        _ex = src._ex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaiter GetAwaiter() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaiter2 GetAwaiter(SuppressedTypes suppressedTypes, bool requireResult = false)
        => new(this, requireResult, null, (int)suppressedTypes);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaitable GetAwaitable(IExecutor? executor, int options = 0) => new(this, executor, options);

    /// <summary>
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="suppressedTypes">需要压栈的异常</param>
    /// <param name="options">调度选项</param>
    /// <param name="requireResult">是否需要返回结果</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaitable2 GetAwaitable(IExecutor? executor, SuppressedTypes suppressedTypes, int options = 0,
                                              bool requireResult = false) =>
        new(this, requireResult, executor, (int)suppressedTypes | options);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaitable2 GetAwaitable(SuppressedTypes suppressedTypes, bool requireResult = false) =>
        new(this, requireResult, null, (int)suppressedTypes);

    #region factory

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture FromResult(object? r = null) {
        return r == null ? default : new ValueFuture(r, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture FromException(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture(null, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture FromException(ExceptionDispatchInfo ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture(null, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture FromCancelled(int cancelCode = CancelCodes.REASON_DEFAULT) {
        Exception ex = StacklessCancellationException.InstOf(cancelCode);
        return new ValueFuture(null, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture FromTaskResult(TaskResult taskResult) {
        taskResult.Deconstruct(out object? result, out object? ex);
        return new ValueFuture(result, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueFuture InternalFromException(object ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture(null, ex);
    }

    #endregion

    /// <summary>
    /// 关联的任务id
    /// </summary>
    public long TaskId => _taskId;

    /// <summary>
    /// 绑定任务Id
    /// </summary>
    public ValueFuture WithTaskId(long taskId) {
        return new ValueFuture(in this, taskId);
    }

    /// <summary>
    /// 关联的任务的状态
    /// </summary>
    public TaskStatus Status {
        get {
            if (_future == null) {
                if (_ex == null) {
                    return TaskStatus.Success;
                }
                return _ex is OperationCanceledException ? TaskStatus.Cancelled : TaskStatus.Failed;
            }
            if (_future is IValuePromise valuePromise) {
                return valuePromise.GetStatus(_reentryId);
            }
            IFuture future = (IFuture)_future;
            return future.Status;
        }
    }

    /// <summary>
    /// 查询任务是否已完成
    /// </summary>
    public bool IsCompleted {
        get {
            if (_future == null) {
                return true;
            }
            if (_future is IValuePromise valuePromise) {
                return valuePromise.GetStatus(_reentryId).IsCompleted();
            }
            IFuture future = (IFuture)_future;
            return future.IsCompleted;
        }
    }

    /// <summary>
    /// 转换为可多次await的ValueFuture
    /// </summary>
    /// <returns></returns>
    public ValueFuture Preserve() => new ValueFuture(AsFuture());

    /// <summary>
    /// 转换为普通的Future
    /// 该方法应当避免调用多次，且不可以在await以后调用
    /// </summary>
    public IFuture AsFuture() {
        if (_future == null) {
            if (_ex == null) {
                return _result == null
                    ? Promise<object>.COMPLETED
                    : Promise<object>.FromResult(_result);
            }
            if (_ex is OperationCanceledException canceledException) {
                return Promise<object>.FromException(canceledException);
            }
            ExceptionDispatchInfo dispatchInfo = (ExceptionDispatchInfo)_ex;
            return Promise<object>.FromException(dispatchInfo);
        }
        if (_future is IValuePromise valuePromise) {
            return valuePromise.AsFuture(_reentryId);
        }
        return (IFuture)_future;
    }

    /// <summary>
    /// 持久化结果
    /// 
    /// 1.只能在任务已完成的状态下调用
    /// 2.该方法可以释放Future的引用
    /// </summary>
    /// <param name="requireResult">是否保留结果</param>
    /// <returns></returns>
    public ValueFuture Memorize(bool requireResult = false) {
        if (!IsCompleted) {
            throw new InvalidOperationException();
        }
        TaskResult taskResult = GetResult(SuppressedTypes.All, requireResult);
        taskResult.Deconstruct(out object? result, out object? ex);
        return new ValueFuture(result, ex);
    }

    /// <summary>
    /// 如果用户不需要结果，可以调用该函数，告知Promise在任务完成后自动回收。
    /// 也用于压制警告
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Forget() {
        if (_future is IValuePromise valuePromise) {
            valuePromise.Forget(_reentryId);
        }
    }

    /// <summary>
    /// 拆箱（小心使用）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [Beta("不安全")]
    public ValueFuture<T> Unbox<T>(bool allowUnsafe = true) {
        if (_future == null) {
            if (_ex == null) {
                T r = _result == null ? default : (T)_result;
                return ValueFuture<T>.FromResult(r);
            }
            return ValueFuture<T>.InternalFromException(_ex);
        }
        if (_future is IValuePromise<T> valuePromise) {
            return new ValueFuture<T>(valuePromise, _reentryId);
        }
        if (_future is IFuture<T> future) {
            return new ValueFuture<T>(future);
        }
        if (allowUnsafe && _future is IValuePromise<object> unsafePromise) {
            return ValueFuture<T>.UnsafeCreate(unsafePromise, _reentryId);
        }
        throw new InvalidOperationException();
    }

    #region internal

    // internal是因为不希望用户调用

    /// <summary>
    /// 获取任务的结果
    /// 
    /// ps：不对外，会触发Promise回收
    /// </summary>
    internal void GetResult() {
        if (_future == null) {
            if (_ex == null) {
                return;
            }
            if (_ex is OperationCanceledException canceledException) {
                throw BetterCancellationException.Capture(canceledException);
            }
            ExceptionDispatchInfo dispatchInfo = (ExceptionDispatchInfo)_ex;
            dispatchInfo.Throw();
            return;
        }
        if (_future is IValuePromise valuePromise) {
            valuePromise.GetVoidResult(_reentryId);
            return;
        }
        IFuture future = (IFuture)_future;
        future.ThrowIfFailedOrCancelled();
    }

    /// <summary>
    /// 获取任务的结果，可抑制异常的抛出
    /// </summary>
    /// <param name="suppressedTypes">禁止抛出信息</param>
    /// <param name="requireResult">是否返回装箱的结果</param>
    /// <returns></returns>
    internal TaskResult GetResult(SuppressedTypes suppressedTypes, bool requireResult) {
        if (_future == null) {
            if (_ex == null) {
                return TaskResult.FromResult(requireResult ? _result : null);
            }
            if (_ex is OperationCanceledException canceledException) {
                if (suppressedTypes.HasFlag(SuppressedTypes.Cancellation)) {
                    return TaskResult.FromException(canceledException);
                }
                throw BetterCancellationException.Capture(canceledException);
            }
            ExceptionDispatchInfo dispatchInfo = (ExceptionDispatchInfo)_ex;
            if (suppressedTypes.HasFlag(SuppressedTypes.Error)) {
                return TaskResult.FromException(dispatchInfo);
            }
            dispatchInfo.Throw();
            return default;
        }
        if (_future is IValuePromise valuePromise) {
            if (suppressedTypes.IsSuppressible(valuePromise.GetStatus(_reentryId))) {
                return TaskResult.InternalFromException(valuePromise.GetExceptionOrDispatchInfo(_reentryId));
            }
            if (requireResult) {
                return TaskResult.FromResult(valuePromise.GetResult(_reentryId));
            } else {
                valuePromise.GetVoidResult(_reentryId);
                return default;
            }
        }
        IFuture future = (IFuture)_future;
        if (suppressedTypes.IsSuppressible(future.Status)) {
            return TaskResult.InternalFromException(future.ExceptionOrDispatchInfoNow());
        }
        if (requireResult) {
            return TaskResult.FromResult(future.Get());
        } else {
            future.ThrowIfFailedOrCancelled();
            return default;
        }
    }

    internal void OnCompleted(Action<object> action, object state, IExecutor? executor, int options) {
        if (_future == null) throw new InvalidOperationException();
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (_future is IValuePromise valuePromise) {
            if (executor != null) {
                valuePromise.OnCompletedAsync(_reentryId, executor, action, state, options);
            } else {
                valuePromise.OnCompleted(_reentryId, action, state, options);
            }
        } else {
            IFuture future = (IFuture)_future;
            if (executor != null) {
                future.OnCompletedAsync(executor, action, state, options);
            } else {
                future.OnCompleted(action, state, options);
            }
        }
    }

    #endregion
}

/// <summary>
/// 1.框架会默认缓存await创建的状态机，可通过<see cref="TaskPoolConfig"/>调整池大小。
/// 2.状态机默认会在用户获取执行结果的时候回收，因此不可在await返回之后继续使用该Future对象 。
/// 3.如果用户不需要任务的执行结果，需调用<see cref="Forget"/>告知Promise在任务完成后自动回收。
/// </summary>
/// <typeparam name="T"></typeparam>
[AsyncMethodBuilder(typeof(AsyncValueFutureMethodBuilder<>))]
public readonly struct ValueFuture<T>
{
    private readonly object? _future;
    private readonly int _reentryId;
    private readonly long _taskId;

    private readonly T? _result;
    private readonly object? _ex;

    /** 通过工厂方法创建 */
    private ValueFuture(T? result, object? ex) {
        _future = null;
        _reentryId = 0;
        _taskId = 0;
        _result = result;
        _ex = ex != null ? AbstractPromise.WrapException(ex) : null;
    }

    public ValueFuture(IFuture<T> future) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = 0;
        _taskId = 0;
        _result = default;
        _ex = null;
    }

    public ValueFuture(IValuePromise<T> future, int reentryId) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = reentryId;
        _taskId = 0;
        _result = default;
        _ex = null;
    }

    private ValueFuture(IValuePromise<object> future, int reentryId) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = reentryId;
        _taskId = 0;
        _result = default;
        _ex = null;
    }

    private ValueFuture(in ValueFuture<T> src, long taskId) {
        _future = src._future;
        _reentryId = src._reentryId;
        _taskId = taskId;
        _result = src._result;
        _ex = src._ex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaiter<T> GetAwaiter() => new(this);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaiter2<T> GetAwaiter(SuppressedTypes suppressedTypes)
        => new(this, null, (int)suppressedTypes);

    /// <summary>
    /// <see cref="IFuture.GetAwaitable"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaitable<T> GetAwaitable(IExecutor? executor, int options = 0) => new(this, executor, options);

    /// <summary>
    /// <see cref="IFuture.GetAwaitable"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaitable2<T> GetAwaitable(IExecutor? executor, SuppressedTypes suppressedTypes, int options = 0) =>
        new(this, executor, (int)suppressedTypes | options);

    /// <summary>
    /// 我们在大量的场景仅仅想禁用取消异常，因此提供快捷方法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaitable2<T> GetAwaitable(SuppressedTypes suppressedTypes) => new(this, null, (int)suppressedTypes);

    #region factory

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture<T> FromResult(T? result) {
        return new ValueFuture<T>(result, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture<T> FromException(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture<T>(default, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture<T> FromException(ExceptionDispatchInfo ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture<T>(default, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture<T> FromCancelled(int cancelCode = CancelCodes.REASON_DEFAULT) {
        Exception ex = StacklessCancellationException.InstOf(cancelCode);
        return new ValueFuture<T>(default, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueFuture<T> FromTaskResult(TaskResult<T> taskResult) {
        taskResult.Deconstruct(out T result, out object? ex);
        return new ValueFuture<T>(result, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueFuture<T> InternalFromException(object ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture<T>(default, ex);
    }

    /// <summary>
    /// 以不安全的方式创建一个future
    ///
    /// Q：该方法的作用？
    /// A：虽然<see cref="IValuePromise{T}"/>是池化的对象，但如果每一个泛型类型都使用独立的对象池，那么内存空间的利用率是低效的；
    /// 在CS的网络通信中，如RPC，结果类型通常是引用类型，为每个引用类型维护单独的对象池会造成大量不必要的浪费。
    /// 在这类场景，我们可以在底层总是使用object类型，而返回给用户的<see cref="ValueFuture{T}"/>是具体类型，从而优化对象的利用效率。
    /// </summary>
    /// <param name="future"></param>
    /// <param name="reentryId"></param>
    /// <returns></returns>
    public static ValueFuture<T> UnsafeCreate(IValuePromise<object> future, int reentryId) {
        return new ValueFuture<T>(future, reentryId);
    }

    #endregion

    /// <summary>
    /// 关联的任务id
    /// </summary>
    public long TaskId => _taskId;

    /// <summary>
    /// 绑定任务Id
    /// </summary>
    public ValueFuture<T> WithTaskId(long taskId) {
        return new ValueFuture<T>(in this, taskId);
    }

    /// <summary>
    /// 获取关联任务的状态
    /// </summary>
    public TaskStatus Status {
        get {
            if (_future == null) {
                if (_ex == null) {
                    return TaskStatus.Success;
                }
                return _ex is OperationCanceledException ? TaskStatus.Cancelled : TaskStatus.Failed;
            }
            if (_future is IValuePromise valuePromise) {
                return valuePromise.GetStatus(_reentryId);
            }
            IFuture future = (IFuture)_future;
            return future.Status;
        }
    }

    /// <summary>
    /// 查询任务是否已完成
    /// </summary>
    public bool IsCompleted {
        get {
            if (_future == null) {
                return true;
            }
            if (_future is IValuePromise valuePromise) {
                return valuePromise.GetStatus(_reentryId).IsCompleted();
            }
            IFuture future = (IFuture)_future;
            return future.IsCompleted;
        }
    }

    /// <summary>
    /// 转换为可多次await的ValueFuture
    /// </summary>
    /// <returns></returns>
    public ValueFuture<T> Preserve() => new ValueFuture<T>(AsFuture());

    /// <summary>
    /// 转换为普通的Future
    /// 该方法应当避免调用多次，且不可以在await以后调用
    /// </summary>
    public IFuture<T> AsFuture() {
        if (_future == null) {
            if (_ex == null) {
                return (!typeof(T).IsValueType && _result == null) // 避免测试null装箱
                    ? Promise<T>.COMPLETED
                    : Promise<T>.FromResult(_result);
            }
            if (_ex is OperationCanceledException canceledException) {
                return Promise<T>.FromException(canceledException);
            }
            ExceptionDispatchInfo dispatchInfo = (ExceptionDispatchInfo)_ex;
            return Promise<T>.FromException(dispatchInfo);
        }
        if (_future is IValuePromise<T> valuePromise) {
            return valuePromise.AsFuture(_reentryId);
        }
        if (_future is IValuePromise<object> unsafePromise) {
            return unsafePromise.AsFuture<T>(_reentryId);
        }
        return (IFuture<T>)_future;
    }

    /// <summary>
    /// 持久化结果
    /// 
    /// 1.只能在任务已完成的状态下调用
    /// 2.该方法可以释放Future的引用
    /// </summary>
    public ValueFuture<T> Memorize() {
        if (!IsCompleted) {
            throw new InvalidOperationException();
        }
        TaskResult<T> taskResult = GetResult(SuppressedTypes.All);
        taskResult.Deconstruct(out T result, out object? ex);
        return new ValueFuture<T>(result, ex);
    }

    /// <summary>
    /// 1.忽略执行结果
    /// 2.也用于压制警告
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Forget() {
        if (_future is IValuePromise valuePromise) {
            valuePromise.Forget(_reentryId);
        }
    }

    /// <summary>
    /// 装箱为非泛型的<see cref="ValueFuture"/>
    /// 如果选择追踪最终结果，可以再通过<see cref="ValueFuture.GetAwaitable(SuppressedTypes, bool)"/>统一处理结果。
    /// </summary>
    /// <param name="requireResult">是否需要最终结果</param>
    /// <returns></returns>
    public ValueFuture Box(bool requireResult = true) {
        if (_future == null) {
            if (_ex == null) {
                return ValueFuture.FromResult(requireResult ? _result : null);
            }
            return ValueFuture.InternalFromException(_ex);
        }
        if (_future is IValuePromise valuePromise) {
            return new ValueFuture(valuePromise, _reentryId);
        }
        return new ValueFuture((IFuture)_future);
    }

    #region internal

    // internal是因为不希望用户调用

    /// <summary>
    /// 获取任务的结果
    /// 
    /// ps：不对外，会触发Promise回收
    /// </summary>
    internal T GetResult() {
        if (_future == null) {
            if (_ex == null) {
                return _result;
            }
            if (_ex is OperationCanceledException canceledException) {
                throw BetterCancellationException.Capture(canceledException);
            }
            ExceptionDispatchInfo dispatchInfo = (ExceptionDispatchInfo)_ex;
            dispatchInfo.Throw();
            return default;
        }
        if (_future is IValuePromise<T> valuePromise) {
            return valuePromise.GetResult(_reentryId);
        }
        if (_future is IValuePromise<object> unsafePromise) {
            object result = unsafePromise.GetResult(_reentryId);
            return result == null ? default : (T)result;
        }
        IFuture<T> future = (IFuture<T>)_future;
        return future.Get();
    }

    /// <summary>
    /// 获取任务的结果，可抑制异常的抛出
    /// </summary>
    /// <param name="suppressedTypes">禁止抛出信息</param>
    /// <returns></returns>
    internal TaskResult<T> GetResult(SuppressedTypes suppressedTypes) {
        if (_future == null) {
            if (_ex == null) {
                return TaskResult<T>.FromResult(_result);
            }
            if (_ex is OperationCanceledException canceledException) {
                if (suppressedTypes.HasFlag(SuppressedTypes.Cancellation)) {
                    return TaskResult<T>.FromException(canceledException);
                }
                throw BetterCancellationException.Capture(canceledException);
            }
            ExceptionDispatchInfo dispatchInfo = (ExceptionDispatchInfo)_ex;
            if (suppressedTypes.HasFlag(SuppressedTypes.Error)) {
                return TaskResult<T>.FromException(dispatchInfo);
            }
            dispatchInfo.Throw();
            return default;
        }
        if (_future is IValuePromise<T> valuePromise) {
            if (suppressedTypes.IsSuppressible(valuePromise.GetStatus(_reentryId))) {
                return TaskResult<T>.InternalFromException(valuePromise.GetExceptionOrDispatchInfo(_reentryId));
            }
            return TaskResult<T>.FromResult(valuePromise.GetResult(_reentryId));
        }
        if (_future is IValuePromise<object> unsafePromise) {
            if (suppressedTypes.IsSuppressible(unsafePromise.GetStatus(_reentryId))) {
                return TaskResult<T>.InternalFromException(unsafePromise.GetExceptionOrDispatchInfo(_reentryId));
            }
            object result = unsafePromise.GetResult(_reentryId);
            return TaskResult<T>.FromResult(result == null ? default : (T)result);
        }
        IFuture<T> future = (IFuture<T>)_future;
        if (suppressedTypes.IsSuppressible(future.Status)) {
            return TaskResult<T>.InternalFromException(future.ExceptionOrDispatchInfoNow());
        }
        return TaskResult<T>.FromResult(future.Get());
    }

    internal void OnCompleted(Action<object> action, object state, IExecutor? executor, int options) {
        if (_future == null) throw new InvalidOperationException();
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (_future is IValuePromise valuePromise) {
            if (executor != null) {
                valuePromise.OnCompletedAsync(_reentryId, executor, action, state, options);
            } else {
                valuePromise.OnCompleted(_reentryId, action, state, options);
            }
        } else {
            IFuture future = (IFuture)_future;
            if (executor != null) {
                future.OnCompletedAsync(executor, action, state, options);
            } else {
                future.OnCompleted(action, state, options);
            }
        }
    }

    #endregion
}
}