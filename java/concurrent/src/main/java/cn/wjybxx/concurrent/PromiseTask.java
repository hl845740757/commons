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
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * ps：该类的数据是（部分）开放的，以支持不同的扩展。
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public class PromiseTask<V> implements IFutureTask<V> {

    /**
     * 优先级的掩码 -- 8bit，最大255。
     * 1.放在低8位，减少运算，优先级的计算频率高于其它部分。
     * 2.大于{@link TaskOption}的中的64阶段。
     */
    protected static final int MASK_PRIORITY = 0xFF;
    /** 任务类型的掩码 -- 4bit，可省去大量的instanceof测试 */
    protected static final int MASK_TASK_TYPE = 0x0F00;
    /** 调度类型的掩码 -- 4bit，最大16种 */
    protected static final int MASK_SCHEDULE_TYPE = 0xF000;
    /** 是否已经声明任务的归属权 */
    protected static final int MASK_CLAIMED = 1 << 16;
    /** 分时任务是否已启动 */
    protected static final int MASK_STARTED = 1 << 17;
    /** 分时任务是否已停止 */
    protected static final int MASK_STOPPED = 1 << 18;
    /** 延时任务有超时时间 */
    protected static final int MASK_TIMEOUT = 1 << 20;
    /** 延时任务已触发过 */
    protected static final int MASK_TRIGGERED = 1 << 21;

    protected static final int OFFSET_PRIORITY = 0;
    /** 任务类型的偏移量 */
    protected static final int OFFSET_TASK_TYPE = 8;
    /** 调度类型的偏移量 */
    protected static final int OFFSET_SCHEDULE_TYPE = 12;
    /** 最大队列id */
    protected static final int MAX_PRIORITY = 255;

    /** 用户的任务 */
    private Object task;
    /** 任务上下文 */
    protected IContext ctx;
    /** 调度选项 */
    protected final int options;
    /** 任务关联的promise - 用户可能在任务完成后继续访问，因此不能清理 */
    protected final IPromise<V> promise;
    /** 控制标记 */
    protected int ctl;

    /**
     * @param task    用户的任务，支持的类型见{@link TaskBuilder#taskType(Object)}
     * @param ctx     任务关联的上下文
     * @param options 任务的调度选项
     * @param promise 任务关联的promise
     */
    public PromiseTask(Object task, IContext ctx, int options, IPromise<V> promise) {
        this(task, ctx, options, promise, TaskBuilder.taskType(task));
    }

    /**
     * @param builder 任务构建器
     * @param promise 任务关联的promise
     */
    public PromiseTask(TaskBuilder<V> builder, IPromise<V> promise) {
        this(builder.getTask(), builder.getCtx(), builder.getOptions(), promise, builder.getType());
    }

    public PromiseTask(Object task, IContext ctx, int options, IPromise<V> promise, int taskType) {
        this.task = Objects.requireNonNull(task, "action");
        this.ctx = ctx == null ? IContext.NONE : ctx;
        this.options = options;
        this.promise = Objects.requireNonNull(promise, "promise");
        this.ctl |= (taskType << OFFSET_TASK_TYPE);
        // 注入promise
        if (taskType == TaskBuilder.TYPE_TIMESHARING) {
            @SuppressWarnings("unchecked") TimeSharingTask<V> timeSharingTask = (TimeSharingTask<V>) task;
            timeSharingTask.inject(this.ctx, this.promise);
        }
    }

    // region factory

    public static PromiseTask<?> ofAction(Runnable action, IContext ctx, int options, IPromise<?> promise) {
        return new PromiseTask<>(action, ctx, options, promise, TaskBuilder.TYPE_ACTION);
    }

    public static PromiseTask<?> ofAction(Consumer<? super IContext> action, IContext ctx, int options, IPromise<?> promise) {
        return new PromiseTask<>(action, ctx, options, promise, TaskBuilder.TYPE_ACTION_CTX);
    }

    public static <V> PromiseTask<V> ofFunction(Callable<? extends V> action, IContext ctx, int options, IPromise<V> promise) {
        return new PromiseTask<>(action, ctx, options, promise, TaskBuilder.TYPE_FUNC);
    }

    public static <V> PromiseTask<V> ofFunction(Function<? super IContext, ? extends V> action, IContext ctx, int options, IPromise<V> promise) {
        return new PromiseTask<>(action, ctx, options, promise, TaskBuilder.TYPE_FUNC_CTX);
    }

    public static <V> PromiseTask<V> ofBuilder(TaskBuilder<V> builder, IPromise<V> promise) {
        return new PromiseTask<>(builder, promise);
    }
    // endregion

    // region open

    @Override
    public final int getOptions() {
        return options;
    }

    /** 任务是否启用了指定选项 */
    public final boolean isEnabled(int taskOption) {
        return TaskOption.isEnabled(options, taskOption);
    }

    /** 获取绑定的任务 */
    public final Object getTask() {
        return task;
    }

    /** 获取任务所属的队列id */
    public final int getPriority() {
        return (ctl & MASK_PRIORITY);
    }

    /** @param priority 任务的优先级，范围 [0, 255] */
    public final void setPriority(int priority) {
        if (priority < 0 || priority > MAX_PRIORITY) {
            throw new IllegalArgumentException("priority: " + MAX_PRIORITY);
        }
        ctl &= ~MASK_PRIORITY;
        ctl |= (priority);
    }

    /** 获取任务的类型 -- 在可能包含分时任务的情况下要进行判断 */
    public final int getTaskType() {
        return (ctl & MASK_TASK_TYPE) >> OFFSET_TASK_TYPE;
    }

    /** 获取任务的调度类型 */
    public final int getScheduleType() {
        return (ctl & MASK_SCHEDULE_TYPE) >> OFFSET_SCHEDULE_TYPE;
    }

    /** 设置任务的调度类型 -- 应该在添加到队列之前设置 */
    protected final void setScheduleType(int scheduleType) {
        ctl |= (scheduleType << OFFSET_SCHEDULE_TYPE);
    }

    /** 是否已经声明任务的归属权 */
    protected final boolean isClaimed() {
        return (ctl & MASK_CLAIMED) != 0;
    }

    /** 将任务标记为已申领 */
    protected final void setClaimed() {
        ctl |= MASK_CLAIMED;
    }

    /** 分时任务是否启动 */
    protected final boolean isStarted() {
        return (ctl & MASK_STARTED) != 0;
    }

    /** 将分时任务标记为已启动 */
    protected final void setStarted() {
        ctl |= MASK_STARTED;
    }

    /** 获取ctl中的某个bit */
    protected final boolean getCtlBit(int mask) {
        return (ctl & mask) != 0;
    }

    /** 设置ctl中的某个bit */
    protected final void setCtlBit(int mask, boolean value) {
        if (value) {
            ctl |= mask;
        } else {
            ctl &= ~mask;
        }
    }

    /** 获取任务绑的Promise - 允许子类重写返回值类型 */
    @Override
    public IPromise<V> future() {
        return promise;
    }

    // endregion

    // region core

    public void clear() {
        task = null;
        ctx = null;
    }

    /** 运行分时任务 */
    @SuppressWarnings("unchecked")
    protected final void runTimeSharing() throws Exception {
        TimeSharingTask<V> task = (TimeSharingTask<V>) this.task;
        if (!isStarted()) {
            IPromise<V> promise = this.promise;
            IContext ctx = this.ctx;
            task.start(ctx, promise);
            setStarted();

            if (promise.isDone()) {
                stopTask(task, promise, ctx);
                return;
            }
            // 需要捕获task -- 避免和clear冲突，我们使用另一个对象来捕获上下文；同时绑定回调线程为当前Executor
            StopInvoker<V> invoker = new StopInvoker<>(task, ctx);
            promise.onCompletedAsync(promise.executor(), invoker, TaskOption.STAGE_TRY_INLINE);
        }
        task.update(ctx, promise);
    }

    /** 运行其它类型任务 */
    @SuppressWarnings("unchecked")
    protected final V runTask() throws Exception {
        int type = (ctl & MASK_TASK_TYPE) >> OFFSET_TASK_TYPE;
        switch (type) {
            case TaskBuilder.TYPE_ACTION -> {
                Runnable task = (Runnable) this.task;
                task.run();
                return null;
            }
            case TaskBuilder.TYPE_FUNC -> {
                Callable<V> task = (Callable<V>) this.task;
                return task.call();
            }
            case TaskBuilder.TYPE_FUNC_CTX -> {
                Function<IContext, V> task = (Function<IContext, V>) this.task;
                return task.apply(ctx);
            }
            case TaskBuilder.TYPE_ACTION_CTX -> {
                Consumer<IContext> task = (Consumer<IContext>) this.task;
                task.accept(ctx);
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
        IContext ctx = this.ctx;
        if (ctx.cancelToken().isCancelling()) {
            trySetCancelled(promise, ctx);
            clear();
            return;
        }
        if (promise.trySetComputing()) {
            try {
                if (getTaskType() == TaskBuilder.TYPE_TIMESHARING) {
                    runTimeSharing();
                    if (!promise.isDone()) {
                        promise.trySetException(StacklessTimeoutException.INST);
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

    protected static void trySetCancelled(IPromise<?> promise, IContext ctx) {
        int cancelCode = ctx.cancelToken().cancelCode();
        assert cancelCode != 0;
        promise.trySetCancelled(cancelCode);
    }

    protected static void trySetCancelled(IPromise<?> promise, IContext ctx, int def) {
        int cancelCode = ctx.cancelToken().cancelCode();
        if (cancelCode == 0) cancelCode = def;
        promise.trySetCancelled(cancelCode);
    }

    private static class StopInvoker<V> implements Consumer<Future<?>> {

        TimeSharingTask<V> task;
        IContext ctx;

        public StopInvoker(TimeSharingTask<V> task, IContext ctx) {
            this.task = task;
            this.ctx = ctx;
        }

        @Override
        public void accept(Future<?> future) {
            @SuppressWarnings("unchecked") IPromise<V> promise = (IPromise<V>) future;
            stopTask(task, promise, ctx);
            task = null;
        }
    }

    private static <V> void stopTask(TimeSharingTask<V> task, IPromise<V> promise, IContext ctx) {
        try {
            task.stop(ctx, promise);
        } catch (Throwable ex) {
            FutureLogger.logCause(ex, "task.stop caught exception");
        }
    }
}