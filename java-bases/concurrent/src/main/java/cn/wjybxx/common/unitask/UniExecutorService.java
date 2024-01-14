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

package cn.wjybxx.common.unitask;


import cn.wjybxx.common.concurrent.*;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.List;
import java.util.concurrent.Callable;
import java.util.concurrent.ExecutorService;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 用于在当前线程延迟执行任务的Executor -- {@link IExecutorService}。
 * 即：该Executor仍然在当前线程（提交任务的线程）执行提交的任务，只是会延迟执行。
 * <h3>时序要求</h3>
 * 我们限定逻辑是在当前线程执行的，必须保证先提交的任务先执行。
 * <h3>限制单帧任务数</h3>
 * 由于是在当前线程执行对应的逻辑，因而必须限制单帧执行的任务数，以避免占用过多的资源，同时，限定单帧任务数可避免死循环。
 * <h3>外部驱动</h3>
 * 由于仍然是在当前线程执行，因此需要外部进行驱动，外部需要定时调用{@link #update()}
 * <h3>指定执行阶段</h3>
 * 如果Executor支持在特定的阶段执行给定的任务，需要响应{@link TaskOption#MASK_SCHEDULE_PHASE}指定的阶段。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public interface UniExecutorService extends IExecutor {

    /**
     * 心跳方法
     * 外部需要每一帧调用该方法以执行任务。
     */
    void update();

    /**
     * 为避免死循环或占用过多cpu，单次{@link #update()}可能存在一些限制，因此可能未执行所有的可执行任务。
     * 该方法用于探测是否还有可执行的任务，如果外部可以分配更多的资源。
     *
     * @return 如果还有可执行任务则返回true，否则返回false
     */
    boolean needMoreTicks();

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
    boolean isShutdown();

    /**
     * 是否已进入终止状态，一旦进入终止状态，表示生命周期真正结束。
     *
     * @return 如果已处于终止状态，则返回true
     */
    boolean isTerminated();

    /**
     * 返回Future将在Executor终止时进入完成状态。
     * 1. 返回Future应当是只读的，{@link UniFuture#asReadonly()}
     * 2. 用户可以在该Future上等待。
     */
    UniFuture<?> terminationFuture();

    /**
     * 请求关闭 ExecutorService，不再接收新的任务。
     * ExecutorService在执行完现有任务后，进入关闭状态。
     * 如果 ExecutorService 正在关闭，或已经关闭，则方法不产生任何效果。
     * <p>
     * 该方法会立即返回，如果想等待 ExecutorService 进入终止状态，
     * 可以通过{@link #terminationFuture()}监听进入完成状态事件。
     */
    void shutdown();

    /**
     * JDK文档：
     * 请求关闭 ExecutorService，<b>尝试取消所有正在执行的任务，停止所有待执行的任务，并不再接收新的任务。</b>
     * 如果 ExecutorService 已经关闭，则方法不产生任何效果。
     * <p>
     * 该方法会立即返回，如果想等待 ExecutorService 进入终止状态，
     * 可以通过{@link #terminationFuture()}监听进入完成状态事件。
     *
     * @return 被取消的任务
     */
    List<Runnable> shutdownNow();

    // endregion

    // region submit

    /**
     * 创建一个promise以用于任务调度
     * 如果当前Executor是{@link SingleThreadExecutor}，返回的future将禁止在当前EventLoop上执行阻塞操作。
     *
     * @param ctx 任务关联的上下文
     * @implNote 通常应该绑定当前executor
     */
    default <V> UniPromise<V> newPromise(IContext ctx) {
        return new UniPromise<>(this, ctx);
    }

    /**
     * 创建一个promise以用于任务调度
     * 如果当前Executor是{@link SingleThreadExecutor}，返回的future将禁止在当前EventLoop上执行阻塞操作。
     *
     * @implNote 通常应该绑定当前executor
     */
    default <V> UniPromise<V> newPromise() {
        return new UniPromise<>(this, null);
    }

    default <V> UniFuture<V> submit(@Nonnull TaskBuilder<V> builder) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofBuilder(builder, newPromise(builder.getCtx()));
        execute(futureTask, builder.getOptions());
        return futureTask.future();
    }

    default <T> UniFuture<T> submit(Callable<T> task) {
        UniPromiseTask<T> futureTask = UniPromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    default <V> UniFuture<V> submitFunc(Function<? super IContext, V> task, IContext ctx) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofFunction(task, newPromise(ctx));
        execute(futureTask, 0);
        return futureTask.future();
    }

    default <V> UniFuture<V> submitFunc(Function<? super IContext, V> task, IContext ctx, int options) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofFunction(task, newPromise(ctx));
        execute(futureTask, options);
        return futureTask.future();
    }

    default UniFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofConsumer(task, newPromise(ctx));
        execute(futureTask, 0);
        return futureTask.future();
    }

    default UniFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx, int options) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofConsumer(task, newPromise(ctx));
        execute(futureTask, options);
        return futureTask.future();
    }

    default <V> UniFuture<V> submitCall(Callable<V> task) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    default <V> UniFuture<V> submitCall(Callable<V> task, int options) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, options);
        return futureTask.future();
    }

    default UniFuture<?> submitRun(Runnable task) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofRunnable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    /** 该方法可能和{@link ExecutorService#submit(Runnable, Object)}冲突，因此我们要带后缀 */
    default UniFuture<?> submitRun(Runnable task, int options) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofRunnable(task, newPromise(null));
        execute(futureTask, options);
        return futureTask.future();
    }

    // endregion

}