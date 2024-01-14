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

import cn.wjybxx.common.concurrent.IContext;
import cn.wjybxx.common.concurrent.ScheduledBuilder;
import cn.wjybxx.common.concurrent.TaskBuilder;

import javax.annotation.Nonnull;
import java.util.concurrent.Callable;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 子类需要在{@link #execute(Runnable, int)}的时候为任务赋值id和options
 *
 * @author wjybxx
 * date - 2023/4/7
 */
public abstract class AbstractUniScheduledExecutor
        extends AbstractUniExecutor
        implements UniScheduledExecutor {

    // region schedule

    @Override
    public <V> UniScheduledFuture<V> schedule(ScheduledBuilder<V> builder) {
        UniPromise<V> promise = newPromise(builder.getCtx());
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofBuilder(builder, promise, 0, tickTime());
        execute(promiseTask, builder.getOptions());
        return promiseTask;
    }

    @Override
    public UniScheduledFuture<?> scheduleAction(Consumer<? super IContext> task, IContext ctx, long delay) {
        long triggerTime = UniScheduledPromiseTask.triggerTime(delay, tickTime());
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofConsumer(task, newPromise(ctx), 0, triggerTime);
        execute(promiseTask, 0);
        return promiseTask;
    }

    @Override
    public <V> UniScheduledFuture<V> scheduleFunc(Function<? super IContext, V> task, IContext ctx, long delay) {
        long triggerTime = UniScheduledPromiseTask.triggerTime(delay, tickTime());
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofFunction(task, newPromise(ctx), 0, triggerTime);
        execute(promiseTask, 0);
        return promiseTask;
    }

    @Override
    public UniScheduledFuture<?> schedule(Runnable task, long delay) {
        long triggerTime = UniScheduledPromiseTask.triggerTime(delay, tickTime());
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofRunnable(task, newPromise(), 0, triggerTime);
        execute(promiseTask, 0);
        return promiseTask;
    }

    @Override
    public <V> UniScheduledFuture<V> schedule(Callable<V> task, long delay) {
        long triggerTime = UniScheduledPromiseTask.triggerTime(delay, tickTime());
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofCallable(task, newPromise(), 0, triggerTime);
        execute(promiseTask, 0);
        return promiseTask;
    }

    @Override
    public UniScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period) {
        ScheduledBuilder<?> sb = ScheduledBuilder.newRunnable(task)
                .setFixedRate(initialDelay, period);

        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofBuilder(sb, newPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask;
    }

    @Override
    public UniScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay) {
        ScheduledBuilder<?> sb = ScheduledBuilder.newRunnable(task)
                .setFixedDelay(initialDelay, delay);

        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofBuilder(sb, newPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask;
    }
    // endregion

    // 重写submit，修改task类型

    // region submit

    @Override
    public <V> UniFuture<V> submit(@Nonnull TaskBuilder<V> builder) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofBuilder(builder, newPromise(builder.getCtx()), 0, tickTime());
        execute(promiseTask, builder.getOptions());
        return promiseTask.future();
    }

    @Override
    public void execute(Consumer<? super IContext> action, IContext ctx) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofConsumer(action, newPromise(), 0, tickTime());
        execute(promiseTask, 0);
    }

    @Override
    public void execute(Consumer<? super IContext> action, IContext ctx, int options) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofConsumer(action, newPromise(), 0, tickTime());
        execute(promiseTask, options);
    }

    @Override
    public <T> UniFuture<T> submit(Callable<T> task) {
        UniScheduledPromiseTask<T> promiseTask = UniScheduledPromiseTask.ofCallable(task, newPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public <V> UniFuture<V> submitFunc(Function<? super IContext, V> task, IContext ctx) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofFunction(task, newPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public <V> UniFuture<V> submitFunc(Function<? super IContext, V> task, IContext ctx, int options) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofFunction(task, newPromise(), 0, tickTime());
        execute(promiseTask, options);
        return promiseTask.future();
    }

    @Override
    public UniFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofConsumer(task, newPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public UniFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx, int options) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofConsumer(task, newPromise(), 0, tickTime());
        execute(promiseTask, options);
        return promiseTask.future();
    }

    @Override
    public <V> UniFuture<V> submitCall(Callable<V> task) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofCallable(task, newPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public <V> UniFuture<V> submitCall(Callable<V> task, int options) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofCallable(task, newPromise(), 0, tickTime());
        execute(promiseTask, options);
        return promiseTask.future();
    }

    @Override
    public UniFuture<?> submitRun(Runnable task) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofRunnable(task, newPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public UniFuture<?> submitRun(Runnable task, int options) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofRunnable(task, newPromise(), 0, tickTime());
        execute(promiseTask, options);
        return promiseTask.future();
    }

    // endregion

    /**
     * 当前线程的时间
     * 1. 可以使用缓存的时间，也可以使用实时查询，只要不破坏任务的执行约定即可。
     * 2. 如果使用缓存时间，接口中并不约定时间的更新时机，也不约定一个大循环只更新一次。也就是说，线程可能在任意时间点更新缓存的时间，只要不破坏线程安全性和约定的任务时序。
     */
    protected abstract long tickTime();

    /**
     * 请求将当前任务重新压入队列
     * 1.一定从当前线程调用
     * 2.如果无法继续调度任务，则取消任务
     *
     * @param triggered 是否是执行之后压入队列；通常用于在执行成功之后降低优先级
     */
    protected abstract void reSchedulePeriodic(UniScheduledPromiseTask<?> futureTask, boolean triggered);

    /**
     * 请求删除给定的任务
     * 1.可能从其它线程调用，需考虑线程安全问题
     */
    protected abstract void removeScheduled(UniScheduledPromiseTask<?> futureTask);

}