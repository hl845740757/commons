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

package cn.wjybxx.unitask;

import cn.wjybxx.concurrent.EventLoopState;

import java.util.ArrayDeque;
import java.util.ArrayList;
import java.util.Deque;
import java.util.List;
import java.util.concurrent.TimeUnit;

/**
 * 默认的executor实现，通过限制每帧执行的任务数来平滑开销
 *
 * @author wjybxx
 * date 2023/4/3
 */
public class DefaultExecutor extends AbstractUniExecutor {

    private final Deque<Runnable> taskQueue = new ArrayDeque<>();
    private final UniPromise<Void> terminationPromise = new UniPromise<>(this);
    private final UniFuture<Void> terminationFuture = terminationPromise.asReadonly();
    private int state = EventLoopState.ST_UNSTARTED;

    private final int countLimit;
    private final long nanoTimeLimit;

    public DefaultExecutor() {
        this(-1, -1, TimeUnit.NANOSECONDS);
    }

    public DefaultExecutor(int countLimit) {
        this(countLimit, -1, TimeUnit.NANOSECONDS);
    }

    /**
     * @param countLimit 每帧允许运行的最大任务数，-1表示不限制；不可以为0
     * @param timeLimit  每帧允许的最大时间，-1表示不限制；不可以为0
     */
    public DefaultExecutor(int countLimit, long timeLimit, TimeUnit timeUnit) {
        ensureNegativeOneOrPositive(countLimit, "countLimit");
        ensureNegativeOneOrPositive(timeLimit, "timeLimit");
        this.countLimit = countLimit;
        this.nanoTimeLimit = timeLimit > 0 ? timeUnit.toNanos(timeLimit) : -1;
    }

    private static void ensureNegativeOneOrPositive(long val, String name) {
        if (!(val == -1 || val > 0)) {
            throw new IllegalArgumentException(name + " must be -1 or positive");
        }
    }

    @Override
    public void update() {
        final int batchSize = this.countLimit;
        final long nanosPerFrame = this.nanoTimeLimit;
        final Deque<Runnable> taskQueue = this.taskQueue;

        // 频繁取系统时间的性能不好，因此分两个模式运行
        Runnable task;
        int count = 0;
        if (nanosPerFrame <= 0) {
            while ((task = taskQueue.pollFirst()) != null) {
                try {
                    task.run();
                } catch (Throwable e) {
                    logCause(e);
                }
                if ((batchSize > 0 && ++count >= batchSize)) {
                    break; // 强制中断，避免占用太多资源或死循环风险
                }
            }
        } else {
            final long startTime = System.nanoTime();
            while ((task = taskQueue.pollFirst()) != null) {
                try {
                    task.run();
                } catch (Throwable e) {
                    logCause(e);
                }
                if ((batchSize > 0 && ++count >= batchSize)
                        || (System.nanoTime() - startTime >= nanosPerFrame)) {
                    break;  // 强制中断，避免占用太多资源或死循环风险
                }
            }
        }
        if (isShuttingDown() && taskQueue.isEmpty()) {
            state = EventLoopState.ST_TERMINATED;
            terminationPromise.trySetResult(null);
        }
    }

    @Override
    public boolean needMoreTicks() {
        return !taskQueue.isEmpty();
    }

    @Override
    public void execute(Runnable command, int options) {
        if (options != 0 && (command instanceof UniPromiseTask<?> promiseTask)) {
            promiseTask.setOptions(options);
        }
        taskQueue.offer(command);
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
        taskQueue.clear();
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
    public UniFuture<?> terminationFuture() {
        return terminationFuture;
    }
    // endregion
}