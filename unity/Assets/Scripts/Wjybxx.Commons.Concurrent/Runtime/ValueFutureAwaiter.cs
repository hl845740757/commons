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
using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 
/// </summary>
public readonly struct ValueFutureAwaiter : ICriticalNotifyCompletion
{
    private readonly ValueFuture _future;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <summary>
    /// 
    /// </summary>
    public ValueFutureAwaiter(ValueFuture future, IExecutor? executor = null, int options = 0) {
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
    public void GetResult() {
        _future.GetResult();
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, _executor, _options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, _executor, _options);
    }

    /// <summary>
    /// 添加一个Future完成时的回调
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state) {
        OnCompleted(continuation, state, _executor, _options);
    }

    private void OnCompleted(Action<object> continuation, object state,
                             IExecutor? executor, int options) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        _future.OnCompleted(continuation, state, executor, options);
    }
}

public readonly struct ValueFutureAwaiter<T> : ICriticalNotifyCompletion
{
    private readonly ValueFuture<T> _future;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <summary>
    /// 
    /// </summary>
    public ValueFutureAwaiter(ValueFuture<T> future, IExecutor? executor, int options = 0) {
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
    public T GetResult() {
        return _future.GetResult();
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, _executor, _options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void UnsafeOnCompleted(Action continuation) {
        OnCompleted(FutureAwaiter.invoker, continuation, _executor, _options);
    }

    /// <summary>
    /// 添加一个Future完成时的回调
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnCompleted(Action<object> continuation, object state) {
        OnCompleted(continuation, state, _executor, _options);
    }

    private void OnCompleted(Action<object> continuation, object state,
                             IExecutor? executor, int options) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        _future.OnCompleted(continuation, state, executor, options);
    }
}
}