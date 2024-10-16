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
 * 默认的实现仅仅是简单的将任务分配给某个{@link EventLoop}执行
 *
 * @author wjybxx
 * date 2023/4/8
 */
@SuppressWarnings("deprecation")
public abstract class AbstractEventLoopGroup implements EventLoopGroup {

    @Override
    public void execute(@Nonnull Runnable command) {
        select().execute(command);
    }

    @Override
    public void execute(Runnable command, int options) {
        select().execute(command, options);
    }

    // region submit
    @Override
    public <T> IPromise<T> newPromise() {
        return new Promise<>(this);
    }

    @Override
    public <T> IFuture<T> submit(@Nonnull TaskBuilder<T> builder) {
        return select().submit(builder);
    }

    @Override
    public <T> IFuture<T> submitFunc(Callable<? extends T> task) {
        return select().submitFunc(task);
    }

    @Override
    public <T> IFuture<T> submitFunc(Callable<? extends T> task, int options) {
        return select().submitFunc(task, options);
    }

    @Override
    public <T> IFuture<T> submitFunc(Callable<? extends T> task, ICancelToken cancelToken, int options) {
        return select().submitFunc(task, cancelToken, options);
    }

    @Override
    public <T> IFuture<T> submitFunc(Function<? super IContext, ? extends T> task, IContext ctx, int options) {
        return select().submitFunc(task, ctx, options);
    }

    @Override
    public IFuture<?> submitAction(Runnable task) {
        return select().submitAction(task);
    }

    @Override
    public IFuture<?> submitAction(Runnable task, int options) {
        return select().submitAction(task, options);
    }

    @Override
    public IFuture<?> submitAction(Runnable task, ICancelToken cancelToken, int options) {
        return select().submitAction(task, cancelToken, options);
    }

    @Override
    public IFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx, int options) {
        return select().submitAction(task, ctx, options);
    }

    @Nonnull
    @Override
    public final <T> IFuture<T> submit(@Nonnull Callable<T> task) {
        return select().submitFunc(task);
    }

    @Nonnull
    @Override
    public final IFuture<?> submit(@Nonnull Runnable task) {
        return select().submitAction(task);
    }

    // endregion

    // region schedule

    @Override
    public <V> IScheduledPromise<V> newScheduledPromise() {
        return new ScheduledPromise<>(this);
    }

    @Override
    public <T> IScheduledFuture<T> schedule(ScheduledTaskBuilder<T> builder) {
        return select().schedule(builder);
    }

    @Override
    public IScheduledFuture<?> scheduleAction(Runnable task, long delay, TimeUnit unit, ICancelToken cancelToken) {
        return select().scheduleAction(task, delay, unit, cancelToken);
    }

    @Override
    public <T> IScheduledFuture<T> scheduleFunc(Callable<T> task, long delay, TimeUnit unit, ICancelToken cancelToken) {
        return select().scheduleFunc(task, delay, unit, cancelToken);
    }

    @Nonnull
    @Override
    public IScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay, TimeUnit unit, ICancelToken cancelToken) {
        return select().scheduleWithFixedDelay(task, initialDelay, delay, unit, cancelToken);
    }

    @Nonnull
    @Override
    public IScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period, TimeUnit unit, ICancelToken cancelToken) {
        return select().scheduleAtFixedRate(task, initialDelay, period, unit, cancelToken);
    }

    @Nonnull
    @Override
    public final IScheduledFuture<?> schedule(Runnable task, long delay, TimeUnit unit) {
        return select().scheduleAction(task, delay, unit, ICancelToken.NONE);
    }

    @Nonnull
    @Override
    public final <T> IScheduledFuture<T> schedule(Callable<T> task, long delay, TimeUnit unit) {
        return select().scheduleFunc(task, delay, unit, ICancelToken.NONE);
    }

    @Nonnull
    @Override
    public final IScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay, TimeUnit unit) {
        return select().scheduleWithFixedDelay(task, initialDelay, delay, unit, ICancelToken.NONE);
    }

    @Nonnull
    @Override
    public final IScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period, TimeUnit unit) {
        return select().scheduleAtFixedRate(task, initialDelay, period, unit, ICancelToken.NONE);
    }
    // endregion

    // 以下API并不常用，因此不做优化

    @Nonnull
    @Override
    public <T> List<Future<T>> invokeAll(@Nonnull Collection<? extends Callable<T>> tasks)
            throws InterruptedException {
        return select().invokeAll(tasks);
    }

    @Nonnull
    @Override
    public <T> List<Future<T>> invokeAll(@Nonnull Collection<? extends Callable<T>> tasks, long timeout, @Nonnull TimeUnit unit)
            throws InterruptedException {
        return select().invokeAll(tasks, timeout, unit);
    }

    @Nonnull
    @Override
    public <T> T invokeAny(@Nonnull Collection<? extends Callable<T>> tasks)
            throws InterruptedException, ExecutionException {
        return select().invokeAny(tasks);
    }

    @Override
    public <T> T invokeAny(@Nonnull Collection<? extends Callable<T>> tasks, long timeout, @Nonnull TimeUnit unit)
            throws InterruptedException, ExecutionException, TimeoutException {
        return select().invokeAny(tasks, timeout, unit);
    }

}