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

package cn.wjybxx.unitask;

import cn.wjybxx.base.function.TriConsumer;
import cn.wjybxx.base.function.TriFunction;
import cn.wjybxx.concurrent.ICompletionStage;
import cn.wjybxx.concurrent.IContext;
import cn.wjybxx.concurrent.TaskOption;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.concurrent.CompletionStage;
import java.util.concurrent.Executor;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 单线程版的{@link ICompletionStage}
 *
 * <h3>Async的含义</h3>
 * 既然是单线程的，又何来异步一说？这里的异步是指不立即执行给定的行为，而是提交到Executor等待调度。<br>
 * 这有什么作用？有几个作用：
 * 1.让出CPU，避免过多的任务集中处理。
 * 2.延迟到特定阶段执行 -- 通过{@link TaskOption}指定。
 *
 * @author wjybxx
 * date - 2024/1/10
 */
@SuppressWarnings("unused")
@NotThreadSafe
public interface UniCompletionStage<T> {

    /**
     * 当前计算绑定的上下文
     * 1.在添加下游任务时，如果没有显式指定Context，将继承当前Future的上下文。
     * 2.如果用户显式指定null，将被替换为{@link IContext#NONE}。
     * 3.因此下游任务中的ctx参数永远不为null。
     */
    @Nonnull
    IContext ctx();

    /**
     * 任务绑定的Executor
     * 1.请务必确保是单线程的。
     * 2.下游自动继承当前任务的Executor。
     */
    @Nonnull
    Executor executor();

    /**
     * 返回一个Future，保持与这个Stage相同的完成结果。
     * 如果这个Stage已经是一个Future，这个方法可以返回这个Stage本身。
     * 可以返回Readonly的Future。
     */
    @Nonnull
    UniFuture<T> toFuture();

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
    <U> UniCompletionStage<U> composeApply(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn,
                                           @Nullable IContext ctx, int options);

    <U> UniCompletionStage<U> composeApply(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn);

    <U> UniCompletionStage<U> composeApplyAsync(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn);

    <U> UniCompletionStage<U> composeApplyAsync(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn,
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
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    <U> UniCompletionStage<U> composeCall(Function<? super IContext, ? extends UniCompletionStage<U>> fn,
                                          @Nullable IContext ctx, int options);

    <U> UniCompletionStage<U> composeCall(Function<? super IContext, ? extends UniCompletionStage<U>> fn);

    <U> UniCompletionStage<U> composeCallAsync(Function<? super IContext, ? extends UniCompletionStage<U>> fn);

    <U> UniCompletionStage<U> composeCallAsync(Function<? super IContext, ? extends UniCompletionStage<U>> fn,
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
    UniCompletionStage<T> composeCatching(Class<X> exceptionType,
                                          BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback,
                                          @Nullable IContext ctx, int options);

    <X extends Throwable>
    UniCompletionStage<T> composeCatching(Class<X> exceptionType,
                                          BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback);

    <X extends Throwable>
    UniCompletionStage<T> composeCatchingAsync(Class<X> exceptionType,
                                               BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback);

    <X extends Throwable>
    UniCompletionStage<T> composeCatchingAsync(Class<X> exceptionType,
                                               BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback,
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
    <U> UniCompletionStage<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn,
                                            @Nullable IContext ctx, int options);

    <U> UniCompletionStage<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn);

    <U> UniCompletionStage<U> composeHandleAsync(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn);

    <U> UniCompletionStage<U> composeHandleAsync(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn,
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
    <U> UniCompletionStage<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options);

    <U> UniCompletionStage<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn);

    <U> UniCompletionStage<U> thenApplyAsync(BiFunction<? super IContext, ? super T, ? extends U> fn);

    <U> UniCompletionStage<U> thenApplyAsync(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options);

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
    UniCompletionStage<Void> thenAccept(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options);

    UniCompletionStage<Void> thenAccept(BiConsumer<? super IContext, ? super T> action);

    UniCompletionStage<Void> thenAcceptAsync(BiConsumer<? super IContext, ? super T> action);

    UniCompletionStage<Void> thenAcceptAsync(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options);

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
    <U> UniCompletionStage<U> thenCall(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options);

    <U> UniCompletionStage<U> thenCall(Function<? super IContext, ? extends U> fn);

    <U> UniCompletionStage<U> thenCallAsync(Function<? super IContext, ? extends U> fn);

    <U> UniCompletionStage<U> thenCallAsync(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options);
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
    UniCompletionStage<Void> thenRun(Consumer<? super IContext> action, @Nullable IContext ctx, int options);

    UniCompletionStage<Void> thenRun(Consumer<? super IContext> action);

    UniCompletionStage<Void> thenRunAsync(Consumer<? super IContext> action);

    UniCompletionStage<Void> thenRunAsync(Consumer<? super IContext> action, @Nullable IContext ctx, int options);
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
    UniCompletionStage<T> catching(Class<X> exceptionType,
                                   BiFunction<? super IContext, ? super X, ? extends T> fallback,
                                   @Nullable IContext ctx, int options);

    <X extends Throwable>
    UniCompletionStage<T> catching(Class<X> exceptionType,
                                   BiFunction<? super IContext, ? super X, ? extends T> fallback);

    <X extends Throwable>
    UniCompletionStage<T> catchingAsync(Class<X> exceptionType,
                                        BiFunction<? super IContext, ? super X, ? extends T> fallback);

    <X extends Throwable>
    UniCompletionStage<T> catchingAsync(Class<X> exceptionType,
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
    <U> UniCompletionStage<U> thenHandle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn,
                                         @Nullable IContext ctx, int options);

    <U> UniCompletionStage<U> thenHandle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn);

    <U> UniCompletionStage<U> thenHandleAsync(
            TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn);

    <U> UniCompletionStage<U> thenHandleAsync(
            TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn,
            @Nullable IContext ctx, int options);
    // endregion

    // region when-简单监听

    /**
     * 该方法返回一个新的{@code Future}，无论当前{@code Future}执行成功还是失败，给定的操作都将执行，且返回的{@code Future}始终以相同的结果进入完成状态。
     * 与方法{@link #thenHandle(TriFunction)}不同，此方法不是为转换完成结果而设计的，因此提供的操作不应引发异常。<br>
     * 1.如果确实出现了异常，则仅仅记录一个日志，不向下传播(这里与JDK实现不同)。
     * 2.如果用户主动取消了返回的Future，则结果可能不同。
     * <p>
     * {@link CompletionStage#whenComplete(BiConsumer)}
     *
     * @param ctx     上下文，如果为null，则继承当前Stage的上下文
     * @param options 调度选项，默认使用0即可，可参考{@link TaskOption}
     */
    UniCompletionStage<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action,
                                       @Nullable IContext ctx, int options);

    UniCompletionStage<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action);

    UniCompletionStage<T> whenCompleteAsync(TriConsumer<? super IContext, ? super T, ? super Throwable> action);

    UniCompletionStage<T> whenCompleteAsync(TriConsumer<? super IContext, ? super T, ? super Throwable> action,
                                            @Nullable IContext ctx, int options);
    // endregion

}