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

namespace Wjybxx.Commons;

/// <summary>
/// Future的等待器
/// 实现<see cref="IFuture{T}"/>是因为C#原生库不支持await传参，因此我们需要预构建Awaiter。
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct FutureAwaiter<T> : IFuture<T>, ICriticalNotifyCompletion
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

    public bool IsCompleted => future.IsDone;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetResult() {
        return future.Get();
    }

    /// <summary>
    /// 添加一个完成时的回调。
    /// 通常而言，该接口由StateMachine调用，因此接口参数类型限定为<see cref="Action"/>
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

    public FutureAwaiter<T> GetAwaiter() {
        return this;
    }

    public FutureAwaiter<T> GetAwaiter(IExecutor executor, int options = 0) {
        if (ReferenceEquals(this.executor, executor) && this.options == options) {
            return this;
        }
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return new FutureAwaiter<T>(future, executor);
    }


    #region 转发

    public IContext Context => future.Context;
    public IExecutor Executor => future.Executor;

    public IFuture<T> AsReadonly() {
        return future.AsReadonly();
    }

    public FutureState State => future.State;

    public bool IsPending => future.IsPending;

    public bool IsComputing => future.IsComputing;

    public bool IsDone => future.IsDone;

    public bool IsCancelled => future.IsCancelled;

    public bool IsSucceeded => future.IsSucceeded;

    public bool IsFailed => future.IsFailed;

    public bool IsFailedOrCancelled => future.IsFailedOrCancelled;

    public bool GetNow(out T result) {
        return future.GetNow(out result);
    }

    public T ResultNow() {
        return future.ResultNow();
    }

    public Exception ExceptionNow(bool throwIfCancelled = true) {
        return future.ExceptionNow(throwIfCancelled);
    }

    #endregion
}