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

import cn.wjybxx.common.concurrent.IContext;
import cn.wjybxx.common.concurrent.ScheduledTaskBuilder;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.concurrent.Callable;
import java.util.function.Consumer;
import java.util.function.Function;

/**
 * {@inheritDoc}
 * 定时任务调度器，时间单位取决于具体的实现，通常是毫秒 -- 也可能是帧数。
 *
 * <h3>时序保证</h3>
 * 1. 单次执行的任务之间，有严格的时序保证，当过期时间(超时时间)相同时，先提交的一定先执行。
 * 2. 周期性执行的的任务，仅首次执行具备时序保证，当进入周期运行时，与其它任务之间便不具备时序保证。
 *
 * <h3>避免死循环</h3>
 * 子类实现必须在保证时序的条件下解决可能的死循环问题。
 * Q: 死循环是如何产生的？
 * A: 对于周期性任务，我们严格要求了周期间隔大于0，因此周期性的任务不会引发无限循环问题。
 * 但如果用户基于{@link #schedule(Runnable, long)}实现循环，则在执行回调时可能添加一个立即执行的task（超时时间小于等于0），则可能陷入死循环。
 * 这种情况一般不是有意为之，而是某些特殊情况下产生的，比如：下次执行的延迟是计算出来的，而算出来的延迟总是为0或负数（线程缓存了时间戳，导致计算结果同一帧不会变化）。
 * 如果很好的限制了单帧执行的任务数，可以避免死循环。不过，错误的调用仍然可能导致其它任务得不到执行。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public interface UniScheduledExecutor extends UniExecutorService {

    /**
     * 为避免过多的参数和重载方法，我们通过Builder构建更为复杂的任务。
     *
     * @param builder 任务构建器
     * @param <V>     任务的结果类型
     * @return future
     */
    <V> UniScheduledFuture<V> schedule(ScheduledTaskBuilder<V> builder);

    /**
     * 延迟指定时间后执行给定的任务
     *
     * @param task 要执行的任务
     * @param ctx  上下文-主要是取消令牌
     */
    UniScheduledFuture<?> scheduleAction(Consumer<? super IContext> task, IContext ctx, long delay);

    /**
     * 延迟指定时间后执行给定的任务
     *
     * @param task 要执行的任务
     * @param ctx  上下文-主要是取消令牌
     */
    <V> UniScheduledFuture<V> scheduleFunc(Function<? super IContext, V> task, IContext ctx, long delay);

    // region jdk风格

    /**
     * 延迟指定时间后执行给定的任务
     * {@inheritDoc}
     */
    UniScheduledFuture<?> schedule(Runnable task, long delay);

    /**
     * 延迟指定时间后执行给定的任务
     * {@inheritDoc}
     */
    <V> UniScheduledFuture<V> schedule(Callable<V> task, long delay);

    /**
     * 以固定频率执行给定的任务（少执行了会补）
     * {@inheritDoc}
     */
    UniScheduledFuture<?> scheduleAtFixedRate(Runnable task, long initialDelay, long period);

    /**
     * 以固定延迟执行给定的任务(少执行了就少执行了)
     * {@inheritDoc}
     */
    UniScheduledFuture<?> scheduleWithFixedDelay(Runnable task, long initialDelay, long delay);

    // endregion
}