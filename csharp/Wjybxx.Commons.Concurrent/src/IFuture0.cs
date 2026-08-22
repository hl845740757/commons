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
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 1. 该非泛型接口用于支持统一操作，不提供具体实现。
/// 2. void可通过byte/int/bool泛型替代 -- 推荐byte。
/// 3. C#由于支持async/await语法，因此未像<see cref="Task{TResult}"/>一样提供大量的回调接口；一方面是async/await代码更易读，另一方面是真泛型下实现成本太高。
/// 4. 在我的设计中，Future是不重用的，因此获取结果等接口无token参数。
/// 5. 要支持显式的异步编程，需要将Future暴露给用户，也就无法管理Future生命周期，也就无法轻易重用。
/// </summary>
[AsyncMethodBuilder(typeof(AsyncFutureMethodBuilder))]
public interface IFuture
{
    /// <summary>
    /// 任务关联的线程。
    ///
    /// 1.对于异步任务，Executor是其执行线程；而对于同步任务，Executor不一定是其执行线程 -- 继承得来的而已。
    /// 2.在添加下游任务时，如果没有显式指定Executor，将继承当前任务的Executor。
    /// 3.Executor主要用于死锁检测，相关接口<see cref="ISingleThreadExecutor"/>
    ///
    /// 注意：由于死锁检测并不完全正确，当你需要绕过死锁检测时，可通过添加下游任务重新指定Executor来绕过。
    /// </summary>
    IExecutor? Executor { get; }

    #region 状态查询

    /** 获取future的状态枚举值 */
    TaskStatus Status { get; }

    /// <summary>
    /// 如果future关联的任务仍处于等待执行的状态，则返回true
    /// （换句话说，如果任务仍在排队，则返回true）
    /// </summary>
    bool IsPending => Status == TaskStatus.Pending;

    /** 如果future关联的任务正在执行中，则返回true */
    bool IsComputing => Status == TaskStatus.Computing;

    /** 如果future已进入完成状态，且是成功完成，则返回true。 */
    bool IsSucceeded => Status == TaskStatus.Success;

    /** 如果future已进入完成状态，且是失败状态，则返回true */
    bool IsFailed => Status == TaskStatus.Failed;

    /** 如果future关联的任务在正常完成被取消，则返回true。 */
    bool IsCancelled => Status == TaskStatus.Cancelled;

    /** 如果future已进入完成状态(成功、失败、被取消)，则返回true */
    bool IsCompleted => Status >= TaskStatus.Success;

    /**
     * 在JDK的约定中，取消和failed是分离的，我们仍保持这样的约定；
     * 但有些时候，我们需要将取消也视为失败的一种，因此需要快捷的方法。
     */
    bool IsFailedOrCancelled => Status > TaskStatus.Success;

    #endregion

    #region 非阻塞结果查询

    /// <summary>
    /// 非阻塞方式获取Future的执行结果
    /// </summary>
    /// <exception cref="InvalidOperationException">如果任务不是成功完成状态</exception>
    /// <returns>任务关联的结果</returns>
    object ResultNow();

    /// <summary>
    /// 非阻塞方式获取导致Future失败的原因
    /// 
    /// </summary>
    /// <param name="throwIfCancelled">任务取消的状态下是否抛出状态异常</param>
    /// <exception cref="InvalidOperationException">如果任务不是失败完成状态</exception>
    /// <returns></returns>
    Exception ExceptionNow(bool throwIfCancelled = true);

    /// <summary>
    /// 返回原始的异常数据
    /// 
    /// 返回值类型：<see cref="OperationCanceledException"/>或<see cref="ExceptionDispatchInfo"/>，
    /// 用于解决C#异常信息传递开销问题。
    /// </summary>
    /// <returns></returns>
    object ExceptionOrDispatchInfoNow();

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
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态或超时。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <returns></returns>
    /// <exception cref="TimeoutException">等待超时</exception>
    object Get(TimeSpan timeout);

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
    /// 添加一个监听器
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(Action<IFuture, object?> continuation, object? state,
                     CancellationToken cancelToken = default, int options = 0);

    /// <summary>
    /// 添加一个监听器
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(IExecutor executor, Action<IFuture, object?> continuation, object? state,
                          CancellationToken cancelToken = default, int options = 0);

    /// <summary>
    /// 添加一个监听器
    /// (该接口不接收future参数，主要用于异步状态机；慎重传入取消令牌，传入取消令牌的情况下状态机回调可能不被执行)
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(Action<object?> continuation, object? state,
                     CancellationToken cancelToken = default, int options = 0);

    /// <summary>
    /// 添加一个监听器
    /// (该接口不接收future参数，主要用于异步状态机；慎重传入取消令牌，传入取消令牌的情况下状态机回调可能不被执行)
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(IExecutor executor, Action<object?> continuation, object? state,
                          CancellationToken cancelToken = default, int options = 0);

    #endregion
}
}