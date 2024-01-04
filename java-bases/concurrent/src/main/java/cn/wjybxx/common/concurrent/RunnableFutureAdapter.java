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
package cn.wjybxx.common.concurrent;

import java.util.Objects;
import java.util.concurrent.*;

/**
 * {@link RunnableFuture}是一个不太好的抽象，将任务的调度和Future的职责混在一起，对扩展性有影响。
 * 该适配器用于解决继承{@link AbstractExecutorService}的问题
 *
 * @author wjybxx
 * date - 2023/11/2
 */
public class RunnableFutureAdapter<V> implements RunnableFuture<V> {

    private final ICompletableFuture<V> future;
    private final Callable<V> task;

    public RunnableFutureAdapter(ICompletableFuture<V> future, Callable<V> task) {
        this.future = Objects.requireNonNull(future);
        this.task = Objects.requireNonNull(task);
    }

    public ICompletableFuture<V> getFuture() {
        return future;
    }

    public Callable<V> getTask() {
        return task;
    }

    @Override
    public void run() {
        try {
            V result = task.call();
            future.complete(result);
        } catch (Throwable ex) {
            future.completeExceptionally(ex);
        }
    }

    @Override
    public boolean cancel(boolean mayInterruptIfRunning) {
        return future.cancel(mayInterruptIfRunning);
    }

    @Override
    public boolean isCancelled() {
        return future.isCancelled();
    }

    @Override
    public boolean isDone() {
        return future.isDone();
    }

    @Override
    public V get() throws InterruptedException, ExecutionException {
        return future.get();
    }

    @Override
    public V get(long timeout, TimeUnit unit) throws InterruptedException, ExecutionException, TimeoutException {
        return future.get(timeout, unit);
    }

    @Override
    public V resultNow() {
        return future.resultNow();
    }

    @Override
    public Throwable exceptionNow() {
        return future.exceptionNow();
    }

    @Override
    public State state() {
        return future.state();
    }

}
