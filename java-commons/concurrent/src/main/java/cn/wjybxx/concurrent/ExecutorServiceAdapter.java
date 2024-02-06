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

import java.util.Collection;
import java.util.List;
import java.util.concurrent.*;

/**
 * 适配{@link java.util.concurrent.ExecutorService#invokeAll(Collection)}等方法。
 *
 * @author wjybxx
 * date - 2024/1/29
 */
@SuppressWarnings("NullableProblems")
public final class ExecutorServiceAdapter extends AbstractExecutorService {

    public final IExecutorService eventLoop;

    public ExecutorServiceAdapter(IExecutorService eventLoop) {
        this.eventLoop = eventLoop;
    }

    @Override
    public void execute(Runnable command) {
        eventLoop.execute(command);
    }

    @Override
    public void shutdown() {
        eventLoop.shutdown();
    }

    @Override
    public List<Runnable> shutdownNow() {
        return eventLoop.shutdownNow();
    }

    @Override
    public boolean isShutdown() {
        return eventLoop.isShutdown();
    }

    @Override
    public boolean isTerminated() {
        return eventLoop.isTerminated();
    }

    @Override
    public boolean awaitTermination(long timeout, TimeUnit unit) throws InterruptedException {
        return eventLoop.awaitTermination(timeout, unit);
    }

    @Override
    public void close() {
        eventLoop.close();
    }

    @Override
    protected <T> RunnableFuture<T> newTaskFor(Runnable runnable, T value) {
        return new RunnableFutureAdapter<>(eventLoop.newPromise(), Executors.callable(runnable, value));
    }

    @Override
    protected <T> RunnableFuture<T> newTaskFor(Callable<T> callable) {
        return new RunnableFutureAdapter<>(eventLoop.newPromise(), callable);
    }

    /** 继承{@link ForwardFuture}可以少实现方法 */
    private static class RunnableFutureAdapter<V> extends ForwardFuture<V> implements RunnableFuture<V> {

        final IPromise<V> promise;
        final Callable<? extends V> task;

        public RunnableFutureAdapter(IPromise<V> promise, Callable<? extends V> task) {
            super(promise);
            this.promise = promise;
            this.task = task;
        }

        @Override
        public void run() {
            IPromise<V> promise = this.promise;
            ICancelToken cancelToken = promise.ctx().cancelToken();
            if (cancelToken.isCancelling()) {
                promise.trySetCancelled(cancelToken.cancelCode());
                return;
            }
            if (promise.trySetComputing()) {
                try {
                    V result = task.call();
                    promise.trySetResult(result);
                } catch (Throwable e) {
                    promise.trySetException(e);
                }
            }
        }
    }
}
