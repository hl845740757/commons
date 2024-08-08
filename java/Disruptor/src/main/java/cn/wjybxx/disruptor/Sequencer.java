/*
 * Copyright 2024 wjybxx(845740757@qq.com)
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

package cn.wjybxx.disruptor;


import javax.annotation.Nonnull;
import javax.annotation.Nullable;

/**
 * 序号生成器
 * 1. 序号生成器是生产者和消费者协调的集成。
 * 2. 不继承{@link ProducerBarrier}是为了避免暴露不必要的接口给等待策略等。
 *
 * <h3>安全停止</h3>
 * 要安全停止整个系统，必须调用所有消费者的{@link ConsumerBarrier#alert()}方法停止消费者，
 * 然后调用{@link ProducerBarrier#removeDependentBarrier(SequenceBarrier)}从生产者要追踪的屏障中删除。
 * 否则会导致死锁！！！
 * ps: {@code Sequencer}默认不追踪所有的{@link SequenceBarrier}，因此依赖用户进行管理。
 *
 * @author wjybxx
 * date - 2024/1/16
 */
public interface Sequencer {

    // region disruptor

    /**
     * 添加序号生成器需要追踪的网关屏障（新增的末端消费者消费序列/进度），
     * Sequencer（生产者）会持续跟踪它们的进度信息，以协调生产者和消费者之间的速度。
     * 即生产者想使用一个序号时必须等待所有的网关Sequence处理完该序号。
     * <p>
     * Add the specified gating sequences to this instance of the Disruptor.  They will
     * safely and atomically added to the list of gating sequences.
     *
     * @param gatingBarriers The sequences to add.
     */
    default void addGatingBarriers(SequenceBarrier... gatingBarriers) {
        getProducerBarrier().addDependentBarriers(gatingBarriers);
    }

    /**
     * 移除这些网关屏障，不再跟踪它们的进度信息；
     * 特殊用法：如果移除了所有的消费者，那么生产者便不会被阻塞，就能从{@link ProducerBarrier#next()}中退出。
     * <p>
     * Remove the specified gatingBarrier from this sequencer.
     *
     * @param gatingBarrier to be removed.
     * @return <tt>true</tt> if this gatingBarrier was found, <tt>false</tt> otherwise.
     */
    default boolean removeGatingBarrier(SequenceBarrier gatingBarrier) {
        return getProducerBarrier().removeDependentBarrier(gatingBarrier);
    }

    // region wjybxx

    /**
     * 默认等待策略
     */
    @Nonnull
    WaitStrategy getWaitStrategy();

    /**
     * 获取生产者屏障 --用于生产者申请和发布数据。
     */
    ProducerBarrier getProducerBarrier();

    /**
     * 使用默认的等待策略创建一个【单线程消费者】使用的屏障。
     * ps: 用户可以创建自己的自定义实例。
     *
     * @param barriersToTrack 该组消费者依赖的屏障
     * @return 默认的消费者屏障
     */
    default ConsumerBarrier newSingleConsumerBarrier(SequenceBarrier... barriersToTrack) {
        return new SingleConsumerBarrier(getProducerBarrier(), getWaitStrategy(), barriersToTrack);
    }

    /**
     * 使用给定的等待策略创建一个【单线程消费者】使用的屏障。
     * ps: 用户可以创建自己的自定义实例。
     *
     * @param waitStrategy    该组消费者的等待策略
     * @param barriersToTrack 该组消费者依赖的屏障
     * @return 默认的消费者屏障
     */
    default ConsumerBarrier newSingleConsumerBarrier(WaitStrategy waitStrategy, SequenceBarrier... barriersToTrack) {
        if (waitStrategy == null) waitStrategy = getWaitStrategy();
        return new SingleConsumerBarrier(getProducerBarrier(), waitStrategy, barriersToTrack);
    }

    /**
     * 使用默认的等待策略创建一个【多线程消费者】使用的屏障。
     * ps: 用户可以创建自己的自定义实例。
     *
     * @param workerCount     消费者数量
     * @param barriersToTrack 该组消费者依赖的屏障
     * @return 默认的消费者屏障
     */
    default ConsumerBarrier newMultiConsumerBarrier(int workerCount, SequenceBarrier... barriersToTrack) {
        return new MultiConsumerBarrier(getProducerBarrier(), workerCount, getWaitStrategy(), barriersToTrack);
    }

    /**
     * 使用默认的等待策略创建一个【多线程消费者】使用的屏障。
     * ps: 用户可以创建自己的自定义实例。
     *
     * @param workerCount     消费者数量
     * @param waitStrategy    该组消费者的等待策略
     * @param barriersToTrack 该组消费者依赖的屏障
     * @return 默认的消费者屏障
     */
    default ConsumerBarrier newMultiConsumerBarrier(int workerCount, WaitStrategy waitStrategy, SequenceBarrier... barriersToTrack) {
        if (waitStrategy == null) waitStrategy = getWaitStrategy();
        return new MultiConsumerBarrier(getProducerBarrier(), workerCount, waitStrategy, barriersToTrack);
    }
    // endregion
}