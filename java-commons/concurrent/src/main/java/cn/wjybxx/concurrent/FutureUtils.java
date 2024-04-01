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

import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.function.TriFunction;
import cn.wjybxx.base.mutable.MutableObject;
import cn.wjybxx.base.time.CachedTimeProvider;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/9
 */
public class FutureUtils {

    private static final TriFunction<IContext, Object, Throwable, Object> EXCEPTION_TO_NULL = (ctx, o, throwable) -> {
        if (throwable != null) {
            return null;
        } else {
            return o;
        }
    };

    // region 异常处理

    /**
     * 如果future失败则返回null
     */
    @SuppressWarnings("unchecked")
    public static <V> BiFunction<V, Throwable, V> treatExceptionAsNull() {
        return (BiFunction<V, Throwable, V>) EXCEPTION_TO_NULL;
    }

    /**
     * {@link CompletableFuture}总是使用{@link CompletionException}包装异常，我们需要找到原始异常
     */
    public static Throwable unwrapCompletionException(Throwable t) {
        while (t instanceof CompletionException && t.getCause() != null) {
            t = t.getCause();
        }
        return t;
    }

    public static Throwable getCause(CompletableFuture<?> future) {
        // jdk21 支持直接获取普通异常
        if (future.isCompletedExceptionally()) {
            return future.exceptionNow();
        }
        // jdk 不支持直接获取取消异常
        if (future.isCancelled()) {
            MutableObject<Throwable> causeHolder = new MutableObject<>(); // visitor
            future.whenComplete((v, cause) -> causeHolder.setValue(cause));
            return causeHolder.getValue();
        }
        return null;
    }

    public static Throwable getCause(IFuture<?> future) {
        if (future.isFailedOrCancelled()) {
            return future.exceptionNow(false);
        }
        return null;
    }

    // endregion

    // region future工具方法

    public static <V> IPromise<V> fromJDKFuture(CompletionStage<V> stage) {
        IPromise<V> promise = new Promise<>();
        stage.whenComplete(((v, throwable) -> {
            if (throwable != null) {
                promise.trySetException(throwable);
            } else {
                promise.trySetResult(v);
            }
        }));
        return promise;
    }

    public static <V> CompletableFuture<V> toJDKFuture(ICompletionStage<V> stage) {
        CompletableFuture<V> future = new CompletableFuture<>();
        stage.whenComplete(((ctx, v, throwable) -> {
            if (throwable != null) {
                future.completeExceptionally(throwable);
            } else {
                future.complete(v);
            }
        }));
        return future;
    }

    // region set-future

    public static <V> void setFuture(IPromise<? super V> output, ICompletionStage<V> input) {
        Objects.requireNonNull(output, "output");
        input.whenComplete((ctx, v, throwable) -> {
            if (throwable != null) {
                output.trySetException(throwable);
            } else {
                output.trySetResult(v);
            }
        });
    }

    public static <V> void setFuture(IPromise<? super V> output, CompletionStage<V> input) {
        Objects.requireNonNull(output, "output");
        input.whenComplete((v, throwable) -> {
            if (throwable != null) {
                output.trySetException(throwable);
            } else {
                output.trySetResult(v);
            }
        });
    }

    public static <V> void setFutureAsync(Executor executor, IPromise<? super V> output, ICompletionStage<V> input) {
        setFutureAsync(executor, output, input, 0);
    }

    public static <V> void setFutureAsync(Executor executor, IPromise<? super V> output, ICompletionStage<V> input, int options) {
        Objects.requireNonNull(output, "output");
        Objects.requireNonNull(executor, "executor");
        input.whenCompleteAsync(executor, (ctx, v, throwable) -> {
            if (throwable != null) {
                output.trySetException(throwable);
            } else {
                output.trySetResult(v);
            }
        }, null, options);
    }

    public static <V> void setFutureAsync(Executor executor, IPromise<? super V> output, CompletionStage<V> input) {
        input.whenCompleteAsync(((v, throwable) -> {
            if (throwable != null) {
                output.trySetException(throwable);
            } else {
                output.trySetResult(v);
            }
        }), executor);
    }

    //
    public static <V> void setFuture(CompletableFuture<? super V> output, CompletionStage<V> input) {
        Objects.requireNonNull(output, "output");
        input.whenComplete((v, throwable) -> {
            if (throwable != null) {
                output.completeExceptionally(throwable);
            } else {
                output.complete(v);
            }
        });
    }

    public static <V> void setFuture(CompletableFuture<? super V> output, ICompletionStage<V> input) {
        Objects.requireNonNull(output, "output");
        input.whenComplete((ctx, v, throwable) -> {
            if (throwable != null) {
                output.completeExceptionally(throwable);
            } else {
                output.complete(v);
            }
        });
    }

    public static <V> void setFutureAsync(Executor executor, CompletableFuture<? super V> output, CompletionStage<V> input) {
        input.whenCompleteAsync(((v, throwable) -> {
            if (throwable != null) {
                output.completeExceptionally(throwable);
            } else {
                output.complete(v);
            }
        }), executor);
    }

    public static <V> void setFutureAsync(Executor executor, CompletableFuture<? super V> output, ICompletionStage<V> input) {
        setFutureAsync(executor, output, input, 0);
    }

    public static <V> void setFutureAsync(Executor executor, CompletableFuture<? super V> output, ICompletionStage<V> input, int options) {
        input.whenCompleteAsync(executor, ((ctx, v, throwable) -> {
            if (throwable != null) {
                output.completeExceptionally(throwable);
            } else {
                output.complete(v);
            }
        }), null, options);
    }

    /** 由{@link EventLoop}通知返回的{@link IPromise} */
    public static <V> IPromise<V> toEventLoopPromise(EventLoop eventLoop, ICompletionStage<V> input) {
        IPromise<V> result = eventLoop.newPromise();
        setFutureAsync(eventLoop, result, input, TaskOption.STAGE_TRY_INLINE);
        return result;
    }

    // endregion

    /** @return 如果future在指定时间内进入了完成状态，则返回true */
    public static boolean await(CompletableFuture<?> future, long timeout, TimeUnit unit) throws InterruptedException {
        if (timeout <= 0) {
            return future.isDone();
        }
        if (future.isDone()) {
            return true;
        }

        try {
            future.get(timeout, unit);
            return true;
        } catch (TimeoutException ignore) {
            return false;
        } catch (ExecutionException ignore) {
            return true;
        }
    }

    /** @return 如果future在指定时间内进入了完成状态，则返回true */
    public static boolean awaitUninterruptedly(CompletableFuture<?> future, long timeout, TimeUnit unit) {
        if (timeout <= 0) {
            return future.isDone();
        }
        if (future.isDone()) {
            return true;
        }

        boolean interrupted = false;
        final long endTime = System.nanoTime() + unit.toNanos(timeout);
        try {
            do {
                final long remainNano = endTime - System.nanoTime();
                if (remainNano <= 0) {
                    return false;
                }

                try {
                    future.get(remainNano, TimeUnit.NANOSECONDS);
                    return true;
                } catch (TimeoutException ignore) {
                    return false;
                } catch (ExecutionException ignore) {
                    return true;
                } catch (InterruptedException e) {
                    interrupted = true;
                }
            } while (!future.isDone());
        } finally {
            if (interrupted) {
                ThreadUtils.recoveryInterrupted();
            }
        }
        return true; // 循环外isDone
    }

    // endregion

    // region future工厂方法
    public static <V> IPromise<V> newPromise() {
        return new Promise<>();
    }

    public static <V> IPromise<V> newPromise(Executor executor) {
        return new Promise<>(executor);
    }

    public static <V> IFuture<V> completedFuture(V result) {
        return Promise.completedPromise(result);
    }

    public static <V> IFuture<V> completedFuture(V result, Executor executor) {
        return Promise.completedPromise(result, executor);
    }

    public static <V> IFuture<V> failedFuture(Throwable cause) {
        return Promise.failedPromise(cause);
    }

    public static <V> IFuture<V> failedFuture(Throwable cause, Executor executor) {
        return Promise.failedPromise(cause, executor);
    }

    public static FutureCombiner newCombiner() {
        return new FutureCombiner();
    }

    public static JDKFutureCombiner newJdkCombiner() {
        return new JDKFutureCombiner();
    }

    // endregion

    // region eventloop

    public static boolean inEventLoop(@Nullable Executor executor) {
        return executor instanceof SingleThreadExecutor eventLoop && eventLoop.inEventLoop();
    }

    public static void ensureInEventLoop(SingleThreadExecutor eventLoop) {
        if (!eventLoop.inEventLoop()) {
            throw new GuardedOperationException("Must be called from EventLoop thread");
        }
    }

    public static void ensureInEventLoop(SingleThreadExecutor eventLoop, String msg) {
        if (!eventLoop.inEventLoop()) {
            throw new GuardedOperationException(msg);
        }
    }

    /** @see #newTimeProvider(EventLoop, long) */
    public static CachedTimeProvider newTimeProvider(EventLoop eventLoop) {
        return new EventLoopTimeProvider(eventLoop, System.currentTimeMillis());
    }

    /**
     * 创建一个支持缓存的时间提供器，且可以多线程安全访问。
     * 你需要调用{@link CachedTimeProvider#setTime(long)}更新时间值，且应该只有一个线程调用更新方法。
     *
     * @param eventLoop 负责更新时间的线程
     * @param curTime   初始时间
     * @return timeProvider - threadSafe
     */
    public static CachedTimeProvider newTimeProvider(EventLoop eventLoop, long curTime) {
        return new EventLoopTimeProvider(eventLoop, curTime);
    }
    // endregion

    // region submit

    public static <T> IFuture<T> submit(IExecutor executor, @Nonnull TaskBuilder<T> builder) {
        IPromise<T> promise = newPromise(executor);
        PromiseTask<T> futureTask = PromiseTask.ofBuilder(builder, promise);
        executor.execute(futureTask);
        return promise;
    }

    public static <V> IFuture<V> submitFunc(Executor executor, Callable<? extends V> task) {
        IPromise<V> promise = newPromise(executor);
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, null, 0, promise);
        executor.execute(futureTask);
        return promise;
    }

    public static <V> IFuture<V> submitFunc(IExecutor executor, Callable<? extends V> task, int options) {
        IPromise<V> promise = newPromise(executor);
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, null, options, promise);
        executor.execute(futureTask);
        return promise;
    }

    public static <V> IFuture<V> submitFunc(Executor executor, Function<? super IContext, ? extends V> task, IContext ctx) {
        IPromise<V> promise = newPromise(executor);
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, ctx, 0, promise);
        executor.execute(futureTask);
        return promise;
    }

    public static <V> IFuture<V> submitFunc(IExecutor executor, Function<? super IContext, ? extends V> task, IContext ctx, int options) {
        IPromise<V> promise = newPromise(executor);
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, ctx, options, promise);
        executor.execute(futureTask);
        return promise;
    }

    public static IFuture<?> submitAction(Executor executor, Runnable action) {
        IPromise<Object> promise = newPromise(executor);
        PromiseTask<?> futureTask = PromiseTask.ofAction(action, null, 0, promise);
        executor.execute(futureTask);
        return promise;
    }

    public static IFuture<?> submitAction(IExecutor executor, Runnable action, int options) {
        IPromise<Object> promise = newPromise(executor);
        PromiseTask<?> futureTask = PromiseTask.ofAction(action, null, options, promise);
        executor.execute(futureTask);
        return promise;
    }

    public static IFuture<?> submitAction(Executor executor, Consumer<? super IContext> task, IContext ctx) {
        IPromise<Object> promise = newPromise(executor);
        PromiseTask<?> futureTask = PromiseTask.ofAction(task, ctx, 0, promise);
        executor.execute(futureTask);
        return promise;
    }

    public static IFuture<?> submitAction(IExecutor executor, Consumer<? super IContext> task, IContext ctx, int options) {
        IPromise<Object> promise = newPromise(executor);
        PromiseTask<?> futureTask = PromiseTask.ofAction(task, ctx, options, promise);
        executor.execute(futureTask);
        return promise;
    }

    public static void execute(Executor executor, Consumer<? super IContext> action, IContext ctx) {
        ITask futureTask = toTask(action, ctx, 0);
        executor.execute(futureTask);
    }

    public static void execute(IExecutor executor, Consumer<? super IContext> action, IContext ctx, int options) {
        ITask futureTask = toTask(action, ctx, options);
        executor.execute(futureTask);
    }

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

    // region task适配

    public static ITask toTask(Runnable action, int options) {
        // 注意：不论options是否为0都需要封装，否则action为ITask类型时将产生错误
        Objects.requireNonNull(action, "action");
        return new Task1(action, options);
    }

    public static ITask toTask(Consumer<? super IContext> action, IContext ctx, int options) {
        Objects.requireNonNull(action, "action");
        return new Task3(action, ctx, options);
    }

    // endregion

    // region 适配类

    @ThreadSafe
    private static class EventLoopTimeProvider implements CachedTimeProvider {

        private final EventLoop eventLoop;
        private volatile long time;

        private EventLoopTimeProvider(EventLoop eventLoop, long time) {
            this.eventLoop = eventLoop;
            setTime(time);
        }

        public void setTime(long curTime) {
            if (eventLoop.inEventLoop()) {
                this.time = curTime;
            } else {
                throw new GuardedOperationException("setTime from another thread");
            }
        }

        @Override
        public long getTime() {
            return time;
        }

        @Override
        public String toString() {
            return "EventLoopTimeProvider{" +
                    "curTime=" + time +
                    '}';
        }
    }

    private static class Task1 implements ITask {

        private Runnable action;
        private final int options;

        public Task1(Runnable action, int options) {
            this.action = action;
            this.options = options;
        }

        @Override
        public int getOptions() {
            return options;
        }

        @Override
        public void run() {
            Runnable action = this.action;
            {
                this.action = null;
            }
            action.run();
        }
    }

    private static class Task3 implements ITask {

        private Consumer<? super IContext> action;
        private IContext ctx;
        private final int options;

        public Task3(Consumer<? super IContext> action, IContext ctx, int options) {
            this.action = action;
            this.ctx = ctx;
            this.options = options;
        }

        @Override
        public int getOptions() {
            return options;
        }

        @Override
        public void run() {
            Consumer<? super IContext> action = this.action;
            IContext ctx = this.ctx;
            {
                this.action = null;
                this.ctx = null;
            }
            if (ctx != null && ctx.cancelToken().isCancelling()) {
                return; // 抛出异常没有意义，检测信号即可
            }
            action.accept(ctx);
        }
    }
    // endregion

}