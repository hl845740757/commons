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

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Objects;
import java.util.concurrent.TimeUnit;

/**
 * 基于{@link RingBuffer}的序列生成器。
 *
 * @author wjybxx
 * date - 2024/1/16
 */
public abstract class RingBufferSequencer implements Sequencer, ProducerBarrier {

    /** 生产者的序列，表示生产者的进度。 */
    protected final Sequence cursor = new Sequence(INITIAL_SEQUENCE);
    /**
     * 网关屏障，序号生成器必须和这些屏障满足以下约束:
     * cursor-bufferSize <= Min(gatingSequence)
     * 即：所有的gatingBarrier让出下一个插槽后，生产者才能获取该插槽。
     * <p>
     * 对于生产者来讲，它只需要关注消费链最末端的消费者的进度（因为它们的进度是最慢的）。
     * 即：gatingBarrier就是所有消费链末端的消费们所拥有的的Sequence。
     */
    protected volatile SequenceBarrier[] gatingBarriers = new SequenceBarrier[0];

    /** ringBuffer有效数据缓冲区大小 */
    protected final int bufferSize;
    /** 等待序号时的睡眠时间 -- 如果为0则使用自旋等待 */
    protected final long sleepNanos;
    /** 默认等待策略 */
    private final WaitStrategy waitStrategy;
    /** 序号阻塞器 -- 用于唤醒等待生产者发布数据的消费者 */
    protected final SequenceBlocker blocker;

    public RingBufferSequencer(int bufferSize, long sleepNanos, WaitStrategy waitStrategy, @Nullable SequenceBlocker blocker) {
        this.bufferSize = bufferSize;
        this.sleepNanos = sleepNanos;
        this.waitStrategy = Objects.requireNonNull(waitStrategy);
        this.blocker = blocker;
    }

    /**
     * The capacity of the data structure to hold entries.
     *
     * @return the size of the RingBuffer.
     */
    public int getBufferSize() {
        return bufferSize;
    }

    /**
     * 当前剩余容量。
     * 并不一定具有价值，因为多线程模型下查询容器的当前大小，它反映的总是一个旧值。
     * <p>
     * Get the remaining capacity for this sequencer.
     *
     * @return The number of slots remaining.
     */
    public abstract long remainingCapacity();

    @Deprecated
    @Override
    public void claim(long sequence) {
        // 生产者只可以调用一次claim
        if (!cursor.compareAndSet(INITIAL_SEQUENCE, sequence)) {
            throw new IllegalStateException();
        }
    }

    // region sequencer

    @Override
    public final ProducerBarrier getProducerBarrier() {
        return this;
    }

    @Nullable
    @Override
    public final SequenceBlocker getBlocker() {
        return blocker;
    }

    @Nonnull
    @Override
    public final WaitStrategy getWaitStrategy() {
        return waitStrategy;
    }

    @Override
    public final void signalAllWhenBlocking() {
        if (blocker != null) {
            blocker.signalAll();
        }
    }

    @Override
    public long tryNext(int n, long timeout, TimeUnit unit) {
        return Util.tryNext(n, timeout, unit, this, sleepNanos);
    }

    // endregion

    // region barrier

    @Override
    public final Sequence groupSequence() {
        return cursor;
    }

    @Override
    public long sequence() {
        return cursor.getVolatile();
    }

    @Override
    public final long dependentSequence() {
        return Util.getMinimumSequence(gatingBarriers, Long.MAX_VALUE);
    }

    @Override
    public final long minimumSequence() {
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

    // region static

    private static final VarHandle VH_GATING_BARRIERS;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_GATING_BARRIERS = l.findVarHandle(RingBufferSequencer.class, "gatingBarriers", SequenceBarrier[].class);
        } catch (Exception e) {
            throw new ExceptionInInitializerError(e);
        }
    }

    // endregion
}