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

package cn.wjybxx.sequential;

import cn.wjybxx.base.time.TimeProvider;
import cn.wjybxx.concurrent.*;

import javax.annotation.Nonnull;
import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.concurrent.Executor;
import java.util.concurrent.TimeUnit;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class UniFutureUtils {

    // region factory

    public static <V> UniPromise<V> newPromise() {
        return new UniPromise<>();
    }

    public static <V> UniPromise<V> newPromise(Executor executor) {
        return new UniPromise<>(executor);
    }

    public static <V> IFuture<V> completedFuture(V result) {
        return UniPromise.completedPromise(result);
    }

    public static <V> IFuture<V> completedFuture(V result, Executor executor) {
        return UniPromise.completedPromise(result, executor);
    }

    public static <V> IFuture<V> failedFuture(Throwable ex) {
        return UniPromise.failedPromise(ex);
    }

    public static <V> IFuture<V> failedFuture(Throwable ex, Executor executor) {
        return UniPromise.failedPromise(ex, executor);
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
     * 返回的{@link UniScheduledExecutor#update()}默认不执行tick过程中新增加的任务
     *
     * @param timeProvider 用于调度器获取当前时间
     */
    public static UniScheduledExecutor newScheduledExecutor(TimeProvider timeProvider) {
        return new DefaultUniScheduledExecutor(timeProvider);
    }

    public static UniScheduledExecutor newScheduledExecutor(TimeProvider timeProvider, int initCapacity) {
        return new DefaultUniScheduledExecutor(timeProvider, initCapacity);
    }

    // endregion

    // region submit

    // region submit func
    public static <T> IFuture<T> submitFunc(Executor executor, Callable<? extends T> task) {
        IPromise<T> promise = newPromise(executor);
        executor.execute(PromiseTask.ofFunction(task, null, 0, promise));
        return promise;
    }

    public static <T> IFuture<T> submitFunc(IExecutor executor, Callable<? extends T> task, int options) {
        IPromise<T> promise = newPromise(executor);
        executor.execute(PromiseTask.ofFunction(task, null, options, promise));
        return promise;
    }

    public static <T> IFuture<T> submitFunc(Executor executor, Callable<? extends T> task, ICancelToken cancelToken) {
        IPromise<T> promise = newPromise(executor);
        executor.execute(PromiseTask.ofFunction(task, cancelToken, 0, promise));
        return promise;
    }

    public static <T> IFuture<T> submitFunc(IExecutor executor, Callable<? extends T> task, ICancelToken cancelToken, int options) {
        IPromise<T> promise = newPromise(executor);
        executor.execute(PromiseTask.ofFunction(task, cancelToken, options, promise));
        return promise;
    }

    public static <T> IFuture<T> submitFunc(Executor executor, Function<? super IContext, ? extends T> task, IContext ctx) {
        IPromise<T> promise = newPromise(executor);
        executor.execute(PromiseTask.ofFunction(task, ctx, 0, promise));
        return promise;
    }

    public static <T> IFuture<T> submitFunc(IExecutor executor, Function<? super IContext, ? extends T> task, IContext ctx, int options) {
        IPromise<T> promise = newPromise(executor);
        executor.execute(PromiseTask.ofFunction(task, ctx, options, promise));
        return promise;
    }
    // endregion

    // region submit action
    public static IFuture<?> submitAction(Executor executor, Runnable action) {
        IPromise<Object> promise = newPromise(executor);
        executor.execute(PromiseTask.ofAction(action, null, 0, promise));
        return promise;
    }

    public static IFuture<?> submitAction(IExecutor executor, Runnable action, int options) {
        IPromise<Object> promise = newPromise(executor);
        executor.execute(PromiseTask.ofAction(action, null, options, promise));
        return promise;
    }

    public static IFuture<?> submitAction(Executor executor, Runnable action, ICancelToken cancelToken) {
        IPromise<Object> promise = newPromise(executor);
        executor.execute(PromiseTask.ofAction(action, cancelToken, 0, promise));
        return promise;
    }

    public static IFuture<?> submitAction(IExecutor executor, Runnable action, ICancelToken cancelToken, int options) {
        IPromise<Object> promise = newPromise(executor);
        executor.execute(PromiseTask.ofAction(action, cancelToken, options, promise));
        return promise;
    }

    public static IFuture<?> submitAction(Executor executor, Consumer<? super IContext> task, IContext ctx) {
        IPromise<Object> promise = newPromise(executor);
        executor.execute(PromiseTask.ofAction(task, ctx, 0, promise));
        return promise;
    }

    public static IFuture<?> submitAction(IExecutor executor, Consumer<? super IContext> task, IContext ctx, int options) {
        IPromise<Object> promise = newPromise(executor);
        executor.execute(PromiseTask.ofAction(task, ctx, options, promise));
        return promise;
    }
    //endregion

    // region execute

    public static void execute(Executor executor, Runnable action, ICancelToken cancelToken) {
        ITask futureTask = FutureUtils.toTask(action, cancelToken, 0);
        executor.execute(futureTask);
    }

    public static void execute(Executor executor, Runnable action, ICancelToken cancelToken, int options) {
        ITask futureTask = FutureUtils.toTask(action, cancelToken, options);
        executor.execute(futureTask);
    }

    public static void execute(Executor executor, Consumer<? super IContext> action, IContext ctx) {
        ITask futureTask = FutureUtils.toTask(action, ctx, 0);
        executor.execute(futureTask);
    }

    public static void execute(IExecutor executor, Consumer<? super IContext> action, IContext ctx, int options) {
        ITask futureTask = FutureUtils.toTask(action, ctx, options);
        executor.execute(futureTask);
    }

    // endregion

    // endregion

    // region jdk-风格

    public static <T> IFuture<T> callAsync(Executor executor, Callable<? extends T> supplier) {
        Objects.requireNonNull(supplier);
        return submitFunc(executor, supplier);
    }

    public static <T> IFuture<T> supplyAsync(Executor executor, Supplier<? extends T> supplier) {
        Objects.requireNonNull(supplier);
        return submitFunc(executor, supplier::get);
    }

    public static IFuture<?> anyOf(IFuture<?> futures) {
        return new FutureCombiner()
                .addAll(futures)
                .anyOf();
    }

    public static IFuture<?> allOf(IFuture<?>... futures) {
        return new FutureCombiner()
                .addAll(futures)
                .selectAll();
    }

    // endregion

}