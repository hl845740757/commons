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
public class RingBufferEventSequencer<T> implements EventSequencer<T> {

    private final RingBuffer<T> buffer;
    private final RingBufferSequencer sequencer;

    private RingBufferEventSequencer(Builder<T> builder) {
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

    public RingBuffer<T> getBuffer() {
        return buffer;
    }

    // region buffer

    @Override
    public final T get(long sequence) {
        return buffer.getElement(sequence);
    }

    @Override
    public final T producerGet(long sequence) {
        return buffer.getElement(sequence);
    }

    @Override
    public final T consumerGet(long sequence) {
        return buffer.getElement(sequence);
    }

    @Override
    public void producerSet(long sequence, T data) {
        Objects.requireNonNull(data);
        buffer.setElement(sequence, data);
    }

    @Override
    public void consumerSet(long sequence, T data) {
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

    @Override
    public DataProvider<T> dataProvider() {
        return buffer;
    }
    // endregion

    // region producer

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
    public static <T> Builder<T> newMultiProducer(EventFactory<? extends T> factory) {
        return new Builder<T>(factory)
                .setProducerType(ProducerType.MULTI);
    }

    /** 单线程生产者builder */
    public static <T> Builder<T> newSingleProducer(EventFactory<? extends T> factory) {
        return new Builder<T>(factory)
                .setProducerType(ProducerType.SINGLE);
    }

    public static class Builder<T> extends EventSequencerBuilder<T> {

        private ProducerType producerType = ProducerType.MULTI;
        private int bufferSize = 8192;

        public Builder(EventFactory<? extends T> factory) {
            super(factory);
        }

        @Override
        public RingBufferEventSequencer<T> build() {
            return new RingBufferEventSequencer<>(this);
        }

        /** 生产者的类型 */
        public ProducerType getProducerType() {
            return producerType;
        }

        public Builder<T> setProducerType(ProducerType producerType) {
            this.producerType = Objects.requireNonNull(producerType);
            return this;
        }

        /** 环形缓冲区的大小 */
        public int getBufferSize() {
            return bufferSize;
        }

        public Builder<T> setBufferSize(int bufferSize) {
            this.bufferSize = bufferSize;
            return this;
        }

        @Override
        public Builder<T> setProducerSleepNanos(long producerSleepNanos) {
            return (Builder<T>) super.setProducerSleepNanos(producerSleepNanos);
        }

        @Override
        public Builder<T> setWaitStrategy(WaitStrategy waitStrategy) {
            return (Builder<T>) super.setWaitStrategy(waitStrategy);
        }

        @Override
        public Builder<T> enableBlocker() {
            return (Builder<T>) super.enableBlocker();
        }

        @Override
        public Builder<T> disableBlocker() {
            return (Builder<T>) super.disableBlocker();
        }
    }

    // endregion
}