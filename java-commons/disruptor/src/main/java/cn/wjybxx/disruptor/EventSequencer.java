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

import java.util.concurrent.TimeUnit;

/**
 * 事件生成器
 * 事件生成器是{@link Sequencer}和{@link DataProvider}的集成，每一种实现通常都和数据结构绑定。
 *
 * @author wjybxx
 * date - 2024/1/15
 */
public interface EventSequencer<T> extends DataProvider<T> {

    /** 无界空间对应的常量 */
    int UNBOUNDED_CAPACITY = -1;

    // region disruptor

    /**
     * 数据结构大小
     * 1.如果为【无界】数据结构，则返回-1；
     * 2.如果为【有界】数据结构，则返回真实值。
     * <p>
     * The capacity of the data structure to hold entries.
     *
     * @return the size of the RingBuffer.
     */
    int capacity();

    /**
     * 当前剩余容量
     * 1.并不一定具有价值，因为多线程模型下查询容器的当前大小时，它反映的总是一个旧值。
     * 2.如果为【无界】数据结构，可能返回任意值（大于0），但建议返回{@link Integer#MAX_VALUE}。
     * 3.如果为【有界】数据结构，则返回真实的值。
     * <p>
     * Get the remaining capacity for this sequencer.
     *
     * @return The number of slots remaining.
     */
    long remainingCapacity();

    // endregion

    // region wjybxx

    /** 获取序号生成器 -- 用于特殊需求 */
    Sequencer sequencer();

    /** 获取生产者屏障 -- 生产者发布数据 */
    ProducerBarrier producerBarrier();

    // endregion

    // region 转发-提高易用性
    // sequencer

    /** 添加一个网关屏障 -- 消费链最末端的消费者屏障 */
    default void addGatingBarriers(SequenceBarrier... gatingBarriers) {
        sequencer().addGatingBarriers(gatingBarriers);
    }

    /** 移除一个网关屏障 -- 消费链最末端的消费者屏障 */
    default boolean removeGatingBarrier(SequenceBarrier gatingBarrier) {
        return sequencer().removeGatingBarrier(gatingBarrier);
    }

    /** 创建一个【单消费者】的屏障 -- 使用默认的等待策略 */
    default ConsumerBarrier newSingleConsumerBarrier(SequenceBarrier... barriersToTrack) {
        return sequencer().newSingleConsumerBarrier(barriersToTrack);
    }

    /** 创建一个【单消费者】的屏障 -- 使用自定义等待策略 */
    default ConsumerBarrier newSingleConsumerBarrier(WaitStrategy waitStrategy, SequenceBarrier... barriersToTrack) {
        return sequencer().newSingleConsumerBarrier(waitStrategy, barriersToTrack);
    }

    /** 创建一个【多消费者】的屏障 -- 使用自定义等待策略 */
    default ConsumerBarrier newMultiConsumerBarrier(int workerCount, SequenceBarrier... barriersToTrack) {
        return sequencer().newMultiConsumerBarrier(workerCount, barriersToTrack);
    }

    /** 创建一个【多消费者】的屏障 -- 使用自定义等待策略 */
    default ConsumerBarrier newMultiConsumerBarrier(int workerCount, WaitStrategy waitStrategy, SequenceBarrier... barriersToTrack) {
        return sequencer().newMultiConsumerBarrier(workerCount, waitStrategy, barriersToTrack);
    }

    // producer
    default boolean hasAvailableCapacity(int requiredCapacity) {
        return producerBarrier().hasAvailableCapacity(requiredCapacity);
    }

    default long next() {
        return producerBarrier().next();
    }

    default long next(int n) {
        return producerBarrier().next(n);
    }

    default long tryNext() {
        return producerBarrier().tryNext();
    }

    default long tryNext(int n) {
        return producerBarrier().tryNext(n);
    }

    default long nextInterruptibly() throws InterruptedException {
        return producerBarrier().nextInterruptibly();
    }

    default long nextInterruptibly(int n) throws InterruptedException {
        return producerBarrier().nextInterruptibly(n);
    }

    default long tryNext(int n, long timeout, TimeUnit unit) {
        return producerBarrier().tryNext(n, timeout, unit);
    }

    default void publish(long sequence) {
        producerBarrier().publish(sequence);
    }

    default void publish(long lo, long hi) {
        producerBarrier().publish(lo, hi);
    }
    // endregion
}
