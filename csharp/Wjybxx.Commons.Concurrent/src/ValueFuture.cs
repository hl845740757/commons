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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 
/// </summary>
[AsyncMethodBuilder(typeof(AsyncValueFutureMethodBuilder))]
public readonly struct ValueFuture
{
    public static readonly ValueFuture COMPLETED = new ValueFuture();
    public static readonly ValueFuture CANCELLED = new ValueFuture(null, StacklessCancellationException.Default);

    private readonly object? _future;
    private readonly int _reentryId;
    private readonly object? _result;
    private readonly object? _ex;

    /** 用工厂方法构建，避免歧义 */
    private ValueFuture(object? r, object? ex) {
        _future = null;
        _reentryId = 0;
        _result = r;
        _ex = ex != null ? AbstractPromise.WrapException(ex) : null;
    }

    public ValueFuture(IFuture future) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = 0;
        _result = null;
        _ex = null;
    }

    public ValueFuture(IValuePromise future, int reentryId) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = reentryId;
        _result = null;
        _ex = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaiter GetAwaiter() => new(this);

    /// <summary>
    /// <see cref="IFuture.GetAwaitable"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaitable GetAwaitable(IExecutor executor, int options = 0) => new(this, executor, options);

    /// <summary>
    /// <see cref="IFuture.GetAwaitable"/>
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="suppressedTypes">需要压栈的异常</param>
    /// <param name="options">调度选项</param>
    /// <param name="requireResult">是否需要返回结果</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SuppressibleAwaitable GetAwaitable(IExecutor executor, SuppressedTypes suppressedTypes, int options = 0,
                                              bool requireResult = false) =>
        new(this, executor, (int)suppressedTypes | options, requireResult);

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
    public static ValueFuture FromCancelled(int cancelCode = 1) {
        Exception ex = StacklessCancellationException.InstOf(cancelCode);
        return new ValueFuture(null, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ValueFuture InternalFromException(object ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture(null, ex);
    }

    #endregion

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
                // 可能是子类，子类有额外数据 -- 避免创建额外实例
                return _ex.GetType() == typeof(OperationCanceledException)
                    ? Promise<object>.CANCELLED
                    : Promise<object>.FromException(canceledException);
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
    /// 是否是Future的包装类
    /// </summary>
    public bool IsWrapper => _future != null;

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

    internal static readonly Action<object> invoker = (state) => ((Action)state).Invoke();

    internal void OnCompleted(Action action, IExecutor? executor, int options) {
        if (_future == null) {
            throw new IllegalStateException();
        }
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (_future is IValuePromise valuePromise) {
            if (executor != null) {
                valuePromise.OnCompletedAsync(_reentryId, executor, invoker, action, options);
            } else {
                valuePromise.OnCompleted(_reentryId, invoker, action, options);
            }
        } else {
            IFuture future = (IFuture)_future;
            if (executor != null) {
                future.OnCompletedAsync(executor, invoker, action, options);
            } else {
                future.OnCompleted(invoker, action, options);
            }
        }
    }

    #endregion
}

/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
[AsyncMethodBuilder(typeof(AsyncValueFutureMethodBuilder<>))]
public readonly struct ValueFuture<T>
{
    public static readonly ValueFuture<T> COMPLETED = new ValueFuture<T>(default, null);
    public static readonly ValueFuture<T> CANCELLED = new ValueFuture<T>(default, StacklessCancellationException.Default);
    private static readonly bool IsReferenceType = typeof(T).IsClass;

    private readonly object? _future;
    private readonly int _reentryId;

    private readonly T? _result;
    private readonly object? _ex;

    /** 通过工厂方法创建 */
    private ValueFuture(T? result, object? ex) {
        _future = null;
        _reentryId = 0;
        _result = result;
        _ex = ex != null ? AbstractPromise.WrapException(ex) : null;
    }

    public ValueFuture(IFuture<T> future) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = 0;
        _result = default;
        _ex = null;
    }

    public ValueFuture(IValuePromise<T> future, int reentryId) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = reentryId;
        _result = default;
        _ex = null;
    }

    private ValueFuture(IValuePromise<object> future, int reentryId) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = reentryId;
        _result = default;
        _ex = null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaiter<T> GetAwaiter() => new(this);

    /// <summary>
    /// <see cref="IFuture.GetAwaitable"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueFutureAwaitable<T> GetAwaitable(IExecutor executor, int options = 0) => new(this, executor, options);

    /// <summary>
    /// <see cref="IFuture.GetAwaitable"/>
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SuppressibleAwaitable<T> GetAwaitable(IExecutor executor, SuppressedTypes suppressedTypes, int options = 0) =>
        new(this, executor, (int)suppressedTypes | options);

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
    public static ValueFuture<T> FromCancelled(int cancelCode = 1) {
        Exception ex = StacklessCancellationException.InstOf(cancelCode);
        return new ValueFuture<T>(default, ex);
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
                return (IsReferenceType && _result == null) // 避免测试null装箱
                    ? Promise<T>.COMPLETED
                    : Promise<T>.FromResult(_result);
            }
            if (_ex is OperationCanceledException canceledException) {
                // 可能是子类，有特殊数据 -- 避免创建额外实例
                return _ex.GetType() == typeof(OperationCanceledException)
                    ? Promise<T>.CANCELLED
                    : Promise<T>.FromException(canceledException);
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
    /// 是否是Future的包装类
    /// </summary>
    public bool IsWrapper => _future != null;

    /// <summary>
    /// 装箱为非泛型的<see cref="ValueFuture"/>
    /// 如果选择追踪最终结果，可以再通过<see cref="ValueFuture.GetAwaitable(IExecutor, SuppressedTypes, int)"/>统一处理结果。
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
            return (T)unsafePromise.GetResult(_reentryId);
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
            return TaskResult<T>.FromResult((T)unsafePromise.GetResult(_reentryId));
        }
        IFuture<T> future = (IFuture<T>)_future;
        if (suppressedTypes.IsSuppressible(future.Status)) {
            return TaskResult<T>.InternalFromException(future.ExceptionOrDispatchInfoNow());
        }
        return TaskResult<T>.FromResult(future.Get());
    }

    internal void OnCompleted(Action action, IExecutor? executor, int options) {
        if (_future == null) {
            throw new IllegalStateException();
        }
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (_future is IValuePromise valuePromise) {
            if (executor != null) {
                valuePromise.OnCompletedAsync(_reentryId, executor, ValueFuture.invoker, action, options);
            } else {
                valuePromise.OnCompleted(_reentryId, ValueFuture.invoker, action, options);
            }
        } else {
            IFuture future = (IFuture)_future;
            if (executor != null) {
                future.OnCompletedAsync(executor, ValueFuture.invoker, action, options);
            } else {
                future.OnCompleted(ValueFuture.invoker, action, options);
            }
        }
    }

    #endregion
}
}