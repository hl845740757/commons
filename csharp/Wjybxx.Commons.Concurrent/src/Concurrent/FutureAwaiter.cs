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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 原始类型Future的等待器
/// awaiter默认不返回结果。
/// </summary>
public readonly struct FutureAwaiter : ICriticalNotifyCompletion
{
    private static readonly Action<IFuture, object> INVOKER = (_, state) => ((Action)state).Invoke();

    private readonly IFuture _future;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOption.STAGE_TRY_INLINE"/></param>
    public FutureAwaiter(IFuture future, IExecutor? executor = null, int options = 0) {
        _future = future;
        _executor = executor;
        _options = options;
    }

    // 1.IsCompleted
    // IsCompleted只在Start后调用一次，EventLoop可以通过接口查询是否已在线程中
    public bool IsCompleted {
        get {
            if (!_future.IsCompleted) return false;
            if (_executor == null) return true;
            return TaskOption.IsEnabled(_options, TaskOption.STAGE_TRY_INLINE)
                   && Executors.InEventLoop(_executor);
        }
    }

    // 2. GetResult
    // 状态机只在IsCompleted为true时，和OnCompleted后调用GetResult，因此在目标线程中 -- 不可手动调用
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult() {
        _future.ThrowIfFailedOrCancelled();
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    public void OnCompleted(Action continuation) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (_executor == null) {
            _future.OnCompleted(INVOKER, continuation, _options);
        } else {
            _future.OnCompletedAsync(_executor, INVOKER, continuation, _options);
        }
    }

    public void UnsafeOnCompleted(Action continuation) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (_executor == null) {
            _future.OnCompleted(INVOKER, continuation, _options);
        } else {
            _future.OnCompletedAsync(_executor, INVOKER, continuation, _options);
        }
    }
}

/// <summary>
/// Future的等待器
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct FutureAwaiter<T> : ICriticalNotifyCompletion
{
    private static readonly Action<IFuture<T>, object> INVOKER = (_, state) => ((Action)state).Invoke();

    private readonly IFuture<T> _future;
    private readonly IExecutor? _executor;
    private readonly int _options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future">future</param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOption.STAGE_TRY_INLINE"/></param>
    public FutureAwaiter(IFuture<T> future, IExecutor? executor = null, int options = 0) {
        _future = future;
        _executor = executor;
        _options = options;
    }

    // 1.IsCompleted
    // IsCompleted只在Start后调用一次，EventLoop可以通过接口查询是否已在线程中
    public bool IsCompleted {
        get {
            if (!_future.IsCompleted) return false;
            if (_executor == null) return true;
            return TaskOption.IsEnabled(_options, TaskOption.STAGE_TRY_INLINE)
                   && Executors.InEventLoop(_executor);
        }
    }

    // 2. GetResult
    // 状态机只在IsCompleted为true时，和OnCompleted后调用GetResult，因此在目标线程中 -- 不可手动调用
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetResult() {
        return _future.Get();
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    public void OnCompleted(Action continuation) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (_executor == null) {
            _future.OnCompleted(INVOKER, continuation, _options);
        } else {
            _future.OnCompletedAsync(_executor, INVOKER, continuation, _options);
        }
    }

    public void UnsafeOnCompleted(Action continuation) {
        if (continuation == null) throw new ArgumentNullException(nameof(continuation));
        if (_executor == null) {
            _future.OnCompleted(INVOKER, continuation, _options);
        } else {
            _future.OnCompletedAsync(_executor, INVOKER, continuation, _options);
        }
    }
}
}