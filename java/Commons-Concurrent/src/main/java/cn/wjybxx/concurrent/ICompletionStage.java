/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.concurrent;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.CompletionStage;
import java.util.concurrent.Executor;
import java.util.concurrent.RejectedExecutionException;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;

/**
 * <h3>线程控制</h3>
 * 虽然我在Stage上约定了记录Executor的方法，但是：Future上的监听器默认由【使Future进入完成状态的线程】通知！！！
 * 因此Stage下游的【同步任务】的执行线程是不确定的！这可能导致线程安全问题、
 * <p>
 * 如果期望控制下游任务的执行线程，请调用{@code Async}命名结尾的方法添加任务，并指定Executor；
 * 如果觉得总是通过提交任务保证线程有额外的开销，或者可能导致时序问题，可使用{@link TaskOptions#STAGE_TRY_INLINE}选项，
 * Future在通知时，会判断是否已在目标线程，如果已在目标线程则同步执行，否则提交任务异步执行。
 * <p>
 * 注意:
 * 1. Async方法只保证给定的Action在目标线程执行，而不能保证其后续操作所在的线程。<br>
 * 2. 如果用于执行任务的Executor已关闭，则切换线程会失败，任务会以{@link RejectedExecutionException}失败<br>
 *
 * <h3>小心Compose</h3>
 * Compose操作，最容易犯的错误是：认为{@code ComposeAsync}的下游任务在给定的Executor中执行。
 * Compose操作，不论是否是Async方法，都不能直接保证下游任务的执行线程；Compose操作的下游任务总是由使返回的Future进入完成状态的线程通知；
 * 因此，要保证下游任务的运行线程，必须再添加一个Stage来保证。
 * <pre>{@code
 *      // 错误的方式：认为thenApply也在给定的executor中执行
 *      future.composeApplyAsync(executor, (v) -> {
 *          // fn在参数指定的executor线程中执行
 *          // ExecutorUtils.inEventLoop(executor) == true
 *          return nextStage;
 *      })
 *      .thenApply((v) -> {
 *          // 该Action的执行线程不确定，由使上游Future进入完成状态的线程通知
 *          // ExecutorUtils.inEventLoop(executor) == ?
 *      });
 *
 *      // 正确的方式：为保证下游任务的运行线程，必须再添加一个Async的Stage
 *      future.composeApplyAsync(executor, (v) -> {
 *          // fn在参数指定的executor线程中执行
 *          // ExecutorUtils.inEventLoop(executor) == true
 *          return nextStage;
 *      })
 *      .thenApplyAsync(executor, (v) -> {
 *          // 启用了STAGE_TRY_INLINE，若已在目标线程则同步执行，否则提交到executor执行
 *          // ExecutorUtils.inEventLoop(executor) == true
 *      }, TaskOptions.STAGE_TRY_INLINE, null);
 * }</pre>
 *
 * <h3>行为取消</h3>
 * Stage并不直接提供删除Action的方法，要取消行为，可将{@code cancelToken}参数指定为有效的取消令牌。
 * Stage会在执行用户的Action之前检查取消信号，另外用户的Action在运行的过程中也可主动检测取消信号。
 *
 * <h3>其它</h3>
 * 1.关于Future之间的聚合，见：{@link FutureCombiner}
 * 2.java参数不支持默认值，为减少方法数，我们只提供一种重载。
 * 3.大家可以先熟悉JDK的{@link CompletionStage}和{@link CompletableFuture}
 * 4.该接口其实类似于C#的{@code Awaitable}，只能异步等待，不能阻塞获取结果。
 *
 * @author wjybxx
 * date - 2024/1/10
 */
@SuppressWarnings("unused")
@ThreadSafe
public interface ICompletionStage<T> {

    /**
     * 任务绑定的Executor
     * 1.对于异步任务，Executor是其执行线程；而对于同步任务，Executor不一定是其执行线程 -- 继承得来的而已。
     * 2.在添加下游任务时，如果没有显式指定Executor，将继承当前Stage的Executor。
     * 3.Executor主要用于死锁检测，为去除{@link IEventLoop}的依赖，设计了{@link SingleThreadExecutor}接口
     * <p>
     * 注意：由于死锁检测并不完全正确，当你需要绕过死锁检测时，可通过添加下游任务重新指定Executor来绕过。
     */
    @Nullable
    Executor executor();

    /**
     * 返回一个Future，保持与这个Stage相同的完成结果。
     * 如果这个Stage已经是一个Future，这个方法可以返回这个Stage本身。
     */
    @Nonnull
    IFuture<T> toFuture();

    /**
     * 该接口用于与依赖{@link CompletableFuture}的库进行协作
     */
    default CompletableFuture<T> toCompletableFuture() {
        return ExecutorUtils.toJDKFuture(this);
    }

    // region compose-管道

    /**
     * 该方法表示在当前{@code Future}与返回的{@code Future}中插入一个异步操作，构建异步管道 => 这是链式调用的核心API。
     * 该方法对应我们日常流中使用的{@link java.util.stream.Stream#flatMap(Function)}操作。
     * <p>
     * 该方法返回一个新的{@code Future}，它的最终结果与指定的{@code Function}返回的{@code Future}结果相同。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数。
     * <p>
     * {@link CompletionStage#thenCompose(Function)}
     * (为了减少重载，没有定义不含cancelToken的方法)
     *
     * @param options     调度选项，默认使用0即可，可参考{@link TaskOptions}
     * @param cancelToken 取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     */
    <U> ICompletionStage<U> composeApply(Function<? super T, ? extends ICompletionStage<U>> fn,
                                         int options, @Nullable ICancelToken cancelToken);

    <U> ICompletionStage<U> composeApply(Function<? super T, ? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeApplyAsync(Executor executor,
                                              Function<? super T, ? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeApplyAsync(Executor executor,
                                              Function<? super T, ? extends ICompletionStage<U>> fn,
                                              int options, @Nullable ICancelToken cancelToken);

    /**
     * 该方法表示在当前{@code Future}与返回的{@code Future}中插入一个异步操作，构建异步管道
     * 该方法对应我们日常流中使用的{@link java.util.stream.Stream#flatMap(Function)}操作。
     * <p>
     * 该方法返回一个新的{@code Future}，它的最终结果与指定的{@code Function}返回的{@code Future}结果相同。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则执行给定的操作。
     * <p>
     * {@link CompletionStage#thenCompose(Function)}
     * {@link #composeApply(Function)}
     *
     * @param options     调度选项，默认使用0即可，可参考{@link TaskOptions}
     * @param cancelToken 取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     */
    <U> ICompletionStage<U> composeCall(Supplier<? extends ICompletionStage<U>> fn,
                                        int options, @Nullable ICancelToken cancelToken);

    <U> ICompletionStage<U> composeCall(Supplier<? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeCallAsync(Executor executor,
                                             Supplier<? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeCallAsync(Executor executor,
                                             Supplier<? extends ICompletionStage<U>> fn,
                                             int options, @Nullable ICancelToken cancelToken);

    /**
     * 它表示能从从特定的异常中恢复，并异步返回一个正常结果。
     * <p>
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}正常完成，则给定的动作不会执行，且返回的{@code Future}使用相同的结果值进入完成状态。
     * 如果当前{@code Future}执行失败，则其异常信息将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     *
     * @param fallback    恢复函数
     * @param options     调度选项，默认使用0即可，可参考{@link TaskOptions}
     * @param cancelToken 取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     */
    <X extends Throwable>
    ICompletionStage<T> composeCatching(Class<X> exceptionType,
                                        Function<? super X, ? extends ICompletionStage<T>> fallback,
                                        int options, @Nullable ICancelToken cancelToken);

    <X extends Throwable>
    ICompletionStage<T> composeCatching(Class<X> exceptionType,
                                        Function<? super X, ? extends ICompletionStage<T>> fallback);

    <X extends Throwable>
    ICompletionStage<T> composeCatchingAsync(Executor executor, Class<X> exceptionType,
                                             Function<? super X, ? extends ICompletionStage<T>> fallback);

    <X extends Throwable>
    ICompletionStage<T> composeCatchingAsync(Executor executor, Class<X> exceptionType,
                                             Function<? super X, ? extends ICompletionStage<T>> fallback,
                                             int options, @Nullable ICancelToken cancelToken);

    /**
     * 它表示既能接收任务的正常结果，也可以接收任务异常结果，并异步返回一个运算结果。
     * <p>
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 不论当前{@code Future}成功还是失败，都将执行给定的操作，返回的{@code Future}的结果取决于指定操作的执行结果。
     *
     * @param options     调度选项，默认使用0即可，可参考{@link TaskOptions}
     * @param cancelToken 取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     */
    <U> ICompletionStage<U> composeHandle(BiFunction<? super T, ? super Throwable, ? extends ICompletionStage<U>> fn,
                                          int options, @Nullable ICancelToken cancelToken);

    <U> ICompletionStage<U> composeHandle(BiFunction<? super T, ? super Throwable, ? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeHandleAsync(Executor executor,
                                               BiFunction<? super T, ? super Throwable, ? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeHandleAsync(Executor executor,
                                               BiFunction<? super T, ? super Throwable, ? extends ICompletionStage<U>> fn,
                                               int options, @Nullable ICancelToken cancelToken);
    // endregion

    // region apply

    /**
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * {@link CompletionStage#thenApply(Function)}
     *
     * @param options     调度选项，默认使用0即可，可参考{@link TaskOptions}
     * @param cancelToken 取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     */
    <U> ICompletionStage<U> thenApply(Function<? super T, ? extends U> fn, int options, @Nullable ICancelToken cancelToken);

    <U> ICompletionStage<U> thenApply(Function<? super T, ? extends U> fn);

    <U> ICompletionStage<U> thenApplyAsync(Executor executor,
                                           Function<? super T, ? extends U> fn);

    <U> ICompletionStage<U> thenApplyAsync(Executor executor,
                                           Function<? super T, ? extends U> fn, int options, @Nullable ICancelToken cancelToken);

    // endregion

    // region accept

    /**
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * {@link CompletionStage#thenAccept(Consumer)}
     *
     * @param options     调度选项，默认使用0即可，可参考{@link TaskOptions}
     * @param cancelToken 取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     */
    ICompletionStage<Void> thenAccept(Consumer<? super T> action, int options, @Nullable ICancelToken cancelToken);

    ICompletionStage<Void> thenAccept(Consumer<? super T> action);

    ICompletionStage<Void> thenAcceptAsync(Executor executor,
                                           Consumer<? super T> action);

    ICompletionStage<Void> thenAcceptAsync(Executor executor,
                                           Consumer<? super T> action, int options, @Nullable ICancelToken cancelToken);

    // endregion

    // region call

    /**
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则执行给定的操作，返回的{@code Future}的结果取决于指定操作的执行结果。
     *
     * @param options     调度选项，默认使用0即可，可参考{@link TaskOptions}
     * @param cancelToken 取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     */
    <U> ICompletionStage<U> thenCall(Supplier<? extends U> fn, int options, @Nullable ICancelToken cancelToken);

    <U> ICompletionStage<U> thenCall(Supplier<? extends U> fn);

    <U> ICompletionStage<U> thenCallAsync(Executor executor,
                                          Supplier<? extends U> fn);

    <U> ICompletionStage<U> thenCallAsync(Executor executor,
                                          Supplier<? extends U> fn, int options, @Nullable ICancelToken cancelToken);
    // endregion

    // region run

    /**
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则执行给定的操作，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * {@link CompletionStage#thenRun(Runnable)}
     *
     * @param options     调度选项，默认使用0即可，可参考{@link TaskOptions}
     * @param cancelToken 取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     */
    ICompletionStage<Void> thenRun(Runnable action, int options, @Nullable ICancelToken cancelToken);

    ICompletionStage<Void> thenRun(Runnable action);

    ICompletionStage<Void> thenRunAsync(Executor executor,
                                        Runnable action);

    ICompletionStage<Void> thenRunAsync(Executor executor,
                                        Runnable action, int options, @Nullable ICancelToken cancelToken);
    // endregion

    // region catching-异常处理

    /**
     * 它表示能从从特定的异常中恢复，并返回一个正常结果。
     * <p>
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}正常完成，则给定的动作不会执行，且返回的{@code Future}使用相同的结果值进入完成状态。
     * 如果当前{@code Future}执行失败，则其异常信息将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * 不得不说JDK的{@link CompletionStage#exceptionally(Function)}这个名字太差劲了，实现的也不够好，因此我们不使用它，
     * 这里选择了Guava中的实现
     *
     * @param exceptionType 能处理的异常类型
     * @param fallback      异常恢复函数
     * @param cancelToken   取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     * @param options       调度选项，默认使用0即可，可参考{@link TaskOptions}
     */
    <X extends Throwable>
    ICompletionStage<T> catching(Class<X> exceptionType,
                                 Function<? super X, ? extends T> fallback,
                                 int options, @Nullable ICancelToken cancelToken);

    <X extends Throwable>
    ICompletionStage<T> catching(Class<X> exceptionType,
                                 Function<? super X, ? extends T> fallback);

    <X extends Throwable>
    ICompletionStage<T> catchingAsync(Executor executor, Class<X> exceptionType,
                                      Function<? super X, ? extends T> fallback);

    <X extends Throwable>
    ICompletionStage<T> catchingAsync(Executor executor, Class<X> exceptionType,
                                      Function<? super X, ? extends T> fallback,
                                      int options, @Nullable ICancelToken cancelToken);
    // endregion

    // region handle

    /**
     * 该方法表示既能处理当前计算的正常结果，又能处理当前结算的异常结果(可以将异常转换为新的结果)，并返回一个新的结果。
     * <p>
     * 该方法返回一个新的{@code Future}，无论当前{@code Future}执行成功还是失败，给定的操作都将执行。
     * 如果当前{@code Future}执行成功，而指定的动作出现异常，则返回的{@code Future}以该异常完成。
     * 如果当前{@code Future}执行失败，且指定的动作出现异常，则返回的{@code Future}以新抛出的异常进入完成状态。
     * <p>
     * {@link CompletionStage#handle(BiFunction)}
     *
     * @param options     调度选项，默认使用0即可，可参考{@link TaskOptions}
     * @param cancelToken 取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     */
    <U> ICompletionStage<U> handle(BiFunction<? super T, Throwable, ? extends U> fn,
                                   int options, @Nullable ICancelToken cancelToken);

    <U> ICompletionStage<U> handle(BiFunction<? super T, Throwable, ? extends U> fn);

    <U> ICompletionStage<U> handleAsync(Executor executor,
                                        BiFunction<? super T, Throwable, ? extends U> fn);

    <U> ICompletionStage<U> handleAsync(Executor executor,
                                        BiFunction<? super T, Throwable, ? extends U> fn,
                                        int options, @Nullable ICancelToken cancelToken);
    // endregion

    // region when-简单监听

    /**
     * 该方法返回一个新的{@code Future}，无论当前{@code Future}执行成功还是失败，给定的操作都将执行，且返回的{@code Future}始终以相同的结果进入完成状态。
     * 与方法{@link #handle(BiFunction)}不同，此方法不是为转换完成结果而设计的，因此提供的操作不应引发异常。<br>
     * 1.如果action出现了异常，则仅仅记录一个日志，不向下传播(这里与JDK实现不同) -- 应当避免抛出异常。
     * 2.如果用户主动取消了返回的Future，或者用于异步执行的Executor已关闭，则不会以相同的结果进入完成状态。
     *
     * @param options     调度选项，默认使用0即可，可参考{@link TaskOptions}
     * @param cancelToken 取消令牌，如果为null，则视为{@link ICancelToken#NONE}
     */
    ICompletionStage<T> whenComplete(BiConsumer<? super T, ? super Throwable> action,
                                     int options, @Nullable ICancelToken cancelToken);

    ICompletionStage<T> whenComplete(BiConsumer<? super T, ? super Throwable> action);

    ICompletionStage<T> whenCompleteAsync(Executor executor,
                                          BiConsumer<? super T, ? super Throwable> action);

    ICompletionStage<T> whenCompleteAsync(Executor executor,
                                          BiConsumer<? super T, ? super Throwable> action,
                                          int options, @Nullable ICancelToken cancelToken);
    // endregion

}
