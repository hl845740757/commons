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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 
/// </summary>
[AsyncMethodBuilder(typeof(AsyncValueFutureMethodBuilder))]
public readonly struct ValueFuture
{
    public static readonly ValueFuture COMPLETED = new ValueFuture();
    public static readonly ValueFuture CANCELLED = new ValueFuture(StacklessCancellationException.INST1);

    private readonly object? _future;
    private readonly int _reentryId;
    private readonly Exception? _ex;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ex">如果为null，则表示成功</param>
    public ValueFuture(Exception? ex) {
        _future = null;
        _reentryId = 0;
        _ex = ex;
    }

    public ValueFuture(IFuture future) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = 0;
        _ex = null;
    }

    public ValueFuture(IValuePromise future, int reentryId) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _reentryId = reentryId;
        _ex = null;
    }

    public ValueFutureAwaiter GetAwaiter() => new ValueFutureAwaiter(this);

    public ValueFutureAwaitable GetAwaitable(IExecutor executor, int options = 0) => new ValueFutureAwaitable(this, executor, options);

    public static ValueFuture FromResult() {
        return new ValueFuture((Exception)null);
    }

    public static ValueFuture FromException(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture(ex);
    }

    public static ValueFuture FromCancelled(int cancelCode) {
        Exception ex = StacklessCancellationException.InstOf(cancelCode);
        return new ValueFuture(ex);
    }

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
    /// 转换为普通的Future
    /// 该方法应当避免调用多次，且不可以在await以后调用
    /// </summary>
    public IFuture AsFuture() {
        if (_future == null) {
            if (_ex != null) {
                return Promise<int>.FromException(_ex);
            }
            return Promise<int>.FromResult(0);
        }
        if (_future is IFuture future) {
            return future;
        }
        IValuePromise valuePromise = (IValuePromise)_future;
        TaskStatus status = valuePromise.GetStatus(_reentryId);
        switch (status) {
            case TaskStatus.Success: {
                valuePromise.GetVoidResult(_reentryId);
                return Promise<int>.FromResult(0);
            }
            case TaskStatus.Cancelled:
            case TaskStatus.Failed: {
                Exception ex = valuePromise.GetException(_reentryId);
                return Promise<int>.FromException(ex);
            }
            default: {
                Promise<int> promise = new Promise<int>();
                valuePromise.SetVoidPromiseWhenCompleted(_reentryId, promise);
                return promise;
            }
        }
    }

    public ValueFuture Preserve() => new ValueFuture(AsFuture());

    #region internal

    // internal是因为不希望用户调用

    /// <summary>
    /// 获取任务的结果
    /// 
    /// ps：不对外，会触发Promise回收
    /// </summary>
    internal void GetResult() {
        if (_future == null) {
            if (_ex != null) {
                throw _ex; // TODO 会丢失堆栈
            }
            return;
        }
        if (_future is IValuePromise valuePromise) {
            valuePromise.GetVoidResult(_reentryId);
        } else {
            IFuture future = (IFuture)_future;
            future.ThrowIfFailedOrCancelled();
        }
    }

    internal static readonly Action<object> driverCallBack = (state) => ((Action)state).Invoke();
    private static readonly Action<IFuture, object> futureCallback = (_, state) => ((Action)state).Invoke();

    internal void OnCompleted(Action action, IExecutor? executor, int options) {
        if (_future == null) {
            throw new IllegalStateException();
        }
        if (action == null) throw new ArgumentNullException(nameof(action));
        if (_future is IValuePromise valuePromise) {
            valuePromise.OnCompleted(_reentryId, driverCallBack, action, executor, options);
        } else {
            IFuture future = (IFuture)_future;
            if (executor != null) {
                future.OnCompletedAsync(executor, futureCallback, action, options);
            } else {
                future.OnCompleted(futureCallback, action, options);
            }
        }
    }

    #endregion
}

[AsyncMethodBuilder(typeof(AsyncValueFutureMethodBuilder<>))]
public readonly struct ValueFuture<T>
{
    public static readonly ValueFuture<T> COMPLETED = new ValueFuture<T>(default, null);
    public static readonly ValueFuture<T> CANCELLED = new ValueFuture<T>(default, StacklessCancellationException.INST1);

    private readonly object? _future;
    private readonly int _reentryId;

    private readonly T? _result;
    private readonly Exception? _ex;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="ex">如果为null，则表示成功</param>
    public ValueFuture(T? result, Exception? ex) {
        _future = null;
        _reentryId = 0;
        _result = result;
        _ex = ex;
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

    public ValueFutureAwaiter<T> GetAwaiter() => new ValueFutureAwaiter<T>(this);

    public ValueFutureAwaitable<T> GetAwaitable(IExecutor executor, int options = 0) => new ValueFutureAwaitable<T>(this, executor, options);

    public static ValueFuture<T> FromResult(T result) {
        return new ValueFuture<T>(result, null);
    }

    public static ValueFuture<T> FromException(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture<T>(default, ex);
    }

    public static ValueFuture<T> FromCancelled(int cancelCode) {
        Exception ex = StacklessCancellationException.InstOf(cancelCode);
        return new ValueFuture<T>(default, ex);
    }

    /// <summary>
    /// 获取任务当前的状态
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
    /// 转换为普通的Future
    /// 该方法应当避免调用多次，且不可以在await以后调用
    /// </summary>
    public IFuture<T> AsFuture() {
        if (_future == null) {
            if (_ex != null) {
                return Promise<T>.FromException(_ex);
            }
            return Promise<T>.FromResult(_result);
        }
        if (_future is IFuture<T> future) {
            return future;
        }
        IValuePromise<T> valuePromise = (IValuePromise<T>)_future;
        TaskStatus status = valuePromise.GetStatus(_reentryId);
        switch (status) {
            case TaskStatus.Success: {
                return Promise<T>.FromResult(valuePromise.GetResult(_reentryId));
            }
            case TaskStatus.Cancelled:
            case TaskStatus.Failed: {
                Exception ex = valuePromise.GetException(_reentryId);
                return Promise<T>.FromException(ex);
            }
            default: {
                Promise<T> promise = new Promise<T>();
                valuePromise.SetPromiseWhenCompleted(_reentryId, promise);
                return promise;
            }
        }
    }

    public ValueFuture<T> Preserve() => new ValueFuture<T>(AsFuture());

    public ValueFuture ToVoid() {
        if (_future == null) {
            if (_ex != null) {
                return new ValueFuture(_ex);
            }
            return ValueFuture.COMPLETED;
        }
        if (_future is IFuture future) {
            return new ValueFuture(future);
        }
        return new ValueFuture((IValuePromise)_future, _reentryId);
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
            if (_ex != null) {
                throw _ex; // TODO 会丢失堆栈
            }
            return _result;
        }
        if (_future is IValuePromise<T> valuePromise) {
            return valuePromise.GetResult(_reentryId);
        } else {
            IFuture<T> future = (IFuture<T>)_future;
            return future.Get();
        }
    }

    private static readonly Action<IFuture<T>, object> futureCallback = (_, state) => ((Action)state).Invoke();

    internal void OnCompleted(Action action, IExecutor? executor, int options) {
        if (_future == null) {
            throw new IllegalStateException();
        }

        if (action == null) throw new ArgumentNullException(nameof(action));
        if (_future is IValuePromise<T> valuePromise) {
            valuePromise.OnCompleted(_reentryId, ValueFuture.driverCallBack, action, executor, options);
        } else {
            IFuture<T> future = (IFuture<T>)_future;
            if (executor != null) {
                future.OnCompletedAsync(executor, futureCallback, action, options);
            } else {
                future.OnCompleted(futureCallback, action, options);
            }
        }
    }

    #endregion
}
}