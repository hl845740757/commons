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

import cn.wjybxx.disruptor.StacklessTimeoutException;

import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.concurrent.Future;
import java.util.concurrent.RunnableFuture;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 1.实现{@link RunnableFuture}是为了适配JDK的实现类，实际上更建议组合。
 * 2.该类的数据是（部分）开放的，以支持不同的扩展。
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public class PromiseTask<V> implements IFutureTask<V> {

    /**
     * queueId的掩码 -- 8bit，最大255。
     * 1.放在低8位，减少运算，queueId的计算频率高于其它部分。
     * 2.大于{@link TaskOption}的中的64阶段。
     */
    protected static final int maskQueueId = 0xFF;
    /** 任务类型的掩码 -- 4bit，可省去大量的instanceof测试 */
    protected static final int maskTaskType = 0x0F00;
    /** 调度类型的掩码 -- 4bit，最大16种 */
    protected static final int maskScheduleType = 0xF000;
    /** 是否已经声明任务的归属权 */
    protected static final int maskClaimed = 1 << 16;
    /** 分时任务是否已启动 */
    protected static final int maskStarted = 1 << 17;
    /** 分时任务是否已停止 */
    protected static final int maskStopped = 1 << 18;

    protected static final int offsetQueueId = 0;
    protected static final int offsetTaskType = 8;
    protected static final int offsetScheduleType = 12;
    protected static final int maxQueueId = 255;

    /** 用户的任务 */
    private Object action;
    /** 用户可能在任务完成后继续访问，因此不能清理 */
    protected final IPromise<V> promise;
    /** 控制标记 */
    protected int ctl;
    /** 调度选项 */
    protected int options;

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

    public PromiseTask(Object action, IPromise<V> promise, int taskType) {
        this.action = Objects.requireNonNull(action, "action");
        this.promise = Objects.requireNonNull(promise, "promise");
        this.ctl |= (taskType << offsetTaskType);
        // 注入promise
        if (taskType == TaskBuilder.TYPE_TIMESHARING) {
            @SuppressWarnings("unchecked") TimeSharingTask<V> timeSharingTask = (TimeSharingTask<V>) action;
            timeSharingTask.inject(promise);
        }
    }

    // region factory

    public static PromiseTask<?> ofRunnable(Runnable action, IPromise<?> promise) {
        return new PromiseTask<>(action, promise, TaskBuilder.TYPE_RUNNABLE);
    }

    public static <V> PromiseTask<V> ofCallable(Callable<? extends V> action, IPromise<V> promise) {
        return new PromiseTask<>(action, promise, TaskBuilder.TYPE_CALLABLE);
    }

    public static <V> PromiseTask<V> ofFunction(Function<? super IContext, ? extends V> action, IPromise<V> promise) {
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
    @Override
    public final void setOptions(int options) {
        this.options = options;
    }

    @Override
    public final int getOptions() {
        return options;
    }

    /** 任务是否启用了指定选项 */
    public boolean isEnable(int taskOption) {
        return TaskOption.isEnabled(options, taskOption);
    }

    /** 获取绑定的任务 */
    public final Object getAction() {
        return action;
    }

    /** 获取任务所属的队列id */
    public final int getQueueId() {
        return (ctl & maskQueueId);
    }

    /** @param queueId 队列id，范围 [0, 255] */
    public final void setQueueId(int queueId) {
        if (queueId < 0 || queueId > maxQueueId) {
            throw new IllegalArgumentException("queueId: " + maxQueueId);
        }
        ctl &= ~maskQueueId;
        ctl |= (queueId);
    }

    /** 获取任务的类型 -- 在可能包含分时任务的情况下要进行判断 */
    public final int getTaskType() {
        return (ctl & maskTaskType) >> offsetTaskType;
    }

    /** 获取任务的调度类型 */
    public final int getScheduleType() {
        return (ctl & maskScheduleType) >> offsetScheduleType;
    }

    /** 设置任务的调度类型 -- 应该在添加到队列之前设置 */
    public final void setScheduleType(int scheduleType) {
        ctl |= (scheduleType << offsetScheduleType);
    }

    /** 是否是循环任务 */
    public final boolean isPeriodic() {
        return getScheduleType() != 0;
    }

    /** 是否已经声明任务的归属权 */
    public final boolean isClaimed() {
        return (ctl & maskClaimed) != 0;
    }

    /** 将任务标记为已申领 */
    public final void setClaimed() {
        ctl |= maskClaimed;
    }

    /** 分时任务是否启动 */
    public final boolean isStarted() {
        return (ctl & maskStarted) != 0;
    }

    /** 将分时任务标记为已启动 */
    public final void setStarted() {
        ctl |= maskStarted;
    }

    /** 允许子类重写返回值类型 */
    @Override
    public IFuture<V> future() {
        return promise;
    }

    /** 获取任务绑的Promise - 允许子类重写返回值类型 */
    public IPromise<V> getPromise() {
        return promise;
    }

    // endregion

    // region core

    public void clear() {
        action = null;
    }

    /** 运行分时任务 */
    @SuppressWarnings("unchecked")
    protected final void runTimeSharing() throws Exception {
        TimeSharingTask<V> task = (TimeSharingTask<V>) action;
        if (!isStarted()) {
            IPromise<V> promise = this.promise;
            task.start(promise);
            setStarted();

            if (promise.isDone()) {
                stopTask(task, promise);
                return;
            }
            // 需要捕获task -- 避免和clear冲突，我们使用另一个对象来捕获上下文；同时绑定回调线程为当前Executor
            StopInvoker<V> invoker = new StopInvoker<>(task);
            promise.onCompletedAsync(promise.executor(), invoker, TaskOption.STAGE_TRY_INLINE);
        }
        task.update(promise);
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
                    runTimeSharing();
                    if (!promise.isDone()) {
                        promise.trySetException(StacklessTimeoutException.INSTANCE);
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

    private static class StopInvoker<V> implements Consumer<Future<?>> {

        TimeSharingTask<V> task;

        public StopInvoker(TimeSharingTask<V> task) {
            this.task = task;
        }

        @Override
        public void accept(Future<?> future) {
            @SuppressWarnings("unchecked") IPromise<V> promise = (IPromise<V>) future;
            stopTask(task, promise);
            task = null;
        }
    }

    private static <V> void stopTask(TimeSharingTask<V> task, IPromise<V> promise) {
        try {
            task.stop(promise);
        } catch (Throwable ex) {
            FutureLogger.logCause(ex, "task.stop caught exception");
        }
    }
}