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

import cn.wjybxx.base.time.TimeProvider;
import cn.wjybxx.common.concurrent.FutureUtils;
import cn.wjybxx.common.concurrent.IContext;
import cn.wjybxx.common.concurrent.IExecutor;

import java.util.concurrent.Callable;
import java.util.concurrent.Executor;
import java.util.concurrent.TimeUnit;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class SameThreads {

    // region 异常处理

    // endregion

    public static <V> UniPromise<V> newPromise(Executor executor) {
        return new UniPromise<>(executor);
    }

    public static <V> UniPromise<V> newPromise(Executor executor, IContext ctx) {
        return new UniPromise<>(executor, ctx);
    }

    public static UniExecutorService newExecutor() {
        return new DefaultUniExecutor();
    }

    /**
     * @param countLimit 每帧允许运行的最大任务数，-1表示不限制；不可以为0
     * @param timeLimit  每帧允许的最大时间，-1表示不限制；不可以为0
     */
    public static UniExecutorService newExecutor(int countLimit, long timeLimit, TimeUnit timeUnit) {
        return new DefaultUniExecutor(countLimit, timeLimit, timeUnit);
    }

    /**
     * 返回的{@link UniScheduledExecutor#tick()}默认不执行tick过程中新增加的任务
     *
     * @param timeProvider 用于调度器获取当前时间
     */
    public static UniScheduledExecutor newScheduledExecutor(TimeProvider timeProvider) {
        return new UniScheduledExecutorImpl(timeProvider);
    }

    public static UniScheduledExecutor newScheduledExecutor(TimeProvider timeProvider, int initCapacity) {
        return new UniScheduledExecutorImpl(timeProvider, initCapacity);
    }

    // endregion

    // region submit

    public static <V> UniFuture<V> submitFunc(Executor executor, Function<? super IContext, V> task, IContext ctx) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofFunction(task, newPromise(executor, ctx));
        executor.execute(futureTask);
        return futureTask.future();
    }

    public static <V> UniFuture<V> submitFunc(IExecutor executor, Function<? super IContext, V> task, IContext ctx, int options) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofFunction(task, newPromise(executor, ctx));
        executor.execute(futureTask, options);
        return futureTask.future();
    }

    public static UniFuture<?> submitAction(Executor executor, Consumer<? super IContext> task, IContext ctx) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofConsumer(task, newPromise(executor, ctx));
        executor.execute(futureTask);
        return futureTask.future();
    }

    public static UniFuture<?> submitAction(IExecutor executor, Consumer<? super IContext> task, IContext ctx, int options) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofConsumer(task, newPromise(executor, ctx));
        executor.execute(futureTask, options);
        return futureTask.future();
    }

    public static <V> UniFuture<V> submitCall(Executor executor, Callable<V> task) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofCallable(task, newPromise(executor));
        executor.execute(futureTask);
        return futureTask.future();
    }

    public static <V> UniFuture<V> submitCall(IExecutor executor, Callable<V> task, int options) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofCallable(task, newPromise(executor));
        executor.execute(futureTask, options);
        return futureTask.future();
    }

    public static UniFuture<?> submitRun(Executor executor, Runnable action) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofRunnable(action, newPromise(executor));
        executor.execute(futureTask);
        return futureTask.future();
    }

    public static UniFuture<?> submitRun(IExecutor executor, Runnable action, int options) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofRunnable(action, newPromise(executor));
        executor.execute(futureTask, options);
        return futureTask.future();
    }

    public static void execute(Executor executor, Consumer<? super IContext> action, IContext ctx) {
        executor.execute(FutureUtils.toRunnable(action, ctx));
    }

    public static void execute(IExecutor executor, Consumer<? super IContext> action, IContext ctx, int options) {
        executor.execute(FutureUtils.toRunnable(action, ctx), options);
    }

    // endregion

}