/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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
import cn.wjybxx.base.collection.DefaultIndexedPriorityQueue;
import cn.wjybxx.base.collection.IndexedPriorityQueue;
import cn.wjybxx.base.concurrent.CancelCodes;
import cn.wjybxx.base.time.TimeUtils;

import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicLong;
import java.util.function.BiConsumer;

/**
 * @author wjybxx
 * date - 2025/3/23
 */
public class DisruptorSchedulerHelper implements ISchedulerHelper {

    private final IDisruptorEventLoop<?> eventLoop;
    private final IndexedPriorityQueue<ScheduledPromiseTask<?>> taskQueue;
    private final BiConsumer<ICancelToken, Object> _onCancelRequested;

    public DisruptorSchedulerHelper(IDisruptorEventLoop<?> eventLoop) {
        this.eventLoop = eventLoop;
        this.taskQueue = new DefaultIndexedPriorityQueue<>(ScheduledPromiseTask::compareToExplicitly, 64);
        this._onCancelRequested = this::onCancelRequested;
    }

    // region core

    /**
     * 处理周期性任务，传入的限制只有在遇见低优先级任务的时候才生效，因此限制为0则表示遇见低优先级任务立即结束
     * (为避免时序错误，处理周期性任务期间不响应关闭，不容易安全实现)
     *
     * @param shuttingDownMode 是否是退出模式，退出模式下不再重复执行任务
     */
    public void update(long tickTime, boolean shuttingDownMode) {
        final IndexedPriorityQueue<ScheduledPromiseTask<?>> taskQueue = this.taskQueue;
        final IDisruptorEventLoop<?> eventLoop = this.eventLoop;

        ScheduledPromiseTask<?> futureTask;
        while ((futureTask = taskQueue.peek()) != null && !eventLoop.isShutdown()) {
            if (tickTime < futureTask.getTriggerTime()) {
                return;
            }
            taskQueue.poll();

            boolean enqueued = false;
            if (shuttingDownMode) {
                // 关闭模式下，不再重复执行任务
                if (futureTask.isTriggered() || futureTask.trigger(tickTime)) {
                    futureTask.cancel(CancelCodes.REASON_SHUTDOWN);
                }
            } else if (futureTask.trigger(tickTime)) {
                // 非关闭模式下，如果检测到开始关闭，也不再重复执行任务
                if (eventLoop.isShuttingDown()) {
                    futureTask.cancel(CancelCodes.REASON_SHUTDOWN);
                } else {
                    taskQueue.offer(futureTask);
                    enqueued = true;
                }
            }
            if (!enqueued) {
                ScheduledPromiseTask.release(futureTask);
            }
        }
    }

    @Override
    public void doSchedule(ScheduledPromiseTask<?> futureTask) {
        assert eventLoop.inEventLoop() && futureTask.getId() >= 0;
        ICancelToken cancelToken = futureTask.getCancelToken();
        // 检测取消和关闭，避免不必要的启动和停止(监听器)
        if (eventLoop.isShutdown() || cancelToken.isRequested()) {
            int cancelCode = eventLoop.isShutdown() ? CancelCodes.REASON_SHUTDOWN : cancelToken.cancelCode();
            futureTask.cancel(cancelCode);
            ScheduledPromiseTask.release(futureTask);
            return;
        }
        if (cancelToken.canBeCancelled() && needListenCancelToken(futureTask)) {
            IRegistration registration = cancelToken.thenAccept(_onCancelRequested, new Canceller(futureTask, futureTask.getId()));
            futureTask.setCancelRegistration(registration);
        }

        long tickTime = eventLoop.tickTime();
        if (tickTime < futureTask.getTriggerTime()) {
            taskQueue.add(futureTask);
            return;
        }
        // 和上面update逻辑相同
        if (futureTask.trigger(tickTime)) {
            if (eventLoop.isShuttingDown()) {
                futureTask.cancel(CancelCodes.REASON_SHUTDOWN);
            } else {
                taskQueue.add(futureTask);
            }
        }
    }

    private boolean needListenCancelToken(ScheduledPromiseTask<?> futureTask) {
        // 执行间隔短的任务就不主动注册监听器，以避免不必要的开销，暂定5S
        return (futureTask.getOptions() & TaskOptions.LISTEN_CANCEL_TOKEN) != 0
                || futureTask.getTriggerTime() - eventLoop.tickTime() > 5 * TimeUtils.NANOS_PER_SECOND
                || futureTask.getPeriod() > 5 * 5 * TimeUtils.NANOS_PER_SECOND;
    }

    private void onCancelRequested(ICancelToken cancelToken, Object ctx) {
        Canceller canceller = (Canceller) ctx;
        if (eventLoop.inEventLoop()) {
            cancel(canceller.futureTask, canceller.taskId, cancelToken.cancelCode());
        } else {
            // 如果在其它线程，尝试发布一个删除任务（能收到取消信号，通常证明Task还未结束）
            long sequence = eventLoop.tryNextSequence(1);
            if (sequence < 0) {
                return; // TODO 可通过GlobalEventLoop不断重试
            }
            IAgentEvent event = eventLoop.getEvent(sequence);
            event.setType(DisruptorEventLoop.TYPE_REMOVE_SCHEDULE);
            event.setObj1(canceller.futureTask);
            event.setLongVal1(canceller.taskId);
            event.setLongVal2(cancelToken.cancelCode());
            eventLoop.publish(sequence);
        }
    }

    public void cancel(ScheduledPromiseTask<?> futureTask, long taskId, int cancelCode) {
        if (futureTask.getId() != taskId) {
            return;
        }
        // 如果不在调度队列，应当正在执行Trigger方法，在执行完用户回调后会检测到取消信号
        if (taskQueue.remove(futureTask)) {
            futureTask.cancel(cancelCode);
            ScheduledPromiseTask.release(futureTask);
        }
    }

    /** 清理任务队列 */
    public void clearTaskQueue() {
        // 需要归还到池中
        ScheduledPromiseTask<?> futureTask;
        while ((futureTask = taskQueue.poll()) != null) {
            futureTask.cancel(CancelCodes.REASON_SHUTDOWN);
            ScheduledPromiseTask.release(futureTask);
        }
    }

    // endregion

    // region simple

    @Override
    public IEventLoop eventLoop() {
        return eventLoop;
    }

    @Override
    public boolean inEventLoop() {
        return eventLoop.inEventLoop();
    }

    @Override
    public long tickTime() {
        return eventLoop.tickTime();
    }

    @Override
    public long normalize(long worldTime, TimeUnit timeUnit) {
        return timeUnit.toNanos(worldTime);
    }

    @Override
    public long denormalize(long localTime, TimeUnit timeUnit) {
        return timeUnit.convert(localTime, TimeUnit.NANOSECONDS);
    }

    @Override
    public long nextId() {
        return idGenerator.incrementAndGet();
    }

    // endregion

    /**
     * 1.改用该id方案后，先进入到Disruptor队列的任务可能有更大的Id —— 先提交任务的线程，在这里不一定先手。
     * 2.改用该id方案后，可以自由创建定时任务 —— 不再需要竞争Disruptor队列。
     */
    private static final AtomicLong idGenerator = new AtomicLong();

    private static class Canceller {

        final ScheduledPromiseTask<?> futureTask;
        final long taskId;

        public Canceller(ScheduledPromiseTask<?> futureTask, long taskId) {
            this.futureTask = futureTask;
            this.taskId = taskId;
        }
    }
}