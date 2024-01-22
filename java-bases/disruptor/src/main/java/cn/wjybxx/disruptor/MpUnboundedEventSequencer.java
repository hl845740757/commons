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

package cn.wjybxx.disruptor;

import java.util.Objects;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2024/1/20
 */
public class MpUnboundedEventSequencer<E> implements EventSequencer<E> {

    private final MpUnboundedBuffer<E> buffer;
    private final MpUnboundedBufferSequencer<E> sequencer;

    public MpUnboundedEventSequencer(Builder<E> builder) {
        Objects.requireNonNull(builder);
        buffer = new MpUnboundedBuffer<>(builder.getChunkSize(),
                builder.getMaxPooledChunks(),
                builder.getFactory());
        sequencer = new MpUnboundedBufferSequencer<>(buffer,
                builder.getWaitStrategy(),
                builder.getBlocker());
    }

    public MpUnboundedBuffer<E> getBuffer() {
        return buffer;
    }

    /** 判断两个序号是否在同一个块 */
    public boolean inSameChunk(long seq1, long seq2) {
        return buffer.inSameChunk(seq1, seq2);
    }

    /** 手动触发回收 -- 该方法可安全调用 */
    public boolean tryMoveHeadToNext() {
        return buffer.tryMoveHeadToNext(sequencer.minimumSequence());
    }

    /** 手动触发回收，慎重调用该方法，序号错误将导致严重bug */
    public boolean tryMoveHeadToNext(long gatingSequence) {
        return buffer.tryMoveHeadToNext(gatingSequence);
    }

    // region buffer

    @Override
    public final E get(long sequence) {
        return buffer.get(sequence);
    }

    @Override
    public final E producerGet(long sequence) {
        return buffer.producerGet(sequence);
    }

    @Override
    public final E consumerGet(long sequence) {
        return buffer.consumerGet(sequence);
    }

    @Override
    public int capacity() {
        return UNBOUNDED_CAPACITY;
    }

    @Override
    public long remainingCapacity() {
        return Integer.MAX_VALUE;
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

    public static <E> Builder<E> newBuilder() {
        return new Builder<>();
    }

    public static <E> Builder<E> newBuilder(EventFactory<? extends E> factory) {
        return new Builder<E>()
                .setFactory(factory);
    }

    public static class Builder<E> extends EventSequencerBuilder<E> {

        private int chunkSize = 1024;
        private int maxPooledChunks = 8;

        @Override
        public MpUnboundedEventSequencer<E> build() {
            return new MpUnboundedEventSequencer<>(this);
        }

        /** 单个块大小 */
        public int getChunkSize() {
            return chunkSize;
        }

        public Builder<E> setChunkSize(int chunkSize) {
            this.chunkSize = chunkSize;
            return this;
        }

        /** 缓存块数量 */
        public int getMaxPooledChunks() {
            return maxPooledChunks;
        }

        public Builder<E> setMaxPooledChunks(int maxPooledChunks) {
            this.maxPooledChunks = maxPooledChunks;
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
}