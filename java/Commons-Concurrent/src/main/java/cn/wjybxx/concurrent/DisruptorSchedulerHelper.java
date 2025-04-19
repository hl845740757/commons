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

import cn.wjybxx.base.collection.DefaultIndexedPriorityQueue;
import cn.wjybxx.base.collection.IndexedPriorityQueue;
import cn.wjybxx.base.concurrent.CancelCodes;

import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2025/3/23
 */
public class DisruptorSchedulerHelper implements ISchedulerHelper {

    /** 周期性任务队列 -- 既有的任务都是先于Sequencer中的任务提交的 */
    private final IndexedPriorityQueue<ScheduledPromiseTask<?>> taskQueue;
    private final IDisruptorEventLoop<?> eventLoop;

    public DisruptorSchedulerHelper(IDisruptorEventLoop<?> eventLoop) {
        this.eventLoop = eventLoop;
        this.taskQueue = new DefaultIndexedPriorityQueue<>(ScheduledPromiseTask::compareToExplicitly, 64);
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
            if (tickTime < futureTask.getNextTriggerTime()) {
                return;
            }
            taskQueue.poll();
            if (shuttingDownMode) {
                // 关闭模式下，不再重复执行任务
                if (futureTask.isTriggered() || futureTask.trigger(tickTime)) {
                    futureTask.cancel(CancelCodes.REASON_SHUTDOWN);
                }
            } else if (futureTask.trigger(tickTime)) {
                // 非关闭模式下，如果检测到开始关闭，也不再重复执行任务 -- 和下面相同
                if (eventLoop.isShuttingDown()) {
                    futureTask.cancel(CancelCodes.REASON_SHUTDOWN);
                } else {
                    taskQueue.offer(futureTask);
                }
            }
        }
    }

    @Override
    public void doSchedule(ScheduledPromiseTask<?> futureTask) {
        assert eventLoop.inEventLoop() && futureTask.getId() >= 0;
        long tickTime = eventLoop.tickTime();
        if (tickTime < futureTask.getNextTriggerTime()) {
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

    @Override
    public void onCancelRequested(ICancelToken cancelToken, Object ctx) {
        int cancelCode = cancelToken.cancelCode();
        long taskId = (long) ctx;
        if (eventLoop.inEventLoop()) {
            // 如果不在调度队列，应当正在执行Trigger方法，在执行完用户回调后会检测到取消信号
            ScheduledPromiseTask<?> task = removeTask(taskId);
            if (task != null) {
                task.cancel(cancelCode);
            }
        } else {
            // 如果在其它线程，尝试发布一个删除任务（能收到取消信号，通常证明Task还未结束）
            long sequence = eventLoop.tryNextSequence(1);
            if (sequence < 0) {
                return; // TODO 可通过GlobalEventLoop不断重试
            }
            IAgentEvent event = eventLoop.getEvent(sequence);
            event.setType(DisruptorEventLoop.TYPE_REMOVE_SCHEDULE);
            event.setLongVal1(taskId);
            eventLoop.publish(sequence);
        }
    }

    /** 删除指定id的任务 */
    public ScheduledPromiseTask<?> removeTask(long taskId) {
        // 暂时迭代处理
        for (ScheduledPromiseTask<?> task : taskQueue) {
            if (task.getId() == taskId) {
                taskQueue.remove(task);
                return task;
            }
        }
        return null;
    }

    /** 清理任务队列 */
    public void clearTaskQueue() {
        // 需要归还到池中
        ScheduledPromiseTask<?> futureTask;
        while ((futureTask = taskQueue.poll()) != null) {
            futureTask.cancel(CancelCodes.REASON_SHUTDOWN);
        }
    }

    // endregion

    // region simple

    @Override
    public long tickTime() {
        return eventLoop.tickTime();
    }

    @Override
    public boolean isShutDown() {
        return eventLoop.isShutdown();
    }

    @Override
    public boolean inEventLoop() {
        return eventLoop.inEventLoop();
    }

    @Override
    public long normalize(long worldTime, TimeUnit timeUnit) {
        return timeUnit.toNanos(worldTime);
    }

    @Override
    public long denormalize(long localTime, TimeUnit timeUnit) {
        return timeUnit.convert(localTime, TimeUnit.NANOSECONDS);
    }
    // endregion
}