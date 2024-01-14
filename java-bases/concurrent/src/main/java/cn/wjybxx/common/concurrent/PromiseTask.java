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

package cn.wjybxx.common.concurrent;

import cn.wjybxx.base.func.TriConsumer;
import cn.wjybxx.base.func.TriFunction;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Objects;
import java.util.concurrent.*;
import java.util.function.BiConsumer;
import java.util.function.BiFunction;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 这个实现没有特殊逻辑，可开放给用户
 * 实现{@link RunnableFuture}是为了适配JDK的实现类，实际上更建议组合。
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public class PromiseTask<V> implements IFutureTask<V>, RunnableFuture<V>, IFuture<V> {

    /**
     * queueId的掩码 -- 8bit，最大255。
     * 1.放在低8位，减少运算，queueId的计算频率高于其它部分。
     * 2.大于{@link TaskOption}的中的64阶段。
     */
    static final int maskQueueId = 0xFF;
    /** 任务类型的掩码 -- 4bit，可省去大量的instanceof测试 */
    static final int maskTaskType = 0x0F00;
    /** 调度类型的掩码 -- 4bit，最大16种 */
    static final int maskScheduleType = 0xF000;
    /** 是否已经声明任务的归属权 */
    static final int maskClaimed = 1 << 16;

    static final int offsetQueueId = 0;
    static final int offsetTaskType = 8;
    static final int offsetScheduleType = 12;
    static final int maxQueueId = 255;

    /** 用户的任务 */
    private Object action;
    /** 用户可能在任务完成后继续访问，因此不能清理 */
    final IPromise<V> promise;
    /** 控制标记 */
    int ctl;
    /** 调度选项 */
    int options;

    /**
     * @param action  用户的任务，支持的类型见{@link TaskBuilder#taskType(Object)}
     * @param promise 任务关联的promise
     */
    public PromiseTask(Object action, IPromise<V> promise) {
        this(action, promise, TaskBuilder.taskType(action));
    }

    /**
     * 注意：此时并不会保存任务的options，options应当由executor在放入队列前设置到该task。
     * {@link #setOptions(int)}
     *
     * @param builder 任务构建器
     * @param promise 任务关联的promise
     */
    public PromiseTask(TaskBuilder<V> builder, IPromise<V> promise) {
        this(builder.getTask(), promise, builder.getType());
    }

    PromiseTask(Object action, IPromise<V> promise, int taskType) {
        this.action = Objects.requireNonNull(action, "action");
        this.promise = Objects.requireNonNull(promise, "promise");
        this.ctl |= (taskType << offsetTaskType);
    }

    // region factory

    public static PromiseTask<?> ofRunnable(Runnable action, IPromise<?> promise) {
        return new PromiseTask<>(action, promise, TaskBuilder.TYPE_RUNNABLE);
    }

    public static <V> PromiseTask<V> ofCallable(Callable<V> action, IPromise<V> promise) {
        return new PromiseTask<>(action, promise, TaskBuilder.TYPE_CALLABLE);
    }

    public static <V> PromiseTask<V> ofFunction(Function<? super IContext, V> action, IPromise<V> promise) {
        return new PromiseTask<>(action, promise, TaskBuilder.TYPE_FUNCTION);
    }

    public static PromiseTask<?> ofConsumer(Consumer<? super IContext> action, IPromise<?> promise) {
        return new PromiseTask<>(action, promise, TaskBuilder.TYPE_CONSUMER);
    }

    public static <V> PromiseTask<V> ofBuilder(TaskBuilder<V> builder, IPromise<V> promise) {
        return new PromiseTask<>(builder, promise);
    }
    // endregion

    // region open

    /**
     * 1.executor应当在调度任务之前设置options
     * 2.该接口为了避免对提交的任务进行二次封装。
     */
    public final void setOptions(int options) {
        this.options = options;
    }

    public final int getOptions() {
        return options;
    }

    public boolean isEnable(int taskOption) {
        return TaskOption.isEnabled(options, taskOption);
    }

    @Override
    public final IFuture<V> future() {
        return promise;
    }

    /** 获取绑定的任务 */
    protected final Object getAction() {
        return action;
    }

    /** 开放给外部子类 */
    protected final IPromise<V> getPromise() {
        return promise;
    }

    /** 获取任务的类型 -- 在可能包含分时任务的情况下要进行判断 */
    protected final int getTaskType() {
        return (ctl & maskTaskType) >> offsetTaskType;
    }

    /** 获取任务的调度类型 */
    protected int getScheduleType() {
        return (ctl & maskScheduleType) >> offsetScheduleType;
    }

    /** 设置任务的调度类型 -- 应该在添加到队列之前设置 */
    protected void setScheduleType(int scheduleType) {
        ctl |= (scheduleType << offsetScheduleType);
    }

    /** 是否已经声明任务的归属权 */
    protected boolean isClaimed() {
        return (ctl & maskClaimed) != 0;
    }

    /** 将任务标记为已申领 */
    protected void setClaimed() {
        ctl |= maskClaimed;
    }

    /** 获取任务所属的队列id */
    protected int getQueueId() {
        return (ctl & maskQueueId);
    }

    /** @param queueId 队列id，范围 [0, 255] */
    protected void setQueueId(int queueId) {
        if (queueId < 0 || queueId > maxQueueId) {
            throw new IllegalArgumentException("queueId: " + maxQueueId);
        }
        ctl &= ~maskQueueId;
        ctl |= (queueId);
    }

    // endregion

    /** 运行分时任务 */
    @SuppressWarnings("unchecked")
    protected final ResultHolder<V> runTimeSharing() throws Exception {
        TimeSharingTask<V> task = (TimeSharingTask<V>) action;
        return task.step(promise.ctx());
    }

    /** 运行其它类型任务 */
    @SuppressWarnings("unchecked")
    protected final V runTask() throws Exception {
        int type = (ctl & maskTaskType) >> offsetTaskType;
        switch (type) {
            case TaskBuilder.TYPE_RUNNABLE -> {
                Runnable task = (Runnable) action;
                task.run();
                return null;
            }
            case TaskBuilder.TYPE_CALLABLE -> {
                Callable<V> task = (Callable<V>) action;
                return task.call();
            }
            case TaskBuilder.TYPE_FUNCTION -> {
                Function<IContext, V> task = (Function<IContext, V>) action;
                return task.apply(promise.ctx());
            }
            case TaskBuilder.TYPE_CONSUMER -> {
                Consumer<IContext> task = (Consumer<IContext>) action;
                task.accept(promise.ctx());
                return null;
            }
            default -> {
                throw new AssertionError("type: " + type);
            }
        }
    }

    protected void clear() {
        action = null;
    }

    @Override
    public void run() {
        IPromise<V> promise = this.promise;
        if (promise.ctx().cancelToken().isCancelling()) {
            promise.trySetCancelled();
            clear();
            return;
        }
        if (promise.trySetComputing()) {
            try {
                if (getTaskType() == TaskBuilder.TYPE_TIMESHARING) {
                    @SuppressWarnings("unchecked") TimeSharingTask<V> task = (TimeSharingTask<V>) action;
                    ResultHolder<V> holder = task.step(promise.ctx());
                    if (holder != null) {
                        promise.trySetResult(holder.getResult());
                    } else {
                        promise.trySetException(TimeSharingTimeoutException.INSTANCE);
                    }
                } else {
                    V result = runTask();
                    promise.trySetResult(result);
                }
            } catch (Throwable e) {
                promise.trySetException(e);
            }
        }
        clear();
    }

    // region 转发适配

    @Override
    @Nonnull
    public IContext ctx() {
        return promise.ctx();
    }

    @Override
    @Nullable
    public Executor executor() {
        return promise.executor();
    }

    @Override
    @Nonnull
    public IFuture<V> toFuture() {
        return promise.toFuture();
    }

    @Override
    public <U> ICompletionStage<U> composeApply(BiFunction<? super IContext, ? super V, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return promise.composeApply(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> composeApply(BiFunction<? super IContext, ? super V, ? extends ICompletionStage<U>> fn) {
        return promise.composeApply(fn);
    }

    @Override
    public <U> ICompletionStage<U> composeApplyAsync(Executor executor, BiFunction<? super IContext, ? super V, ? extends ICompletionStage<U>> fn) {
        return promise.composeApplyAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> composeApplyAsync(Executor executor, BiFunction<? super IContext, ? super V, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return promise.composeApplyAsync(executor, fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> composeCall(Function<? super IContext, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return promise.composeCall(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> composeCall(Function<? super IContext, ? extends ICompletionStage<U>> fn) {
        return promise.composeCall(fn);
    }

    @Override
    public <U> ICompletionStage<U> composeCallAsync(Executor executor, Function<? super IContext, ? extends ICompletionStage<U>> fn) {
        return promise.composeCallAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> composeCallAsync(Executor executor, Function<? super IContext, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return promise.composeCallAsync(executor, fn, ctx, options);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> composeCatching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<V>> fallback, @Nullable IContext ctx, int options) {
        return promise.composeCatching(exceptionType, fallback, ctx, options);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> composeCatching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<V>> fallback) {
        return promise.composeCatching(exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> composeCatchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<V>> fallback) {
        return promise.composeCatchingAsync(executor, exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> composeCatchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends ICompletionStage<V>> fallback, @Nullable IContext ctx, int options) {
        return promise.composeCatchingAsync(executor, exceptionType, fallback, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> composeHandle(TriFunction<? super IContext, ? super V, ? super Throwable, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return promise.composeHandle(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> composeHandle(TriFunction<? super IContext, ? super V, ? super Throwable, ? extends ICompletionStage<U>> fn) {
        return promise.composeHandle(fn);
    }

    @Override
    public <U> ICompletionStage<U> composeHandleAsync(Executor executor, TriFunction<? super IContext, ? super V, ? super Throwable, ? extends ICompletionStage<U>> fn) {
        return promise.composeHandleAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> composeHandleAsync(Executor executor, TriFunction<? super IContext, ? super V, ? super Throwable, ? extends ICompletionStage<U>> fn, @Nullable IContext ctx, int options) {
        return promise.composeHandleAsync(executor, fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenApply(BiFunction<? super IContext, ? super V, ? extends U> fn, @Nullable IContext ctx, int options) {
        return promise.thenApply(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenApply(BiFunction<? super IContext, ? super V, ? extends U> fn) {
        return promise.thenApply(fn);
    }

    @Override
    public <U> ICompletionStage<U> thenApplyAsync(Executor executor, BiFunction<? super IContext, ? super V, ? extends U> fn) {
        return promise.thenApplyAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> thenApplyAsync(Executor executor, BiFunction<? super IContext, ? super V, ? extends U> fn, @Nullable IContext ctx, int options) {
        return promise.thenApplyAsync(executor, fn, ctx, options);
    }

    @Override
    public ICompletionStage<Void> thenAccept(BiConsumer<? super IContext, ? super V> action, @Nullable IContext ctx, int options) {
        return promise.thenAccept(action, ctx, options);
    }

    @Override
    public ICompletionStage<Void> thenAccept(BiConsumer<? super IContext, ? super V> action) {
        return promise.thenAccept(action);
    }

    @Override
    public ICompletionStage<Void> thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super V> action) {
        return promise.thenAcceptAsync(executor, action);
    }

    @Override
    public ICompletionStage<Void> thenAcceptAsync(Executor executor, BiConsumer<? super IContext, ? super V> action, @Nullable IContext ctx, int options) {
        return promise.thenAcceptAsync(executor, action, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenCall(Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        return promise.thenCall(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenCall(Function<? super IContext, ? extends U> fn) {
        return promise.thenCall(fn);
    }

    @Override
    public <U> ICompletionStage<U> thenCallAsync(Executor executor, Function<? super IContext, ? extends U> fn) {
        return promise.thenCallAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> thenCallAsync(Executor executor, Function<? super IContext, ? extends U> fn, @Nullable IContext ctx, int options) {
        return promise.thenCallAsync(executor, fn, ctx, options);
    }

    @Override
    public ICompletionStage<Void> thenRun(Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        return promise.thenRun(action, ctx, options);
    }

    @Override
    public ICompletionStage<Void> thenRun(Consumer<? super IContext> action) {
        return promise.thenRun(action);
    }

    @Override
    public ICompletionStage<Void> thenRunAsync(Executor executor, Consumer<? super IContext> action) {
        return promise.thenRunAsync(executor, action);
    }

    @Override
    public ICompletionStage<Void> thenRunAsync(Executor executor, Consumer<? super IContext> action, @Nullable IContext ctx, int options) {
        return promise.thenRunAsync(executor, action, ctx, options);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> catching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends V> fallback, @Nullable IContext ctx, int options) {
        return promise.catching(exceptionType, fallback, ctx, options);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> catching(Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends V> fallback) {
        return promise.catching(exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> catchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends V> fallback) {
        return promise.catchingAsync(executor, exceptionType, fallback);
    }

    @Override
    public <X extends Throwable> ICompletionStage<V> catchingAsync(Executor executor, Class<X> exceptionType, BiFunction<? super IContext, ? super X, ? extends V> fallback, @Nullable IContext ctx, int options) {
        return promise.catchingAsync(executor, exceptionType, fallback, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenHandle(TriFunction<? super IContext, ? super V, Throwable, ? extends U> fn, @Nullable IContext ctx, int options) {
        return promise.thenHandle(fn, ctx, options);
    }

    @Override
    public <U> ICompletionStage<U> thenHandle(TriFunction<? super IContext, ? super V, Throwable, ? extends U> fn) {
        return promise.thenHandle(fn);
    }

    @Override
    public <U> ICompletionStage<U> thenHandleAsync(Executor executor, TriFunction<? super IContext, ? super V, Throwable, ? extends U> fn) {
        return promise.thenHandleAsync(executor, fn);
    }

    @Override
    public <U> ICompletionStage<U> thenHandleAsync(Executor executor, TriFunction<? super IContext, ? super V, Throwable, ? extends U> fn, @Nullable IContext ctx, int options) {
        return promise.thenHandleAsync(executor, fn, ctx, options);
    }

    @Override
    public ICompletionStage<V> whenComplete(TriConsumer<? super IContext, ? super V, ? super Throwable> action, @Nullable IContext ctx, int options) {
        return promise.whenComplete(action, ctx, options);
    }

    @Override
    public ICompletionStage<V> whenComplete(TriConsumer<? super IContext, ? super V, ? super Throwable> action) {
        return promise.whenComplete(action);
    }

    @Override
    public ICompletionStage<V> whenCompleteAsync(Executor executor, TriConsumer<? super IContext, ? super V, ? super Throwable> action) {
        return promise.whenCompleteAsync(executor, action);
    }

    @Override
    public ICompletionStage<V> whenCompleteAsync(Executor executor, TriConsumer<? super IContext, ? super V, ? super Throwable> action, @Nullable IContext ctx, int options) {
        return promise.whenCompleteAsync(executor, action, ctx, options);
    }

    @Override
    public IFuture<V> asReadonly() {
        return promise.asReadonly();
    }

    @Override
    @Deprecated
    public boolean cancel(boolean mayInterruptIfRunning) {
        return promise.cancel(mayInterruptIfRunning);
    }

    @Override
    public State state() {
        return promise.state();
    }

    @Override
    public FutureState futureState() {
        return promise.futureState();
    }

    @Override
    public boolean isPending() {
        return promise.isPending();
    }

    @Override
    public boolean isComputing() {
        return promise.isComputing();
    }

    @Override
    public boolean isDone() {
        return promise.isDone();
    }

    @Override
    public boolean isCancelled() {
        return promise.isCancelled();
    }

    @Override
    public boolean isSucceeded() {
        return promise.isSucceeded();
    }

    @Override
    public boolean isFailed() {
        return promise.isFailed();
    }

    @Override
    public boolean isFailedOrCancelled() {
        return promise.isFailedOrCancelled();
    }

    @Override
    public V getNow() {
        return promise.getNow();
    }

    @Override
    public V getNow(V valueIfAbsent) {
        return promise.getNow(valueIfAbsent);
    }

    @Override
    public V resultNow() {
        return promise.resultNow();
    }

    @Override
    public Throwable exceptionNow() {
        return promise.exceptionNow();
    }

    @Override
    public Throwable exceptionNow(boolean throwIfCancelled) {
        return promise.exceptionNow(throwIfCancelled);
    }

    @Override
    public V get() throws InterruptedException, ExecutionException {
        return promise.get();
    }

    @Override
    public V get(long timeout, @Nonnull TimeUnit unit) throws InterruptedException, ExecutionException, TimeoutException {
        return promise.get(timeout, unit);
    }

    @Override
    public boolean await(long timeout, TimeUnit unit) throws InterruptedException {
        return promise.await(timeout, unit);
    }

    @Override
    public boolean awaitUninterruptibly(long timeout, TimeUnit unit) {
        return promise.awaitUninterruptibly(timeout, unit);
    }

    @Override
    public IFuture<V> await() throws InterruptedException {
        return promise.await();
    }

    @Override
    public IFuture<V> awaitUninterruptibly() {
        return promise.awaitUninterruptibly();
    }

    @Override
    public V join() {
        return promise.join();
    }

    @Override
    public boolean tryTransferTo(IPromise<? super V> output) {
        return promise.tryTransferTo(output);
    }

    @Override
    public void onCompleted(BiConsumer<? super IContext, ? super IFuture<V>> action, @Nonnull IContext context, int options) {
        promise.onCompleted(action, context, options);
    }

    @Override
    public void onCompletedAsync(Executor executor, BiConsumer<? super IContext, ? super IFuture<V>> action, @Nonnull IContext context, int options) {
        promise.onCompletedAsync(executor, action, context, options);
    }

    @Override
    public void onCompleted(Consumer<? super IFuture<V>> action, int options) {
        promise.onCompleted(action, options);
    }

    @Override
    public void onCompletedAsync(Executor executor, Consumer<? super IFuture<V>> action, int options) {
        promise.onCompletedAsync(executor, action, options);
    }

    // endregion
}