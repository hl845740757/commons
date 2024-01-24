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

import java.util.concurrent.locks.Condition;

/**
 * @param <E> 事件类型
 * @author wjybxx
 * date - 2024/1/18
 */
public abstract class EventSequencerBuilder<E> {

    private EventFactory<? extends E> factory;
    private long producerSleepNanos = 1;
    private WaitStrategy waitStrategy = TimeoutSleepingWaitStrategy.INSTANCE;
    private SequenceBlocker blocker;

    /** 构建最终的对象 */
    public abstract EventSequencer<E> build();

    /** 事件对象工厂 */
    public EventFactory<? extends E> getFactory() {
        return factory;
    }

    public EventSequencerBuilder<E> setFactory(EventFactory<? extends E> factory) {
        this.factory = factory;
        return this;
    }

    /**
     * 生产者等待消费者时每次的挂起时间
     * 注意：
     * 1. 是每次尝试的睡眠时间。
     * 2. 如果为0则不挂起线程，而是忙等（空自旋）
     * 3. 如果大于0则挂起一段时间
     */
    public long getProducerSleepNanos() {
        return producerSleepNanos;
    }

    public EventSequencerBuilder<E> setProducerSleepNanos(long producerSleepNanos) {
        this.producerSleepNanos = producerSleepNanos;
        return this;
    }

    /** 默认的等待策略 */
    public WaitStrategy getWaitStrategy() {
        return waitStrategy;
    }

    public EventSequencerBuilder<E> setWaitStrategy(WaitStrategy waitStrategy) {
        this.waitStrategy = waitStrategy;
        return this;
    }

    /** 获取序列阻塞器 */
    public SequenceBlocker getBlocker() {
        return blocker;
    }

    /**
     * 启用序号阻塞器。
     * 1. 如果存在需要通过{@link Condition}等待生产者发布序号的消费者，则需要启用blocker。
     * 2. 默认情况下不启用。
     */
    public EventSequencerBuilder<E> enableBlocker() {
        blocker = new SequenceBlocker();
        return this;
    }

    public EventSequencerBuilder<E> disableBlocker() {
        blocker = null;
        return this;
    }

}
