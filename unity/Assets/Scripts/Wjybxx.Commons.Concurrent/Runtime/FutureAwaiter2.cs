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
using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 原始类型Future的等待器，返回<see cref="TaskResult"/>形式的结果
/// (awaiter不返回结果，仅用于查询任务的完成状态)
/// </summary>
public readonly struct FutureAwaiter2 : ICriticalNotifyCompletion
{
    private readonly IFuture _future;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <summary>
    ///
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    public FutureAwaiter2(IFuture future, IExecutor? executor = null, int options = 0) {
        _future = future;
        _executor = executor;
        _options = options;
    }

    // 1.IsCompleted
    // IsCompleted只在Start后调用一次，EventLoop可以通过接口查询是否已在线程中
    public bool IsCompleted {
        get {
            if (!_future.IsCompleted) return false;
            return ExecutorUtil.IsInlinable(_executor, _options);
        }
    }

    // 2. GetResult
    // 状态机只在IsCompleted为true时，和OnCompleted后调用GetResult，因此在目标线程中
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TaskResult GetResult() {
        if (ExecutorUtil.IsSuppressible(_options, _future.Status)) {
            return TaskResult.InternalFromException(_future.ExceptionOrDispatchInfoNow());
        }
        _future.ThrowIfFailedOrCancelled();
        return TaskResult.COMPLETED;
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, _options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, _options);
    }

    /// <summary>
    /// 添加一个Future完成时的回调
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state) {
        OnCompleted(continuation, state, _options);
    }

    private void OnCompleted(Action<object> continuation, object state, int options) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (_executor == null) {
            _future.OnCompleted(continuation, state, options);
        } else {
            _future.OnCompletedAsync(_executor, continuation, state, options);
        }
    }
}

/// <summary>
/// Future的等待器，返回<see cref="TaskResult{T}"/>形式的结果
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct FutureAwaiter2<T> : ICriticalNotifyCompletion
{
    private readonly IFuture<T> _future;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <summary>
    ///
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    public FutureAwaiter2(IFuture<T> future, IExecutor? executor = null, int options = 0) {
        _future = future;
        _executor = executor;
        _options = options;
    }

    // 1.IsCompleted
    // IsCompleted只在Start后调用一次，EventLoop可以通过接口查询是否已在线程中
    public bool IsCompleted {
        get {
            if (!_future.IsCompleted) return false;
            return ExecutorUtil.IsInlinable(_executor, _options);
        }
    }

    // 2. GetResult
    // 状态机只在IsCompleted为true时，和OnCompleted后调用GetResult，因此在目标线程中
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public TaskResult<T> GetResult() {
        if (ExecutorUtil.IsSuppressible(_options, _future.Status)) {
            return TaskResult<T>.InternalFromException(_future.ExceptionOrDispatchInfoNow());
        }
        return TaskResult<T>.FromResult(_future.Get());
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, _options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, _options);
    }

    /// <summary>
    /// 添加一个Future完成时的回调
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state) {
        OnCompleted(continuation, state, _options);
    }

    private void OnCompleted(Action<object> continuation, object state, int options) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (_executor == null) {
            _future.OnCompleted(continuation, state, options);
        } else {
            _future.OnCompletedAsync(_executor, continuation, state, options);
        }
    }
}
}
