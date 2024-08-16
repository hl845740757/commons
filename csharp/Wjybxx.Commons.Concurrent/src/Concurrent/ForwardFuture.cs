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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 该实现主要用于屏蔽Promise中的写接口。
///
/// ps:Csharp的Future没有取消接口，因此无需显式的Readonly实现。
/// </summary>
/// <typeparam name="T"></typeparam>
public class ForwardFuture<T> : IFuture<T>
{
    protected readonly IFuture<T> future;

    public ForwardFuture(IFuture<T> future) {
        this.future = future ?? throw new ArgumentNullException(nameof(future));
    }

    #region 不可直接转发

    public IFuture<T> AsReadonly() {
        return this;
    }

    public IFuture<T> Await() {
        future.Await();
        return this;
    }

    public IFuture<T> AwaitUninterruptibly() {
        future.AwaitUninterruptibly();
        return this;
    }

    public FutureAwaiter<T> GetAwaiter() {
        return new FutureAwaiter<T>(this); // 不可转发，避免封装泄漏
    }

    #endregion

    #region 转发

    public IExecutor? Executor => future.Executor;

    public TaskStatus Status => future.Status;

    public bool IsPending => future.IsPending;

    public bool IsComputing => future.IsComputing;

    public bool IsCompleted => future.IsCompleted;

    public bool IsCancelled => future.IsCancelled;

    public bool IsSucceeded => future.IsSucceeded;

    public bool IsFailed => future.IsFailed;

    public bool IsFailedOrCancelled => future.IsFailedOrCancelled;

    public bool Await(TimeSpan timeout) {
        return future.Await(timeout);
    }

    public bool AwaitUninterruptibly(TimeSpan timeout) {
        return future.AwaitUninterruptibly(timeout);
    }

    public T ResultNow() {
        return future.ResultNow();
    }

    public Exception ExceptionNow(bool throwIfCancelled = true) {
        return future.ExceptionNow(throwIfCancelled);
    }

    public T Get() {
        return future.Get();
    }

    public T Join() {
        return future.Join();
    }

    public void OnCompleted(Action<IFuture<T>> continuation, int options = 0) {
        future.OnCompleted(continuation, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>> continuation, int options = 0) {
        future.OnCompletedAsync(executor, continuation, options);
    }

    public void OnCompleted(Action<IFuture<T>, object> continuation, object state, int options = 0) {
        future.OnCompleted(continuation, state, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>, object> continuation, object state, int options = 0) {
        future.OnCompletedAsync(executor, continuation, state, options);
    }

    public void OnCompleted(Action<object?> continuation, object? state, int options = 0) {
        future.OnCompleted(continuation, state, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<object?> continuation, object? state, int options = 0) {
        future.OnCompletedAsync(executor, continuation, state, options);
    }

    #endregion
}
}