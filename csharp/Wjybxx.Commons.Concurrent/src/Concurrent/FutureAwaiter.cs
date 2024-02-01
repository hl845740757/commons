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
/// 原始类型Future的等待器
/// awaiter默认不返回结果。
/// </summary>
public readonly struct FutureAwaiter : ICriticalNotifyCompletion
{
    private static readonly Action<IFuture, object> Invoker = (_, state) => ((Action)state).Invoke();

    private readonly IFuture future;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future">需要等待的future</param>
    /// <exception cref="ArgumentNullException"></exception>
    public FutureAwaiter(IFuture future) {
        this.future = future ?? throw new ArgumentNullException(nameof(future));
    }

    // 1.IsCompleted
    public bool IsCompleted => future.IsDone;

    // 2. GetResult
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void GetResult() {
        future.Await();
        if (future.IsFailedOrCancelled) {
            throw future.ExceptionNow(false);
        }
    }

    // 3. OnCompleted
    /// <summary>
    /// 添加一个Future完成时的回调。
    /// ps：通常而言，该接口由StateMachine调用，因此接口参数为<see cref="Action"/>。
    /// </summary>
    /// <param name="continuation">回调任务</param>
    public void OnCompleted(Action continuation) {
        future.OnCompleted(Invoker, continuation);
    }

    public void UnsafeOnCompleted(Action continuation) {
        future.OnCompleted(Invoker, continuation);
    }
}

/// <summary>
/// Future的等待器
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct FutureAwaiter<T> : ICriticalNotifyCompletion
{
    private static readonly Action<IFuture<T>, object> Invoker = (_, state) => ((Action)state).Invoke();

    private readonly IFuture<T> future;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="future">需要等待的future</param>
    /// <exception cref="ArgumentNullException"></exception>
    public FutureAwaiter(IFuture<T> future) {
        this.future = future ?? throw new ArgumentNullException(nameof(future));
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
        future.OnCompleted(Invoker, continuation);
    }

    public void UnsafeOnCompleted(Action continuation) {
        future.OnCompleted(Invoker, continuation);
    }
}