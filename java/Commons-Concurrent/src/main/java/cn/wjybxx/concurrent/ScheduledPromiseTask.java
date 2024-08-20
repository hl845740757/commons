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

import cn.wjybxx.base.IRegistration;
import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.collection.IndexedElement;
import cn.wjybxx.base.concurrent.CancelCodes;

import javax.annotation.Nonnull;
import java.util.concurrent.Callable;
import java.util.concurrent.Delayed;
import java.util.concurrent.TimeUnit;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 定时任务的Task抽象
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public final class ScheduledPromiseTask<V> extends PromiseTask<V>
        implements IScheduledFutureTask<V>, IndexedElement, CancelTokenListener {

    /** 任务的唯一id - 如果构造时未传入，要小心可见性问题 */
    private long id = -1;
    /** 提前计算的，逻辑上的下次触发时间 - 非volatile，不对用户开放 */
    private long nextTriggerTime;
    /** 任务的执行间隔 - 不再有特殊意义 */
    private long period;

    /** 截止时间 -- 有效性见{@link #MASK_HAS_DEADLINE} */
    private long deadline;
    /** 剩余次数 -- 有效性见{@link #MASK_HAS_COUNTDOWN} */
    private int countdown;

    /** 用于避免具体类型依赖 */
    private IScheduledHelper helper;
    /** 在队列中的下标 */
    private int queueIndex = INDEX_NOT_FOUND;
    /** 接收用户取消信号的句柄 -- 延时任务需要及时删除任务 */
    private IRegistration cancelRegistration;

    /**
     * @param promise         任务关联的promise
     * @param nextTriggerTime 任务的首次触发时间
     */
    private ScheduledPromiseTask(ScheduledTaskBuilder<V> builder, IScheduledPromise<V> promise,
                                 IScheduledHelper helper, long nextTriggerTime, long period) {
        super(builder, promise);
        this.helper = helper;
        this.nextTriggerTime = nextTriggerTime;
        this.period = period;
        setScheduleType(builder.getScheduleType());
    }

    /** 用于简单情况下的对象创建 -- 非周期性任务 */
    private ScheduledPromiseTask(Object action, Object ctx, int options, IScheduledPromise<V> promise, int taskType,
                                 IScheduledHelper helper, long nextTriggerTime) {
        super(action, ctx, options, promise, taskType);
        this.helper = helper;
        this.nextTriggerTime = nextTriggerTime;
        this.period = 0;
    }

    // region builder

    public static ScheduledPromiseTask<?> ofAction(Runnable action, ICancelToken cancelToken, int options,
                                                   IScheduledPromise<?> promise, IScheduledHelper helper, long triggerTime) {
        return new ScheduledPromiseTask<>(action, cancelToken, options, promise, TaskBuilder.TYPE_ACTION,
                helper, triggerTime);
    }

    public static ScheduledPromiseTask<?> ofAction(Consumer<? super IContext> action, IContext ctx, int options,
                                                   IScheduledPromise<?> promise, IScheduledHelper helper, long triggerTime) {
        return new ScheduledPromiseTask<>(action, ctx, options, promise, TaskBuilder.TYPE_ACTION_CTX,
                helper, triggerTime);
    }

    public static <V> ScheduledPromiseTask<V> ofFunction(Callable<? extends V> action, ICancelToken cancelToken, int options,
                                                         IScheduledPromise<V> promise, IScheduledHelper helper, long triggerTime) {
        return new ScheduledPromiseTask<>(action, cancelToken, options, promise, TaskBuilder.TYPE_FUNC,
                helper, triggerTime);
    }

    public static <V> ScheduledPromiseTask<V> ofFunction(Function<? super IContext, ? extends V> action, IContext ctx, int options,
                                                         IScheduledPromise<V> promise, IScheduledHelper helper, long triggerTime) {
        return new ScheduledPromiseTask<>(action, ctx, options, promise, TaskBuilder.TYPE_FUNC_CTX,
                helper, triggerTime);
    }

    public static <V> ScheduledPromiseTask<V> ofBuilder(TaskBuilder<V> builder, IScheduledPromise<V> promise, IScheduledHelper helper) {
        if (builder instanceof ScheduledTaskBuilder<V> sb) {
            return ofBuilder(sb, promise, helper);
        }
        return new ScheduledPromiseTask<>(builder.getTask(), builder.getCtx(), builder.getOptions(), promise, builder.getType(),
                helper, helper.tickTime());
    }

    /**
     * @param builder builder
     * @param promise 监听结果的promise
     * @param helper  helper
     * @return PromiseTask
     */
    public static <V> ScheduledPromiseTask<V> ofBuilder(ScheduledTaskBuilder<V> builder, IScheduledPromise<V> promise,
                                                        IScheduledHelper helper) {
        final long triggerTime = helper.triggerTime(builder.getInitialDelay(), builder.getTimeUnit());
        final long period = builder.isPeriodic()
                ? helper.triggerPeriod(builder.getPeriod(), builder.getTimeUnit())
                : 0;
        ScheduledPromiseTask<V> promiseTask = new ScheduledPromiseTask<>(builder, promise, helper, triggerTime, period);
        if (builder.isPeriodic()) {
            if (builder.hasTimeout()) {
                promiseTask.enableTimeout(helper.triggerTime(builder.getTimeout(), builder.getTimeUnit()));
            }
            if (builder.hasCountLimit()) {
                promiseTask.enableCountLimit(builder.getCountLimit());
            }
        }
        return promiseTask;
    }

    // endregion

    // region api-对EventLoop开放

    public long getId() {
        return id;
    }

    public void setId(long id) {
        this.id = id;
    }

    public long getNextTriggerTime() {
        return nextTriggerTime;
    }

    public void setNextTriggerTime(long nextTriggerTime) {
        this.nextTriggerTime = nextTriggerTime;
    }

    /** 获取任务的调度类型 */
    public int getScheduleType() {
        return (ctl & MASK_SCHEDULE_TYPE) >> OFFSET_SCHEDULE_TYPE;
    }

    /** 设置任务的调度类型 -- 应该在添加到队列之前设置 */
    private void setScheduleType(int scheduleType) {
        ctl |= (scheduleType << OFFSET_SCHEDULE_TYPE);
    }

    /** 任务是否触发过 -- 通常用于降低优先级 */
    public boolean isTriggered() {
        return (ctl & MASK_TRIGGERED) != 0;
    }

    /** 将任务标记为已触发过 */
    private void setTriggered() {
        ctl |= MASK_TRIGGERED;
    }

    /** 获取任务所属的队列id */
    public int getPriority() {
        return (ctl & MASK_PRIORITY);
    }

    /** @param priority 任务的优先级，范围 [0, 15] */
    public void setPriority(int priority) {
        if (priority < 0 || priority > MAX_PRIORITY) {
            throw new IllegalArgumentException("priority: " + MAX_PRIORITY);
        }
        ctl &= ~MASK_PRIORITY;
        ctl |= (priority);
    }

    @Override
    public boolean isPeriodic() {
        return (ctl & MASK_SCHEDULE_TYPE) != 0; // 无需位移
    }

    @Override
    public int collectionIndex(Object collection) {
        return queueIndex;
    }

    @Override
    public void collectionIndex(Object collection, int index) {
        this.queueIndex = index;
    }

    @Override
    public void clear() {
        super.clear();
        closeRegistration();
        id = -1;
        nextTriggerTime = 0;
        period = 0;
        helper = null;
    }

    private boolean hasTimeout() {
        return (ctl & PromiseTask.MASK_HAS_DEADLINE) != 0;
    }

    private void enableTimeout(long deadline) {
        ctl |= PromiseTask.MASK_HAS_DEADLINE;
        this.deadline = deadline;
    }

    private boolean hasCountLimit() {
        return (ctl & PromiseTask.MASK_HAS_COUNTDOWN) != 0;
    }

    private void enableCountLimit(int countdown) {
        ctl |= PromiseTask.MASK_HAS_COUNTDOWN;
        this.countdown = countdown;
    }

    // endregion

    // region core

    @Override
    public void run() {
        if (helper == null) {
            return; // 在任务执行完毕后收到取消信号
        }
        long tickTime = helper.tickTime();
        // 显式测试一次时间，适应多种EventLoop
        if (tickTime < nextTriggerTime) {
            // 未达触发时间时，显式测试一次取消
            ICancelToken cancelToken = getCancelToken();
            if (cancelToken.isCancelling() || promise.isDone()) {
                trySetCancelled(promise, cancelToken, CancelCodes.REASON_DEFAULT);
                helper.onCompleted(this);
            } else {
                helper.reschedule(this);
            }
            return;
        }
        if (trigger(tickTime)) {
            helper.reschedule(this);
        } else {
            helper.onCompleted(this);
        }
    }

    /**
     * 外部确定性触发，不需要回调的方式重新压入队列
     *
     * @return 如果需要再压入队列则返回true
     */
    public boolean trigger(long tickTime) {
        boolean firstTrigger = (ctl & MASK_TRIGGERED) == 0;
        if (firstTrigger) {
            ctl |= MASK_TRIGGERED;
        }

        final int scheduleType = getScheduleType();
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            super.run();
            return false;
        }

        IPromise<V> promise = this.promise;
        ICancelToken cancelToken = getCancelToken();
        // 检测取消信号 -- 为兼容，还要检测来自future的取消，即isComputing...
        if (cancelToken.isCancelling()) {
            trySetCancelled(promise, cancelToken);
            return false;
        }
        if (firstTrigger) {
            if (!promise.trySetComputing()) {
                return false;
            }
        } else if (!promise.isComputing()) {
            return false;
        }
        if (TaskOptions.isEnabled(options, TaskOptions.TIMEOUT_BEFORE_RUN)
                && hasTimeout() && deadline <= tickTime) {
            promise.trySetException(StacklessTimeoutException.INST);
            return false;
        }

        try {
            // 周期性任务，只有分时任务可以有结果
            if (getTaskType() == TaskBuilder.TYPE_TIMESHARING) {
                ResultHolder<V> holder = runTimeSharing(firstTrigger);
                if (holder != null) {
                    promise.trySetResult(holder.getResult());
                    return false;
                }
            } else {
                runTask();
            }
        } catch (Throwable ex) {
            ThreadUtils.recoveryInterrupted(ex);
            if (!canCaughtException(ex)) {
                promise.trySetException(ex);
                return false;
            }
            FutureLogger.logCause(ex, "periodic task caught exception");
        }
        // 任务执行后检测取消
        if (cancelToken.isCancelling() || !promise.isComputing()) {
            trySetCancelled(promise, cancelToken);
            return false;
        }
        // 未被取消的情况下检测超时
        if (hasTimeout() && deadline <= tickTime) {
            promise.trySetException(StacklessTimeoutException.INST);
            return false;
        }
        // 检测次数限制
        if (hasCountLimit() && (--countdown < 1)) {
            promise.trySetException(StacklessTimeoutException.INST_COUNT_LIMIT);
            return false;
        }
        setNextRunTime(tickTime, scheduleType);
        return true;
    }

    private boolean canCaughtException(Throwable ex) {
        if (getScheduleType() == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            return false;
        }
        if (getTaskType() == TaskBuilder.TYPE_TIMESHARING) {
            return false;
        }
        return TaskOptions.isEnabled(options, TaskOptions.CAUGHT_EXCEPTION);
    }

    private void setNextRunTime(long tickTime, int scheduleType) {
        long maxDelay = hasTimeout() ? (deadline - tickTime) : Long.MAX_VALUE;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE) {
            nextTriggerTime = nextTriggerTime + Math.clamp(period, 1, maxDelay); // 逻辑时间
        } else {
            nextTriggerTime = tickTime + Math.clamp(period, 1, maxDelay); // 真实时间
        }
    }
    // endregion

    // region cancel

    /** 监听取消令牌中的取消信号 */
    public void registerCancellation() {
        // java端放弃监听future的完成事件，延迟删除
        ICancelToken cancelToken = getCancelToken();
        if (cancelRegistration == null && cancelToken.canBeCancelled()) {
            cancelRegistration = cancelToken.thenNotify(this);
        }
    }

    /** 该方法为中转方法，EventLoop不应该调用 */
    @Deprecated
    @Override
    public void onCancelRequested(ICancelToken cancelToken) {
        // 用户通过令牌发起取消
        helper.onCancelRequested(this, cancelToken.cancelCode());
    }

    private void closeRegistration() {
        IRegistration cancelRegistration = this.cancelRegistration;
        if (cancelRegistration != null) {
            this.cancelRegistration = null;
            cancelRegistration.close();
        }
    }
    // endregion

    @Override
    public long getDelay(@Nonnull TimeUnit unit) {
        return helper.getDelay(nextTriggerTime, unit);
    }

    @Override
    public int compareTo(@Nonnull Delayed o) {
        return compareToExplicitly((ScheduledPromiseTask<?>) o);
    }

    public int compareToExplicitly(ScheduledPromiseTask<?> other) {
        if (other == this) {
            return 0;
        }
        int r = Long.compare(nextTriggerTime, other.nextTriggerTime);
        if (r != 0) {
            return r;
        }
        // 未触发的放前面
        r = Boolean.compare(isTriggered(), other.isTriggered());
        if (r != 0) {
            return r;
        }
        // 再按id排序
        r = Long.compare(id, other.id);
        if (r == 0) {
            throw new IllegalStateException("lhs.id: %d, rhs.id: %d".formatted(id, other.id));
        }
        return r;
    }

}