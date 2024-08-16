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
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Objects;
import java.util.concurrent.TimeUnit;

/**
 * @author wjybxx
 * date - 2024/1/20
 */
public final class MpUnboundedBufferSequencer<T> implements ProducerBarrier, Sequencer {

    /**
     * 生产者组的序号。
     * 1. 生产者先根据sequence计算当前应该填充的chunk的索引（编号），也根据sequence计算落在该chunk的哪个槽位。
     * 2. 这仍然是一个预更新值，因为是多生产者模型。
     */
    private final Sequence cursor = new Sequence();
    /**
     * 网关屏障，序号生成器必须和这些屏障满足以下约束:
     * cursor-bufferSize <= Min(gatingSequence)
     * 即：所有的gatingBarrier让出下一个插槽后，生产者才能获取该插槽。
     * <p>
     * 对于生产者来讲，它只需要关注消费链最末端的消费者的进度（因为它们的进度是最慢的）。
     * 即：gatingBarrier就是所有消费链末端的消费们所拥有的的Sequence。（想一想食物链）
     */
    private volatile SequenceBarrier[] gatingBarriers = new SequenceBarrier[0];

    /** 关联的数据结构 -- 信息在buffer上 */
    private final MpUnboundedBuffer<T> buffer;
    /** 默认等待策略 */
    private final WaitStrategy waitStrategy;
    /** 序号阻塞器 -- 用于唤醒等待生产者发布数据的消费者 */
    private final SequenceBlocker blocker;

    public MpUnboundedBufferSequencer(MpUnboundedBuffer<T> buffer, WaitStrategy waitStrategy, @Nullable SequenceBlocker blocker) {
        this.buffer = Objects.requireNonNull(buffer, "buffer");
        this.waitStrategy = Objects.requireNonNull(waitStrategy, "waitStrategy");
        this.blocker = blocker;
    }

    @Deprecated
    @Override
    public void claim(long sequence) {
        if (!cursor.compareAndSet(INITIAL_SEQUENCE, sequence)) {
            throw new IllegalStateException();
        }
        buffer.claim(sequence);
    }

    // region producer

    @Override
    public void publish(long sequence) {
        MpUnboundedBufferChunk<T> chunk = buffer.producerChunkForSequence(sequence);
        chunk.publish((int) (sequence - chunk.minSequence()));
    }

    @Override
    public void publish(long lo, final long hi) {
        MpUnboundedBufferChunk<T> chunk = buffer.producerChunkForSequence(lo);
        while (hi > chunk.maxSequence()) {
            long minSequence = chunk.minSequence();
            chunk.publish((int) (lo - minSequence), chunk.length() - 1);

            lo = minSequence + chunk.length();
            chunk = buffer.producerChunkForSequence(lo); // 下一个块可能尚未构造
        }
        {
            long minSequence = chunk.minSequence();
            chunk.publish((int) (lo - minSequence), (int) (hi - minSequence));
        }
    }

    @Override
    public boolean isPublished(long sequence) {
        MpUnboundedBufferChunk<T> chunk = buffer.consumerChunkForSequence(sequence);
        return chunk.isPublished((int) (sequence - chunk.minSequence()));
    }

    /** 消费者可能看见尚未发布的块的序号 */
    @Override
    public long getHighestPublishedSequence(long lo, final long hi) {
        MpUnboundedBufferChunk<T> chunk = buffer.consumerChunkForSequence(lo);
        while (hi > chunk.maxSequence()) {
            long minSequence = chunk.minSequence();
            int maxIndex = chunk.length() - 1;
            int highestIndex = chunk.getHighestPublishedSequence((int) (lo - minSequence), maxIndex);
            if (highestIndex != maxIndex) {
                return minSequence + highestIndex;
            }
            chunk = chunk.lvNext();
            if (chunk == null) { // 下一个块可能尚未被填充（正在构造）
                return minSequence + highestIndex;
            }
            lo = chunk.minSequence();
        }
        {
            long minSequence = chunk.minSequence();
            int maxIndex = (int) (hi - minSequence);
            return minSequence + chunk.getHighestPublishedSequence((int) (lo - minSequence), maxIndex);
        }
    }

    @Override
    public boolean hasAvailableCapacity(int requiredCapacity) {
        if (requiredCapacity < 0) {
            throw new IllegalArgumentException("requiredCapacity: " + requiredCapacity);
        }
        return true;
    }

    @Override
    public long next() {
        return nextImpl(1);
    }

    @Override
    public long next(int n) {
        return nextImpl(n);
    }

    @Override
    public long nextInterruptibly() {
        return nextImpl(1);
    }

    @Override
    public long nextInterruptibly(int n) {
        return nextImpl(n);
    }

    @Override
    public long tryNext() {
        return nextImpl(1);
    }

    @Override
    public long tryNext(int n) {
        return nextImpl(n);
    }

    @Override
    public long tryNext(int n, long timeout, TimeUnit unit) {
        return nextImpl(n);
    }

    private long nextImpl(int n) {
        if (n < 1) {
            throw new IllegalArgumentException("n: " + n);
        }
        long current;
        long next;
        do {
            current = cursor.getVolatile();
            next = current + n;
        }
        while (!cursor.compareAndSet(current, next));
        // 注意：此时消费者已能看见最新的序号，但新的块可能尚未分配
        // 生产者尝试进入新块时回收旧块，可能不及时，但足够安全和开销小
        if (!buffer.inSameChunk(current, next)) {
            buffer.tryMoveHeadToNext(Util.getMinimumSequence(gatingBarriers, current));
            buffer.producerChunkForSequence(next);
        }
        return next;
    }
    // endregion

    // region sequencer

    @Override
    public ProducerBarrier getProducerBarrier() {
        return this;
    }

    @Nonnull
    @Override
    public WaitStrategy getWaitStrategy() {
        return waitStrategy;
    }

    @Nullable
    @Override
    public SequenceBlocker getBlocker() {
        return blocker;
    }

    @Override
    public void signalAllWhenBlocking() {
        if (blocker != null) {
            blocker.signalAll();
        }
    }

    // endregion

    // region barrier

    @Override
    public Sequence groupSequence() {
        return cursor;
    }

    @Override
    public long sequence() {
        return cursor.getVolatile();
    }

    @Override
    public long dependentSequence() {
        return Util.getMinimumSequence(gatingBarriers, Long.MAX_VALUE);
    }

    @Override
    public long minimumSequence() {
        return Util.getMinimumSequence(gatingBarriers, cursor.getVolatile());
    }

    @Override
    public void addDependentBarriers(SequenceBarrier... barriersToTrack) {
        Util.addBarriers(VH_GATING_BARRIERS, this, barriersToTrack);
    }

    @Override
    public boolean removeDependentBarrier(SequenceBarrier barrier) {
        return Util.removeBarrier(VH_GATING_BARRIERS, this, barrier);
    }

    // endregion

    private static final VarHandle VH_GATING_BARRIERS;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_GATING_BARRIERS = l.findVarHandle(MpUnboundedBufferSequencer.class, "gatingBarriers", SequenceBarrier[].class);
        } catch (Exception e) {
            throw new ExceptionInInitializerError(e);
        }
    }
}