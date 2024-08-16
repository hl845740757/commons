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

import javax.annotation.concurrent.NotThreadSafe;
import java.util.Objects;
import java.util.concurrent.Callable;
import java.util.concurrent.TimeUnit;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * 该对象为临时对象，应避免共享
 *
 * @author wjybxx
 * date 2023/4/14
 */
@NotThreadSafe
public final class ScheduledTaskBuilder<V> extends TaskBuilder<V> {

    /** 执行一次 */
    public static final byte SCHEDULE_ONCE = 0;
    /** 固定延迟 -- 两次执行的间隔大于等于给定的延迟 */
    public static final byte SCHEDULE_FIXED_DELAY = 1;
    /** 固定频率 -- 执行次数 */
    public static final byte SCHEDULE_FIXED_RATE = 2;
    /** 动态延迟 -- 每次执行后计算下一次的延迟 */
    public static final byte SCHEDULE_DYNAMIC_DELAY = 3;

    private byte scheduleType = 0;
    private long initialDelay;
    private long period;
    private long timeout = -1;
    private TimeUnit timeUnit = TimeUnit.MILLISECONDS;

    private ScheduledTaskBuilder(int type, Object task) {
        super(type, task);
    }

    private ScheduledTaskBuilder(int type, Object task, Object ctx) {
        super(type, task, ctx);
    }

    public ScheduledTaskBuilder(TaskBuilder<? extends V> taskBuilder) {
        super(taskBuilder);
    }

    // region factory

    public static ScheduledTaskBuilder<Object> newAction(Runnable task) {
        return new ScheduledTaskBuilder<>(TYPE_ACTION, task);
    }

    public static ScheduledTaskBuilder<Object> newAction(Runnable task, ICancelToken cancelToken) {
        return new ScheduledTaskBuilder<>(TYPE_ACTION, task, cancelToken);
    }

    public static ScheduledTaskBuilder<Object> newAction(Consumer<IContext> task, IContext ctx) {
        return new ScheduledTaskBuilder<>(TYPE_ACTION_CTX, task, ctx);
    }

    public static <V> ScheduledTaskBuilder<V> newFunc(Callable<? extends V> task) {
        return new ScheduledTaskBuilder<>(TYPE_FUNC, task);
    }

    public static <V> ScheduledTaskBuilder<V> newFunc(Callable<? extends V> task, ICancelToken cancelToken) {
        return new ScheduledTaskBuilder<>(TYPE_FUNC, task, cancelToken);
    }

    public static <V> ScheduledTaskBuilder<V> newFunc(Function<IContext, ? extends V> task, IContext ctx) {
        return new ScheduledTaskBuilder<>(TYPE_FUNC_CTX, task, ctx);
    }

    // PECS -- Task消费泛型参数
    public static <V> ScheduledTaskBuilder<V> newTimeSharing(TimeSharingTask<? super V> task) {
        return new ScheduledTaskBuilder<>(TYPE_TIMESHARING, task, IContext.NONE);
    }

    public static <V> ScheduledTaskBuilder<V> newTimeSharing(TimeSharingTask<? super V> task, IContext ctx) {
        return new ScheduledTaskBuilder<>(TYPE_TIMESHARING, task, ctx);
    }

    /** 适用于禁止初始延迟小于0的情况 */
    public static void validateInitialDelay(long initialDelay) {
        if (initialDelay < 0) {
            throw new IllegalArgumentException(
                    String.format("initialDelay: %d (expected: >= 0)", initialDelay));
        }
    }

    public static void validatePeriod(long period) {
        if (period == 0) {
            throw new IllegalArgumentException("period: 0 (expected: != 0)");
        }
    }

    // endregion

    // region 调度方式

    public int getScheduleType() {
        return scheduleType;
    }

    public long getInitialDelay() {
        return initialDelay;
    }

    public long getPeriod() {
        return period;
    }

    public boolean isPeriodic() {
        return scheduleType != 0;
    }

    public boolean isOnlyOnce() {
        return scheduleType == SCHEDULE_ONCE;
    }

    public ScheduledTaskBuilder<V> setOnlyOnce(long delay) {
        this.scheduleType = SCHEDULE_ONCE;
        this.initialDelay = delay;
        this.period = 0;
        return this;
    }

    public ScheduledTaskBuilder<V> setOnlyOnce(long delay, TimeUnit unit) {
        setOnlyOnce(delay);
        this.timeUnit = Objects.requireNonNull(unit);
        return this;
    }

    public boolean isFixedDelay() {
        return scheduleType == SCHEDULE_FIXED_DELAY;
    }

    public ScheduledTaskBuilder<V> setFixedDelay(long initialDelay, long period) {
        validatePeriod(period);
        this.scheduleType = SCHEDULE_FIXED_DELAY;
        this.initialDelay = initialDelay;
        this.period = period;
        return this;
    }

    public ScheduledTaskBuilder<V> setFixedDelay(long initialDelay, long period, TimeUnit unit) {
        setFixedDelay(initialDelay, period);
        this.timeUnit = Objects.requireNonNull(unit);
        return this;
    }

    public boolean isFixedRate() {
        return scheduleType == SCHEDULE_FIXED_RATE;
    }

    public ScheduledTaskBuilder<V> setFixedRate(long initialDelay, long period) {
        validateInitialDelay(initialDelay);
        validatePeriod(period);
        this.scheduleType = SCHEDULE_FIXED_RATE;
        this.initialDelay = initialDelay;
        this.period = period;
        return this;
    }

    public ScheduledTaskBuilder<V> setFixedRate(long initialDelay, long period, TimeUnit unit) {
        setFixedRate(initialDelay, period);
        this.timeUnit = Objects.requireNonNull(unit);
        return this;
    }

    /**
     * 设置周期性任务的超时时间（非分时任务也可以）
     * <p>
     * 注意：
     * 1. -1表示无限制，大于等于0表示有限制
     * 2. 我们总是在执行任务后检查是否超时，以确保至少会执行一次
     * 3. 超时是一个不准确的调度，不保证超时后能立即结束
     */
    public ScheduledTaskBuilder<V> setTimeout(long timeout) {
        if (timeout < -1) {
            throw new IllegalArgumentException("invalid timeout " + timeout);
        }
        this.timeout = timeout;
        return this;
    }

    /**
     * 通过预估执行次数限制超时时间
     * 该方法对于fixedRate类型的任务有帮助
     *
     * @param count 期望的执行次数
     */
    public ScheduledTaskBuilder<V> setTimeoutByCount(int count) {
        if (count < 1) {
            throw new IllegalArgumentException("invalid count " + count);
        }
        // 这里需要max(0,)，否则可能使得timeout的值越界，initialDelay可能是小于0的
        if (count == 1) {
            this.timeout = Math.max(0, initialDelay);
        } else {
            this.timeout = Math.max(0, initialDelay + (count - 1) * period);
        }
        return this;
    }

    public long getTimeout() {
        return timeout;
    }

    /** 是否设置了超时时间 */
    public boolean hasTimeout() {
        return timeout != -1;
    }

    /** 设置时间单位 */
    public ScheduledTaskBuilder<V> setTimeUnit(TimeUnit timeUnit) {
        this.timeUnit = Objects.requireNonNull(timeUnit);
        return this;
    }

    public TimeUnit getTimeUnit() {
        return timeUnit;
    }

    // endregion

    // region overrides

    @Override
    public ScheduledTaskBuilder<V> enable(int taskOption) {
        super.enable(taskOption);
        return this;
    }

    @Override
    public ScheduledTaskBuilder<V> disable(int taskOption) {
        super.disable(taskOption);
        return this;
    }

    @Override
    public ScheduledTaskBuilder<V> setSchedulePhase(int phase) {
        super.setSchedulePhase(phase);
        return this;
    }

    public ScheduledTaskBuilder<V> setPriority(int priority) {
        super.setPriority(priority);
        return this;
    }

    @Override
    public ScheduledTaskBuilder<V> setOptions(int options) {
        super.setOptions(options);
        return this;
    }

    @Override
    public TaskBuilder<V> setCtx(IContext ctx) {
        super.setCtx(ctx);
        return this;
    }

    @Override
    public TaskBuilder<V> setCancelToken(ICancelToken cancelToken) {
        super.setCancelToken(cancelToken);
        return this;
    }

    // endregion

    // endregion

}