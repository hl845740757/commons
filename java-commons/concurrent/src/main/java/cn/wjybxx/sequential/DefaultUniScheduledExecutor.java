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
import cn.wjybxx.concurrent.EventLoopState;
import cn.wjybxx.concurrent.ICancelToken;
import cn.wjybxx.concurrent.IFuture;
import cn.wjybxx.concurrent.IFutureTask;

import javax.annotation.Nonnull;
import java.util.ArrayList;
import java.util.Comparator;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class DefaultUniScheduledExecutor extends AbstractUniScheduledExecutor implements UniScheduledExecutor {

    private static final Comparator<UniScheduledPromiseTask<?>> queueTaskComparator = UniScheduledPromiseTask::compareToExplicitly;
    private static final int DEFAULT_INITIAL_CAPACITY = 16;

    private final TimeProvider timeProvider;
    private final IndexedPriorityQueue<UniScheduledPromiseTask<?>> taskQueue;
    private final UniPromise<Void> terminationPromise = new UniPromise<>(this);
    private final IFuture<Void> terminationFuture = terminationPromise.asReadonly();
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
        this.taskQueue = new DefaultIndexedPriorityQueue<>(queueTaskComparator, initCapacity);
        this.tickTime = timeProvider.getTime();
    }

    @Override
    public void update() {
        // 需要缓存下来，一来用于计算下次调度时间，二来避免优先级错乱
        final long curTime = timeProvider.getTime();
        tickTime = curTime;

        // 记录最后一个任务id，避免执行本次tick期间添加的任务
        final long barrierTaskId = sequencer;
        final IndexedPriorityQueue<UniScheduledPromiseTask<?>> taskQueue = this.taskQueue;
        UniScheduledPromiseTask<?> queueTask;
        while ((queueTask = taskQueue.peek()) != null) {
            // 优先级最高的任务不需要执行，那么后面的也不需要执行
            if (curTime < queueTask.getNextTriggerTime()) {
                return;
            }
            // 本次tick期间新增的任务，不立即执行，避免死循环或占用过多cpu
            if (queueTask.getId() > barrierTaskId) {
                return;
            }

            taskQueue.poll();
            if (queueTask.trigger(tickTime)) {
                if (isShutdown()) { // 已请求关闭
                    queueTask.cancelWithoutRemove();
                } else {
                    taskQueue.offer(queueTask);
                }
            }
        }
    }

    @Override
    public boolean needMoreTicks() {
        UniScheduledPromiseTask<?> queueTask = taskQueue.peek();
        return queueTask != null && queueTask.getNextTriggerTime() <= tickTime;
    }

    @Override
    public void execute(@Nonnull Runnable command) {
        if (isShuttingDown()) {
            // 暂时直接取消
            if (command instanceof IFutureTask<?> promiseTask) {
                promiseTask.future().trySetCancelled(ICancelToken.REASON_SHUTDOWN);
            }
            return;
        }
        if (command instanceof UniScheduledPromiseTask<?> promiseTask) {
            promiseTask.setId(++sequencer);
            if (delayExecute(promiseTask)) {
                promiseTask.registerCancellation();
            }
        } else {
            UniScheduledPromiseTask<?> promiseTask = UniScheduledPromiseTask.ofAction(command, 0, newScheduledPromise(), ++sequencer, tickTime);
            if (delayExecute(promiseTask)) {
                promiseTask.registerCancellation();
            }
        }
    }

    private boolean delayExecute(UniScheduledPromiseTask<?> futureTask) {
        if (isShuttingDown()) {
            // 默认直接取消，暂不添加拒绝处理器
            futureTask.cancelWithoutRemove();
            return false;
        } else {
            taskQueue.add(futureTask);
            return true;
        }
    }

    // region lifecycle

    @Override
    public void shutdown() {
        if (state < EventLoopState.ST_SHUTTING_DOWN) {
            state = EventLoopState.ST_SHUTTING_DOWN;
        }
    }

    @Override
    public List<Runnable> shutdownNow() {
        ArrayList<Runnable> result = new ArrayList<>(taskQueue);
        taskQueue.clearIgnoringIndexes();
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
        return terminationFuture;
    }
    // endregion

    // region 内部实现

    @Override
    protected long tickTime() {
        return tickTime;
    }

    @Override
    protected void reSchedulePeriodic(UniScheduledPromiseTask<?> futureTask, boolean triggered) {
        delayExecute(futureTask);
    }

    @Override
    protected void removeScheduled(UniScheduledPromiseTask<?> futureTask) {
        taskQueue.removeTyped(futureTask);
    }

}