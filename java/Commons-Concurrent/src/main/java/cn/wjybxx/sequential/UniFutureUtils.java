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

import cn.wjybxx.base.time.TimeProvider;
import cn.wjybxx.concurrent.IFuture;

import java.util.concurrent.Executor;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date 2023/4/3
 */
public class UniFutureUtils {

    // region factory

    public static <V> UniPromise<V> newPromise() {
        return new UniPromise<>();
    }

    public static <V> UniPromise<V> newPromise(Executor executor) {
        return new UniPromise<>(executor);
    }

    public static <V> IFuture<V> completedFuture(V result) {
        return UniPromise.completedPromise(result);
    }

    public static <V> IFuture<V> completedFuture(V result, Executor executor) {
        return UniPromise.completedPromise(result, executor);
    }

    public static <V> IFuture<V> failedFuture(Throwable ex) {
        return UniPromise.failedPromise(ex);
    }

    public static <V> IFuture<V> failedFuture(Throwable ex, Executor executor) {
        return UniPromise.failedPromise(ex, executor);
    }

    public static UniExecutorService newExecutor() {
        return new DefaultUniExecutor();
    }

    /**
     * @param countLimit 每帧允许运行的最大任务数，-1表示不限制；不可以为0
     * @param timeLimit  每帧允许的最大时间，-1表示不限制；不可以为0
     */
    public static UniExecutorService newExecutor(int countLimit, long timeLimit, TimeUnit timeUnit) {
        return new DefaultUniExecutor(countLimit, timeLimit, timeUnit);
    }

    /**
     * 返回的{@link UniScheduledExecutor#update()}默认不执行tick过程中新增加的任务
     *
     * @param timeProvider 用于调度器获取当前时间
     */
    public static UniScheduledExecutor newScheduledExecutor(TimeProvider timeProvider) {
        return new DefaultUniScheduledExecutor(timeProvider);
    }

    public static UniScheduledExecutor newScheduledExecutor(TimeProvider timeProvider, int initCapacity) {
        return new DefaultUniScheduledExecutor(timeProvider, initCapacity);
    }

    // endregion

}