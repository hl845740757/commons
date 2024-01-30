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

import javax.annotation.Nonnull;
import java.util.concurrent.Delayed;
import java.util.concurrent.TimeUnit;

/**
 * 可获取结果的延时任务
 *
 * @author wjybxx
 * date - 2024/1/29
 */
public interface IScheduledFutureTask<V> extends IFutureTask<V>, Delayed {

    @Override
    IScheduledFuture<V> future();

    /** 关联的任务是否是周期性任务 */
    boolean isPeriodic();

    /**
     * 获取任务下次执行的延迟。
     * ps：该接口的可见性取决于实现，某些实现不提供即时的可见性，查询可能是不准确的。
     */
    @Override
    long getDelay(@Nonnull TimeUnit unit);

    @Override
    int compareTo(@Nonnull Delayed o);

}