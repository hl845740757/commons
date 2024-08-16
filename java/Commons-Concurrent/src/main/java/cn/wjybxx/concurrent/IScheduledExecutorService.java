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

import java.util.concurrent.Callable;
import java.util.concurrent.ScheduledExecutorService;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;

/**
 * 不继承{@link ScheduledExecutorService}，JDK的{@link ScheduledFuture}设计有问题。
 *
 * @author wjybxx
 * date - 2024/1/9
 */
@SuppressWarnings("NullableProblems")
public interface IScheduledExecutorService extends IExecutorService, ScheduledExecutorService {

    /**
     * 创建一个promise以用于任务调度
     * 如果当前Executor是{@link SingleThreadExecutor}，返回的future将禁止在当前EventLoop上执行阻塞操作。
     *
     * @implNote 通常应该绑定当前executor
     */
    <V> IScheduledPromise<V> newScheduledPromise();

    /**
     * 为避免过多的参数和重载方法，我们通过Builder构建更为复杂的任务。
     *
     * @param builder 任务构建器。
     * @param <V>     任务的结果类型
     * @return future
     */
    <V> IScheduledFuture<V> schedule(ScheduledTaskBuilder<V> builder);

    /**
     * 延迟指定时间后执行给定的任务
     *
     * @param task        要执行的任务
     * @param cancelToken 取消令牌
     */
    <V> IScheduledFuture<V> scheduleFunc(Callable<V> task, long delay, TimeUnit unit, ICancelToken cancelToken);

    /**
     * 延迟指定时间后执行给定的任务
     *
     * @param task        要执行的任务
     * @param cancelToken 取消令牌
     */
    IScheduledFuture<?> scheduleAction(Runnable task, long delay, TimeUnit unit, ICancelToken cancelToken);

    /**
     * 以固定延迟执行给定的任务(少执行了就少执行了)
     */
    IScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay, TimeUnit unit, ICancelToken cancelToken);

    /**
     * 以固定频率执行给定的任务（少执行了会补-慎用）
     */
    IScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period, TimeUnit unit, ICancelToken cancelToken);

    // region jdk

    /**
     * 延迟指定时间后执行给定的任务
     * {@inheritDoc}
     */
    @Override
    default IScheduledFuture<?> schedule(Runnable task, long delay, TimeUnit unit) {
        return scheduleAction(task, delay, unit, ICancelToken.NONE);
    }

    /**
     * 延迟指定时间后执行给定的任务
     * {@inheritDoc}
     */
    @Override
    default <V> IScheduledFuture<V> schedule(Callable<V> task, long delay, TimeUnit unit) {
        return scheduleFunc(task, delay, unit, ICancelToken.NONE);
    }

    /**
     * 以固定延迟执行给定的任务(少执行了就少执行了)
     * {@inheritDoc}
     */
    @Override
    default IScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay, TimeUnit unit) {
        return scheduleWithFixedDelay(task, initialDelay, delay, unit, ICancelToken.NONE);
    }

    /**
     * 以固定频率执行给定的任务（少执行了会补-慎用）
     * {@inheritDoc}
     */
    @Override
    default IScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period, TimeUnit unit) {
        return scheduleAtFixedRate(task, initialDelay, period, unit, ICancelToken.NONE);
    }

    // ENDREGION
}