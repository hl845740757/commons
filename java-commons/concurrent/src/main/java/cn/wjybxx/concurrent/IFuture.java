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
import java.util.concurrent.*;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 一定要阅读{@link ICompletionStage}中关于上下文和线程控制的说明、
 *
 * @author wjybxx
 * date - 2023/11/6
 */
@ThreadSafe
public interface IFuture<T> extends Future<T>, ICompletionStage<T> {

    /**
     * 返回只读的Future视图，
     * 如果Future是一个提供了写接口的Promise，则返回一个只读的Future视图，返回的实例会在当前Promise进入完成状态时进入完成状态。
     * <p>
     * 1. 一般情况下我们通过接口隔离即可达到读写分离目的，这可以节省开销；在大规模链式调用的情况下，Promise继承Future很有效。
     * 2. 但如果觉得返回Promise实例给任务的发起者不够安全，可创建Promise的只读视图返回给用户
     * 3. 这里不要求返回的必须是同一个实例，每次都可以创建一个新的实例。
     */
    IFuture<T> asReadonly();

    /**
     * {@inheritDoc}
     *
     * @param mayInterruptIfRunning 在链式调用下没有意义
     * @deprecated 通过ctx中的 {@link ICancelToken} 发起取消请求，该方法仅用于和旧代码和外部库交互。
     */
    @Deprecated
    @Override
    boolean cancel(boolean mayInterruptIfRunning);

    // region 状态查询
    @Override
    State state();

    /** 获取更为详细的future枚举值 */
    FutureState futureState();

    /**
     * 如果future关联的任务仍处于等待执行的状态，则返回true
     * （换句话说，如果任务仍在排队，则返回true）
     */
    default boolean isPending() {
        return futureState() == FutureState.PENDING;
    }

    /** 如果future关联的任务正在执行中，则返回true */
    default boolean isComputing() {
        return futureState() == FutureState.COMPUTING;
    }

    /** 如果future已进入完成状态(成功、失败、被取消)，则返回true */
    @Override
    default boolean isDone() {
        return futureState().isDone();
    }

    /** 如果future关联的任务在正常完成被取消，则返回true。 */
    @Override
    default boolean isCancelled() {
        return futureState() == FutureState.CANCELLED;
    }

    /** 如果future已进入完成状态，且是成功完成，则返回true。 */
    default boolean isSucceeded() {
        return futureState() == FutureState.SUCCESS;
    }

    /** 如果future已进入完成状态，且是失败状态，则返回true */
    default boolean isFailed() {
        return futureState() == FutureState.FAILED;
    }

    /**
     * 在JDK的约定中，取消和failed是分离的，我们仍保持这样的约定；
     * 但有些时候，我们需要将取消也视为失败的一种，因此需要快捷的方法。
     */
    default boolean isFailedOrCancelled() {
        return futureState().isFailedOrCancelled();
    }

    // endregion

    // region 非阻塞查询

    /**
     * 获取关联的计算结果 -- 非阻塞。
     * 如果对应的计算失败，则抛出对应的异常。
     * 如果计算成功，则返回计算结果。
     * 如果计算尚未完成，则返回null。
     * <p>
     * 如果future关联的task没有返回值(操作完成返回null)，对于这种情况，你可以使用{@link #isSucceeded()}作为判断任务是否成功执行的更好选择。
     *
     * @throws CompletionException   计算失败
     * @throws CancellationException 被取消
     */
    default T getNow() {
        return getNow(null);
    }

    /**
     * 尝试获取计算结果 -- 非阻塞
     * 如果对应的计算失败，则抛出对应的异常。
     * 如果计算成功，则返回计算结果。
     * 如果计算尚未完成，则返回给定值。
     *
     * @param valueIfAbsent 计算尚未完成时的返回值
     * @throws CompletionException   计算失败
     * @throws CancellationException 被取消
     */
    T getNow(T valueIfAbsent);

    /**
     * 非阻塞方式获取Future的执行结果<p>
     * {@inheritDoc}
     *
     * @throws IllegalStateException 如果任务不是成功完成状态
     */
    @Override
    T resultNow();

    /**
     * 非阻塞方式获取导致Future失败的原因，不适用被取消的Future；
     * 如果需要获取取消异常，可使用{@link #exceptionNow(boolean)}。
     * <p>
     * {@inheritDoc}
     *
     * @throws IllegalStateException 如果任务不是失败完成状态
     */
    @Override
    default Throwable exceptionNow() {
        return exceptionNow(true); // true以兼容jdk
    }

    /**
     * 获取导致任务失败的异常，可获取取消异常
     *
     * @param throwIfCancelled 任务取消的状态下是否抛出状态异常
     */
    Throwable exceptionNow(boolean throwIfCancelled);

    // endregion

    // region 阻塞查询和等待

    /** {@inheritDoc} */
    @Override
    T get() throws InterruptedException, ExecutionException;

    /** {@inheritDoc} */
    @Override
    T get(long timeout, @Nonnull TimeUnit unit) throws InterruptedException, ExecutionException, TimeoutException;

    /**
     * 阻塞到任务完成
     *
     * @throws CompletionException   计算失败
     * @throws CancellationException 被取消
     */
    T join();

    /** @return 如果任务在这期间进入了完成状态，则返回true */
    boolean await(long timeout, TimeUnit unit) throws InterruptedException;

    /** @return 如果任务在这期间进入了完成状态，则返回true */
    boolean awaitUninterruptibly(long timeout, TimeUnit unit);

    /**
     * 阻塞到任务完成
     *
     * @return this
     */
    IFuture<T> await() throws InterruptedException;

    /**
     * 阻塞到任务完成
     *
     * @return this
     */
    IFuture<T> awaitUninterruptibly();

    // endregion

    // region 其它

    /**
     * 1. 给定的Action将在Future关联的任务完成时执行，无论成功或失败都将执行。
     * 2. 该操作不是链式调用，不会继承上下文！不会继承上下文！不会继承上下文！
     * 3. 通常只用于一些特殊功能 -- 比如大规模监听时减少开销。
     * 4. 暂不设定返回会为this，以免以后需要封装用于删除的句柄等
     *
     * @param context 如果是有效的上下文，执行前会检测取消信号
     * @param options 调度选项，可为0
     */
    void onCompleted(BiConsumer<? super IContext, ? super IFuture<T>> action, @Nonnull IContext context, int options);

    void onCompleted(BiConsumer<? super IContext, ? super IFuture<T>> action, @Nonnull IContext context);

    void onCompletedAsync(Executor executor,
                          BiConsumer<? super IContext, ? super IFuture<T>> action, @Nonnull IContext context);

    void onCompletedAsync(Executor executor,
                          BiConsumer<? super IContext, ? super IFuture<T>> action, @Nonnull IContext context, int options);

    /**
     * 最原始的Future监听接口
     * 该接口在{@link #onCompleted(BiConsumer, IContext, int)}的基础上减少一些开销，不支持ctx参数
     */
    void onCompleted(Consumer<? super IFuture<T>> action, int options);

    void onCompleted(Consumer<? super IFuture<T>> action);

    void onCompletedAsync(Executor executor, Consumer<? super IFuture<T>> action);

    void onCompletedAsync(Executor executor, Consumer<? super IFuture<T>> action, int options);

    // endregion

    // region 重写签名

    @Override
    <U> IFuture<U> composeApply(BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> composeApply(BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn);

    @Override
    <U> IFuture<U> composeApplyAsync(Executor executor, BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn);

    @Override
    <U> IFuture<U> composeApplyAsync(Executor executor, BiFunction<? super IContext, ? super T, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> composeCall(Function<? super IContext, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> composeCall(Function<? super IContext, ? extends ICompletionStage<U>> fn);

    @Override
    <U> IFuture<U> composeCallAsync(Executor executor, Function<? super IContext, ? extends ICompletionStage<U>> fn);

    @Override
    <U> IFuture<U> composeCallAsync(Executor executor, Function<? super IContext, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <X extends Throwable> IFuture<T> composeCatching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback, @Nullable IContext ctx, int options);

    @Override
    <X extends Throwable> IFuture<T> composeCatching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback);

    @Override
    <X extends Throwable> IFuture<T> composeCatchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback);

    @Override
    <X extends Throwable> IFuture<T> composeCatchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<T>> fallback, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn);

    @Override
    <U> IFuture<U> composeHandleAsync(Executor executor, TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn);

    @Override
    <U> IFuture<U> composeHandleAsync(Executor executor, TriFunction<? super IContext, ? super T, ? super Throwable, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn);

    @Override
    <U> IFuture<U> thenApplyAsync(Executor executor, BiFunction<? super IContext, ? super T, ? extends U> fn);

    @Override
    <U> IFuture<U> thenApplyAsync(Executor executor, BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    IFuture<Void> thenAccept(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options);

    @Override
    IFuture<Void> thenAccept(BiConsumer<? super IContext, ? super T> action);

    @Override
    IFuture<Void> thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super T> action);

    @Override
    IFuture<Void> thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> thenCall(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> thenCall(Function<? super IContext, ? extends U> fn);

    @Override
    <U> IFuture<U> thenCallAsync(Executor executor, Function<? super IContext, ? extends U> fn);

    @Override
    <U> IFuture<U> thenCallAsync(Executor executor, Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    IFuture<Void> thenRun(Consumer<? super IContext> action, @Nullable IContext ctx, int options);

    @Override
    IFuture<Void> thenRun(Consumer<? super IContext> action);

    @Override
    IFuture<Void> thenRunAsync(Executor executor, Consumer<? super IContext> action);

    @Override
    IFuture<Void> thenRunAsync(Executor executor, Consumer<? super IContext> action, @Nullable IContext ctx, int options);

    @Override
    <X extends Throwable> IFuture<T> catching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback, @Nullable IContext ctx, int options);

    @Override
    <X extends Throwable> IFuture<T> catching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback);

    @Override
    <X extends Throwable> IFuture<T> catchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback);

    @Override
    <X extends Throwable> IFuture<T> catchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> handle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    <U> IFuture<U> handle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn);

    @Override
    <U> IFuture<U> handleAsync(Executor executor, TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn);

    @Override
    <U> IFuture<U> handleAsync(Executor executor, TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    IFuture<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action, @Nullable IContext ctx, int options);

    @Override
    IFuture<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action);

    @Override
    IFuture<T> whenCompleteAsync(Executor executor, TriConsumer<? super IContext, ? super T, ? super Throwable> action);

    @Override
    IFuture<T> whenCompleteAsync(Executor executor, TriConsumer<? super IContext, ? super T, ? super Throwable> action, @Nullable IContext ctx, int options);


    // endregion
}