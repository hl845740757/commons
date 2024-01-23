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
import cn.wjybxx.concurrent.*;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.concurrent.CancellationException;
import java.util.concurrent.CompletionException;
import java.util.concurrent.ExecutionException;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 单线程版的{@link IFuture}
 * 1. 由于涉及到readonly问题，该抽象不能省去。
 * 2. 该接口中的方法尽量保持和{@link IFuture}相同，更易用。
 * <p>
 * 关于命名：单线程的Future我也写过几版了，但一直没有找到简短又表意清晰的名字，由于客户端开发会用到 UniTask，我决定放弃思考，就用这个名。。。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public interface UniFuture<T> extends UniCompletionStage<T> {

    /**
     * 返回只读的Future视图，
     * 如果Future是一个提供了写接口的Promise，则返回一个只读的Future视图，返回的实例会在当前Promise进入完成状态时进入完成状态。
     * <p>
     * 1. 一般情况下我们通过接口隔离即可达到读写分离目的，这可以节省开销；在大规模链式调用的情况下，Promise继承Future很有效。
     * 2. 但如果觉得返回Promise实例给任务的发起者不够安全，可创建Promise的只读视图返回给用户
     * 3. 这里不要求返回的必须是同一个实例，每次都可以创建一个新的实例。
     */
    UniFuture<T> asReadonly();

    // region 状态查询

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
    default boolean isDone() {
        return futureState().isDone();
    }

    /** 如果future关联的任务在正常完成被取消，则返回true。 */
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
     * {@link IFuture#resultNow()}
     *
     * @throws IllegalStateException 如果任务不是成功完成状态
     */
    T resultNow();

    /**
     * 非阻塞方式获取导致Future失败的原因，不适用被取消的Future；
     * 如果需要获取取消异常，可使用{@link #exceptionNow(boolean)}。<p>
     * {@link IFuture#exceptionNow()}
     *
     * @throws IllegalStateException 如果任务不是失败完成状态
     */
    default Throwable exceptionNow() {
        return exceptionNow(true); // true以兼容jdk
    }

    /**
     * 获取导致任务失败的异常，可获取取消异常
     * * {@link IFuture#exceptionNow(boolean)}
     *
     * @param throwIfCancelled 任务取消的状态下是否抛出状态异常
     */
    Throwable exceptionNow(boolean throwIfCancelled);

    // endregion

    // region 阻塞查询和等待

    /**
     * 如果任务已完成，则立即返回结果，否则抛出异常。
     *
     * @throws ExecutionException         计算失败
     * @throws CancellationException      被取消
     * @throws BlockingOperationException 任务未完成
     */
    T get() throws ExecutionException;

    /**
     * 如果任务已完成，则立即返回结果，否则抛出异常。
     *
     * @throws CompletionException        计算失败
     * @throws CancellationException      被取消
     * @throws BlockingOperationException 任务未完成
     */
    T join();

    // endregion

    // region 其它

    /**
     * 将当前future的结果传输到目标promise
     * 如果当前future已完成，且目标promise尚未完成，则尝试传输结果到promise
     * <p>
     * {@link IPromise#tryTransferFrom(IFuture)}
     *
     * @return 当且仅当future使目标promise进入完成状态时返回true。
     */
    default boolean tryTransferTo(UniPromise<T> output) {
        return output.tryTransferFrom(this);
    }

    /**
     * 1. 给定的Action将在Future关联的任务完成时执行，无论成功或失败都将执行。
     * 2. 该操作不是链式调用，不会继承上下文！不会继承上下文！不会继承上下文！
     * 3. 通常只用于一些特殊功能 -- 比如大规模监听时减少开销。
     * 4. 暂不设定返回会为this，以免以后需要封装用于删除的句柄等
     *
     * @param context 如果是有效的上下文，执行前会检测取消信号
     * @param options 调度选项，可为0
     */
    void onCompleted(BiConsumer<? super IContext, ? super UniFuture<T>> action, @Nonnull IContext context, int options);

    void onCompletedAsync(BiConsumer<? super IContext, ? super UniFuture<T>> action, @Nonnull IContext context, int options);

    /**
     * 最原始的Future监听接口
     * 该接口在{@link #onCompleted(BiConsumer, IContext, int)}的基础上减少一些开销
     */
    void onCompleted(Consumer<? super UniFuture<T>> action, int options);

    void onCompletedAsync(Consumer<? super UniFuture<T>> action, int options);
    // endregion

    // region 重写签名

    @Override
    <U> UniFuture<U> composeApply(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> composeApply(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn);

    @Override
    <U> UniFuture<U> composeApplyAsync(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn);

    @Override
    <U> UniFuture<U> composeApplyAsync(BiFunction<? super IContext, ? super T, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> composeCall(Function<? super IContext, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> composeCall(Function<? super IContext, ? extends UniCompletionStage<U>> fn);

    @Override
    <U> UniFuture<U> composeCallAsync(Function<? super IContext, ? extends UniCompletionStage<U>> fn);

    @Override
    <U> UniFuture<U> composeCallAsync(Function<? super IContext, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <X extends Throwable> UniFuture<T> composeCatching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback, @Nullable IContext ctx, int options);

    @Override
    <X extends Throwable> UniFuture<T> composeCatching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback);

    @Override
    <X extends Throwable> UniFuture<T> composeCatchingAsync(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback);

    @Override
    <X extends Throwable> UniFuture<T> composeCatchingAsync(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends UniCompletionStage<T>> fallback, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> composeHandle(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn);

    @Override
    <U> UniFuture<U> composeHandleAsync(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn);

    @Override
    <U> UniFuture<U> composeHandleAsync(TriFunction<? super IContext, ? super T, ? super Throwable, ? extends UniCompletionStage<U>> fn, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> thenApply(BiFunction<? super IContext, ? super T, ? extends U> fn);

    @Override
    <U> UniFuture<U> thenApplyAsync(BiFunction<? super IContext, ? super T, ? extends U> fn);

    @Override
    <U> UniFuture<U> thenApplyAsync(BiFunction<? super IContext, ? super T, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    UniFuture<Void> thenAccept(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options);

    @Override
    UniFuture<Void> thenAccept(BiConsumer<? super IContext, ? super T> action);

    @Override
    UniFuture<Void> thenAcceptAsync(BiConsumer<? super IContext, ? super T> action);

    @Override
    UniFuture<Void> thenAcceptAsync(BiConsumer<? super IContext, ? super T> action, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> thenCall(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> thenCall(Function<? super IContext, ? extends U> fn);

    @Override
    <U> UniFuture<U> thenCallAsync(Function<? super IContext, ? extends U> fn);

    @Override
    <U> UniFuture<U> thenCallAsync(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    UniFuture<Void> thenRun(Consumer<? super IContext> action, @Nullable IContext ctx, int options);

    @Override
    UniFuture<Void> thenRun(Consumer<? super IContext> action);

    @Override
    UniFuture<Void> thenRunAsync(Consumer<? super IContext> action);

    @Override
    UniFuture<Void> thenRunAsync(Consumer<? super IContext> action, @Nullable IContext ctx, int options);

    @Override
    <X extends Throwable> UniFuture<T> catching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback, @Nullable IContext ctx, int options);

    @Override
    <X extends Throwable> UniFuture<T> catching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback);

    @Override
    <X extends Throwable> UniFuture<T> catchingAsync(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback);

    @Override
    <X extends Throwable> UniFuture<T> catchingAsync(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends T> fallback, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> handle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    <U> UniFuture<U> handle(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn);

    @Override
    <U> UniFuture<U> handleAsync(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn);

    @Override
    <U> UniFuture<U> handleAsync(TriFunction<? super IContext, ? super T, Throwable, ? extends U> fn, @Nullable IContext ctx, int options);

    @Override
    UniFuture<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action, @Nullable IContext ctx, int options);

    @Override
    UniFuture<T> whenComplete(TriConsumer<? super IContext, ? super T, ? super Throwable> action);

    @Override
    UniFuture<T> whenCompleteAsync(TriConsumer<? super IContext, ? super T, ? super Throwable> action);

    @Override
    UniFuture<T> whenCompleteAsync(TriConsumer<? super IContext, ? super T, ? super Throwable> action, @Nullable IContext ctx, int options);

    // endregion
}