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

package cn.wjybxx.single;

import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.collection.IndexedElement;
import cn.wjybxx.concurrent.*;
import cn.wjybxx.disruptor.StacklessTimeoutException;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.concurrent.Callable;
import java.util.concurrent.CancellationException;
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
@NotThreadSafe
public final class UniScheduledPromiseTask<V>
        extends PromiseTask<V>
        implements IScheduledFutureTask<V>, IndexedElement, Consumer<Object> {

    /** 任务的唯一id - 如果构造时未传入，要小心可见性问题 */
    private long id;
    /** 提前计算的，逻辑上的下次触发时间 - 非volatile，不对用户开放 */
    private long nextTriggerTime;
    /** 任务的执行间隔 - 不再有特殊意义 */
    private final long period;
    /** 超时信息 */
    private TimeoutContext timeoutContext;

    /** 在队列中的下标 */
    private int queueIndex = INDEX_NOT_FOUNT;
    /** 接收用户取消信号的句柄 -- 延时任务需要及时删除任务 */
    private IRegistration cancelRegistration;

    /**
     * @param promise         任务关联的promise
     * @param id              任务的id
     * @param nextTriggerTime 任务的首次触发时间
     */
    private UniScheduledPromiseTask(ScheduledTaskBuilder<V> builder, IScheduledPromise<V> promise,
                                    long id, long nextTriggerTime, long period, TimeoutContext timeoutContext) {
        super(builder, promise);
        this.id = id;
        this.nextTriggerTime = nextTriggerTime;
        this.period = period;
        this.timeoutContext = timeoutContext;
        setScheduleType(builder.getScheduleType());
        promise.setTask(this);
    }

    /** 用于简单情况下的创建 */
    UniScheduledPromiseTask(Object action, IScheduledPromise<V> promise, int taskType,
                            long id, long nextTriggerTime, long period,
                            int scheduleType) {
        super(action, promise, taskType);
        this.id = id;
        this.nextTriggerTime = nextTriggerTime;
        this.period = period;
        setScheduleType(scheduleType);
        promise.setTask(this);
    }

    public static UniScheduledPromiseTask<?> ofRunnable(Runnable action, IScheduledPromise<?> promise,
                                                        long id, long nextTriggerTime) {
        return new UniScheduledPromiseTask<>(action, promise, TaskBuilder.TYPE_RUNNABLE,
                id, nextTriggerTime, 0, 0);
    }

    public static <V> UniScheduledPromiseTask<V> ofCallable(Callable<? extends V> action, IScheduledPromise<V> promise,
                                                            long id, long nextTriggerTime) {
        return new UniScheduledPromiseTask<>(action, promise, TaskBuilder.TYPE_CALLABLE,
                id, nextTriggerTime, 0, 0);
    }

    public static <V> UniScheduledPromiseTask<V> ofFunction(Function<? super IContext, ? extends V> action, IScheduledPromise<V> promise,
                                                            long id, long nextTriggerTime) {
        return new UniScheduledPromiseTask<>(action, promise, TaskBuilder.TYPE_FUNCTION,
                id, nextTriggerTime, 0, 0);
    }

    public static UniScheduledPromiseTask<?> ofConsumer(Consumer<? super IContext> action, IScheduledPromise<?> promise,
                                                        long id, long nextTriggerTime) {
        return new UniScheduledPromiseTask<>(action, promise, TaskBuilder.TYPE_CONSUMER,
                id, nextTriggerTime, 0, 0);
    }

    public static <V> UniScheduledPromiseTask<V> ofBuilder(TaskBuilder<V> builder, IScheduledPromise<V> promise,
                                                           long id, long tickTime) {
        if (builder instanceof ScheduledTaskBuilder<V> sb) {
            return ofBuilder(sb, promise, id, tickTime);
        }
        return new UniScheduledPromiseTask<>(builder.getTask(), promise, builder.getType(),
                id, tickTime, 0, 0);
    }

    /**
     * @param builder  builder
     * @param promise  监听结果的promise
     * @param id       给任务分配的id
     * @param tickTime 当前时间(没有单位)
     * @return PromiseTask
     */
    public static <V> UniScheduledPromiseTask<V> ofBuilder(ScheduledTaskBuilder<V> builder, IScheduledPromise<V> promise,
                                                           long id, long tickTime) {
        // 理论上单线程下是可以支持插队的，但插队会导致较强的依赖，暂时先不支持
        final long initialDelay = Math.max(0, builder.getInitialDelay());
        final long triggerTime = tickTime + initialDelay;
        final long period = builder.getPeriod();

        final long timeout = builder.getTimeout();
        TimeoutContext timeoutContext;
        if (builder.isPeriodic() && timeout != -1) {
            timeoutContext = new TimeoutContext(timeout, tickTime);
        } else {
            timeoutContext = null;
        }
        return new UniScheduledPromiseTask<>(builder, promise, id, triggerTime, period, timeoutContext);
    }

    // region internal

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

    @Override
    public int collectionIndex(Object collection) {
        return queueIndex;
    }

    @Override
    public void collectionIndex(Object collection, int index) {
        this.queueIndex = index;
    }

    @SuppressWarnings("unchecked")
    @Override
    public IScheduledFuture<V> future() {
        return (IScheduledFuture<V>) promise;
    }

    @Override
    public IScheduledPromise<V> getPromise() {
        return (IScheduledPromise<V>) promise;
    }

    @Override
    public void clear() {
        super.clear();
        timeoutContext = null;
        closeRegistration();
    }

    // endregion

    // region core

    private AbstractUniScheduledExecutor eventLoop() {
        return (AbstractUniScheduledExecutor) promise.executor();
    }

    @Override
    public void run() {
        AbstractUniScheduledExecutor eventLoop = eventLoop();
        IPromise<V> promise = this.promise;
        // 未及时从队列删除；不要尝试优化，可能尚未到触发时间
        if (promise.isDone() || promise.ctx().cancelToken().isCancelling()) {
            cancelWithoutRemove(ICancelToken.REASON_DEFAULT);
            return;
        }
        long tickTime = eventLoop.tickTime();
        if (tickTime < nextTriggerTime) { // 显式测试一次，适应多种EventLoop
            eventLoop.reSchedulePeriodic(this, false);
            return;
        }
        if (trigger(tickTime)) {
            eventLoop.reSchedulePeriodic(this, true);
        }
    }

    /**
     * 外部确定性触发，不需要回调的方式重新压入队列
     *
     * @return 如果需要再压入队列则返回true
     */
    public boolean trigger(long tickTime) {
        final int scheduleType = getScheduleType();
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            super.run();
            return false;
        }

        IPromise<V> promise = this.promise;
        // 检测取消信号 -- 还要检测来自future的取消...
        if (promise.ctx().cancelToken().isCancelling()) {
            promise.trySetCancelled();
            clear();
            return false;
        }
        if ((options & maskClaimed) == 0) {
            if (!promise.trySetComputing()) {
                clear();
                return false;
            }
            options |= maskClaimed;
        } else if (!promise.isComputing()) {
            clear();
            return false;
        }

        TimeoutContext timeoutContext = this.timeoutContext;
        try {
            if (timeoutContext != null) {
                timeoutContext.beforeCall(tickTime, nextTriggerTime, scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE);
                if (TaskOption.isEnabled(options, TaskOption.TIMEOUT_BEFORE_RUN) && timeoutContext.isTimeout()) {
                    promise.trySetException(StacklessTimeoutException.INSTANCE);
                    clear();
                    return false;
                }
            }
            // 周期性任务，只有分时任务可以有结果
            if (getTaskType() == TaskBuilder.TYPE_TIMESHARING) {
                runTimeSharing();
                if (promise.isDone()) {
                    clear();
                    return false;
                }
            } else {
                runTask();
            }
            // 任务执行后检测取消
            if (promise.ctx().cancelToken().isCancelling() || !promise.isComputing()) {
                promise.trySetCancelled();
                clear();
                return false;
            }
            // 未被取消的情况下检测超时
            if (timeoutContext != null && timeoutContext.isTimeout()) {
                promise.trySetException(StacklessTimeoutException.INSTANCE);
                clear();
                return false;
            }
            setNextRunTime(tickTime, timeoutContext, scheduleType);
            return true;
        } catch (Throwable ex) {
            ThreadUtils.recoveryInterrupted(ex);
            if (canCaughtException(ex)) {
                FutureLogger.logCause(ex, "periodic task caught exception");
                setNextRunTime(tickTime, timeoutContext, scheduleType);
                return true;
            }
            promise.trySetException(ex);
            clear();
            return false;
        }
    }

    private boolean canCaughtException(Throwable ex) {
        if (getScheduleType() == ScheduledTaskBuilder.SCHEDULE_ONCE) {
            return false;
        }
        if (getTaskType() == TaskBuilder.TYPE_TIMESHARING) {
            return false;
        }
        return TaskOption.isEnabled(options, TaskOption.CAUGHT_EXCEPTION);
    }

    private void setNextRunTime(long tickTime, TimeoutContext timeoutContext, int scheduleType) {
        long maxDelay = timeoutContext != null ? timeoutContext.getTimeLeft() : Long.MAX_VALUE;
        if (scheduleType == ScheduledTaskBuilder.SCHEDULE_FIXED_RATE) {
            // 逻辑时间
            nextTriggerTime = nextTriggerTime + Math.min(maxDelay, period);
        } else {
            // 真实时间
            nextTriggerTime = tickTime + Math.min(maxDelay, period);
        }
    }

    // region cancel

    public void cancelWithoutRemove() {
        cancelWithoutRemove(ICancelToken.REASON_SHUTDOWN);
    }

    public void cancelWithoutRemove(int code) {
        closeRegistration();
        promise.trySetCancelled(code);
    }

    public void registerCancellation() {
        ICancelToken cancelToken = promise.ctx().cancelToken();
        if (promise.ctx().cancelToken() == ICancelToken.NONE) {
            return;
        }
        cancelRegistration = cancelToken.thenAccept(this);
    }

    @Override
    public void accept(Object futureOrToken) {
        if (promise.isCancelled()) {
            // 这里难以识别是被谁取消的，但trySetCancelled的异常无堆栈的，而Future.cancel的异常是有堆栈的...
            CancellationException ex = (CancellationException) promise.exceptionNow(false);
            if (!(ex instanceof StacklessCancellationException)) {
                eventLoop().removeScheduled(this);
            }
        } else {
            ICancelToken cancelToken = promise.ctx().cancelToken();
            if (!cancelToken.isCancelling()) {
                return;
            }
            // 用户通过令牌发起取消
            if (promise.trySetCancelled() && !cancelToken.isWithoutRemove()) {
                eventLoop().removeScheduled(this);
            }
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

    /** 计算任务的触发时间 */
    public static long triggerTime(long delay, TimeUnit timeUnit, long tickTime) {
        // 理论上单线程下是可以支持插队的，但插队会导致较强的依赖，暂时先不支持
        final long initialDelay = Math.max(0, delay);
        return tickTime + timeUnit.toMillis(initialDelay);
    }

    @Override
    public long getDelay(@Nonnull TimeUnit unit) {
        long delay = Math.max(0, nextTriggerTime - eventLoop().tickTime());
        return unit.convert(delay, TimeUnit.MILLISECONDS);
    }

    @Override
    public int compareTo(@Nonnull Delayed o) {
        return compareToExplicitly((UniScheduledPromiseTask<?>) o);
    }

    public int compareToExplicitly(UniScheduledPromiseTask<?> other) {
        if (other == this) {
            return 0;
        }
        int r = Integer.compare(getQueueId(), other.getQueueId());
        if (r != 0) {
            return r;
        }
        r = Long.compare(nextTriggerTime, other.nextTriggerTime);
        if (r != 0) {
            return r;
        }
        return Long.compare(id, other.id);
    }

}