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

import java.util.concurrent.Callable;
import java.util.concurrent.TimeUnit;

/**
 * 子类需要在{@link #execute(Runnable)}的时候为任务赋值id和options
 *
 * @author wjybxx
 * date - 2023/4/7
 */
@SuppressWarnings("NullableProblems")
public abstract class AbstractUniScheduledExecutor
        extends AbstractUniExecutor
        implements UniScheduledExecutor {

    // region schedule

    @Override
    public <V> IScheduledPromise<V> newScheduledPromise() {
        return new UniScheduledPromise<>(this);
    }

    protected abstract IScheduledHelper helper();

    @Override
    public <T> IScheduledFuture<T> schedule(ScheduledTaskBuilder<T> builder) {
        IScheduledPromise<T> promise = newScheduledPromise();
        execute(ScheduledPromiseTask.ofBuilder(builder, promise, helper()));
        return promise;
    }

    @Override
    public <T> IScheduledFuture<T> scheduleFunc(Callable<T> task, long delay, TimeUnit unit, ICancelToken cancelToken) {
        IScheduledPromise<T> promise = newScheduledPromise();
        IScheduledHelper helper = helper();

        execute(ScheduledPromiseTask.ofFunction(task, cancelToken, 0, promise, helper, helper.triggerTime(delay, unit)));
        return promise;
    }

    @Override
    public IScheduledFuture<?> scheduleAction(Runnable task, long delay, TimeUnit unit, ICancelToken cancelToken) {
        IScheduledPromise<Object> promise = newScheduledPromise();
        IScheduledHelper helper = helper();

        execute(ScheduledPromiseTask.ofAction(task, cancelToken, 0, promise, helper, helper.triggerTime(delay, unit)));
        return promise;
    }

    @Override
    public IScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period, TimeUnit unit, ICancelToken cancelToken) {
        ScheduledTaskBuilder<Object> builder = ScheduledTaskBuilder.newAction(task, cancelToken)
                .setFixedRate(initialDelay, period, unit);

        IScheduledPromise<Object> promise = newScheduledPromise();
        execute(ScheduledPromiseTask.ofBuilder(builder, promise, helper()));
        return promise;
    }

    @Override
    public IScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay, TimeUnit unit, ICancelToken cancelToken) {
        ScheduledTaskBuilder<Object> builder = ScheduledTaskBuilder.newAction(task, cancelToken)
                .setFixedDelay(initialDelay, delay, unit);

        IScheduledPromise<Object> promise = newScheduledPromise();
        execute(ScheduledPromiseTask.ofBuilder(builder, promise, helper()));
        return promise;
    }

    @Override
    public final IScheduledFuture<?> schedule(Runnable task, long delay, TimeUnit unit) {
        return scheduleAction(task, delay, unit, ICancelToken.NONE);
    }

    @Override
    public final <V> IScheduledFuture<V> schedule(Callable<V> task, long delay, TimeUnit unit) {
        return scheduleFunc(task, delay, unit, ICancelToken.NONE);
    }

    @Override
    public final IScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay, TimeUnit unit) {
        return scheduleWithFixedDelay(task, initialDelay, delay, unit, ICancelToken.NONE);
    }

    @Override
    public final IScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period, TimeUnit unit) {
        return scheduleAtFixedRate(task, initialDelay, period, unit, ICancelToken.NONE);
    }
    // endregion
}