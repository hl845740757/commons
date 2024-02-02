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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// ValueFuture有以下作用：
/// 1. 优化在已完成任务上的等待。
/// 2. 绑定Awaiter的回调线程。
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
[AsyncMethodBuilder(typeof(AsyncFutureMethodBuilder<>))]
public readonly struct ValueFuture<T>
{
#nullable disable
    private readonly IFuture<T> _future;
    private readonly IExecutor _executor;
    private readonly int _options;

    private readonly T _result;
    private readonly Exception _ex;
#nullable enable

    /// <summary>
    /// 创建已完成的Future
    /// </summary>
    /// <param name="result"></param>
    /// <param name="ex"></param>
    private ValueFuture(T? result, Exception? ex) {
        this._future = null;
        this._executor = null;
        this._options = 0;

        this._result = result;
        this._ex = ex;
    }

    /// <summary>
    /// 用于封装为完成的Future
    /// </summary>
    /// <param name="future"></param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项</param>
    public ValueFuture(IFuture<T> future, IExecutor? executor = null, int options = 0) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _executor = executor;
        _options = options;

        _result = default;
        _ex = null;
    }

    /// <summary>
    /// 创建一个成功完成的Future
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static ValueFuture<T> FromResult(T result) {
        return new ValueFuture<T>(result, null);
    }

    /// <summary>
    /// 创建一个已经失败的Future
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ValueFuture<T> FromException(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture<T>(default, ex);
    }

    /// <summary>
    /// 创建一个被取消的Future
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ValueFuture<T> FromCancelled(OperationCanceledException? ex = null) {
        if (ex == null) {
            ex = new BetterCancellationException(1);
        }
        return new ValueFuture<T>(default, ex);
    }

    #region awaiter

    /// <summary>
    /// 获取用于等待的Awaiter
    /// </summary>
    /// <returns></returns>
    public ValueFutureAwaiter<T> GetAwaiter() {
        return new ValueFutureAwaiter<T>(in this, _executor, _options);
    }

    /// <summary>
    /// 获取用在给定线程等待的Awaiter
    /// </summary>
    /// <param name="executor">等待线程</param>
    /// <param name="options">等待线程</param>
    /// <returns></returns>
    public ValueFuture<T> GetAwaiter(IExecutor executor, int options = 0) {
        return new ValueFuture<T>(AsFuture(), executor, options);
    }

    /// <summary>
    /// 转换为正常的Future
    /// </summary>
    public IFuture<T> AsFuture() {
        if (_future != null) {
            return _future;
        }
        if (_ex != null) {
            return Promise<T>.FailedPromise(_ex);
        }
        return Promise<T>.CompletedPromise(_result);
    }

    #endregion

    #region 状态查询

    public TaskStatus Status {
        get {
            if (_future != null) {
                return _future.Status;
            }
            if (_ex == null) {
                return TaskStatus.SUCCESS;
            }
            if (_ex is OperationCanceledException) {
                return TaskStatus.CANCELLED;
            }
            return TaskStatus.FAILED;
        }
    }

    /// <summary>
    /// 如果future关联的任务仍处于等待执行的状态，则返回true
    /// （换句话说，如果任务仍在排队，则返回true）
    /// </summary>
    public bool IsPending => Status == TaskStatus.PENDING;

    /** 如果future关联的任务正在执行中，则返回true */
    public bool IsComputing => Status == TaskStatus.COMPUTING;

    /** 如果future已进入完成状态(成功、失败、被取消)，则返回true */
    public bool IsDone => Status.IsDone();

    /** 如果future关联的任务在正常完成被取消，则返回true。 */
    public bool IsCancelled => Status == TaskStatus.CANCELLED;

    /** 如果future已进入完成状态，且是成功完成，则返回true。 */
    public bool IsSucceeded => Status == TaskStatus.SUCCESS;

    /** 如果future已进入完成状态，且是失败状态，则返回true */
    public bool IsFailed => Status == TaskStatus.FAILED;

    /**
     * 在JDK的约定中，取消和failed是分离的，我们仍保持这样的约定；
     * 但有些时候，我们需要将取消也视为失败的一种，因此需要快捷的方法。
     */
    public bool IsFailedOrCancelled => Status.IsFailedOrCancelled();

    #endregion

    /// <summary>
    /// 获取任务的执行结果
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public T Get() {
        if (_future != null) {
            return _future.Get();
        }
        if (_ex == null) {
            return _result;
        }
        throw _ex;
    }

    private static readonly Action<IFuture<T>, object> Invoker = (_, state) => ((Action)state).Invoke();

    /// <summary>
    /// 用于Awaiter注册回调
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="options">调度选项</param>
    public void OnCompleted(Action continuation, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (_future != null) {
            _future.OnCompleted(Invoker, continuation, options);
        } else {
            continuation();
        }
    }

    /// <summary>
    /// 用于Awaiter注册回调
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="options">调度选项</param>
    public void OnCompletedAsync(IExecutor executor, Action continuation, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));

        if (_future != null) {
            _future.OnCompletedAsync(executor, Invoker, continuation, options);
        } else {
            try {
                executor.Execute(continuation, options);
            }
            catch (RejectedExecutionException e) {
                FutureLogger.LogCause(e, "continuation was rejected by target executor");
            }
            // catch 在当前线程执行了给定的任务且抛出了异常，则直接抛出
        }
    }
}

[AsyncMethodBuilder(typeof(AsyncFutureMethodBuilder))]
public readonly struct ValueFuture
{
#nullable disable
    private readonly IFuture _future;
    private readonly IExecutor _executor;
    private readonly int _options;

    private readonly object _result;
    private readonly Exception _ex;
#nullable enable

    /// <summary>
    /// 创建已完成的Future
    /// </summary>
    /// <param name="result"></param>
    /// <param name="ex"></param>
    private ValueFuture(object? result, Exception? ex) {
        this._future = null;
        this._executor = null;
        this._options = 0;

        this._result = result;
        this._ex = ex;
    }

    /// <summary>
    /// 用于封装为完成的Future
    /// </summary>
    /// <param name="future"></param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项</param>
    public ValueFuture(IFuture future, IExecutor? executor = null, int options = 0) {
        _future = future ?? throw new ArgumentNullException(nameof(future));
        _executor = executor;
        _options = options;

        _result = default;
        _ex = null;
    }

    /// <summary>
    /// 创建一个成功完成的Future
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static ValueFuture FromResult(object? result = null) {
        return new ValueFuture(result, null);
    }

    /// <summary>
    /// 创建一个已经失败的Future
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ValueFuture FromException(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture(null, ex);
    }

    /// <summary>
    /// 创建一个被取消的Future
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ValueFuture FromCancelled(OperationCanceledException? ex = null) {
        if (ex == null) {
            ex = new OperationCanceledException();
        }
        return new ValueFuture(null, ex);
    }

    #region awaiter

    /// <summary>
    /// 获取用于等待的Awaiter
    /// </summary>
    /// <returns></returns>
    public ValueFutureAwaiter GetAwaiter() {
        return new ValueFutureAwaiter(in this, _executor, _options);
    }

    /// <summary>
    /// 获取用在给定线程等待的Awaiter
    /// </summary>
    /// <param name="executor">等待线程</param>
    /// <param name="options">等待线程</param>
    /// <returns></returns>
    public ValueFuture GetAwaiter(IExecutor executor, int options = 0) {
        return new ValueFuture(AsFuture(), executor, options);
    }

    /// <summary>
    /// 转换为正常的Future
    /// </summary>
    public IFuture AsFuture() {
        if (_future != null) {
            return _future;
        }
        if (_ex != null) {
            return Promise<object>.FailedPromise(_ex);
        }
        return Promise<object>.CompletedPromise(_result);
    }

    #endregion

    #region 状态查询

    public TaskStatus Status {
        get {
            if (_future != null) {
                return _future.Status;
            }
            if (_ex == null) {
                return TaskStatus.SUCCESS;
            }
            if (_ex is OperationCanceledException) {
                return TaskStatus.CANCELLED;
            }
            return TaskStatus.FAILED;
        }
    }

    /// <summary>
    /// 如果future关联的任务仍处于等待执行的状态，则返回true
    /// （换句话说，如果任务仍在排队，则返回true）
    /// </summary>
    public bool IsPending => Status == TaskStatus.PENDING;

    /** 如果future关联的任务正在执行中，则返回true */
    public bool IsComputing => Status == TaskStatus.COMPUTING;

    /** 如果future已进入完成状态(成功、失败、被取消)，则返回true */
    public bool IsDone => Status.IsDone();

    /** 如果future关联的任务在正常完成被取消，则返回true。 */
    public bool IsCancelled => Status == TaskStatus.CANCELLED;

    /** 如果future已进入完成状态，且是成功完成，则返回true。 */
    public bool IsSucceeded => Status == TaskStatus.SUCCESS;

    /** 如果future已进入完成状态，且是失败状态，则返回true */
    public bool IsFailed => Status == TaskStatus.FAILED;

    /**
     * 在JDK的约定中，取消和failed是分离的，我们仍保持这样的约定；
     * 但有些时候，我们需要将取消也视为失败的一种，因此需要快捷的方法。
     */
    public bool IsFailedOrCancelled => Status.IsFailedOrCancelled();

    #endregion

    /// <summary>
    /// 获取任务的执行结果
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public object Get() {
        if (_future != null) {
            return _future.Get();
        }
        if (_ex == null) {
            return _result;
        }
        throw _ex;
    }

    private static readonly Action<IFuture, object> Invoker = (_, state) => ((Action)state).Invoke();

    /// <summary>
    /// 用于Awaiter注册回调
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="options">调度选项</param>
    public void OnCompleted(Action continuation, int options = 0) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (_future != null) {
            _future.OnCompleted(Invoker, continuation, options);
        } else {
            continuation();
        }
    }

    /// <summary>
    /// 用于Awaiter注册回调
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="options">调度选项</param>
    public void OnCompletedAsync(IExecutor executor, Action continuation, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));

        if (_future != null) {
            _future.OnCompletedAsync(executor, Invoker, continuation, options);
        } else {
            try {
                executor.Execute(continuation, options);
            }
            catch (RejectedExecutionException e) {
                FutureLogger.LogCause(e, "continuation was rejected by target executor");
            }
            // catch 在当前线程执行了给定的任务且抛出了异常，则直接抛出
        }
    }
}