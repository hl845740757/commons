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

import cn.wjybxx.base.function.TriConsumer;
import cn.wjybxx.base.function.TriFunction;

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

/**
 * <h3>线程控制</h3>
 * 虽然我在Stage上约定了记录Executor的方法，但是：Future上的监听器默认由【使Future进入完成状态的线程】通知！！！
 * 因此Stage下游的【同步任务】的执行线程是不确定的！这可能导致线程安全问题、
 * <p>
 * 如果期望控制下游任务的执行线程，请调用{@code Async}命名结尾的方法添加任务，并指定Executor；
 * 如果觉得总是通过提交任务保证线程有额外的开销，或者可能导致时序问题，可使用{@link TaskOption#STAGE_TRY_INLINE}选项，
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
 *      // 错误的方式
 *      future.composeApplyAsync(executor, (ctx, v) -> {
 *          // 在参数指定的executor线程
 *          // inExecutor(executor) == true;
 *      }), ctx, 0)
 *      .thenApply((ctx, v) -> {
 *          // 在使Future进入完成状态的线程
 *          // inExecutor(executor) == ?
 *      })
 *
 *      // 正确的方式
 *      future.composeApplyAsync(executor, (ctx, v) -> {
 *          // 在参数指定的executor线程
 *          // inExecutor(executor) == true;
 *      }), ctx, 0)
 *      .thenApplyAsync(executor, (ctx, v) -> {
 *          // 在参数指定的executor线程
 *          // inExecutor(executor) == true;
 *      },  ctx, TaskOption.STAGE_TRY_INLINE)
 * }</pre>
 *
 * <h3>行为取消</h3>
 * Stage并不直接提供删除Action的方法，要取消行为，请通过{@link IContext#cancelToken()}发起取消命令。
 * Stage会在执行用户的Action之前检查取消信号，另外用户的Action在运行的过程中也可主动检测取消信号。
 *
 * <h3>其它</h3>
 * 1.关于Future之间的聚合，见：{@link FutureCombiner}
 * 2.java参数不支持默认值，为减少方法数，我们只提供一种重载。
 * 3.大家可以先熟悉JDK的{@link CompletionStage}和{@link CompletableFuture}
 *
 * @author wjybxx
 * date - 2024/1/10
 */
@SuppressWarnings("unused")
@ThreadSafe
public interface ICompletionStage<T> {

    /**
     * 当前计算绑定的上下文
     * 1.在添加下游任务时，如果没有显式指定Context，将继承当前Future的上下文。
     * 2.因此下游任务中的ctx参数永远不为null。
     */
    @Nonnull
    IContext ctx();

    /**
     * 任务绑定的Executor
     * 1.对于异步任务，Executor是其执行线程；而对于同步任务，Executor不一定是其执行线程 -- 继承得来的而已。
     * 2.在添加下游任务时，如果没有显式指定Executor，将继承当前Future的Executor。
     * 3.Executor主要用于死锁检测，为去除{@link EventLoop}的依赖，设计了{@link SingleThreadExecutor}接口
     */
    @Nullable
    Executor executor();

    /**
     * 返回一个Future，保持与这个Stage相同的完成结果。
     * 如果这个Stage已经是一个Future，这个方法可以返回这个Stage本身。
     * 可以返回Readonly的Future。
     */
    @Nonnull
    IFuture<T> toFuture();

    /**
     * 该接口用于与依赖{@link CompletableFuture}的库进行协作
     */
    default CompletableFuture<T> toCompletableFuture() {
        return FutureUtils.toJDKFuture(this);
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
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    <U> ICompletionStage<U> composeApply(BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn,
                                         @Nullable IContext ctx, int options);

    <U> ICompletionStage<U> composeApply(BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeApplyAsync(Executor executor,
                                              BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeApplyAsync(Executor executor,
                                              BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn,
                                              @Nullable IContext ctx, int options);

    /**
     * 该方法表示在当前{@code Future}与返回的{@code Future}中插入一个异步操作，构建异步管道
     * 该方法对应我们日常流中使用的{@link java.util.stream.Stream#flatMap(Function)}操作。
     * <p>
     * 该方法返回一个新的{@code Future}，它的最终结果与指定的{@code Function}返回的{@code Future}结果相同。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数。
     * <p>
     * {@link CompletionStage#thenCompose(Function)}
     * {@link #composeApply(BiFunction)}
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文 -- 要覆盖请使用{@link IContext#NONE}
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    <U> ICompletionStage<U> composeCall(Function<? super IContext, ? extends ICompletionStage<U>> fn,
                                        @Nullable IContext ctx, int options);

    <U> ICompletionStage<U> composeCall(Function<? super IContext, ? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeCallAsync(Executor executor,
                                             Function<? super IContext, ? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeCallAsync(Executor executor,
                                             Function<? super IContext, ? extends ICompletionStage<U>> fn,
                                             @Nullable IContext ctx, int options);

    /**
     * 它表示能从从特定的异常中恢复，并异步返回一个正常结果。
     * <p>
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}正常完成，则给定的动作不会执行，且返回的{@code Future}使用相同的结果值进入完成状态。
     * 如果当前{@code Future}执行失败，则其异常信息将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     *
     * @param fallback 恢复函数
     * @param ctx      上下文，如果为null，则继承当前Stage的上下文
     * @param options  调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    <X extends Throwable>
    ICompletionStage<T> composeCatching(Class<X> exceptionType,
                                        BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback,
                                        @Nullable IContext ctx, int options);

    <X extends Throwable>
    ICompletionStage<T> composeCatching(Class<X> exceptionType,
                                        BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback);

    <X extends Throwable>
    ICompletionStage<T> composeCatchingAsync(Executor executor, Class<X> exceptionType,
                                             BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback);

    <X extends Throwable>
    ICompletionStage<T> composeCatchingAsync(Executor executor, Class<X> exceptionType,
                                             BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback,
                                             @Nullable IContext ctx, int options);

    /**
     * 它表示既能接收任务的正常结果，也可以接收任务异常结果，并异步返回一个运算结果。
     * <p>
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 不论当前{@code Future}成功还是失败，都将执行给定的操作，返回的{@code Future}的结果取决于指定操作的执行结果。
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    <U> ICompletionStage<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn,
                                          @Nullable IContext ctx, int options);

    <U> ICompletionStage<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeHandleAsync(Executor executor,
                                               TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn);

    <U> ICompletionStage<U> composeHandleAsync(Executor executor,
                                               TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn,
                                               @Nullable IContext ctx, int options);
    // endregion

    // region apply

    /**
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * {@link CompletionStage#thenApply(Function)}
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    <U> ICompletionStage<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options);

    <U> ICompletionStage<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn);

    <U> ICompletionStage<U> thenApplyAsync(Executor executor,
                                           BiFunction<? super IContext, ? super T, ? extends U> fn);

    <U> ICompletionStage<U> thenApplyAsync(Executor executor,
                                           BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options);

    // endregion

    // region accept

    /**
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则当前{@code Future}的执行结果将作为指定操作的执行参数，返回的{@code Future}的结果取决于指定操作的执行结果。
     * <p>
     * {@link CompletionStage#thenAccept(Consumer)}
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    ICompletionStage<Void> thenAccept(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options);

    ICompletionStage<Void> thenAccept(BiConsumer<? super IContext, ? super T> action);

    ICompletionStage<Void> thenAcceptAsync(Executor executor,
                                           BiConsumer<? super IContext, ? super T> action);

    ICompletionStage<Void> thenAcceptAsync(Executor executor,
                                           BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options);

    // endregion

    // region call

    /**
     * 该方法返回一个新的{@code Future}，它的结果由当前{@code Future}驱动。
     * 如果当前{@code Future}执行失败，则返回的{@code Future}将以相同的原因失败，且指定的动作不会执行。
     * 如果当前{@code Future}执行成功，则执行给定的操作，返回的{@code Future}的结果取决于指定操作的执行结果。
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    <U> ICompletionStage<U> thenCall(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options);

    <U> ICompletionStage<U> thenCall(Function<? super IContext, ? extends U> fn);

    <U> ICompletionStage<U> thenCallAsync(Executor executor,
                                          Function<? super IContext, ? extends U> fn);

    <U> ICompletionStage<U> thenCallAsync(Executor executor,
                                          Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options);
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
    ICompletionStage<Void> thenRun(Consumer<? super IContext> action, @Nullable IContext ctx, int options);

    ICompletionStage<Void> thenRun(Consumer<? super IContext> action);

    ICompletionStage<Void> thenRunAsync(Executor executor,
                                        Consumer<? super IContext> action);

    ICompletionStage<Void> thenRunAsync(Executor executor,
                                        Consumer<? super IContext> action, @Nullable IContext ctx, int options);
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
     * @param ctx           上下文，如果为null，则继承当前Stage的上下文
     * @param options       调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    <X extends Throwable>
    ICompletionStage<T> catching(Class<X> exceptionType,
                                 BiFunction<? super IContext, ? super X, ? extends T> fallback,
                                 @Nullable IContext ctx, int options);

    <X extends Throwable>
    ICompletionStage<T> catching(Class<X> exceptionType,
                                 BiFunction<? super IContext, ? super X, ? extends T> fallback);

    <X extends Throwable>
    ICompletionStage<T> catchingAsync(Executor executor, Class<X> exceptionType,
                                      BiFunction<? super IContext, ? super X, ? extends T> fallback);

    <X extends Throwable>
    ICompletionStage<T> catchingAsync(Executor executor, Class<X> exceptionType,
                                      BiFunction<? super IContext, ? super X, ? extends T> fallback,
                                      @Nullable IContext ctx, int options);
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
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    <U> ICompletionStage<U> handle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn,
                                   @Nullable IContext ctx, int options);

    <U> ICompletionStage<U> handle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn);

    <U> ICompletionStage<U> handleAsync(Executor executor,
                                        TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn);

    <U> ICompletionStage<U> handleAsync(Executor executor,
                                        TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn,
                                        @Nullable IContext ctx, int options);
    // endregion

    // region when-简单监听

    /**
     * 该方法返回一个新的{@code Future}，无论当前{@code Future}执行成功还是失败，给定的操作都将执行，且返回的{@code Future}始终以相同的结果进入完成状态。
     * 与方法{@link #handle(TriFunction)}不同，此方法不是为转换完成结果而设计的，因此提供的操作不应引发异常。<br>
     * 1.如果确实出现了异常，则仅仅记录一个日志，不向下传播(这里与JDK实现不同)。
     * 2.如果用户主动取消了返回的Future，则结果可能不同。
     * 3.异步情况下，如果目标Executor已开始关闭，则结果可能不同。
     * 4.建议whenComplete用在链的末尾，不要传递返回的Future给其它对象，否则可能导致安全问题。
     * <p>
     * {@link CompletionStage#whenComplete(BiConsumer)}
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    ICompletionStage<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action,
                                     @Nullable IContext ctx, int options);

    ICompletionStage<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action);

    ICompletionStage<T> whenCompleteAsync(Executor executor,
                                          TriConsumer<? super IContext, ? super T, ? super Throwable> action);

    ICompletionStage<T> whenCompleteAsync(Executor executor,
                                          TriConsumer<? super IContext, ? super T, ? super Throwable> action,
                                          @Nullable IContext ctx, int options);
    // endregion

}