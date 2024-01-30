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

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// Future的等待器
/// 实现<see cref="IFuture{T}"/>是因为C#原生库不支持await传参，因此我们需要预构建Awaiter。
/// </summary>
/// <typeparam name="T"></typeparam>
[AsyncMethodBuilder(typeof(FutureAwaiterMethodBuilder<>))]
public readonly struct FutureAwaiter<T> : ICriticalNotifyCompletion
{
    private static readonly Action<IFuture<T>, object> Invoker = (_, state) => ((Action)state).Invoke();

    private readonly IFuture<T> future;
    private readonly IExecutor? executor;
    private readonly int options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future">需要等待的future</param>
    /// <param name="executor">回调线程</param>
    /// <param name="options">调度选项</param>
    /// <exception cref="ArgumentNullException"></exception>
    public FutureAwaiter(IFuture<T> future, IExecutor? executor = null, int options = 0) {
        this.future = future ?? throw new ArgumentNullException(nameof(future));
        this.executor = executor;
        this.options = options;
    }

    // 1.IsCompleted
    public bool IsCompleted => future.IsDone;

    // 2. GetResult
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetResult() {
        return future.Get();
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    public void OnCompleted(Action continuation) {
        if (executor == null) {
            future.OnCompleted(Invoker, continuation, options);
        } else {
            future.OnCompletedAsync(executor, Invoker, continuation, options);
        }
    }

    public void UnsafeOnCompleted(Action continuation) {
        if (executor == null) {
            future.OnCompleted(Invoker, continuation, options);
        } else {
            future.OnCompletedAsync(executor, Invoker, continuation, options);
        }
    }

    // 用于构建绑定Executor的Awaiter
    public FutureAwaiter<T> GetAwaiter() {
        return this;
    }
}