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
import cn.wjybxx.base.pool.ConcurrentObjectPool;

import javax.annotation.Nonnull;
import java.util.concurrent.Callable;
import java.util.concurrent.Delayed;
import java.util.concurrent.TimeUnit;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 定时任务的Task抽象
 * <p>
 * 1.promise只可以在EventLoop下完成。
 * 2.Task不可主动调用回收，应当由调度器触发回收。
 *
 * @author wjybxx
 * date - 2024/1/8
 */
public final class ScheduledPromiseTask<V> extends PromiseTask<V>
        implements IScheduledFutureTask<V>, IndexedElement {

    /** 任务的唯一id - 如果构造时未传入，要小心可见性问题 */
    private long id = -1;
    /** 提前计算的，逻辑上的下次触发时间 - 非volatile，不对用户开放 */
    private long triggerTime;
    /** 任务的执行间隔 - 不再有特殊意义 */
    private long period;
    /** 截止时间 -- 有效性见{@link #MASK_HAS_DEADLINE} */
    private long deadline;
    /** 剩余次数 -- 有效性见{@link #MASK_HAS_COUNTDOWN} */
    private int countdown;

    /** 用于避免具体类型依赖 */
    private ISchedulerHelper helper;
    /** 在队列中的下标 */
    private int qIndex = INDEX_NOT_FOUND;
    /** 接收用户取消信号的句柄 -- 延时任务需要及时删除任务 */
    private IRegistration cancelRegistration;

    private ScheduledPromiseTask() {
    }

    // region init

    /** 用于简单情况下的对象创建 -- 非周期性任务 */
    private void init(int taskType, Object action, Object ctx, int options, IScheduledPromise<V> promise,
                      long firstDelay, TimeUnit timeUnit) {
        init(taskType, action, ctx, options, promise);
        // 时间戳先保存为nanos单位
        this.triggerTime = timeUnit.toNanos(firstDelay);
        this.period = 0;
    }

    private void init(ScheduledTaskBuilder<V> builder, IScheduledPromise<V> promise) {
        init(builder.getType(), builder.getTask(), builder.getCtx(), builder.getOptions(), promise);
        setScheduleType(builder.getScheduleType());
        // 时间戳先保存为nanos单位
        TimeUnit timeUnit = builder.getTimeUnit();
        this.triggerTime = timeUnit.toNanos(builder.getInitialDelay());
        this.period = timeUnit.toNanos(builder.getPeriod());
        // 初始化周期任务数据
        if (builder.isPeriodic()) {
            if (period < 0) throw new RuntimeException("period: " + period);
            if (builder.hasTimeout()) {
                ctl |= PromiseTask.MASK_HAS_DEADLINE;
                this.deadline = timeUnit.toNanos(builder.getTimeout());
            }
            if (builder.hasCountLimit()) {
                ctl |= PromiseTask.MASK_HAS_COUNTDOWN;
                this.countdown = builder.getCountLimit();
            }
        }
    }

    /**
     * 事件循环在将任务插入到队列时调用该方法初始化任务
     *
     * @param helper 事件循环的helper
     */
    public void inject(ISchedulerHelper helper) {
        this.id = helper.nextId();
        this.helper = helper;

        this.triggerTime = helper.triggerTime(triggerTime, TimeUnit.NANOSECONDS);
        if (isPeriodic()) {
            this.period = helper.triggerPeriod(period, TimeUnit.NANOSECONDS);
        }
        // 这里第二次读取TickTime，Deadline可能大于预期值，问题不大
        if (hasDeadline()) {
            this.deadline = helper.triggerTime(deadline, TimeUnit.NANOSECONDS);
        }
    }

    // endregion

    // region api-对EventLoop开放

    public long getId() {
        return id;
    }

    public long getTriggerTime() {
        return triggerTime;
    }

    /** 保留set以允许外部调整优先级 */
    public void setTriggerTime(long triggerTime) {
        this.triggerTime = triggerTime;
    }

    /** 获取任务的调度类型 */
    private int getScheduleType() {
        return (ctl & MASK_SCHEDULE_TYPE) >> OFFSET_SCHEDULE_TYPE;
    }

    /** 设置任务的调度类型 -- 应该在添加到队列之前设置 */
    private void setScheduleType(int scheduleType) {
        ctl |= (scheduleType << OFFSET_SCHEDULE_TYPE);
    }

    /** 设置任务的优先级 */
    public int getPriority() {
        return TaskOptions.getPriority(options);
    }

    /** @param priority 任务的优先级，范围 [0, 31] */
    public void setPriority(int priority) {
        options = TaskOptions.setPriority(options, priority);
    }

    /** 任务是否触发过 -- 通常用于降低优先级 */
    public boolean isTriggered() {
        return (ctl & MASK_TRIGGERED) != 0;
    }

    @Override
    public boolean isPeriodic() {
        return (ctl & MASK_SCHEDULE_TYPE) != 0;
    }

    @Override
    public int collectionIndex(Object collection) {
        return qIndex;
    }

    @Override
    public void collectionIndex(Object collection, int index) {
        this.qIndex = index;
    }

    private boolean hasDeadline() {
        return (ctl & PromiseTask.MASK_HAS_DEADLINE) != 0;
    }

    private boolean hasCountdown() {
        return (ctl & PromiseTask.MASK_HAS_COUNTDOWN) != 0;
    }

    public IRegistration getCancelRegistration() {
        return cancelRegistration;
    }

    public void setCancelRegistration(IRegistration cancelRegistration) {
        this.cancelRegistration = cancelRegistration;
    }

    // endregion

    // region core

    @Override
    protected void reset() {
        super.reset();
        id = -1;
        triggerTime = 0;
        period = 0;
        deadline = 0;
        countdown = 0;
        helper = null;
        qIndex = INDEX_NOT_FOUND;
        cancelRegistration = null;
    }

    @Override
    public void cancel(int cancelCode) {
        // 只支持在EventLoop线程主动取消，否则存在数据可见性问题
        if (helper == null) {
            throw new IllegalStateException();
        }
        assert helper.inEventLoop();
        trySetCancelled(promise, cancelCode);
    }

    @Override
    public void run() {
        if (helper == null) {
            throw new IllegalStateException("helper is uninitialized");
        }
        helper.doSchedule(this);
    }

    /**
     * 外部确定性触发。
     * 该方法由EventLoop调用，不需要回调的方式重新压入队列，而是返回bool值告知EventLoop是否需要继续执行。
     * 在该方法返回false后，EventLoop不可再持有Task的引用。
     *
     * @return 如果需要再压入队列则返回true
     */
    public boolean trigger(long tickTime) {
        if (trigger0(tickTime)) {
            return true;
        }
        closeRegistration();
        return false;
    }

    private boolean trigger0(long tickTime) {
        boolean firstTrigger = (ctl & MASK_TRIGGERED) == 0;
        if (firstTrigger) {
            ctl |= MASK_TRIGGERED;
        }
        // 先检测取消
        IPromise<V> promise = this.promise;
        ICancelToken cancelToken = getCancelToken();
        if (cancelToken.isCancelRequested()) {
            trySetCancelled(promise, cancelToken.cancelCode());
            return false;
        }
        // 一次性任务 -- 不能调用基类的Run
        final int scheduleType = getScheduleType();
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            if (!promise.trySetComputing()) {
                return false;
            }
            try {
                V result = runTask();
                promise.trySetResult(result);
            } catch (Throwable e) {
                promise.trySetException(e);
            }
            return false;
        }
        // 周期性任务
        if (firstTrigger) {
            if (!promise.trySetComputing()) {
                return false;
            }
        } else if (!promise.isComputing()) {
            return false;
        }
        try {
            runTask();
        } catch (Throwable ex) {
            // 通过异常传递结果
            if (ex instanceof TaskResultException taskResultException) {
                @SuppressWarnings("unchecked") V result = (V) taskResultException.getResult();
                promise.trySetResult(result);
                return false;
            }
            ThreadUtils.recoveryInterrupted(ex);
            if (!canCaughtException(ex)) {
                promise.trySetException(ex);
                return false;
            }
            FutureLogger.logCause(ex, "periodic task caught exception");
        }
        // 任务执行后检测取消
        if (cancelToken.isCancelRequested() || !promise.isComputing()) {
            trySetCancelled(promise, cancelToken, CancelCodes.REASON_DEFAULT);
            return false;
        }
        // 未被取消的情况下检测超时
        if (hasDeadline() && deadline <= tickTime) {
            promise.trySetException(StacklessCancellationException.TIMEOUT);
            return false;
        }
        // 检测次数限制
        if (hasCountdown() && (--countdown < 1)) {
            promise.trySetException(StacklessCancellationException.COUNT_LIMIT);
            return false;
        }
        setNextRunTime(tickTime, scheduleType);
        return true;
    }

    private boolean canCaughtException(Throwable ex) {
        if (getScheduleType() == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            return false;
        }
        return TaskOptions.isEnabled(options, TaskOptions.CAUGHT_EXCEPTION);
    }

    private void setNextRunTime(long tickTime, int scheduleType) {
        long maxDelay = hasDeadline() ? (deadline - tickTime) : Long.MAX_VALUE;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE) {
            triggerTime = triggerTime + Math.min(period, maxDelay); // 逻辑时间
        } else {
            triggerTime = tickTime + Math.min(period, maxDelay); // 真实时间
        }
    }

    private void closeRegistration() {
        IRegistration cancelRegistration = this.cancelRegistration;
        if (cancelRegistration != null) {
            this.cancelRegistration = null;
            cancelRegistration.close();
        }
    }
    // endregion

    // region cancel

    private static boolean trySetCancelled(IPromise<?> promise, int cancelCode) {
        return promise.trySetCancelled(cancelCode);
    }

    private static boolean trySetCancelled(IPromise<?> promise, ICancelToken cancelToken, int def) {
        int cancelCode = cancelToken.cancelCode();
        if (cancelCode == 0) cancelCode = def;
        return promise.trySetCancelled(cancelCode);
    }
    // endregion

    @Override
    public long getDelay(@Nonnull TimeUnit unit) {
        return helper.getDelay(triggerTime, unit);
    }

    @Override
    public int compareTo(@Nonnull Delayed o) {
        return compareToExplicitly((ScheduledPromiseTask<?>) o);
    }

    public int compareToExplicitly(ScheduledPromiseTask<?> other) {
        if (other == this) {
            return 0;
        }
        int r = Long.compare(triggerTime, other.triggerTime);
        if (r != 0) {
            return r;
        }
        // 未触发的放前面
        r = Boolean.compare(isTriggered(), other.isTriggered());
        if (r != 0) {
            return r;
        }
        // 再按优先级排序
        r = Integer.compare(getPriority(), other.getPriority());
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

    // region factory

    private static final ConcurrentObjectPool<ScheduledPromiseTask<?>> POOL = new ConcurrentObjectPool<>(
            ScheduledPromiseTask::new, ScheduledPromiseTask::reset,
            TaskPoolConfig.getPoolSize(TaskPoolType.SCHEDULED_PROMISE_TASK));

    public static void release(ScheduledPromiseTask<?> task) {
        POOL.release(task);
    }

    /**
     * 申请一个Task对象，Task在进入完成状态后会自动回收。
     * 注意：该对象不可返回给用户！该对象不可返回给用户！该对象不可返回给用户！
     *
     * @param taskType 任务类型 -- 注意上下文的类型
     * @param task     用户的任务，支持的类型见{@link TaskBuilder}
     * @param ctx      任务关联的上下文
     * @param options  任务的调度选项
     * @param promise  任务关联的promise
     * @param delay    触发延迟
     * @param timeUnit 时间单位
     */
    public static <V> ScheduledPromiseTask<V> acquire(int taskType, Object task, Object ctx, int options, IScheduledPromise<V> promise,
                                                      long delay, TimeUnit timeUnit) {
        @SuppressWarnings("unchecked") ScheduledPromiseTask<V> promiseTask = (ScheduledPromiseTask<V>) POOL.acquire();
        promiseTask.init(taskType, task, ctx, options, promise, delay, timeUnit);
        return promiseTask;
    }

    public static <V> ScheduledPromiseTask<V> acquire(ScheduledTaskBuilder<V> builder, IScheduledPromise<V> promise) {
        @SuppressWarnings("unchecked") ScheduledPromiseTask<V> promiseTask = (ScheduledPromiseTask<V>) POOL.acquire();
        promiseTask.init(builder, promise);
        return promiseTask;
    }

    public static ScheduledPromiseTask<?> ofEmpty(ICancelToken cancelToken, int options,
                                                  IScheduledPromise<?> promise,
                                                  long delay, TimeUnit timeUnit) {
        return acquire(TaskBuilder.TYPE_EMPTY, null, cancelToken, options, promise,
                delay, timeUnit);
    }

    public static ScheduledPromiseTask<?> ofAction(Runnable action, ICancelToken cancelToken, int options,
                                                   IScheduledPromise<?> promise,
                                                   long delay, TimeUnit timeUnit) {
        return acquire(TaskBuilder.TYPE_ACTION, action, cancelToken, options, promise,
                delay, timeUnit);
    }

    public static ScheduledPromiseTask<?> ofAction(Consumer<Object> action, Object ctx, int options,
                                                   IScheduledPromise<?> promise,
                                                   long delay, TimeUnit timeUnit) {
        return acquire(TaskBuilder.TYPE_ACTION_CTX, action, ctx, options, promise,
                delay, timeUnit);
    }

    public static <V> ScheduledPromiseTask<V> ofFunction(Callable<? extends V> action, ICancelToken cancelToken, int options,
                                                         IScheduledPromise<V> promise,
                                                         long delay, TimeUnit timeUnit) {
        return acquire(TaskBuilder.TYPE_FUNC, action, cancelToken, options, promise,
                delay, timeUnit);
    }

    public static <V> ScheduledPromiseTask<V> ofFunction(Function<Object, ? extends V> action, Object ctx, int options,
                                                         IScheduledPromise<V> promise,
                                                         long delay, TimeUnit timeUnit) {
        return acquire(TaskBuilder.TYPE_FUNC_CTX, action, ctx, options, promise,
                delay, timeUnit);
    }

    public static <V> ScheduledPromiseTask<V> ofBuilder(ScheduledTaskBuilder<V> builder, IScheduledPromise<V> promise) {
        return acquire(builder, promise);
    }

    // endregion

}