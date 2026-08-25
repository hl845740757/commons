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
    /// 非阻塞方式获取Future的执行结果
    /// </summary>
    /// <exception cref="InvalidOperationException">如果任务不是成功完成状态</exception>
    /// <returns></returns>
    new T ResultNow();

    /// <summary>
    /// 阻塞式获取计算结果（响应中断）
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// 如果计算成功，则返回计算结果。
    /// (C#保持和系统库一样总是抛出原始异常和原始堆栈，不再封装异常)
    /// </summary>
    /// <exception cref="OperationCanceledException">被取消</exception>
    /// <exception cref="ThreadInterruptedException">线程被中断</exception>
    new T Get();

    /// <summary>
    /// 阻塞式获取计算结果（响应中断）
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态或超时。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// </summary>
    /// <param name="timeout">超时时间</param>
    /// <returns></returns>
    /// <exception cref="TimeoutException">等待超时</exception>
    new T Get(TimeSpan timeout);

    /// <summary>
    /// 阻塞式获取计算结果（不响应中断）
    /// 如果Future关联的任务尚未完成，该方法将阻塞到Future进入完成状态。
    /// 如果对应的计算失败，则抛出对应的异常。
    /// 如果计算成功，则返回计算结果。
    /// (C#保持和系统库一样总是抛出原始异常和原始堆栈，不再封装异常)
    /// </summary>
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
    /// 添加一个监听器 -- 接收future和state参数
    /// </summary>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="options">调度选项，默认使用0即可，可参考<see cref="TaskOptions"/></param>
    /// <param name="cancelToken">取消令牌</param>
    void OnCompleted(Action<IFuture<T>, object?> continuation, object? state,
                     int options = 0, CancellationToken cancelToken = default);

    /// <summary>
    /// 添加一个监听器 -- 接收future和state参数
    /// </summary>
    /// <param name="executor">回调线程</param>
    /// <param name="continuation">回调</param>
    /// <param name="state">回调参数</param>
    /// <param name="options">调度选项</param>
    /// <param name="cancelToken">取消令牌</param>
    void OnCompletedAsync(IExecutor executor, Action<IFuture<T>, object?> continuation, object? state,
                          int options = 0, CancellationToken cancelToken = default);

    #endregion

    #region compose-管道

    /// <summary>
    /// 该方法表示在当前Future与返回的Future中插入一个异步操作，构建异步管道 => 这是链式调用的核心API。
    /// 
    /// 该方法返回一个新的Future，它的最终结果与指定的Func返回的Future结果相同。
    /// 如果当前Future执行失败，则返回的Future将以相同的原因失败，且指定的动作不会执行。
    /// 如果当前Future执行成功，则当前Future的执行结果将作为指定操作的执行参数。
    /// 
    /// (为了减少重载，没有定义不含ctx的方法)
    /// </summary>
    /// <param name="fn">回调函数，第一个参数是ctx，第二个参数是当前Future的结果</param>
    /// <param name="options"></param>
    /// <param name="cancelToken"></param>
    /// <typeparam name="U"></typeparam>
    /// <returns></returns>
    IFuture<U> ComposeApply<U>(Func<T, IFuture<U>> fn,
                               int options = 0, CancellationToken cancelToken = default);

    IFuture<U> ComposeApplyAsync<U>(IExecutor executor, Func<T, IFuture<U>> fn,
                                    int options = 0, CancellationToken cancelToken = default);

    /// <summary>
    /// 该方法表示在当前Future与返回的Future中插入一个异步操作，构建异步管道。
    /// 
    /// 该方法返回一个新的Future，它的最终结果与指定的Func返回的Future结果相同。
    /// 如果当前Future执行失败，则返回的Future将以相同的原因失败，且指定的动作不会执行。
    /// 如果当前Future执行成功，则当前Future的执行结果将作为指定操作的执行参数。
    /// </summary>
    IFuture<U> ComposeCall<U>(Func<IFuture<U>> fn,
                              int options = 0, CancellationToken cancelToken = default);

    IFuture<U> ComposeCallAsync<U>(IExecutor executor, Func<IFuture<U>> fn,
                                   int options = 0, CancellationToken cancelToken = default);

    /// <summary>
    /// 从给定的异常中恢复
    /// </summary>
    /// <param name="fallback">异常恢复函数</param>
    /// <param name="options">调度选项</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <typeparam name="X">异常类型</typeparam>
    /// <returns></returns>
    IFuture<T> ComposeCatching<X>(Func<X, IFuture<T>> fallback,
                                  int options = 0, CancellationToken cancelToken = default) where X : Exception;

    IFuture<T> ComposeCatchingAsync<X>(IExecutor executor, Func<X, IFuture<T>> fallback,
                                       int options = 0, CancellationToken cancelToken = default) where X : Exception;

    /// <summary>
    /// 既可以处理正确结果，也可以处理异常结果
    /// </summary>
    /// <param name="fn">参数1为正常结果，参数2为ex</param>
    /// <param name="options">调度选项</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <typeparam name="U"></typeparam>
    /// <returns></returns>
    IFuture<U> ComposeHandle<U>(Func<T, Exception, IFuture<U>> fn,
                                int options = 0, CancellationToken cancelToken = default);

    IFuture<U> ComposeHandleAsync<U>(IExecutor executor, Func<T, Exception, IFuture<U>> fn,
                                     int options = 0, CancellationToken cancelToken = default);

    #endregion

    #region 普通管道

    IFuture<U> ThenApply<U>(Func<T, U> fn,
                            int options = 0, CancellationToken cancelToken = default);

    IFuture<U> ThenApplyAsync<U>(IExecutor executor, Func<T, U> fn,
                                 int options = 0, CancellationToken cancelToken = default);

    IFuture ThenAccept(Action<T> fn,
                       int options = 0, CancellationToken cancelToken = default);

    IFuture ThenAcceptAsync(IExecutor executor, Action<T> fn,
                            int options = 0, CancellationToken cancelToken = default);

    IFuture<U> ThenCall<U>(Func<U> fn,
                           int options = 0, CancellationToken cancelToken = default);

    IFuture<U> ThenCallAsync<U>(IExecutor executor, Func<U> fn,
                                int options = 0, CancellationToken cancelToken = default);

    IFuture ThenRun(Action fn,
                    int options = 0, CancellationToken cancelToken = default);

    IFuture ThenRunAsync(IExecutor executor, Action fn,
                         int options = 0, CancellationToken cancelToken = default);

    IFuture<T> Catching<X>(Func<X, T> fallback,
                           int options = 0, CancellationToken cancelToken = default) where X : Exception;

    IFuture<T> CatchingAsync<X>(IExecutor executor, Func<X, T> fallback,
                                int options = 0, CancellationToken cancelToken = default) where X : Exception;

    IFuture<U> Handle<U>(Func<T, Exception, U> fn,
                         int options = 0, CancellationToken cancelToken = default);

    IFuture<U> HandleAsync<U>(IExecutor executor, Func<T, Exception, U> fn,
                              int options = 0, CancellationToken cancelToken = default);

    /// <summary>
    /// 该方法返回一个新的{@code Future}，无论当前{@code Future}执行成功还是失败，给定的操作都将执行，且返回的{@code Future}始终以相同的结果进入完成状态。
    /// 与方法{@link #handle(TriFunction)}不同，此方法不是为转换完成结果而设计的，因此提供的操作不应引发异常。
    /// 1.如果action出现了异常，则仅仅记录一个日志，不向下传播(这里与JDK实现不同) -- 应当避免抛出异常。
    /// 2.如果用户主动取消了返回的Future，或者用于异步执行的Executor已关闭，则不会以相同的结果进入完成状态。
    /// </summary>
    IFuture<T> WhenComplete(Action<T, Exception> fn,
                            int options = 0, CancellationToken cancelToken = default);

    IFuture<T> WhenCompleteAsync(IExecutor executor, Action<T, Exception> fn,
                                 int options = 0, CancellationToken cancelToken = default);

    #endregion

    #region 接口适配

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object IFuture.ResultNow() => ResultNow();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object IFuture.Get() => Get();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object IFuture.Get(TimeSpan timeout) => Get(timeout);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    object IFuture.Join() => Join();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IFuture IFuture.Await() => Await();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    IFuture IFuture.AwaitUninterruptibly() => AwaitUninterruptibly();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IFuture.OnCompleted(Action<IFuture, object?> continuation, object? state,
                             int options, CancellationToken cancelToken) {
        OnCompleted(continuation, state, options, cancelToken);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void IFuture.OnCompletedAsync(IExecutor executor, Action<IFuture, object?> continuation, object? state,
                                  int options, CancellationToken cancelToken) {
        OnCompletedAsync(executor, continuation, state, options, cancelToken);
    }

    #endregion
}
}