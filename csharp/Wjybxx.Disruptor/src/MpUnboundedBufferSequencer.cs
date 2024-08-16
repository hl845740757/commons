#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;

#pragma warning disable CS0420

namespace Wjybxx.Disruptor
{
public sealed class MpUnboundedBufferSequencer<T> : ProducerBarrier, Sequencer
{
    /**
     * 生产者组的序号。
     * 1. 生产者先根据sequence计算当前应该填充的chunk的索引（编号），也根据sequence计算落在该chunk的哪个槽位。
     * 2. 这仍然是一个预更新值，因为是多生产者模型。
     */
    private readonly Sequence cursor = new Sequence(SequenceBarrier.INITIAL_SEQUENCE);

    /// <summary>
    /// 网关屏障,序号生成器必须和这些屏障满足以下约束:
    /// <code>cursor-bufferSize &lt;= Min(gatingSequence)</code>
    /// 即：所有的gatingBarrier让出下一个插槽后，生产者才能获取该插槽。
    /// 
    /// 对于生产者来讲，它只需要关注消费链最末端的消费者的进度（因为它们的进度是最慢的）
    /// 即：gatingBarrier就是所有消费链末端的消费们所拥有的的Sequence。
    /// </summary>
    private volatile SequenceBarrier[] gatingBarriers = Array.Empty<SequenceBarrier>();

    /** 关联的数据结构 -- 信息在buffer上 */
    private readonly MpUnboundedBuffer<T> buffer;
    /** 默认等待策略 */
    private readonly WaitStrategy waitStrategy;
    /** 序号阻塞器 -- 用于唤醒等待生产者发布数据的消费者 */
    private readonly SequenceBlocker? blocker;

    public MpUnboundedBufferSequencer(MpUnboundedBuffer<T> buffer, WaitStrategy waitStrategy, SequenceBlocker? blocker) {
        this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        this.waitStrategy = waitStrategy ?? throw new ArgumentNullException(nameof(waitStrategy));
        this.blocker = blocker;
    }

    public void Claim(long sequence) {
        if (!cursor.CompareAndSet(SequenceBarrier.INITIAL_SEQUENCE, sequence)) {
            throw new Exception("state error");
        }
        buffer.Claim(sequence);
    }

    #region producer

    public void Publish(long sequence) {
        MpUnboundedBufferChunk<T> chunk = buffer.ProducerChunkForSequence(sequence);
        chunk.Publish((int)(sequence - chunk.MinSequence()));
    }

    public void Publish(long lo, long hi) {
        MpUnboundedBufferChunk<T> chunk = buffer.ProducerChunkForSequence(lo);
        while (hi > chunk.MaxSequence()) {
            long minSequence = chunk.MinSequence();
            chunk.Publish((int)(lo - minSequence), chunk.Length - 1);

            lo = minSequence + chunk.Length;
            chunk = buffer.ProducerChunkForSequence(lo); // 下一个块可能尚未构造
        }
        {
            long minSequence = chunk.MinSequence();
            chunk.Publish((int)(lo - minSequence), (int)(hi - minSequence));
        }
    }

    public bool IsPublished(long sequence) {
        MpUnboundedBufferChunk<T> chunk = buffer.ConsumerChunkForSequence(sequence);
        return chunk.IsPublished((int)(sequence - chunk.MinSequence()));
    }

    /** 消费者可能看见尚未发布的块的序号 */
    public long GetHighestPublishedSequence(long lo, long hi) {
        MpUnboundedBufferChunk<T> chunk = buffer.ConsumerChunkForSequence(lo);
        while (hi > chunk.MaxSequence()) {
            long minSequence = chunk.MinSequence();
            int maxIndex = chunk.Length - 1;
            int highestIndex = chunk.GetHighestPublishedSequence((int)(lo - minSequence), maxIndex);
            if (highestIndex != maxIndex) {
                return minSequence + highestIndex;
            }
            chunk = chunk.LvNext();
            if (chunk == null) { // 下一个块可能尚未被填充（正在构造）
                return minSequence + highestIndex;
            }
            lo = chunk.MinSequence();
        }
        {
            long minSequence = chunk.MinSequence();
            int maxIndex = (int)(hi - minSequence);
            return minSequence + chunk.GetHighestPublishedSequence((int)(lo - minSequence), maxIndex);
        }
    }

    public bool HasAvailableCapacity(int requiredCapacity) {
        if (requiredCapacity < 0) {
            throw new ArgumentException("requiredCapacity: " + requiredCapacity);
        }
        return true;
    }

    public long Next() {
        return nextImpl(1);
    }

    public long Next(int n) {
        return nextImpl(n);
    }

    public long NextInterruptibly() {
        return nextImpl(1);
    }

    public long NextInterruptibly(int n) {
        return nextImpl(n);
    }

    public long? TryNext() {
        return nextImpl(1);
    }

    public long? TryNext(int n) {
        return nextImpl(n);
    }

    public long? TryNext(int n, TimeSpan timeout) {
        return nextImpl(n);
    }

    private long nextImpl(int n) {
        if (n < 1) {
            throw new ArgumentException("n: " + n);
        }
        long current;
        long next;
        do {
            current = cursor.GetVolatile();
            next = current + n;
        } while (!cursor.CompareAndSet(current, next));
        // 注意：此时消费者已能看见最新的序号，但新的块可能尚未分配
        // 生产者尝试进入新块时回收旧块，可能不及时，但足够安全和开销小
        if (!buffer.InSameChunk(current, next)) {
            buffer.TryMoveHeadToNext(Util.GetMinimumSequence(gatingBarriers, current));
            buffer.ProducerChunkForSequence(next);
        }
        return next;
    }

    #endregion

    #region sequencer

    public ProducerBarrier ProducerBarrier => this;
    public WaitStrategy WaitStrategy => waitStrategy;
    public SequenceBlocker? Blocker => blocker;

    public void SignalAllWhenBlocking() {
        if (blocker != null) {
            blocker.SignalAll();
        }
    }

    # endregion

    #region barrier

    public Sequence GroupSequence() {
        return cursor;
    }

    public long Sequence() {
        return cursor.GetVolatile();
    }

    public long DependentSequence() {
        return Util.GetMinimumSequence(gatingBarriers, long.MaxValue);
    }

    public long MinimumSequence() {
        return Util.GetMinimumSequence(gatingBarriers, cursor.GetVolatile());
    }

    public void AddDependentBarriers(params SequenceBarrier[] barriersToTrack) {
        Util.AddBarriers(ref gatingBarriers, this, barriersToTrack);
    }

    public bool RemoveDependentBarrier(SequenceBarrier barrier) {
        return Util.RemoveBarrier(ref gatingBarriers, this, barrier);
    }

    #endregion
}
}