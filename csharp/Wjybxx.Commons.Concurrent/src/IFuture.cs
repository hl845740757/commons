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

namespace Wjybxx.Commons;

/// <summary>
/// 
/// </summary>
/// <typeparam name="T">任务的结果类型</typeparam>
[AsyncMethodBuilder(typeof(AsyncFutureMethodBuilder<>))]
public interface IFuture<T>
{
    #region ctx

    /// <summary>
    /// 任务绑定的上下文
    /// </summary>
    IContext Context { get; }

    /// <summary>
    /// 任务绑定的线程
    /// </summary>
    IExecutor IExecutor { get; }

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

    #endregion

    #region State

    /** 获取future的状态枚举值 */
    FutureState State { get; }

    /// <summary>
    /// 如果future关联的任务仍处于等待执行的状态，则返回true
    /// （换句话说，如果任务仍在排队，则返回true）
    /// </summary>
    bool IsPending => State == FutureState.PENDING;

    /** 如果future关联的任务正在执行中，则返回true */
    bool IsComputing => State == FutureState.COMPUTING;

    /** 如果future已进入完成状态(成功、失败、被取消)，则返回true */
    bool IsDone => State.IsDone();

    /** 如果future关联的任务在正常完成被取消，则返回true。 */
    bool IsCancelled => State == FutureState.CANCELLED;

    /** 如果future已进入完成状态，且是成功完成，则返回true。 */
    bool IsSucceeded => State == FutureState.SUCCESS;

    /** 如果future已进入完成状态，且是失败状态，则返回true */
    bool IsFailed => State == FutureState.FAILED;

    /**
     * 在JDK的约定中，取消和failed是分离的，我们仍保持这样的约定；
     * 但有些时候，我们需要将取消也视为失败的一种，因此需要快捷的方法。
     */
    bool IsFailedOrCancelled => State.IsFailedOrCancelled();

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
    /// 获取真实的用于等待的Awaiter
    /// </summary>
    /// <returns></returns>
    FutureAwaiter<T> GetAwaiter();

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
    /// <param name="executor">回调线程</param>
    /// <param name="options">延续任务的调度选项，重要参数<see cref="TaskOption.STAGE_TRY_INLINE"/></param>
    /// <returns></returns>
    FutureAwaiter<T> GetAwaiter(IExecutor executor, int options = 0);

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

    #endregion

    #region async

    // region compose-管道

    /// <summary>
    /// 该方法表示在当前{@code Future}与返回的{@code Future}中插入一个异步操作，构建异步管道。
    /// 该方法返回一个新的{@code Future}，它的最终结果与指定的{@code Func}返回的{@code Future}结果相同。
    /// 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
    /// 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数。
    /// 
    /// </summary>
    /// <param name="fn">回调函数</param>
    /// <param name="ctx">上下文，如果为null，则继承当前Stage的上下文</param>
    /// <param name="options">调度选项</param>
    /// <typeparam name="U"></typeparam>
    /// <returns></returns>
    IFuture<U> composeApply<U>(Func<IContext, T, IFuture<U>> fn, IContext? ctx = null, int options = 0);

    IFuture<U> composeApplyAsync<U>(IExecutor executor,
                                    Func<IContext, T, IFuture<U>> fn, IContext? ctx = null, int options = 0);
    
    /// <summary>
    /// 该方法表示在当前{@code Future}与返回的{@code Future}中插入一个异步操作，构建异步管道。
    /// 该方法返回一个新的{@code Future}，它的最终结果与指定的{@code Func}返回的{@code Future}结果相同。
    /// 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
    /// 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数。
    /// 
    /// </summary>
    /// <param name="fn">回调函数</param>
    /// <param name="ctx">上下文，如果为null，则继承当前Stage的上下文</param>
    /// <param name="options">调度选项</param>
    /// <typeparam name="U"></typeparam>
    /// <returns></returns>
    IFuture<U> composeCall<U>(Func<IContext, IFuture<U>> fn, IContext? ctx = null, int options = 0);

    IFuture<U> composeCallAsync<U>(IExecutor executor,
                                   Func<IContext, IFuture<U>> fn, IContext? ctx = null, int options = 0);
    
    /// <summary>
    /// 它表示能从从特定的异常中恢复，并异步返回一个正常结果。
    /// 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
    /// 如果当前{@code Future}正常完成，则给定的动作不会执行，且返回的{@code Future}使用相同的结果值进入完成状态。
    /// 如果当前{@code Future}执行失败，则其异常信息将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
    /// </summary>
    /// <param name="fallback">恢复函数</param>
    /// <param name="ctx">上下文，如果为null，则继承当前Stage的上下文</param>
    /// <param name="options">调度选项</param>
    /// <typeparam name="X">异常类型</typeparam>
    /// <returns></returns>
    IFuture<T> composeCatching<X>(Func<IContext, X, IFuture<T>> fallback, IContext? ctx = null, int options = 0) where X : Exception;

    IFuture<T> composeCatchingAsync<X>(IExecutor executor,
                                       Func<IContext, X, IFuture<T>> fallback, IContext? ctx = null, int options = 0) where X : Exception;

    /// <summary>
    /// 它表示既能接收任务的正常结果，也可以接收任务异常结果，并异步返回一个运算结果。
    /// 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
    /// 不论当前{@code Future}成功还是失败，都将执行给定的操作，返回的{@code Future}的结果取决于指定操作的执行结果。
    /// </summary>
    /// <param name="fn">下游函数</param>
    /// <param name="ctx">上下文，如果为null，则继承当前Stage的上下文</param>
    /// <param name="options">调度选项</param>
    /// <typeparam name="U"></typeparam>
    /// <returns></returns>
    IFuture<U> composeHandle<U>(Func<IContext, T, Exception, IFuture<U>> fn, IContext? ctx = null, int options = 0);

    IFuture<U> composeHandleAsync<U>(IExecutor executor,
                                     Func<IContext, T, Exception, IFuture<U>> fn, IContext? ctx = null, int options = 0);
    // endregion

    // region apply
    
    /// <summary>
    /// 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
    /// 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
    /// 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
    /// </summary>
    /// <param name="fn">下游函数</param>
    /// <param name="ctx">上下文，如果为null，则继承当前Stage的上下文</param>
    /// <param name="options">调度选项</param>
    /// <typeparam name="U"></typeparam>
    /// <returns></returns>
    IFuture<U> thenApply<U>(Func<IContext, T, U> fn, IContext? ctx = null, int options = 0);

    IFuture<U> thenApplyAsync<U>(IExecutor executor,
                                 Func<IContext, T, U> fn, IContext? ctx = null, int options = 0);

    // endregion

    // region accept

    //
    /// <summary>
    /// 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
    /// 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
    /// 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
    /// </summary>
    /// <param name="action">下游函数</param>
    /// <param name="ctx">上下文，如果为null，则继承当前Stage的上下文</param>
    /// <param name="options">调度选项</param>
    /// <returns></returns>
    IFuture<object> thenAccept(Action<IContext, T> action, IContext? ctx = null, int options = 0);


    IFuture<object> thenAcceptAsync(IExecutor executor,
                                    Action<IContext, T> action, IContext? ctx = null, int options = 0);

    // endregion

    // region call

    /// <summary>
    /// 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
    /// 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
    /// 如果当前{@code Future}执行成功，则执行给定的操作，返回的{@code Future}的结果取决于指定操作的执行结果。
    /// </summary>
    /// <param name="fn">下游函数</param>
    /// <param name="ctx">上下文，如果为null，则继承当前Stage的上下文</param>
    /// <param name="options">调度选项</param>
    /// <typeparam name="U"></typeparam>
    /// <returns></returns>
    IFuture<U> thenCall<U>(Func<IContext, U> fn, IContext? ctx = null, int options = 0);

    IFuture<U> thenCallAsync<U>(IExecutor executor,
                                Func<IContext, U> fn, IContext? ctx = null, int options = 0);
    // endregion

    // region run

    /**
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则执行给定的操作，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * {@link CompletionStage#thenRun(Runnable)}
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    IFuture<object> thenRun(Action<IContext> action, IContext? ctx = null, int options = 0);

    IFuture<object> thenRunAsync(IExecutor executor,
                                 Action<IContext> action, IContext? ctx = null, int options = 0);
    // endregion

    // region catching-异常处理

    /**
     * 它表示能从从特定的异常中恢复，并返回一个正常结果。
     * <p>
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}正常完成，则给定的动作不会执行，且返回的{@code Future}使用相同的结果值进入完成状态。
     * 如果当前{@code Future}执行失败，则其异常信息将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * 不得不说JDK的{@link CompletionStage#exceptionally(Func)}这个名字太差劲了，实现的也不够好，因此我们不使用它，
     * 这里选择了Guava中的实现
     *
     * @param exceptionType 能处理的异常类型
     * @param fallback      异常恢复函数
     * @param ctx           上下文，如果为null，则继承当前Stage的上下文
     * @param options       调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    IFuture<T> catching<X>(Func<IContext, X, T> fallback, IContext? ctx = null, int options = 0) where X : Exception;

    IFuture<T> catchingAsync<X>(IExecutor executor,
                                Func<IContext, X, T> fallback, IContext? ctx = null, int options = 0) where X : Exception;
    // endregion

    // region handle

    /**
     * 该方法表示既能处理当前计算的正常结果，又能处理当前结算的异常结果(可以将异常转换为新的结果)，并返回一个新的结果。
     * <p>
     * 该方法返回一个新的{@code Future}，无论当前{@code Future}执行成功还是失败，给定的操作都将执行。
     * 如果当前{@code Future}执行成功，而指定的动作出现异常，则返回的{@code Future}以该异常完成。
     * 如果当前{@code Future}执行失败，且指定的动作出现异常，则返回的{@code Future}以新抛出的异常进入完成状态。
     * <p>
     * {@link CompletionStage#handle(Func)}
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    IFuture<U> handle<U>(Func<IContext, T, Exception, U> fn,
                         IContext? ctx = null, int options = 0);

    IFuture<U> handleAsync<U>(IExecutor executor,
                              Func<IContext, T, Exception, U> fn,
                              IContext? ctx = null, int options = 0);
    // endregion

    // region when-简单监听

    /**
     * 该方法返回一个新的{@code Future}，无论当前{@code Future}执行成功还是失败，给定的操作都将执行，且返回的{@code Future}始终以相同的结果进入完成状态。
     * 与方法{@link #handle(Func)}不同，此方法不是为转换完成结果而设计的，因此提供的操作不应引发异常。<br>
     * 1.如果action出现了异常，则仅仅记录一个日志，不向下传播(这里与JDK实现不同) -- 应当避免抛出异常。
     * 2.如果用户主动取消了返回的Future，或者用于异步执行的Executor已关闭，则不会以相同的结果进入完成状态。
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    IFuture<T> whenComplete(Action<IContext, T, Exception> action,
                            IContext? ctx = null, int options = 0);

    IFuture<T> whenCompleteAsync(IExecutor executor,
                                 Action<IContext, T, Exception> action,
                                 IContext? ctx = null, int options = 0);
    // endregion

    #endregion
}