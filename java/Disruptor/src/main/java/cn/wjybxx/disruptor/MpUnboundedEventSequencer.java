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
 * date - 2024/1/20
 */
public class MpUnboundedEventSequencer<T> implements EventSequencer<T> {

    private final MpUnboundedBuffer<T> buffer;
    private final MpUnboundedBufferSequencer<T> sequencer;

    private MpUnboundedEventSequencer(Builder<T> builder) {
        Objects.requireNonNull(builder);
        buffer = new MpUnboundedBuffer<>(builder.getFactory(),
                builder.getChunkSize(),
                builder.getMaxPooledChunks());
        sequencer = new MpUnboundedBufferSequencer<>(buffer,
                builder.getWaitStrategy(),
                builder.getBlocker());
    }

    /** buffer */
    public MpUnboundedBuffer<T> getBuffer() {
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
    public final T get(long sequence) {
        return buffer.get(sequence);
    }

    @Override
    public final T producerGet(long sequence) {
        return buffer.producerGet(sequence);
    }

    @Override
    public final T consumerGet(long sequence) {
        return buffer.consumerGet(sequence);
    }

    @Override
    public void producerSet(long sequence, T data) {
        buffer.producerSet(sequence, data);
    }

    @Override
    public void consumerSet(long sequence, T data) {
        buffer.consumerSet(sequence, data);
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

    public static <T> Builder<T> newBuilder(EventFactory<? extends T> factory) {
        return new Builder<>(factory);
    }

    public static class Builder<T> extends EventSequencerBuilder<T> {

        private int chunkSize = 1024;
        private int maxPooledChunks = 8;

        public Builder(EventFactory<? extends T> factory) {
            super(factory);
        }

        @Override
        public MpUnboundedEventSequencer<T> build() {
            return new MpUnboundedEventSequencer<>(this);
        }

        /** 单个块大小 */
        public int getChunkSize() {
            return chunkSize;
        }

        public Builder<T> setChunkSize(int chunkSize) {
            this.chunkSize = chunkSize;
            return this;
        }

        /** 缓存块数量 */
        public int getMaxPooledChunks() {
            return maxPooledChunks;
        }

        public Builder<T> setMaxPooledChunks(int maxPooledChunks) {
            this.maxPooledChunks = maxPooledChunks;
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