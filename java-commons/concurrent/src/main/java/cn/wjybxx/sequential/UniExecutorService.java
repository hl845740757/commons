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


import cn.wjybxx.concurrent.IExecutorService;
import cn.wjybxx.concurrent.SingleThreadExecutor;
import cn.wjybxx.concurrent.TaskOption;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.Collection;
import java.util.List;
import java.util.concurrent.Callable;
import java.util.concurrent.Future;
import java.util.concurrent.TimeUnit;

/**
 * 用于在当前线程延迟执行任务的Executor -- {@link IExecutorService}。
 * 即：该Executor仍然在当前线程（提交任务的线程）执行提交的任务，只是会延迟执行。
 *
 * <h3>时序要求</h3>
 * 我们限定逻辑是在当前线程执行的，必须保证先提交的任务先执行。
 *
 * <h3>限制单帧任务数</h3>
 * 由于是在当前线程执行对应的逻辑，因而必须限制单帧执行的任务数，以避免占用过多的资源，同时，限定单帧任务数可避免死循环。
 *
 * <h3>外部驱动</h3>
 * 由于仍然是在当前线程执行，因此需要外部进行驱动，外部需要定时调用{@link #update()}
 *
 * <h3>指定执行阶段</h3>
 * 如果Executor支持在特定的阶段执行给定的任务，需要响应{@link TaskOption#MASK_SCHEDULE_PHASE}指定的阶段。
 *
 * @author wjybxx
 * date 2023/4/3
 */
@NotThreadSafe
public interface UniExecutorService extends IExecutorService, SingleThreadExecutor {

    /**
     * 心跳方法
     * 外部需要每一帧调用该方法以执行任务。
     */
    void update();

    /**
     * 为避免死循环或占用过多cpu，单次{@link #update()}可能存在一些限制，因此可能未执行所有的可执行任务。
     * 该方法用于探测是否还有可执行的任务，如果外部可以分配更多的资源。
     *
     * @return 如果还有可执行任务则返回true，否则返回false
     */
    boolean needMoreTicks();

    // region 废弃api

    @Deprecated
    @Nonnull
    @Override
    default <T> List<Future<T>> invokeAll(Collection<? extends Callable<T>> tasks) {
        throw new UnsupportedOperationException();
    }

    @Deprecated
    @Nonnull
    @Override
    default <T> List<Future<T>> invokeAll(Collection<? extends Callable<T>> tasks, long timeout, TimeUnit unit) {
        throw new UnsupportedOperationException();
    }

    @Deprecated
    @Nonnull
    @Override
    default <T> T invokeAny(Collection<? extends Callable<T>> tasks) {
        throw new UnsupportedOperationException();
    }

    @Deprecated
    @Override
    default <T> T invokeAny(Collection<? extends Callable<T>> tasks, long timeout, TimeUnit unit) {
        throw new UnsupportedOperationException();
    }

    // endregion
}