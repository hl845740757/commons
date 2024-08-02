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

import java.util.Objects;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2024/1/17
 */
public class RingBufferEventSequencer<E> implements EventSequencer<E> {

    private final RingBuffer<E> buffer;
    private final RingBufferSequencer sequencer;

    public RingBufferEventSequencer(Builder<E> builder) {
        Objects.requireNonNull(builder);
        this.buffer = new RingBuffer<>(builder.getFactory(), builder.getBufferSize());
        if (builder.getProducerType() == ProducerType.MULTI) {
            sequencer = new MultiProducerSequencer(
                    builder.getBufferSize(),
                    builder.getProducerSleepNanos(),
                    builder.getWaitStrategy(),
                    builder.getBlocker());
        } else {
            sequencer = new SingleProducerSequencer(
                    builder.getBufferSize(),
                    builder.getProducerSleepNanos(),
                    builder.getWaitStrategy(),
                    builder.getBlocker());
        }
    }

    public RingBuffer<E> getBuffer() {
        return buffer;
    }

    // region buffer

    @Override
    public final E get(long sequence) {
        return buffer.elementAt(sequence);
    }

    @Override
    public final E producerGet(long sequence) {
        return buffer.elementAt(sequence);
    }

    @Override
    public final E consumerGet(long sequence) {
        return buffer.elementAt(sequence);
    }

    @Override
    public void producerSet(long sequence, E data) {
        Objects.requireNonNull(data);
        buffer.setElement(sequence, data);
    }

    @Override
    public void consumerSet(long sequence, E data) {
        buffer.setElement(sequence, data);
    }

    @Override
    public int capacity() {
        return buffer.getBufferSize();
    }

    @Override
    public long remainingCapacity() {
        return sequencer.remainingCapacity();
    }

    @Override
    public Sequencer sequencer() {
        return sequencer;
    }

    @Override
    public ProducerBarrier producerBarrier() {
        return sequencer;
    }

    // endregion

    // region override

    public void addGatingBarriers(SequenceBarrier... gatingBarriers) {
        sequencer.addGatingBarriers(gatingBarriers);
    }

    @Override
    public boolean removeGatingBarrier(SequenceBarrier gatingBarrier) {
        return sequencer.removeGatingBarrier(gatingBarrier);
    }

    @Override
    public ConsumerBarrier newSingleConsumerBarrier(SequenceBarrier... barriersToTrack) {
        return sequencer.newSingleConsumerBarrier(barriersToTrack);
    }

    @Override
    public ConsumerBarrier newSingleConsumerBarrier(WaitStrategy waitStrategy, SequenceBarrier... barriersToTrack) {
        return sequencer.newSingleConsumerBarrier(waitStrategy, barriersToTrack);
    }

    @Override
    public ConsumerBarrier newMultiConsumerBarrier(int workerCount, SequenceBarrier... barriersToTrack) {
        return sequencer.newMultiConsumerBarrier(workerCount, barriersToTrack);
    }

    @Override
    public ConsumerBarrier newMultiConsumerBarrier(int workerCount, WaitStrategy waitStrategy, SequenceBarrier... barriersToTrack) {
        return sequencer.newMultiConsumerBarrier(workerCount, waitStrategy, barriersToTrack);
    }

    @Override
    public boolean hasAvailableCapacity(int requiredCapacity) {
        return sequencer.hasAvailableCapacity(requiredCapacity);
    }

    @Override
    public long next() {
        return sequencer.next(1); // 传入1可减少调用
    }

    @Override
    public long next(int n) {
        return sequencer.next(n);
    }

    @Override
    public long tryNext() {
        return sequencer.tryNext(1);
    }

    @Override
    public long tryNext(int n) {
        return sequencer.tryNext(n);
    }

    @Override
    public long nextInterruptibly() throws InterruptedException {
        return sequencer.nextInterruptibly(1);
    }

    @Override
    public long nextInterruptibly(int n) throws InterruptedException {
        return sequencer.nextInterruptibly(n);
    }

    @Override
    public long tryNext(int n, long timeout, TimeUnit unit) {
        return sequencer.tryNext(n, timeout, unit);
    }

    @Override
    public void publish(long sequence) {
        sequencer.publish(sequence);
    }

    @Override
    public void publish(long lo, long hi) {
        sequencer.publish(lo, hi);
    }
    // endregion

    // region builder

    /** 多线程生产者builder */
    public static <E> Builder<E> newMultiProducer() {
        return new Builder<E>()
                .setProducerType(ProducerType.MULTI);
    }

    /** 多线程生产者builder */
    public static <E> Builder<E> newMultiProducer(EventFactory<? extends E> factory) {
        return new Builder<E>()
                .setProducerType(ProducerType.MULTI)
                .setFactory(factory);
    }

    /** 单线程生产者builder */
    public static <E> Builder<E> newSingleProducer() {
        return new Builder<E>()
                .setProducerType(ProducerType.SINGLE);
    }

    /** 单线程生产者builder */
    public static <E> Builder<E> newSingleProducer(EventFactory<? extends E> factory) {
        return new Builder<E>()
                .setProducerType(ProducerType.SINGLE)
                .setFactory(factory);
    }

    public static class Builder<E> extends EventSequencerBuilder<E> {

        private ProducerType producerType = ProducerType.MULTI;
        private int bufferSize = 8192;

        @Override
        public RingBufferEventSequencer<E> build() {
            return new RingBufferEventSequencer<>(this);
        }

        /** 生产者的类型 */
        public ProducerType getProducerType() {
            return producerType;
        }

        public Builder<E> setProducerType(ProducerType producerType) {
            this.producerType = Objects.requireNonNull(producerType);
            return this;
        }

        /** 环形缓冲区的大小 */
        public int getBufferSize() {
            return bufferSize;
        }

        public Builder<E> setBufferSize(int bufferSize) {
            this.bufferSize = bufferSize;
            return this;
        }

        @Override
        public Builder<E> setFactory(EventFactory<? extends E> factory) {
            return (Builder<E>) super.setFactory(factory);
        }

        @Override
        public Builder<E> setProducerSleepNanos(long producerSleepNanos) {
            return (Builder<E>) super.setProducerSleepNanos(producerSleepNanos);
        }

        @Override
        public Builder<E> setWaitStrategy(WaitStrategy waitStrategy) {
            return (Builder<E>) super.setWaitStrategy(waitStrategy);
        }

        @Override
        public Builder<E> enableBlocker() {
            return (Builder<E>) super.enableBlocker();
        }

        @Override
        public Builder<E> disableBlocker() {
            return (Builder<E>) super.disableBlocker();
        }
    }

    // endregion
}