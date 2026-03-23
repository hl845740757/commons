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
/// 该实现主要用于屏蔽Promise中的写接口。
///
/// ps:
/// 1.Csharp的Future没有取消接口，因此无需显式的Readonly实现。
/// 2.OnCompleted系列接口存在封装泄漏，可能导致错误。
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

    // onCompleted不能直接转发，否则会导致封装泄漏
    public void OnCompleted(Action<IFuture<T>> continuation, int options = 0) {
        future.OnCompleted((_, ctx) => {
            Action<IFuture<T>> act = (Action<IFuture<T>>)ctx;
            act.Invoke(this);
        }, continuation, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>> continuation, int options = 0) {
        future.OnCompletedAsync(executor, (_, ctx) => {
            Action<IFuture<T>> act = (Action<IFuture<T>>)ctx;
            act.Invoke(this);
        }, continuation, options);
    }

    public void OnCompleted(Action<IFuture<T>, object> continuation, object state, int options = 0) {
        future.OnCompleted(WrapAction(continuation), state, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>, object> continuation, object state, int options = 0) {
        future.OnCompletedAsync(executor, WrapAction(continuation), state, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Action<IFuture<T>, object> WrapAction(Action<IFuture<T>, object> action) {
        return (_, ctx) => action(this, ctx);
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

    public object ExceptionOrDispatchInfoNow() {
        return future.ExceptionOrDispatchInfoNow();
    }

    public T Get() {
        return future.Get();
    }

    public T Get(TimeSpan timeout) {
        return future.Get(timeout);
    }

    public T Join() {
        return future.Join();
    }

    public void OnCompleted(Action<object?> continuation, object? state, int options = 0) {
        future.OnCompleted(continuation, state, options);
    }

    public void OnCompletedAsync(IExecutor executor, Action<object?> continuation, object? state, int options = 0) {
        future.OnCompletedAsync(executor, continuation, state, options);
    }

    #endregion

    // stage相关接口返回底层Promise安全，暂不做过多约束
    #region stage

    public IFuture<U> ComposeCall<U>(Func<object, IFuture<U>> fn, object? ctx, int options = 0) {
        return future.ComposeCall(fn, ctx, options);
    }

    public IFuture<U> ComposeCallAsync<U>(IExecutor executor, Func<object, IFuture<U>> fn, object? ctx, int options = 0) {
        return future.ComposeCallAsync(executor, fn, ctx, options);
    }

    public IFuture<U> ThenCall<U>(Func<object, U> fn, object? ctx, int options = 0) {
        return future.ThenCall(fn, ctx, options);
    }

    public IFuture<U> ThenCallAsync<U>(IExecutor executor, Func<object, U> fn, object? ctx, int options = 0) {
        return future.ThenCallAsync(executor, fn, ctx, options);
    }

    public IFuture ThenRun(Action<object> fn, object? ctx, int options = 0) {
        return future.ThenRun(fn, ctx, options);
    }

    public IFuture ThenRunAsync(IExecutor executor, Action<object> fn, object? ctx, int options = 0) {
        return future.ThenRunAsync(executor, fn, ctx, options);
    }

    public IFuture<U> ComposeApply<U>(Func<object, T, IFuture<U>> fn, object? ctx, int options = 0) {
        return future.ComposeApply(fn, ctx, options);
    }

    public IFuture<U> ComposeApplyAsync<U>(IExecutor executor, Func<object, T, IFuture<U>> fn, object? ctx, int options = 0) {
        return future.ComposeApplyAsync(executor, fn, ctx, options);
    }

    public IFuture<T> ComposeCatching<X>(Func<object, X, IFuture<T>> fallback, object? ctx, int options = 0) where X : Exception {
        return future.ComposeCatching(fallback, ctx, options);
    }

    public IFuture<T> ComposeCatchingAsync<X>(IExecutor executor, Func<object, X, IFuture<T>> fallback, object? ctx, int options = 0) where X : Exception {
        return future.ComposeCatchingAsync(executor, fallback, ctx, options);
    }

    public IFuture<U> ComposeHandle<U>(Func<object, T, Exception, IFuture<U>> fn, object? ctx, int options = 0) {
        return future.ComposeHandle(fn, ctx, options);
    }

    public IFuture<U> ComposeHandleAsync<U>(IExecutor executor, Func<object, T, Exception, IFuture<U>> fn, object? ctx, int options = 0) {
        return future.ComposeHandleAsync(executor, fn, ctx, options);
    }

    public IFuture<U> ThenApply<U>(Func<object, T, U> fn, object? ctx, int options = 0) {
        return future.ThenApply(fn, ctx, options);
    }

    public IFuture<U> ThenApplyAsync<U>(IExecutor executor, Func<object, T, U> fn, object? ctx, int options = 0) {
        return future.ThenApplyAsync(executor, fn, ctx, options);
    }

    public IFuture ThenAccept(Action<object, T> fn, object? ctx, int options = 0) {
        return future.ThenAccept(fn, ctx, options);
    }

    public IFuture ThenAcceptAsync(IExecutor executor, Action<object, T> fn, object? ctx, int options = 0) {
        return future.ThenAcceptAsync(executor, fn, ctx, options);
    }

    public IFuture<T> Catching<X>(Func<object, X, T> fallback, object? ctx, int options = 0) where X : Exception {
        return future.Catching(fallback, ctx, options);
    }

    public IFuture<T> CatchingAsync<X>(IExecutor executor, Func<object, X, T> fallback, object? ctx, int options = 0) where X : Exception {
        return future.CatchingAsync(executor, fallback, ctx, options);
    }

    public IFuture<U> Handle<U>(Func<object, T, Exception, U> fn, object? ctx, int options = 0) {
        return future.Handle(fn, ctx, options);
    }

    public IFuture<U> HandleAsync<U>(IExecutor executor, Func<object, T, Exception, U> fn, object? ctx, int options = 0) {
        return future.HandleAsync(executor, fn, ctx, options);
    }

    public IFuture<T> WhenComplete(Action<object, T, Exception> fn, object? ctx, int options = 0) {
        return future.WhenComplete(fn, ctx, options);
    }

    public IFuture<T> WhenComplete(IExecutor executor, Action<object, T, Exception> fn, object? ctx, int options = 0) {
        return future.WhenComplete(executor, fn, ctx, options);
    }

    #endregion
}
}