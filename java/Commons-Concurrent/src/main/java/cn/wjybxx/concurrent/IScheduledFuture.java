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
import java.util.concurrent.ScheduledFuture;
import java.util.concurrent.TimeUnit;

/**
 * 定时任务关联的Future。
 * ps：不能立即获得结果的任务，都应该关联该类型的Future。
 *
 * @author wjybxx
 * date 2023/4/9
 */
@SuppressWarnings("NullableProblems")
public interface IScheduledFuture<V> extends IFuture<V>, ScheduledFuture<V> {

    /**
     * 获取任务下次执行的延迟。
     * ps：
     * 1.该接口的可见性取决于实现，某些实现不提供即时的可见性，查询可能是不准确的。
     * 2.默认抛出异常，因为如果要想该接口长期有效，Future就需要长期持有Task类，这将导致Task类无法池化。
     * 3.该接口理论上可能有用，但我从业以来都没有用过。。。
     * <p>
     * Q：有没有既能池化，又能保持该接口有效的方法呢？
     * A：有的，可以在Future上冗余存储Task的触发时间，而不是直接持有Task类 -- 暂时先不支持。
     */
    @Override
    default long getDelay(TimeUnit unit) {
        throw new UnsupportedOperationException();
    }

    /**
     * JDK的{@link ScheduledFuture}继承{@link Delayed}是个错误，
     * 允许用户查询任务的下次执行延迟是合理的，但暴露的{@code compareTo}则是不必要的，这为Future增加了不必要的职责。
     *
     * @deprecated 该接口并不是提供给用户的，用户不应当调用该方法，实现类也不一定实现该方法。
     */
    @Deprecated
    @Override
    default int compareTo(Delayed o) {
        throw new UnsupportedOperationException();
    }

}