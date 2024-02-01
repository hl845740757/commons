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

package cn.wjybxx.single;

import cn.wjybxx.concurrent.*;

import javax.annotation.Nonnull;
import java.util.concurrent.Callable;
import java.util.concurrent.TimeUnit;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 子类需要在{@link #execute(Runnable, int)}的时候为任务赋值id和options
 *
 * @author wjybxx
 * date - 2023/4/7
 */
@SuppressWarnings("NullableProblems")
public abstract class AbstractUniScheduledExecutor
        extends AbstractUniExecutor
        implements UniScheduledExecutor {

    // region schedule

    @Override
    public <V> IScheduledPromise<V> newScheduledPromise() {
        return new UniScheduledPromise<>(this);
    }

    @Override
    public <V> IScheduledPromise<V> newScheduledPromise(IContext ctx) {
        return new UniScheduledPromise<>(this, ctx);
    }

    @Override
    public <V> IScheduledFuture<V> schedule(ScheduledTaskBuilder<V> builder) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofBuilder(builder, newScheduledPromise(builder.getCtx()), 0, tickTime());
        execute(promiseTask, builder.getOptions());
        return promiseTask.future();
    }

    @Override
    public <V> IScheduledFuture<V> scheduleFunc(Function<? super IContext, V> task, IContext ctx, long delay, TimeUnit unit) {
        long triggerTime = UniScheduledPromiseTask.triggerTime(delay, unit, tickTime());
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofFunction(task, newScheduledPromise(ctx), 0, triggerTime);
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public IScheduledFuture<?> scheduleAction(Consumer<? super IContext> task, IContext ctx, long delay, TimeUnit unit) {
        long triggerTime = UniScheduledPromiseTask.triggerTime(delay, unit, tickTime());
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofConsumer(task, newScheduledPromise(ctx), 0, triggerTime);
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public IScheduledFuture<?> schedule(Runnable task, long delay, TimeUnit unit) {
        long triggerTime = UniScheduledPromiseTask.triggerTime(delay, unit, tickTime());
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofRunnable(task, newScheduledPromise(), 0, triggerTime);
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public <V> IScheduledFuture<V> schedule(Callable<V> task, long delay, TimeUnit unit) {
        long triggerTime = UniScheduledPromiseTask.triggerTime(delay, unit, tickTime());
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofCallable(task, newScheduledPromise(), 0, triggerTime);
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public IScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period, TimeUnit unit) {
        ScheduledTaskBuilder<?> sb = ScheduledTaskBuilder.newRunnable(task)
                .setFixedRate(initialDelay, period, unit);

        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofBuilder(sb, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public IScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay, TimeUnit unit) {
        ScheduledTaskBuilder<?> sb = ScheduledTaskBuilder.newRunnable(task)
                .setFixedDelay(initialDelay, delay, unit);

        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofBuilder(sb, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }
    // endregion

    // endregion

    // 重写submit，修改task类型

    // region submit

    @Override
    public <V> IFuture<V> submit(@Nonnull TaskBuilder<V> builder) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofBuilder(builder, newScheduledPromise(builder.getCtx()), 0, tickTime());
        execute(promiseTask, builder.getOptions());
        return promiseTask.future();
    }

    @Override
    public void execute(Consumer<? super IContext> action, IContext ctx) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofConsumer(action, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, 0);
    }

    @Override
    public void execute(Consumer<? super IContext> action, IContext ctx, int options) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofConsumer(action, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, options);
    }

    @Override
    public <T> IFuture<T> submit(Callable<T> task) {
        UniScheduledPromiseTask<T> promiseTask = UniScheduledPromiseTask.ofCallable(task, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public <V> IFuture<V> submitFunc(Function<? super IContext, ? extends V> task, IContext ctx) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofFunction(task, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public <V> IFuture<V> submitFunc(Function<? super IContext, ? extends V> task, IContext ctx, int options) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofFunction(task, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, options);
        return promiseTask.future();
    }

    @Override
    public IFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofConsumer(task, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public IFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx, int options) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofConsumer(task, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, options);
        return promiseTask.future();
    }

    @Override
    public <V> IFuture<V> submitCall(Callable<? extends V> task) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofCallable(task, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public <V> IFuture<V> submitCall(Callable<? extends V> task, int options) {
        UniScheduledPromiseTask<V> promiseTask = UniScheduledPromiseTask.ofCallable(task, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, options);
        return promiseTask.future();
    }

    @Override
    public IFuture<?> submitRun(Runnable task) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofRunnable(task, newScheduledPromise(), 0, tickTime());
        execute(promiseTask, 0);
        return promiseTask.future();
    }

    @Override
    public IFuture<?> submitRun(Runnable task, int options) {
        UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofRunnable(task, newScheduledPromise(), 0, tickTime());
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
     */
    protected abstract void removeScheduled(UniScheduledPromiseTask<?> futureTask);

}