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

public class ValueFutureAwaiter<T> : INotifyCompletion
{
    private readonly ValueFuture<T> future;
    private readonly IExecutor? executor;
    private readonly int options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future"></param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项</param>
    public ValueFutureAwaiter(in ValueFuture<T> future, IExecutor? executor = null, int options = 0) {
        this.future = future;
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
    public void OnCompleted(Action continuation) {
        if (executor == null) {
            future.OnCompleted(continuation, options);
        } else {
            future.OnCompletedAsync(executor, continuation, options);
        }
    }

    public void UnsafeOnCompleted(Action continuation) {
        if (executor == null) {
            future.OnCompleted(continuation, options);
        } else {
            future.OnCompletedAsync(executor, continuation, options);
        }
    }
}

public class ValueFutureAwaiter : INotifyCompletion
{
    private readonly ValueFuture future;
    private readonly IExecutor? executor;
    private readonly int options;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future"></param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项</param>
    public ValueFutureAwaiter(in ValueFuture future, IExecutor? executor = null, int options = 0) {
        this.future = future;
        this.executor = executor;
        this.options = options;
    }

    // 1.IsCompleted
    public bool IsCompleted => future.IsDone;

    // 2. GetResult
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult() {
        future.Get();
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    public void OnCompleted(Action continuation) {
        if (executor == null) {
            future.OnCompleted(continuation, options);
        } else {
            future.OnCompletedAsync(executor, continuation, options);
        }
    }

    public void UnsafeOnCompleted(Action continuation) {
        if (executor == null) {
            future.OnCompleted(continuation, options);
        } else {
            future.OnCompletedAsync(executor, continuation, options);
        }
    }
}