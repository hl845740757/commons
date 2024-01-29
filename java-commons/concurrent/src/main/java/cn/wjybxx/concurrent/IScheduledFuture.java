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

import java.util.concurrent.Delayed;
import java.util.concurrent.RunnableScheduledFuture;
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;

/**
 * 1.该接口仅用于JDK兼容（类型兼容），不建议用户使用。
 * 2.这里的接口不保证对用户线程的可见性，甚至仅仅是不抛异常而已。
 *
 * @author wjybxx
 * date 2023/4/9
 */
@SuppressWarnings("NullableProblems")
public interface IScheduledFuture<V> extends IFuture<V>, ScheduledFuture<V> {

    /**
     * 该接口的可见性取决于实现，在某些情况下该接口不提供完全的可见性，查询可能是不准确的。
     * {@inheritDoc}
     */
    @Override
    long getDelay(TimeUnit unit);

    /**
     * JDK的{@link ScheduledFuture}继承{@link Delayed}是个错误，
     * 让{@link RunnableScheduledFuture}继承{@link Delayed}都还好。
     * {@link ScheduledFuture}继承{@link Delayed}暴露了实现，也限制了实现。
     *
     * @deprecated 该接口并不是提供给用户的，用户不应当调用该方法，实现类也不一定实现该方法。
     */
    @Deprecated
    @Override
    int compareTo(Delayed o);

}