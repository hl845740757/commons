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

package cn.wjybxx.sequential;

import cn.wjybxx.base.collection.DefaultIndexedPriorityQueue;
import cn.wjybxx.base.collection.IndexedPriorityQueue;
import cn.wjybxx.base.time.TimeProvider;
import cn.wjybxx.concurrent.*;
import cn.wjybxx.disruptor.RingBuffer;

import javax.annotation.Nonnull;
import java.util.*;
import java.util.concurrent.TimeUnit;

/**
 * 时序管理同{@link DisruptorEventLoop}：
 * 我们总是先取得一个时间快照，然后先执行{@link #scheduledTaskQueue}中的任务，再执行{@link RingBuffer}中的任务，因此满足优先级相同时，先提交的任务先执行的约定
 *
 * @author wjybxx
 * date 2023/4/3
 */
public class DefaultUniScheduledExecutor extends AbstractUniScheduledExecutor implements UniScheduledExecutor {

    private static final Comparator<ScheduledPromiseTask<?>> queueTaskComparator = ScheduledPromiseTask::compareToExplicitly;
    private static final int DEFAULT_INITIAL_CAPACITY = 16;

    private final TimeProvider timeProvider;
    private final ArrayDeque<Runnable> taskQueue;
    private final IndexedPriorityQueue<ScheduledPromiseTask<?>> scheduledTaskQueue;
    private final ScheduledHelper scheduledHelper = new ScheduledHelper();
    private final UniPromise<Void> terminationPromise = new UniPromise<>(this);

    private int state = EventLoopState.ST_UNSTARTED;
    /** 为任务分配唯一id，确保先入先出 */
    private long sequencer = 0;
    /** 当前帧的时间戳，缓存下来以避免在tick的过程中产生变化 */
    private long tickTime;

    public DefaultUniScheduledExecutor(TimeProvider timeProvider) {
        this(timeProvider, DEFAULT_INITIAL_CAPACITY);
    }

    public DefaultUniScheduledExecutor(TimeProvider timeProvider, int initCapacity) {
        this.timeProvider = Objects.requireNonNull(timeProvider, "timeProvider");
        this.taskQueue = new ArrayDeque<>(initCapacity);
        this.scheduledTaskQueue = new DefaultIndexedPriorityQueue<>(queueTaskComparator, 16);
        this.tickTime = timeProvider.getTime();
    }

    @Override
    public void update() {
        tickTime = timeProvider.getTime();
        processScheduledQueue(tickTime, isShuttingDown());

        ArrayDeque<Runnable> taskQueue = this.taskQueue;
        Runnable task;
        while ((task = taskQueue.poll()) != null) {
            try {
                task.run();
            } catch (Throwable ex) {
                logCause(ex);
            }
        }
    }

    private void processScheduledQueue(long tickTime, boolean shuttingDownMode) {
        final IndexedPriorityQueue<ScheduledPromiseTask<?>> taskQueue = scheduledTaskQueue;
        ScheduledPromiseTask<?> queueTask;
        while ((queueTask = taskQueue.peek()) != null) {
            if (queueTask.isCancelling()) {
                taskQueue.poll(); // 未及时删除的任务
                queueTask.trySetCancelled();
                scheduledHelper.onCompleted(queueTask);
                continue;
            }
            // 优先级最高的任务不需要执行，那么后面的也不需要执行
            if (tickTime < queueTask.getNextTriggerTime()) {
                return;
            }

            taskQueue.poll();
            if (shuttingDownMode) {
                // 关闭模式下，不再重复执行任务
                if (queueTask.isTriggered() || queueTask.trigger(tickTime)) {
                    queueTask.trySetCancelled();
                    scheduledHelper.onCompleted(queueTask);
                }
            } else {
                // 非关闭模式下，如果检测到开始关闭，也不再重复执行任务
                if (queueTask.trigger(tickTime)) {
                    if (isShuttingDown()) {
                        queueTask.trySetCancelled();
                        scheduledHelper.onCompleted(queueTask);
                    } else {
                        taskQueue.offer(queueTask);
                    }
                } else {
                    scheduledHelper.onCompleted(queueTask);
                }
            }
        }
    }

    @Override
    public boolean needMoreUpdate() {
        ScheduledPromiseTask<?> queueTask = scheduledTaskQueue.peek();
        return queueTask != null && queueTask.getNextTriggerTime() <= tickTime;
    }

    @Override
    public void execute(@Nonnull Runnable command) {
        Objects.requireNonNull(command, "command");
        if (isShuttingDown()) {
            // 暂时直接取消
            if (command instanceof IFutureTask<?> promiseTask) {
                promiseTask.trySetCancelled();
            }
            return;
        }
        taskQueue.offer(command);
        if (command instanceof ScheduledPromiseTask<?> scheduledPromiseTask) {
            scheduledPromiseTask.setId(++sequencer);
            scheduledPromiseTask.registerCancellation();
        }
    }

    // region lifecycle

    @Override
    public void shutdown() {
        if (state < EventLoopState.ST_SHUTTING_DOWN) {
            state = EventLoopState.ST_SHUTTING_DOWN;
        }
    }

    @Nonnull
    @Override
    public List<Runnable> shutdownNow() {
        ArrayList<Runnable> result = new ArrayList<>(taskQueue);
        result.addAll(scheduledTaskQueue);
        scheduledTaskQueue.clearIgnoringIndexes();

        state = EventLoopState.ST_TERMINATED;
        terminationPromise.trySetResult(null);
        return result;
    }

    @Override
    public boolean isShuttingDown() {
        return state >= EventLoopState.ST_SHUTTING_DOWN;
    }

    @Override
    public boolean isShutdown() {
        return state >= EventLoopState.ST_SHUTDOWN;
    }

    @Override
    public boolean isTerminated() {
        return state == EventLoopState.ST_TERMINATED;
    }

    @Override
    public IFuture<?> terminationFuture() {
        return terminationPromise.asReadonly();
    }
    // endregion

    // region 内部实现

    @Override
    protected IScheduledHelper helper() {
        return scheduledHelper;
    }

    private class ScheduledHelper implements IScheduledHelper {

        @Override
        public long tickTime() {
            return tickTime;
        }

        @Override
        public long normalize(long worldTime, TimeUnit timeUnit) {
            return timeUnit.toMillis(worldTime);
        }

        @Override
        public long denormalize(long localTime, TimeUnit timeUnit) {
            return timeUnit.convert(localTime, TimeUnit.MILLISECONDS);
        }

        @Override
        public void reschedule(ScheduledPromiseTask<?> futureTask) {
            if (isShuttingDown()) {
                futureTask.trySetCancelled();
                onCompleted(futureTask);
            } else {
                scheduledTaskQueue.add(futureTask);
            }
        }

        @Override
        public void onCompleted(ScheduledPromiseTask<?> futureTask) {
            futureTask.clear();
        }

        @Override
        public void onCancelRequested(ScheduledPromiseTask<?> futureTask, int cancelCode) {
            scheduledTaskQueue.removeTyped(futureTask);
        }
    }

    // endregion
}