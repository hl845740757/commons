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
import java.util.Collection;
import java.util.List;
import java.util.concurrent.*;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 命名：我们使用清晰的命名后缀，以免使用lambda时的语义不清。
 * 1. {@link Runnable}的方法后缀为Run - 为了避免签名上的冲突。
 * 2. {@link Consumer}的方法后缀为Action
 * 3. {@link Callable}的方法后缀为Call
 * 4. {@link Function}的方法后缀为Func
 *
 * @author wjybxx
 * date - 2024/1/9
 */
@SuppressWarnings("NullableProblems")
public interface IExecutorService extends ExecutorService, IExecutor {

    // region lifecycle

    /**
     * 查询{@link EventLoopGroup}是否处于正在关闭状态。
     * 正在关闭状态下，拒绝接收新任务，当执行完所有任务后，进入关闭状态。
     *
     * @return 如果该{@link EventLoopGroup}管理的所有{@link EventLoop}正在关闭或已关闭则返回true
     */
    boolean isShuttingDown();

    /**
     * 查询{@link EventLoopGroup}是否处于关闭状态。
     * 关闭状态下，拒绝接收新任务，执行退出前的清理操作，执行完清理操作后，进入终止状态。
     *
     * @return 如果已关闭，则返回true
     */
    @Override
    boolean isShutdown();

    /**
     * 是否已进入终止状态，一旦进入终止状态，表示生命周期真正结束。
     *
     * @return 如果已处于终止状态，则返回true
     */
    @Override
    boolean isTerminated();

    /**
     * 返回Future将在Executor终止时进入完成状态。
     * 1. 返回Future应当是只读的，{@link IFuture#asReadonly()}
     * 2. 用户可以在该Future上等待。
     */
    IFuture<?> terminationFuture();

    /**
     * 等待 ExecutorService 进入终止状态
     * 等同于在{@link #terminationFuture()}进行阻塞操作。
     *
     * @param timeout 时间度量
     * @param unit    事件单位
     * @return 在方法返回前是否已进入终止状态
     * @throws InterruptedException 如果在等待期间线程被中断，则抛出该异常。
     */
    @Override
    boolean awaitTermination(long timeout, TimeUnit unit) throws InterruptedException;

    /**
     * 请求关闭 ExecutorService，不再接收新的任务。
     * ExecutorService在执行完现有任务后，进入关闭状态。
     * 如果 ExecutorService 正在关闭，或已经关闭，则方法不产生任何效果。
     * <p>
     * 该方法会立即返回，如果想等待 ExecutorService 进入终止状态，
     * 可以使用{@link #awaitTermination(long, TimeUnit)}或{@link #terminationFuture()} 进行等待
     */
    @Override
    void shutdown();

    /**
     * JDK文档：
     * 请求关闭 ExecutorService，<b>尝试取消所有正在执行的任务，停止所有待执行的任务，并不再接收新的任务。</b>
     * 如果 ExecutorService 已经关闭，则方法不产生任何效果。
     * <p>
     * 该方法会立即返回，如果想等待 ExecutorService 进入终止状态，可以使用{@link #awaitTermination(long, TimeUnit)}
     * 或{@link #terminationFuture()} 进行等待。
     *
     * @return 被取消的任务
     */
    @Override
    List<Runnable> shutdownNow();

    // endregion

    // region submit

    /**
     * 创建一个promise以用于任务调度
     * 如果当前Executor是{@link SingleThreadExecutor}，返回的future将禁止在当前EventLoop上执行阻塞操作。
     *
     * @implNote 通常应该绑定当前executor
     */
    default <T> IPromise<T> newPromise() {
        return new Promise<>(this, null);
    }

    /**
     * 创建一个promise以用于任务调度
     * 如果当前Executor是{@link SingleThreadExecutor}，返回的future将禁止在当前EventLoop上执行阻塞操作。
     *
     * @param ctx 任务关联的上下文
     * @implNote 通常应该绑定当前executor
     */
    default <T> IPromise<T> newPromise(IContext ctx) {
        return new Promise<>(this, ctx);
    }

    default <T> IFuture<T> submit(@Nonnull TaskBuilder<T> builder) {
        PromiseTask<T> futureTask = PromiseTask.ofBuilder(builder, newPromise(builder.getCtx()));
        execute(futureTask, builder.getOptions());
        return futureTask.future();
    }

    /** {@inheritDoc} */
    @Override
    default <T> IFuture<T> submit(@Nonnull Callable<T> task) {
        PromiseTask<T> futureTask = PromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    /**
     * {@inheritDoc}
     * <p>
     * 从Java引入lambda开始，对接收函数式接口的方法进行重载时，必须要具备明显的差异才可。
     * 如果接口的差异过小，建议使用不同的方法名，而不是重载。
     *
     * @deprecated {{@link #submitRun(Runnable)}}
     */
    @Deprecated
    @Override
    default IFuture<?> submit(@Nonnull Runnable task) {
        return submitRun(task);
    }

    /** @deprecated {{@link #submitCall(Callable)}} */
    @Deprecated
    @Override
    default <T> IFuture<T> submit(@Nonnull Runnable task, T result) {
        return submitCall(Executors.callable(task, result));
    }

    default <V> IFuture<V> submitFunc(Function<? super IContext, ? extends V> task, IContext ctx) {
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, newPromise(ctx));
        execute(futureTask, 0);
        return futureTask.future();
    }

    default <V> IFuture<V> submitFunc(Function<? super IContext, ? extends V> task, IContext ctx, int options) {
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, newPromise(ctx));
        execute(futureTask, options);
        return futureTask.future();
    }

    default IFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx) {
        PromiseTask<?> futureTask = PromiseTask.ofConsumer(task, newPromise(ctx));
        execute(futureTask, 0);
        return futureTask.future();
    }

    default IFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx, int options) {
        PromiseTask<?> futureTask = PromiseTask.ofConsumer(task, newPromise(ctx));
        execute(futureTask, options);
        return futureTask.future();
    }

    default <V> IFuture<V> submitCall(Callable<? extends V> task) {
        PromiseTask<V> futureTask = PromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    default <V> IFuture<V> submitCall(Callable<? extends V> task, int options) {
        PromiseTask<V> futureTask = PromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, options);
        return futureTask.future();
    }

    default IFuture<?> submitRun(Runnable task) {
        PromiseTask<?> futureTask = PromiseTask.ofRunnable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    /** 该方法可能和{@link ExecutorService#submit(Runnable, Object)}冲突，因此我们要带后缀 */
    default IFuture<?> submitRun(Runnable task, int options) {
        PromiseTask<?> futureTask = PromiseTask.ofRunnable(task, newPromise(null));
        execute(futureTask, options);
        return futureTask.future();
    }

    // endregion

    // REGION 不建议使用的API
    // 这些API定义在这里是个错误，这是历史遗留问题--应该是最初没想好解决方案。

    @Nonnull
    @Override
    <T> List<Future<T>> invokeAll(Collection<? extends Callable<T>> tasks)
            throws InterruptedException;

    @Nonnull
    @Override
    <T> List<Future<T>> invokeAll(Collection<? extends Callable<T>> tasks, long timeout, TimeUnit unit)
            throws InterruptedException;

    @Nonnull
    @Override
    <T> T invokeAny(Collection<? extends Callable<T>> tasks)
            throws InterruptedException, ExecutionException;

    @Override
    <T> T invokeAny(Collection<? extends Callable<T>> tasks, long timeout, TimeUnit unit) throws
            InterruptedException, ExecutionException, TimeoutException;

    // ENDREGION
}
