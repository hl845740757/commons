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
using System.Threading.Tasks;

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 1. 该非泛型接口用于支持统一操作，不提供具体实现。
/// 2. void可通过byte/int泛型替代。
/// 3. C#由于支持async/await语法，因此未像<see cref="Task{TResult}"/>一样提供大量的回调接口；一方面是async/await代码更易读，另一方面是真泛型下实现成本太高。
/// </summary>
[AsyncMethodBuilder(typeof(AsyncFutureMethodBuilder))]
public interface IFuture
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
    IFuture AsReadonly();

    #region 状态查询

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
    /// 非阻塞方式获取Future的执行结果
    /// </summary>
    /// <exception cref="IllegalStateException">如果任务不是成功完成状态</exception>
    /// <returns>任务关联的结果</returns>
    object ResultNow();

    /// <summary>
    /// 非阻塞方式获取导致Future失败的原因
    /// 
    /// </summary>
    /// <param name="throwIfCancelled">任务取消的状态下是否抛出状态异常</param>
    /// <exception cref="IllegalStateException">如果任务不是失败完成状态</exception>
    /// <returns></returns>
    Exception ExceptionNow(bool throwIfCancelled = true);

    #endregion

    #region 阻塞结果查询

    /// <summary>
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// </summary>
    /// <exception cref="CompletionException">计算失败</exception>
    /// <exception cref="OperationCanceledException">被取消</exception>
    /// <returns>任务关联的结果</returns>
    object Get();

    /// <summary>
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态 -- 不响应中断信号。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// </summary>
    /// <exception cref="CompletionException">计算失败</exception>
    /// <exception cref="OperationCanceledException">被取消</exception>
    /// <returns>任务关联的结果</returns>
    object Join();

    /// <summary>
    /// 阻塞到任务完成
    /// </summary>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    /// <returns>this</returns>
    IFuture Await();

    /// <summary>
    /// 阻塞到任务完成，等待期间不响应中断
    /// </summary>
    /// <returns>this</returns>
    IFuture AwaitUninterruptibly();

    /// <summary>
    /// 阻塞到任务完成或超时
    /// </summary>
    /// <param name="timeout">等待时长</param>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    /// <exception cref="ArgumentException">如果等待时间小于0</exception>
    /// <returns>任务在这期间是否进入了完成状态</returns>
    bool Await(TimeSpan timeout);

    /// <summary>
    /// 阻塞到任务完成或超时，等待期间不响应中断
    /// </summary>
    /// <param name="timeout">等待时长</param>
    /// <exception cref="ArgumentException">如果等待时间小于0</exception>
    /// <returns>任务在这期间是否进入了完成状态</returns>
    bool AwaitUninterruptibly(TimeSpan timeout);

    #endregion

    #region async

    /// <summary>
    /// 获取用于等待的Awaiter
    /// 默认在使Future进入完成状态的线程执行回调，即同步执行回调。
    /// </summary>
    /// <returns></returns>
    FutureAwaiter GetAwaiter() {
        return new FutureAwaiter(this);
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
    ValueFuture GetAwaiter(IExecutor executor, int options = 0) {
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        return new ValueFuture(this, executor, options);
    }

    /// <summary>
    /// 添加一个监听器 -- 接收future参数
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(Action<IFuture> continuation, int options = 0);

    /// <summary>
    /// 添加一个监听器 -- 接收future参数
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(IExecutor executor, Action<IFuture> continuation, int options = 0);

    /// <summary>
    /// 添加一个监听器 -- 接收future和state参数
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(Action<IFuture, object> continuation, object state, int options = 0);

    /// <summary>
    /// 添加一个监听器  -- 接收future和state参数
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(IExecutor executor, Action<IFuture, object> continuation, object state, int options = 0);

    /// <summary>
    /// 添加一个监听器 -- 接收future和context参数
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="context">上下文</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(Action<IFuture, TaskContext> continuation, TaskContext context, int options = 0);

    /// <summary>
    /// 添加一个监听器  -- 接收future和context参数
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="context">上下文</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(IExecutor executor, Action<IFuture, TaskContext> continuation, TaskContext context, int options = 0);

    #endregion
}