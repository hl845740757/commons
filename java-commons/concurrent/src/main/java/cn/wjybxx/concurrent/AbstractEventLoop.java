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

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;
import java.util.concurrent.*;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * @author wjybxx
 * date 2023/4/7
 */
@SuppressWarnings({"NullableProblems", "RedundantMethodOverride"})
public abstract class AbstractEventLoop extends AbstractExecutorService implements EventLoop {

    protected static final Logger logger = LoggerFactory.getLogger(AbstractEventLoop.class);

    protected final EventLoopGroup parent;
    protected final Collection<EventLoop> selfCollection = Collections.singleton(this);

    protected AbstractEventLoop(@Nullable EventLoopGroup parent) {
        this.parent = parent;
    }

    @Override
    public abstract void execute(Runnable command, int options);

    @Override
    public void execute(Runnable command) {
        execute(command, 0);
    }

    @Override
    public void execute(Consumer<? super IContext> action, IContext ctx) {
        execute(FutureUtils.toRunnable(action, ctx));
    }

    @Override
    public void execute(Consumer<? super IContext> action, IContext ctx, int options) {
        execute(FutureUtils.toRunnable(action, ctx), options);
    }

    // region EventLoop

    @Nullable
    @Override
    public EventLoopGroup parent() {
        return parent;
    }

    @Nonnull
    @Override
    public EventLoop select() {
        return this;
    }

    @Nonnull
    @Override
    public EventLoop select(int key) {
        return this;
    }

    @Override
    public int numChildren() {
        return 1;
    }

    @Override
    public final void ensureInEventLoop() {
        if (!inEventLoop()) {
            throw new GuardedOperationException();
        }
    }

    @Override
    public final void ensureInEventLoop(String method) {
        Objects.requireNonNull(method);
        if (!inEventLoop()) {
            throw new GuardedOperationException("Calling " + method + " must in the EventLoop");
        }
    }

    public final void throwIfInEventLoop(String method) {
        if (inEventLoop()) {
            throw new BlockingOperationException("Calling " + method + " from within the EventLoop is not allowed");
        }
    }

    // endregion

    // region submit

    @Override
    public <V> IPromise<V> newPromise(IContext ctx) {
        return new Promise<>(this, ctx);
    }

    @Override
    public <V> IPromise<V> newPromise() {
        return new Promise<>(this, null);
    }

    @Override
    public <V> IFuture<V> submit(@Nonnull TaskBuilder<V> builder) {
        PromiseTask<V> futureTask = PromiseTask.ofBuilder(builder, newPromise(builder.getCtx()));
        execute(futureTask, builder.getOptions());
        return futureTask.future();
    }

    @Override
    public <T> IFuture<T> submit(Callable<T> task) {
        PromiseTask<T> futureTask = PromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    @SuppressWarnings("deprecation")
    @Override
    public IFuture<?> submit(Runnable task) {
        PromiseTask<?> futureTask = PromiseTask.ofRunnable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    @SuppressWarnings("deprecation")
    @Override
    public <T> IFuture<T> submit(Runnable task, T result) {
        return submit(Executors.callable(task, result));
    }

    @Override
    public <V> IFuture<V> submitFunc(Function<? super IContext, ? extends V> task, IContext ctx) {
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, newPromise(ctx));
        execute(futureTask, 0);
        return futureTask.future();
    }

    @Override
    public <V> IFuture<V> submitFunc(Function<? super IContext, ? extends V> task, IContext ctx, int options) {
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, newPromise(ctx));
        execute(futureTask, options);
        return futureTask.future();
    }

    @Override
    public IFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx) {
        PromiseTask<?> futureTask = PromiseTask.ofConsumer(task, newPromise(ctx));
        execute(futureTask, 0);
        return futureTask.future();
    }

    @Override
    public IFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx, int options) {
        PromiseTask<?> futureTask = PromiseTask.ofConsumer(task, newPromise(ctx));
        execute(futureTask, options);
        return futureTask.future();
    }

    @Override
    public <V> IFuture<V> submitCall(Callable<? extends V> task) {
        PromiseTask<V> futureTask = PromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    @Override
    public <V> IFuture<V> submitCall(Callable<? extends V> task, int options) {
        PromiseTask<V> futureTask = PromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, options);
        return futureTask.future();
    }

    @Override
    public IFuture<?> submitRun(Runnable task) {
        PromiseTask<?> futureTask = PromiseTask.ofRunnable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    /** 该方法可能和{@link ExecutorService#submit(Runnable, Object)}冲突，因此我们要带后缀 */
    @Override
    public IFuture<?> submitRun(Runnable task, int options) {
        PromiseTask<?> futureTask = PromiseTask.ofRunnable(task, newPromise(null));
        execute(futureTask, options);
        return futureTask.future();
    }

    // endregion

    // -------------------------------------- invoke阻塞调用检测 --------------------------------------
    @Nonnull
    @Override
    public <T> T invokeAny(@Nonnull Collection<? extends Callable<T>> tasks) throws InterruptedException, ExecutionException {
        throwIfInEventLoop("invokeAny");
        return super.invokeAny(tasks);
    }

    @Nonnull
    @Override
    public <T> T invokeAny(@Nonnull Collection<? extends Callable<T>> tasks, long timeout, @Nonnull TimeUnit unit)
            throws InterruptedException, ExecutionException, TimeoutException {
        throwIfInEventLoop("invokeAny");
        return super.invokeAny(tasks, timeout, unit);
    }

    @Nonnull
    @Override
    public <T> List<Future<T>> invokeAll(@Nonnull Collection<? extends Callable<T>> tasks)
            throws InterruptedException {
        throwIfInEventLoop("invokeAll");
        return super.invokeAll(tasks);
    }

    @Nonnull
    @Override
    public <T> List<Future<T>> invokeAll(@Nonnull Collection<? extends Callable<T>> tasks,
                                         long timeout, @Nonnull TimeUnit unit) throws InterruptedException {
        throwIfInEventLoop("invokeAll");
        return super.invokeAll(tasks, timeout, unit);
    }

    // ---------------------------------------- 迭代 ---------------------------------------

    @Nonnull
    @Override
    public final Iterator<EventLoop> iterator() {
        return selfCollection.iterator();
    }

    @Override
    public final void forEach(Consumer<? super EventLoop> action) {
        selfCollection.forEach(action);
    }

    @Override
    public final Spliterator<EventLoop> spliterator() {
        return selfCollection.spliterator();
    }

    // ---------------------------------------- 工具方法 ---------------------------------------

    protected static void safeExecute(Runnable task) {
        try {
            task.run();
        } catch (Throwable t) {
            if (t instanceof VirtualMachineError) {
                logger.error("A task raised an exception. Task: {}", task, t);
            } else {
                logger.warn("A task raised an exception. Task: {}", task, t);
            }
        }
    }

    protected static void logCause(Throwable t) {
        if (t instanceof VirtualMachineError) {
            logger.error("A task raised an exception.", t);
        } else {
            logger.warn("A task raised an exception.", t);
        }
    }

}