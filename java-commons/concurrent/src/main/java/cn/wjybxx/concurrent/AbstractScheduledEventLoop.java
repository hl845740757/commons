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

import javax.annotation.Nullable;
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
abstract class AbstractScheduledEventLoop extends AbstractEventLoop {

    public AbstractScheduledEventLoop(@Nullable EventLoopGroup parent) {
        super(parent);
    }

    // region schedule

    @Override
    public <V> IScheduledPromise<V> newScheduledPromise() {
        return new ScheduledPromise<>(this);
    }

    @Override
    public <V> IScheduledPromise<V> newScheduledPromise(IContext ctx) {
        return new ScheduledPromise<>(this, ctx);
    }

    @Override
    public <V> IScheduledFuture<V> schedule(ScheduledTaskBuilder<V> builder) {
        ScheduledPromiseTask<V> promiseTask = ScheduledPromiseTask.ofBuilder(builder, newScheduledPromise(builder.getCtx()), 0, tickTime());
        execute(promiseTask);
        return promiseTask.future();
    }

    @Override
    public <V> IScheduledFuture<V> scheduleFunc(Function<? super IContext, V> task, IContext ctx, long delay, TimeUnit unit) {
        long triggerTime = ScheduledPromiseTask.triggerTime(delay, unit, tickTime());
        ScheduledPromiseTask<V> promiseTask = ScheduledPromiseTask.ofFunction(task, 0, newScheduledPromise(ctx), 0, triggerTime);
        execute(promiseTask);
        return promiseTask.future();
    }

    @Override
    public IScheduledFuture<?> scheduleAction(Consumer<? super IContext> task, IContext ctx, long delay, TimeUnit unit) {
        long triggerTime = ScheduledPromiseTask.triggerTime(delay, unit, tickTime());
        ScheduledPromiseTask<?> promiseTask = ScheduledPromiseTask.ofConsumer(task, 0, newScheduledPromise(ctx), 0, triggerTime);
        execute(promiseTask);
        return promiseTask.future();
    }

    @Override
    public IScheduledFuture<?> schedule(Runnable task, long delay, TimeUnit unit) {
        long triggerTime = ScheduledPromiseTask.triggerTime(delay, unit, tickTime());
        ScheduledPromiseTask<?> promiseTask = ScheduledPromiseTask.ofRunnable(task, 0, newScheduledPromise(), 0, triggerTime);
        execute(promiseTask);
        return promiseTask.future();
    }

    @Override
    public <V> IScheduledFuture<V> schedule(Callable<V> task, long delay, TimeUnit unit) {
        long triggerTime = ScheduledPromiseTask.triggerTime(delay, unit, tickTime());
        ScheduledPromiseTask<V> promiseTask = ScheduledPromiseTask.ofCallable(task, 0, newScheduledPromise(), 0, triggerTime);
        execute(promiseTask);
        return promiseTask.future();
    }

    @Override
    public IScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period, TimeUnit unit) {
        ScheduledTaskBuilder<?> sb = ScheduledTaskBuilder.newRunnable(task)
                .setFixedRate(initialDelay, period, unit);

        ScheduledPromiseTask<?> promiseTask = ScheduledPromiseTask.ofBuilder(sb, newScheduledPromise(), 0, tickTime());
        execute(promiseTask);
        return promiseTask.future();
    }

    @Override
    public IScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay, TimeUnit unit) {
        ScheduledTaskBuilder<?> sb = ScheduledTaskBuilder.newRunnable(task)
                .setFixedDelay(initialDelay, delay, unit);

        ScheduledPromiseTask<?> promiseTask = ScheduledPromiseTask.ofBuilder(sb, newScheduledPromise(), 0, tickTime());
        execute(promiseTask);
        return promiseTask.future();
    }
    // endregion

    /**
     * 当前线程的时间 -- 纳秒（非时间戳）
     * 1. 可以使用缓存的时间，也可以使用{@link System#nanoTime()}实时查询，只要不破坏任务的执行约定即可。
     * 2. 如果使用缓存时间，接口中并不约定时间的更新时机，也不约定一个大循环只更新一次。也就是说，线程可能在任意时间点更新缓存的时间，只要不破坏线程安全性和约定的任务时序。
     */
    abstract long tickTime();

    /**
     * 请求将当前任务重新压入队列
     * 1.一定从当前线程调用
     * 2.如果无法继续调度任务，则取消任务
     *
     * @param triggered 是否是执行之后压入队列；通常用于在执行成功之后降低优先级
     */
    abstract void reSchedulePeriodic(ScheduledPromiseTask<?> futureTask, boolean triggered);

    /**
     * 请求删除给定的任务
     * 1.可能从其它线程调用，需考虑线程安全问题
     */
    abstract void removeScheduled(ScheduledPromiseTask<?> futureTask);

}