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

import cn.wjybxx.concurrent.*;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nonnull;
import java.util.concurrent.Callable;
import java.util.concurrent.TimeUnit;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * @author wjybxx
 * date 2023/4/7
 */
@SuppressWarnings("NullableProblems")
public abstract class AbstractUniExecutor implements UniExecutorService {

    protected static final Logger logger = LoggerFactory.getLogger(AbstractUniExecutor.class);

    @Override
    public boolean awaitTermination(long timeout, TimeUnit unit) {
        if (isTerminated()) {
            return true;
        }
        throw new BlockingOperationException("awaitTermination");
    }

    // region execute

    @Override
    public abstract void execute(Runnable command);

    @Override
    public void execute(Runnable command, int options) {
        execute(FutureUtils.toTask(command, options));
    }

    @Override
    public void execute(Consumer<? super IContext> action, IContext ctx) {
        execute(FutureUtils.toTask(action, ctx, 0));
    }

    @Override
    public void execute(Consumer<? super IContext> action, IContext ctx, int options) {
        execute(FutureUtils.toTask(action, ctx, options));
    }
    // endregion

    // region submit
    @Override
    public <V> UniPromise<V> newPromise() {
        return new UniPromise<>(this);
    }

    @Override
    public <T> IFuture<T> submit(@Nonnull TaskBuilder<T> builder) {
        PromiseTask<T> futureTask = PromiseTask.ofBuilder(builder, newPromise());
        execute(futureTask);
        return futureTask.future();
    }

    @Override
    public <T> IFuture<T> submit(Callable<T> task) {
        PromiseTask<T> futureTask = PromiseTask.ofFunction(task, null, 0, newPromise());
        execute(futureTask);
        return futureTask.future();
    }

    @Override
    public <V> IFuture<V> submitFunc(Callable<? extends V> task) {
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, null, 0, newPromise());
        execute(futureTask);
        return futureTask.future();
    }

    @Override
    public <V> IFuture<V> submitFunc(Callable<? extends V> task, int options) {
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, null, options, newPromise());
        execute(futureTask);
        return futureTask.future();
    }

    @Override
    public <V> IFuture<V> submitFunc(Function<? super IContext, ? extends V> task, IContext ctx) {
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, ctx, 0, newPromise());
        execute(futureTask);
        return futureTask.future();
    }

    @Override
    public <V> IFuture<V> submitFunc(Function<? super IContext, ? extends V> task, IContext ctx, int options) {
        PromiseTask<V> futureTask = PromiseTask.ofFunction(task, ctx, options, newPromise());
        execute(futureTask);
        return futureTask.future();
    }

    @Override
    public IFuture<?> submitAction(Runnable task) {
        PromiseTask<?> futureTask = PromiseTask.ofAction(task, null, 0, newPromise());
        execute(futureTask);
        return futureTask.future();
    }

    @Override
    public IFuture<?> submitAction(Runnable task, int options) {
        PromiseTask<?> futureTask = PromiseTask.ofAction(task, null, options, newPromise());
        execute(futureTask);
        return futureTask.future();
    }

    @Override
    public IFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx) {
        PromiseTask<?> futureTask = PromiseTask.ofAction(task, ctx, 0, newPromise());
        execute(futureTask);
        return futureTask.future();
    }

    @Override
    public IFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx, int options) {
        PromiseTask<?> futureTask = PromiseTask.ofAction(task, ctx, options, newPromise());
        execute(futureTask);
        return futureTask.future();
    }

    // endregion

    // ---------------------------------------- 工具方法 ---------------------------------------

    protected static void safeExecute(Runnable task) {
        try {
            task.run();
        } catch (Throwable t) {
            if (t instanceof VirtualMachineError) {
                logger.error("A task raised an exception. Task: {}", task, t);
            } else {
                logger.warn("A task raised an exception. Task: {}", task, t);
            }
        }
    }

    protected static void logCause(Throwable t) {
        if (t instanceof VirtualMachineError) {
            logger.error("A task raised an exception.", t);
        } else {
            logger.warn("A task raised an exception.", t);
        }
    }

}