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
using System.Threading;

namespace Wjybxx.Commons.Concurrent;

/// <summary>
///
/// ps：
/// 1. 在我的设计中，Future是不重用的，因此获取结果等接口无token参数。
/// 2. 要支持显式的异步编程，需要将Future暴露给用户，也就无法轻易重用。
/// </summary>
/// <typeparam name="T">任务的结果类型</typeparam>
[AsyncMethodBuilder(typeof(AsyncFutureMethodBuilder<>))]
public interface IFuture<T>
{
    /// <summary>
    /// 任务绑定的线程
    /// ps:主要用于检测死锁。
    /// </summary>
    IExecutor? Executor { get; }

    /// <summary>
    /// 返回只读的Future视图，
    ///
    /// 如果Future是一个提供了写接口的Promise，则返回一个只读的Future视图，返回的实例会在当前Promise进入完成状态时进入完成状态。
    /// 1. 一般情况下我们通过接口隔离即可达到读写分离目的，这可以节省开销；在大规模链式调用的情况下，Promise继承Future很有效。
    /// 2. 但如果觉得返回Promise实例给任务的发起者不够安全，可创建Promise的只读视图返回给用户
    /// 3. 这里不要求返回的必须是同一个实例，每次都可以创建一个新的实例。
    /// </summary>
    /// <returns></returns>
    IFuture<T> AsReadonly();

    #region State

    /** 获取future的状态枚举值 */
    TaskStatus Status { get; }

    /// <summary>
    /// 如果future关联的任务仍处于等待执行的状态，则返回true
    /// （换句话说，如果任务仍在排队，则返回true）
    /// </summary>
    bool IsPending => Status == TaskStatus.PENDING;

    /** 如果future关联的任务正在执行中，则返回true */
    bool IsComputing => Status == TaskStatus.COMPUTING;

    /** 如果future已进入完成状态(成功、失败、被取消)，则返回true */
    bool IsDone => Status.IsDone();

    /** 如果future关联的任务在正常完成被取消，则返回true。 */
    bool IsCancelled => Status == TaskStatus.CANCELLED;

    /** 如果future已进入完成状态，且是成功完成，则返回true。 */
    bool IsSucceeded => Status == TaskStatus.SUCCESS;

    /** 如果future已进入完成状态，且是失败状态，则返回true */
    bool IsFailed => Status == TaskStatus.FAILED;

    /**
     * 在JDK的约定中，取消和failed是分离的，我们仍保持这样的约定；
     * 但有些时候，我们需要将取消也视为失败的一种，因此需要快捷的方法。
     */
    bool IsFailedOrCancelled => Status.IsFailedOrCancelled();

    #endregion

    #region 非阻塞结果查询

    /// <summary>
    /// 尝试获取计算结果 -- 非阻塞
    /// 如果对应的计算失败，则抛出对应的异常。
    /// 如果计算成功，则返回计算结果。
    /// 如果计算尚未完成，则返回默认值。
    /// </summary>
    /// <param name="result">接收结果的引用</param>
    /// <exception cref="CompletionException">计算失败</exception>
    /// <exception cref="OperationCanceledException">被取消</exception>
    /// <returns>任务是否进入完成状态</returns>
    bool GetNow(out T result);

    /// <summary>
    /// 非阻塞方式获取Future的执行结果
    /// </summary>
    /// <exception cref="IllegalStateException">如果任务不是成功完成状态</exception>
    /// <returns></returns>
    T ResultNow();

    /// <summary>
    /// 非阻塞方式获取导致Future失败的原因
    /// 
    /// </summary>
    /// <param name="throwIfCancelled">任务取消的状态下是否抛出状态异常</param>
    /// <exception cref="IllegalStateException">如果任务不是失败完成状态</exception>
    /// <returns></returns>
    Exception ExceptionNow(bool throwIfCancelled = true);

    #endregion

    #region 阻塞操作

    /// <summary>
    /// 获取计算结果 
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// 如果计算成功，则返回计算结果。
    /// </summary>
    /// <exception cref="CompletionException">计算失败</exception>
    /// <exception cref="OperationCanceledException">被取消</exception>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    T Get();

    /// <summary>
    /// 获取计算结果 
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// 如果计算成功，则返回计算结果。
    /// </summary>
    /// <param name="timeout">等待时长</param>
    /// <exception cref="CompletionException">计算失败</exception>
    /// <exception cref="OperationCanceledException">被取消</exception>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    /// <exception cref="TimeoutException">等待超时</exception>
    /// <returns></returns>
    T Get(TimeSpan timeout);

    /// <summary>
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态 -- 不响应中断信号。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// 如果计算成功，则返回计算结果。
    /// </summary>
    /// <exception cref="CompletionException">计算失败</exception>
    /// <exception cref="OperationCanceledException">被取消</exception>
    /// <returns></returns>
    T Join();

    /// <summary>
    /// 阻塞到任务完成或超时
    /// </summary>
    /// <param name="timeout">等待时长</param>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    /// <returns>任务在这期间是否进入了完成状态</returns>
    bool Await(TimeSpan timeout);

    /// <summary>
    /// 阻塞到任务完成或超时，等待期间不响应中断
    /// </summary>
    /// <param name="timeout">等待时长</param>
    /// <returns></returns>
    bool AwaitUninterruptibly(TimeSpan timeout);

    /// <summary>
    /// 阻塞到任务完成
    /// </summary>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    /// <returns>this</returns>
    IFuture<T> Await();

    /// <summary>
    /// 阻塞到任务完成，等待期间不响应中断
    /// </summary>
    /// <returns>this</returns>
    IFuture<T> AwaitUninterruptibly();

    #endregion

    #region asyncbuilder

    /// <summary>
    /// 获取用于等待的Awaiter
    /// 默认在使Future进入完成状态的线程执行回调，即同步执行回调。
    /// </summary>
    /// <returns></returns>
    FutureAwaiter<T> GetAwaiter() {
        return new FutureAwaiter<T>(this);
    }

    /// <summary>
    /// 获取在指定线程上执行回调的Awaiter
    ///
    /// c#的编译器并未支持该功能，因此需要用户显式调用该方法再await，示例如下：
    /// <code>
    ///     // await后的代码将在eventLoop线程执行
    ///     await future.GetAwaiter(eventLoop); 
    ///
    ///     // 如果future是在eventLoop线程完成的，则同步执行await后的代码，不通过提交异步任务切换线程 
    ///     await future.GetAwaiter(eventLoop, TaskOption.STAGE_TRY_INLINE);
    /// </code>
    /// </summary>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOption.STAGE_TRY_INLINE"/></param>
    /// <returns></returns>
    ValueFuture<T> GetAwaiter(IExecutor executor, int options = 0) {
        return new ValueFuture<T>(this, executor, options);
    }

    /// <summary>
    /// 添加一个监听器
    /// 1. 该接口通常应该由<see cref="FutureAwaiter{T}"/>调用。
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(Action<IFuture<T>> continuation, int options = 0);

    /// <summary>
    /// 添加一个监听器
    /// 1. 该接口通常应该由<see cref="FutureAwaiter{T}"/>调用。
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(IExecutor executor, Action<IFuture<T>> continuation, int options = 0);

    /// <summary>
    /// 添加一个监听器
    /// 1. 该接口通常应该由<see cref="FutureAwaiter{T}"/>调用。
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(Action<IFuture<T>, object> continuation, object state, int options = 0);

    /// <summary>
    /// 添加一个监听器
    /// 1. 该接口通常应该由<see cref="FutureAwaiter{T}"/>调用。
    /// 2. 如果state是<see cref="IContext"/>类型，默认会在执行回调前会检查Context中的取消信号。
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(IExecutor executor, Action<IFuture<T>, object> continuation, object state, int options = 0);

    /// <summary>
    /// 添加一个监听器
    /// 1. 该接口通常应该由<see cref="FutureAwaiter{T}"/>调用。
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(Action<object> continuation, object state, int options = 0);

    /// <summary>
    /// 添加一个监听器
    /// 1. 该接口通常应该由<see cref="FutureAwaiter{T}"/>调用。
    /// 2. 如果state是<see cref="IContext"/>类型，默认会在执行回调前会检查Context中的取消信号。
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(IExecutor executor, Action<object> continuation, object state, int options = 0);

    #endregion
}