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
package cn.wjybxx.common.concurrent2;

import cn.wjybxx.common.concurrent.EventLoop;
import cn.wjybxx.common.concurrent.SingleThreadExecutor;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.concurrent.*;
import java.util.function.BiConsumer;
import java.util.function.Consumer;

/**
 * @author wjybxx
 * date - 2023/11/6
 */
public interface IFuture<T> extends Future<T> {

    /**
     * 任务绑定的上下文
     * 1.用户传入的所有信息最好都能通过future查询，我们将其集中到Ctx上。
     * 2.定时任务的信息默认不放在ctx中，也不在Future上提供接口，以减少不必要的开销、
     */
    IContext ctx();

    /**
     * 任务绑定的Executor
     * 1.可能为null，表示未指定
     * 2.Executor主要用于死锁检测，为去除{@link EventLoop}的依赖，设计了{@link SingleThreadExecutor}接口
     */
    @Nullable
    Executor executor();

    /**
     * 返回只读的Future接口，
     * 如果Future是一个提供了写接口的Promise，则返回一个只读的Future视图，返回的实例会在当前Promise进入完成状态时进入完成状态。
     * <p>
     * 1. 一般情况下我们通过接口隔离即可达到读写分离目的，这可以节省开销；但如果觉得返回Promise实例给任务的发起者不够安全，可创建Promise的只读视图返回给用户
     * 2. 这里不要求返回的必须是同一个实例，每次都可以创建一个新的实例。
     */
    IFuture<T> asReadonly();

    /** @param ctx 是否也转换ctx */
    IFuture<T> asReadonly(boolean ctx);

    /**
     * 尝试取消任务
     * 如果任务尚未开始执行，则取消任务并返回true；
     * 如果任务处于计算中，则取消失败；
     * 如果任务已被取消，则返回true；
     * 如果任务已成功或失败，则返回false。
     * <p>
     * 关于取消：
     * 1.取消应当是协作式的，而不是强制的，因此不能要求取消信号立即被响应 -- jdk的取消约定其实是强制的。
     * 2.通过Future取消只能取消Future关联的任务，而不能取消一组相关的任务。
     * 3.建议通过ctx发起取消请求，该方法仅用于和旧代码和外部库交互。
     */
    @Override
    boolean cancel(boolean mayInterruptIfRunning);

    /** 转换为JDK的Future，以和外部库协作 */
    CompletableFuture<T> toCompletableFuture();

    // region 状态查询

    @Override
    State state();

    /**
     * 如果future关联的任务仍处于等待执行的状态，则返回true
     * （换句话说，如果任务仍在排队，则返回true）
     */
    boolean isPending();

    /**
     * 如果future关联的任务正在执行中，则返回true
     * <p>
     * JDK设定的任务状态{@link State}并未将【等待中】和【执行中】这两种状态分开，这其实是不好的 -- 这可能有兼容性问题。
     * 我本想增加自己的枚举，但增加另一个枚举会让使用者更加糊涂，因此在枚举上我们不区分，但提供更细的查询方法。
     */
    boolean isComputing();

    /** {@inheritDoc} */
    @Override
    default boolean isDone() {
        return state() != State.RUNNING;
    }

    /** {@inheritDoc} */
    @Override
    default boolean isCancelled() {
        return state() == State.CANCELLED;
    }

    /**
     * 如果future已进入完成状态，且是成功完成，则返回true。
     */
    default boolean isSucceeded() {
        return state() == State.SUCCESS;
    }

    /**
     * 如果future已进入完成状态，且是失败状态，则返回true
     */
    default boolean isFailed() {
        return state() == State.FAILED;
    }

    /**
     * 在JDK的约定中，取消和failed是分离的，我们扔保持这样的约定；
     * 但有些时候，我们需要将取消也视为失败的一种，因此需要快捷的方法。
     */
    default boolean isFailedOrCancelled() {
        State state = state();
        return state == State.FAILED
                || state == State.CANCELLED;
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
     * @return 如果当前future已进入完成状态，则执行给定的action且返回true
     */
    boolean acceptNow(BiConsumer<? super T, ? super Throwable> action);

    /** {@inheritDoc} */
    @Override
    default Throwable exceptionNow() {
        return exceptionNow(true); // 默认值为true，以兼容JDK
    }

    /** 任务被取消时返回{@link CancellationException} */
    default Throwable exceptionNow2() {
        return exceptionNow(false);
    }

    /**
     * JDK的Future总是特殊对待取消，有时候我们并不希望如此
     *
     * @param throwIfCancelled 任务取消的状态下是否抛出状态异常，用于兼容JDK
     */
    Throwable exceptionNow(boolean throwIfCancelled);

    // endregion

    // region 阻塞查询和等待

    @Override
    T get() throws InterruptedException, ExecutionException;

    @Override
    T get(long timeout, @Nonnull TimeUnit unit) throws InterruptedException, ExecutionException, TimeoutException;

    /** @return 如果任务在这期间进入了完成状态，则返回true */
    boolean await(long timeout, TimeUnit unit) throws InterruptedException;

    /** @return 如果任务在这期间进入了完成状态，则返回true */
    boolean awaitUninterruptedly(long timeout, TimeUnit unit);

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
    IFuture<T> awaitUninterruptedly();

    /**
     * 阻塞到任务完成
     *
     * @throws CompletionException   计算失败
     * @throws CancellationException 被取消
     */
    T join();

    // endregion

    // region 链式管道


    // endregion

    // region 普通广播
    // 暂不设定返回会为this，以免以后需要封装用于删除的句柄

    /**
     * 给定的Action将在Future关联的任务成功完成时执行
     */
    void onSucceeded(Consumer<? super T> action);

    /**
     * 给定的Action将在Future关联的任务完成时执行，无论成功或失败都将执行
     */
    void onComplete(BiConsumer<? super T, ? super Throwable> action);

    void onSucceededAsync(Consumer<? super T> action, Executor executor);

    void onCompleteAsync(BiConsumer<? super T, ? super Throwable> action, Executor executor);
    // endregion

}