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

package cn.wjybxx.common.unitask;

import cn.wjybxx.common.concurrent.FutureUtils;
import cn.wjybxx.common.concurrent.IContext;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import java.util.concurrent.Callable;
import java.util.concurrent.ExecutorService;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * @author wjybxx
 * date 2023/4/7
 */
public abstract class AbstractUniExecutor implements UniExecutorService {

    protected static final Logger logger = LoggerFactory.getLogger(AbstractUniExecutor.class);

    @Override
    public abstract void execute(Runnable command, int options);

    @Override
    public void execute(Runnable command) {
        execute(command, 0);
    }

    @Override
    public void execute(Consumer<? super IContext> action, IContext ctx) {
        execute(FutureUtils.toRunnable(action, ctx), 0);
    }

    @Override
    public void execute(Consumer<? super IContext> action, IContext ctx, int options) {
        execute(FutureUtils.toRunnable(action, ctx), options);
    }

    // region submit
    @Override
    public <V> UniPromise<V> newPromise(IContext ctx) {
        return new UniPromise<>(this, ctx);
    }

    @Override
    public <V> UniPromise<V> newPromise() {
        return new UniPromise<>(this, null);
    }

    @Override
    public <T> UniFuture<T> submit(Callable<T> task) {
        UniPromiseTask<T> futureTask = UniPromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    @Override
    public <V> UniFuture<V> submitFunc(Function<? super IContext, V> task, IContext ctx) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofFunction(task, newPromise(ctx));
        execute(futureTask, 0);
        return futureTask.future();
    }

    @Override
    public <V> UniFuture<V> submitFunc(Function<? super IContext, V> task, IContext ctx, int options) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofFunction(task, newPromise(ctx));
        execute(futureTask, options);
        return futureTask.future();
    }

    @Override
    public UniFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofConsumer(task, newPromise(ctx));
        execute(futureTask, 0);
        return futureTask.future();
    }

    @Override
    public UniFuture<?> submitAction(Consumer<? super IContext> task, IContext ctx, int options) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofConsumer(task, newPromise(ctx));
        execute(futureTask, options);
        return futureTask.future();
    }

    @Override
    public <V> UniFuture<V> submitCall(Callable<V> task) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    @Override
    public <V> UniFuture<V> submitCall(Callable<V> task, int options) {
        UniPromiseTask<V> futureTask = UniPromiseTask.ofCallable(task, newPromise(null));
        execute(futureTask, options);
        return futureTask.future();
    }

    @Override
    public UniFuture<?> submitRun(Runnable task) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofRunnable(task, newPromise(null));
        execute(futureTask, 0);
        return futureTask.future();
    }

    /** 该方法可能和{@link ExecutorService#submit(Runnable, Object)}冲突，因此我们要带后缀 */
    @Override
    public UniFuture<?> submitRun(Runnable task, int options) {
        UniPromiseTask<?> futureTask = UniPromiseTask.ofRunnable(task, newPromise(null));
        execute(futureTask, options);
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