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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 
/// </summary>
public readonly struct ValueFutureAwaiter2 : ICriticalNotifyCompletion
{
    private readonly ValueFuture _future;
    private readonly IExecutor? _executor;
    private readonly int _options;
    private readonly bool _requireResult;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="requireResult">是否需要获取最终结果</param>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    public ValueFutureAwaiter2(ValueFuture future, bool requireResult, IExecutor? executor = null, int options = 0) {
        _future = future;
        _executor = executor;
        _options = options;
        _requireResult = requireResult;
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
        return _future.GetResult((SuppressedTypes)_options, _requireResult);
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, TaskOptions.STAGE_UNCANCELLABLE_CTX);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, TaskOptions.STAGE_UNCANCELLABLE_CTX);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, int extraOptions = 0) {
        _future.OnCompleted(continuation, state, _executor, _options | extraOptions);
    }
}

public readonly struct ValueFutureAwaiter2<T> : ICriticalNotifyCompletion
{
    private readonly ValueFuture<T> _future;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future"></param>
    /// <param name="executor"></param>
    /// <param name="options"></param>
    public ValueFutureAwaiter2(ValueFuture<T> future, IExecutor? executor = null, int options = 0) {
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
        return _future.GetResult((SuppressedTypes)_options);
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, TaskOptions.STAGE_UNCANCELLABLE_CTX);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, TaskOptions.STAGE_UNCANCELLABLE_CTX);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state, int extraOptions = 0) {
        _future.OnCompleted(continuation, state, _executor, _options | extraOptions);
    }
}
}