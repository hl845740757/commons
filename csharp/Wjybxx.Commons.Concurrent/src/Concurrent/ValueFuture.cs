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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// ValueFuture有以下作用：
/// 1. 优化在已完成任务上的等待。
/// 2. 绑定Awaiter的回调线程。
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct ValueFuture<T> : IFuture<T>
{
#nullable disable
    private readonly IFuture<T> _promise;
    private readonly IExecutor _executor;
    private readonly int _options;

    private readonly T _result;
    private readonly Exception _ex;
#nullable enable

    /// <summary>
    /// 创建已完成的Promise
    /// </summary>
    /// <param name="result"></param>
    /// <param name="ex"></param>
    private ValueFuture(T result, Exception ex) {
        this._promise = null;
        this._executor = null;
        this._options = 0;

        this._result = result;
        this._ex = ex;
    }

    /// <summary>
    /// 用于封装为完成的Future
    /// </summary>
    /// <param name="promise"></param>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项</param>
    public ValueFuture(IFuture<T> promise, IExecutor? executor = null, int options = 0) {
        _promise = promise ?? throw new ArgumentNullException(nameof(promise));
        _executor = executor;
        _options = options;

        _result = default;
        _ex = null;
    }

    /// <summary>
    /// 创建一个成功完成的Promise
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static ValueFuture<T> FromResult(T result) {
        return new ValueFuture<T>(result, null);
    }

    /// <summary>
    /// 创建一个已经失败的Promise
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ValueFuture<T> FromException(Exception ex) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        return new ValueFuture<T>(default, ex);
    }

    /// <summary>
    /// 创建一个被取消的Promise
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static ValueFuture<T> FromCancelled(OperationCanceledException? ex = null) {
        if (ex == null) {
            ex = new BetterCancellationException(1);
        }
        return new ValueFuture<T>(default, ex);
    }

    #region awaiter

    /// <summary>
    /// 返回被代理的Future或装箱的Future
    /// </summary>
    public IFuture<T> Boxed() => _promise ?? this;

    /// <summary>
    /// 获取用于等待的Awaiter
    /// </summary>
    /// <returns></returns>
    public ValueFutureAwaiter<T> GetAwaiter() {
        return new ValueFutureAwaiter<T>(in this, _executor, _options);
    }

    /// <summary>
    /// 获取用于等待的Awaiter
    /// </summary>
    /// <returns></returns>
    FutureAwaiter<T> IFuture<T>.GetAwaiter() {
        return new FutureAwaiter<T>(Boxed());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="executor"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public ValueFuture<T> GetAwaiter(IExecutor executor, int options = 0) {
        return new ValueFuture<T>(Boxed(), _executor, _options);
    }

    #endregion


    #region state

    /// <summary>
    /// 如果future关联的任务仍处于等待执行的状态，则返回true
    /// （换句话说，如果任务仍在排队，则返回true）
    /// </summary>
    public bool IsPending => Status == TaskStatus.PENDING;

    /** 如果future关联的任务正在执行中，则返回true */
    public bool IsComputing => Status == TaskStatus.COMPUTING;

    /** 如果future已进入完成状态(成功、失败、被取消)，则返回true */
    public bool IsDone => Status.IsDone();

    /** 如果future关联的任务在正常完成被取消，则返回true。 */
    public bool IsCancelled => Status == TaskStatus.CANCELLED;

    /** 如果future已进入完成状态，且是成功完成，则返回true。 */
    public bool IsSucceeded => Status == TaskStatus.SUCCESS;

    /** 如果future已进入完成状态，且是失败状态，则返回true */
    public bool IsFailed => Status == TaskStatus.FAILED;

    /**
     * 在JDK的约定中，取消和failed是分离的，我们仍保持这样的约定；
     * 但有些时候，我们需要将取消也视为失败的一种，因此需要快捷的方法。
     */
    public bool IsFailedOrCancelled => Status.IsFailedOrCancelled();
    
    #endregion

    public IExecutor? Executor { get; }

    public TaskStatus Status { get; }

    public bool GetNow(out T result) {
        throw new NotImplementedException();
    }

    public T ResultNow() {
        throw new NotImplementedException();
    }

    public Exception ExceptionNow(bool throwIfCancelled = true) {
        throw new NotImplementedException();
    }

    public T Get() {
        throw new NotImplementedException();
    }

    public T Get(TimeSpan timeout) {
        throw new NotImplementedException();
    }

    public T Join() {
        throw new NotImplementedException();
    }

    public bool Await(TimeSpan timeout) {
        throw new NotImplementedException();
    }

    public bool AwaitUninterruptibly(TimeSpan timeout) {
        throw new NotImplementedException();
    }

    IFuture<T> IFuture<T>.Await() {
        throw new NotImplementedException();
    }

    IFuture<T> IFuture<T>.AwaitUninterruptibly() {
        throw new NotImplementedException();
    }

    public void OnCompleted(Action<IFuture<T>> continuation, int options = 0) {
        throw new NotImplementedException();
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>> continuation, int options = 0) {
        throw new NotImplementedException();
    }

    public void OnCompleted(Action<IFuture<T>, object> continuation, object state, int options = 0) {
        throw new NotImplementedException();
    }

    public void OnCompletedAsync(IExecutor executor, Action<IFuture<T>, object> continuation, object state, int options = 0) {
        throw new NotImplementedException();
    }

    public void OnCompleted(Action<object> continuation, object state, int options = 0) {
        throw new NotImplementedException();
    }

    public void OnCompletedAsync(IExecutor executor, Action<object> continuation, object state, int options = 0) {
        throw new NotImplementedException();
    }

    #endregion
}