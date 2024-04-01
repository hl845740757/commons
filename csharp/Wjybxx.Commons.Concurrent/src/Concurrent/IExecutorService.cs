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
using System.Collections.Generic;
using System.Threading;

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 
/// </summary>
public interface IExecutorService : IExecutor
{
    #region lifecycle

    /**
     * 查询{@link EventLoopGroup}是否处于正在关闭状态。
     * 正在关闭状态下，拒绝接收新任务，当执行完所有任务后，进入关闭状态。
     *
     * @return 如果该{@link EventLoopGroup}管理的所有{@link EventLoop}正在关闭或已关闭则返回true
     */
    bool IsShuttingDown { get; }

    /**
     * 查询{@link EventLoopGroup}是否处于关闭状态。
     * 关闭状态下，拒绝接收新任务，执行退出前的清理操作，执行完清理操作后，进入终止状态。
     *
     * @return 如果已关闭，则返回true
     */
    bool IsShutdown { get; }

    /**
     * 是否已进入终止状态，一旦进入终止状态，表示生命周期真正结束。
     *
     * @return 如果已处于终止状态，则返回true
     */
    bool IsTerminated { get; }

    /// <summary>
    /// 返回Future将在Executor终止时进入完成状态
    /// 1. 返回Future应当是只读的，<see cref="IFuture.AsReadonly"/>
    /// 2. 用户可以在该Future上等待。
    /// </summary>
    /// <returns></returns>
    IFuture TerminationFuture { get; }

    //
    /// <summary>
    /// 等待EventLoopGroup进入终止状态
    /// 等同于在<see cref="TerminationFuture"/>上进行阻塞操作。
    /// 
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <exception cref="ThreadInterruptedException">如果等待期间被中断</exception>
    /// <returns>在方法返回前是否已进入终止状态</returns>
    bool AwaitTermination(TimeSpan timeout) {
        return TerminationFuture.Await(timeout);
    }

    /// <summary>
    /// 请求关闭 ExecutorService，不再接收新的任务。
    /// ExecutorService在执行完现有任务后，进入关闭状态。
    /// 如果 ExecutorService 正在关闭，或已经关闭，则方法不产生任何效果。
    ///
    /// 该方法会立即返回，如果想等待 ExecutorService 进入终止状态，
    /// 可以使用{@link #awaitTermination(long, TimeUnit)}或{@link #terminationFuture()} 进行等待
    /// </summary>
    void Shutdown();

    /// <summary>
    /// 请求关闭 ExecutorService，<b>尝试取消所有正在执行的任务，停止所有待执行的任务，并不再接收新的任务。</b>
    /// 如果 ExecutorService 已经关闭，则方法不产生任何效果。
    ///
    /// 该方法会立即返回，如果想等待 ExecutorService 进入终止状态，可以使用{@link #awaitTermination(long, TimeUnit)}
    /// 或{@link #terminationFuture()} 进行等待。
    ///
    /// 注意：部分Executor实现可能无法返回被取消的任务，只是会尽快关闭。
    /// </summary>
    /// <returns>被取消的任务</returns>
    List<ITask> ShutdownNow();

    #endregion

    #region submit

    /// <summary>
    /// 创建一个与当前Executor绑定的Promise
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns>Promise</returns>
    IPromise<T> NewPromise<T>() => new Promise<T>(this);

    /// <summary>
    /// 创建一个与当前Executor绑定的Promise
    ///
    /// 注意：通常不应该使用该Promise的结果。
    /// </summary>
    /// <returns>Promise</returns>
    IPromise NewPromise() => new Promise<byte>(this);

    /// <summary>
    /// 提交一个任务
    ///
    /// 注意：使用ref仅为了避免防御性拷贝，不会修改对象的状态 —— in关键字仍可能产生拷贝。
    /// </summary>
    /// <param name="builder">任务构建器</param>
    /// <returns></returns>
    IFuture<T> Submit<T>(ref TaskBuilder<T> builder);

    /// <summary>
    /// 提交一个任务
    /// </summary>
    /// <param name="action">待执行的函数</param>
    /// <param name="options">调度选项</param>
    /// <returns></returns>
    IFuture SubmitAction(Action action, int options = 0);

    /// <summary>
    /// 提交一个任务
    /// </summary>
    /// <param name="action">待执行的函数</param>
    /// <param name="context">任务上下文</param>
    /// <param name="options">调度选项</param>
    /// <returns></returns>
    IFuture SubmitAction(Action<IContext> action, IContext context, int options = 0);

    /// <summary>
    /// 提交一个任务
    /// </summary>
    /// <param name="action">待执行的函数</param>
    /// <param name="options">调度选项</param>
    /// <returns></returns>
    IFuture<T> SubmitFunc<T>(Func<T> action, int options = 0);

    /// <summary>
    /// 提交一个任务
    /// </summary>
    /// <param name="action">待执行的函数</param>
    /// <param name="context">任务上下文</param>
    /// <param name="options">调度选项</param>
    /// <returns></returns>
    IFuture<T> SubmitFunc<T>(Func<IContext, T> action, IContext context, int options = 0);

    #endregion
}