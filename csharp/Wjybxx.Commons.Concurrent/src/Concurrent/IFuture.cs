﻿#region LICENSE

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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 1.Future是任务和用户之间通信的媒介。
/// 2.Task可通过Promise向用户传递信息，用户可通过Future主动查询Task的状态。
/// 3.任务和用户之间需要特殊的交互时，需要特殊的Future进行粘合。
///
/// PS：Future实例不适合直接被复用，因为我们的回调参数包含Future对象，导致难以精确确定回收的时机。
/// </summary>
/// <typeparam name="T">任务的结果类型</typeparam>
[AsyncMethodBuilder(typeof(AsyncFutureMethodBuilder<>))]
public interface IFuture<T> : IFuture
{
    #region 重写签名

    /// <summary>
    /// 返回只读的Future视图，
    ///
    /// 如果Future是一个提供了写接口的Promise，则返回一个只读的Future视图，返回的实例会在当前Promise进入完成状态时进入完成状态。
    /// 1. 一般情况下我们通过接口隔离即可达到读写分离目的，这可以节省开销；在大规模链式调用的情况下，Promise继承Future很有效。
    /// 2. 但如果觉得返回Promise实例给任务的发起者不够安全，可创建Promise的只读视图返回给用户
    /// 3. 这里不要求返回的必须是同一个实例，每次都可以创建一个新的实例。
    /// </summary>
    /// <returns></returns>
    new IFuture<T> AsReadonly();

    /// <summary>
    /// 非阻塞方式获取Future的执行结果
    /// </summary>
    /// <exception cref="IllegalStateException">如果任务不是成功完成状态</exception>
    /// <returns></returns>
    new T ResultNow();

    /// <summary>
    /// 获取计算结果 
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// 如果计算成功，则返回计算结果。
    /// </summary>
    /// <exception cref="CompletionException">计算失败</exception>
    /// <exception cref="OperationCanceledException">被取消</exception>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    new T Get();

    /// <summary>
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态 -- 不响应中断信号。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// 如果计算成功，则返回计算结果。
    /// </summary>
    /// <exception cref="CompletionException">计算失败</exception>
    /// <exception cref="OperationCanceledException">被取消</exception>
    /// <returns></returns>
    new T Join();

    /// <summary>
    /// 阻塞到任务完成
    /// </summary>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    /// <returns>this</returns>
    new IFuture<T> Await();

    /// <summary>
    /// 阻塞到任务完成，等待期间不响应中断
    /// </summary>
    /// <returns>this</returns>
    new IFuture<T> AwaitUninterruptibly();

    #endregion

    #region asyncbuilder

    /// <summary>
    /// 获取用于等待的Awaiter
    /// 1. await时，如果Future已进入完成状态，回调在当前线程执行 —— C#语言机制。
    /// 2. 如果Future尚未进入完成状态，则默认在使Future进入完成状态的线程执行回调，即同步执行回调。
    ///
    /// ps：await语法底层的实现，导致我们无法精确控制await的回调线程；必须在Executor上进行等待才可确保线程。
    /// </summary>
    /// <returns></returns>
    new FutureAwaiter<T> GetAwaiter() {
        return new FutureAwaiter<T>(this);
    }

    /// <summary>
    /// 获取在指定线程上执行回调的Awaiter
    ///
    /// c#的编译器并未支持该功能，因此需要用户显式调用该方法再await，示例如下：
    /// <code>
    ///     // await后的代码将在eventLoop线程执行
    ///     await future.GetAwaitable(eventLoop); 
    ///
    ///     // 如果future是在eventLoop线程完成的，则同步执行await后的代码，不通过提交异步任务切换线程 
    ///     await future.GetAwaitable(eventLoop, TaskOption.STAGE_TRY_INLINE);
    /// </code>
    /// </summary>
    /// <param name="executor">awaiter的回调线程</param>
    /// <param name="options">awaiter的调度选项，重要参数<see cref="TaskOptions.STAGE_TRY_INLINE"/></param>
    /// <returns></returns>
    new FutureAwaitable<T> GetAwaitable(IExecutor executor, int options = 0) => new FutureAwaitable<T>(this, executor, options);

    /// <summary>
    /// 添加一个监听器
    /// 1. 该接口通常应该由<see cref="FutureAwaiter{T}"/>调用。
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(Action<IFuture<T>> continuation, int options = 0);

    /// <summary>
    /// 添加一个监听器
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(IExecutor executor, Action<IFuture<T>> continuation, int options = 0);

    /// <summary>
    /// 添加一个监听器 -- 接收future和state参数
    /// 1. 该接口通常应该由<see cref="FutureAwaiter{T}"/>调用。
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="options">调度选项</param>
    void OnCompleted(Action<IFuture<T>, object?> continuation, object? state, int options = 0);

    /// <summary>
    /// 添加一个监听器 -- 接收future和state参数
    /// ps：如果不期望检测state中潜在的取消信号，可通过<see cref="TaskOptions.STAGE_UNCANCELLABLE_CTX"/>关闭。
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="options">调度选项</param>
    void OnCompletedAsync(IExecutor executor, Action<IFuture<T>, object?> continuation, object? state, int options = 0);

    #endregion

    #region 接口适配

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IFuture IFuture.AsReadonly() => AsReadonly();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object IFuture.ResultNow() => ResultNow();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object IFuture.Get() => Get();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object IFuture.Join() => Join();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IFuture IFuture.Await() => Await();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IFuture IFuture.AwaitUninterruptibly() => AwaitUninterruptibly();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IFuture.OnCompleted(Action<IFuture> continuation, int options) {
        OnCompleted(continuation, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IFuture.OnCompletedAsync(IExecutor executor, Action<IFuture> continuation, int options) {
        OnCompletedAsync(executor, continuation, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IFuture.OnCompleted(Action<IFuture, object?> continuation, object? state, int options) {
        OnCompleted(continuation, state, options);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IFuture.OnCompletedAsync(IExecutor executor, Action<IFuture, object?> continuation, object? state, int options) {
        OnCompletedAsync(executor, continuation, state, options);
    }

    #endregion
}
}