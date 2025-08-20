#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
/// 该结构用于装箱
/// </summary>
public readonly struct TaskResult
{
    public static readonly TaskResult COMPLETED = new TaskResult();
    public static readonly TaskResult CANCELLED = new TaskResult(null, StacklessCancellationException.Default);

    /// <summary>
    /// 正常结果
    /// </summary>
    private readonly object? _result;
    /// <summary>
    /// 异常信息
    /// <see cref="OperationCanceledException"/> or <see cref="ExceptionDispatchInfo"/>
    /// </summary>
    private readonly object? _ex;

    /** 通过工厂方法创建 */
    private TaskResult(object? result, object? ex) {
        _result = result;
        _ex = ex == null ? null : AbstractPromise.WrapException(ex);
    }

    #region factory

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskResult FromResult(object? r = null) {
        return new TaskResult(r, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskResult FromException(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new TaskResult(null, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskResult FromException(ExceptionDispatchInfo ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new TaskResult(null, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskResult FromCancelled(int cancelCode = 1) {
        Exception ex = StacklessCancellationException.InstOf(cancelCode);
        return new TaskResult(null, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TaskResult InternalFromException(object ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new TaskResult(null, ex);
    }

    #endregion

    /// <summary>
    /// 任务是否执行成功
    /// </summary>
    public bool IsSucceeded => _ex == null;

    /// <summary>
    /// 任务是否被取消
    /// </summary>
    public bool IsCancelled => _ex is OperationCanceledException;

    /// <summary>
    /// 任务是否执行失败
    /// </summary>
    public bool IsFailed => _ex != null && _ex is not OperationCanceledException;

#nullable disable
    /// <summary>
    /// 获取任务的结果，只有成功的情况下可调用
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public object Result {
        get {
            if (_ex != null) {
                throw new InvalidOperationException();
            }
            return _result;
        }
    }
#nullable restore

    /// <summary>
    /// 获取任务关联的异常，成功的情况下返回null
    /// (应当避免多次调用)
    /// </summary>
    public Exception? Exception {
        get {
            if (_ex is OperationCanceledException canceledException) {
                return canceledException;
            }
            if (_ex is ExceptionDispatchInfo dispatchInfo) {
                return ExceptionUtil.RestoreStackTrace(dispatchInfo);
            }
            return null;
        }
    }

    /// <summary>
    /// 获取取消码
    ///
    /// 注：如果任务是被取消的，则返回对应的取消码，如果不包含取消码信息，则返回默认的取消码。
    /// </summary>
    public int CancelCode {
        get {
            if (_ex is BetterCancellationException ex2) {
                return ex2.Code;
            }
            if (_ex is OperationCanceledException) {
                return CancelCodes.REASON_DEFAULT;
            }
            return 0;
        }
    }

    /// <summary>
    /// 获取任务的异常信息
    /// </summary>
    public ExceptionDispatchInfo? ExceptionDispatchInfo {
        get {
            if (_ex is ExceptionDispatchInfo dispatchInfo) {
                return dispatchInfo;
            }
            return null;
        }
    }

    /// <summary>
    /// 抛出关联的异常
    /// </summary>
    public void Throw() {
        if (_ex is OperationCanceledException canceledException) {
            throw canceledException;
        }
        if (_ex is ExceptionDispatchInfo dispatchInfo) {
            dispatchInfo.Throw();
        }
    }

    /// <summary>
    /// 拆箱
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public TaskResult<T> Unbox<T>() {
        return _ex != null
            ? TaskResult<T>.InternalFromException(_ex)
            : TaskResult<T>.FromResult(_result == null ? default : (T)_result);
    }

    /// <summary>
    /// 将失败的Result转换为其它类型
    /// (其实也可以Box再Unbox)
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <returns></returns>
    public TaskResult<U> CastFailed<U>() {
        if (_ex == null) throw new InvalidOperationException();
        return TaskResult<U>.InternalFromException(_ex);
    }

    internal void Deconstruct(out object? result, out object? ex) {
        result = _result;
        ex = _ex;
    }
}

/// <summary>
/// 任务的执行结果
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct TaskResult<T>
{
    public static readonly TaskResult<T> COMPLETED = new TaskResult<T>();
    public static readonly TaskResult<T> CANCELLED = new TaskResult<T>(default, StacklessCancellationException.Default);

    /// <summary>
    /// 正常结果
    /// </summary>
    private readonly T _result;
    /// <summary>
    /// 异常信息
    /// <see cref="OperationCanceledException"/> or <see cref="ExceptionDispatchInfo"/>
    /// </summary>
    private readonly object? _ex;

    /** 通过工厂方法创建 */
    private TaskResult(T result, object? ex) {
        _result = result;
        _ex = ex == null ? null : AbstractPromise.WrapException(ex);
    }

    #region factory

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskResult<T> FromResult(T r) {
        return new TaskResult<T>(r, null);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskResult<T> FromException(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new TaskResult<T>(default, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskResult<T> FromException(ExceptionDispatchInfo ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new TaskResult<T>(default, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskResult<T> FromCancelled(int cancelCode = 1) {
        Exception ex = StacklessCancellationException.InstOf(cancelCode);
        return new TaskResult<T>(default, ex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static TaskResult<T> InternalFromException(object ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new TaskResult<T>(default, ex);
    }

    #endregion

    /// <summary>
    /// 任务是否执行成功
    /// </summary>
    public bool IsSucceeded => _ex == null;

    /// <summary>
    /// 任务是否被取消
    /// </summary>
    public bool IsCancelled => _ex is OperationCanceledException;

    /// <summary>
    /// 任务是否执行失败
    /// </summary>
    public bool IsFailed => _ex != null && _ex is not OperationCanceledException;

#nullable disable
    /// <summary>
    /// 获取任务的结果，只有成功的情况下可调用
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public T Result {
        get {
            if (_ex != null) {
                throw new InvalidOperationException();
            }
            return _result;
        }
    }
#nullable restore

    /// <summary>
    /// 获取任务关联的异常，成功的情况下返回null
    /// (应当避免多次调用)
    /// </summary>
    public Exception? Exception {
        get {
            if (_ex is OperationCanceledException canceledException) {
                return canceledException;
            }
            if (_ex is ExceptionDispatchInfo dispatchInfo) {
                return ExceptionUtil.RestoreStackTrace(dispatchInfo);
            }
            return null;
        }
    }

    /// <summary>
    /// 获取取消码
    ///
    /// 注：如果任务是被取消的，则返回对应的取消码，如果不包含取消码信息，则返回默认的取消码。
    /// </summary>
    public int CancelCode {
        get {
            if (_ex is BetterCancellationException ex2) {
                return ex2.Code;
            }
            if (_ex is OperationCanceledException) {
                return CancelCodes.REASON_DEFAULT;
            }
            return 0;
        }
    }

    /// <summary>
    /// 获取任务的异常信息
    /// </summary>
    public ExceptionDispatchInfo? ExceptionDispatchInfo {
        get {
            if (_ex is ExceptionDispatchInfo dispatchInfo) {
                return dispatchInfo;
            }
            return null;
        }
    }

    /// <summary>
    /// 抛出关联的异常
    /// </summary>
    public void Throw() {
        if (_ex is OperationCanceledException canceledException) {
            throw canceledException;
        }
        if (_ex is ExceptionDispatchInfo dispatchInfo) {
            dispatchInfo.Throw();
        }
    }

    /// <summary>
    /// 装箱结果
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TaskResult Box() {
        return _ex != null ? TaskResult.InternalFromException(_ex) : TaskResult.FromResult(_result);
    }

    /// <summary>
    /// 将失败的Result转换为其它类型
    /// (其实也可以Box再Unbox)
    /// </summary>
    /// <typeparam name="U"></typeparam>
    /// <returns></returns>
    public TaskResult<U> CastFailed<U>() {
        if (_ex == null) throw new InvalidOperationException();
        return TaskResult<U>.InternalFromException(_ex);
    }

    internal void Deconstruct(out T result, out object? ex) {
        result = _result;
        ex = _ex;
    }
}
}