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
public abstract class RingBufferSequencer : ProducerBarrier, Sequencer
{
    /** 生产者的序列，表示生产者的进度。 */
    protected readonly Sequence cursor = new Sequence(SequenceBarrier.INITIAL_SEQUENCE);

    /// <summary>
    /// 网关屏障,序号生成器必须和这些屏障满足以下约束:
    /// <code>cursor-bufferSize &lt;= Min(gatingSequence)</code>
    /// 即：所有的gatingBarrier让出下一个插槽后，生产者才能获取该插槽。
    /// 
    /// 对于生产者来讲，它只需要关注消费链最末端的消费者的进度（因为它们的进度是最慢的）
    /// 即：gatingBarrier就是所有消费链末端的消费们所拥有的的Sequence。
    /// </summary>
    protected volatile SequenceBarrier[] gatingBarriers = Array.Empty<SequenceBarrier>();

    /** ringBuffer有效数据缓冲区大小 */
    protected readonly int bufferSize;
    /** 自旋参数，大于0表示自旋，否则表示Sleep 1毫秒 */
    protected readonly int spinIterations;
    /** 默认等待策略 */
    private readonly WaitStrategy waitStrategy;
    /** 序号阻塞器 -- 用于唤醒等待生产者发布数据的消费者 */
    protected readonly SequenceBlocker? blocker;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bufferSize">RingBuffer大小</param>
    /// <param name="spinIterations">自旋参数，大于0表示自旋，否则表示Sleep 1毫秒</param>
    /// <param name="waitStrategy">默认等待策略</param>
    /// <param name="blocker">用于唤醒消费者的锁</param>
    /// <exception cref="ArgumentNullException"></exception>
    public RingBufferSequencer(int bufferSize, int spinIterations, WaitStrategy waitStrategy, SequenceBlocker? blocker) {
        this.bufferSize = bufferSize;
        this.spinIterations = spinIterations;
        this.waitStrategy = waitStrategy ?? throw new ArgumentNullException(nameof(waitStrategy));
        this.blocker = blocker;
    }

    /** 缓冲区大小 */
    public int BufferSize => bufferSize;

    /// <summary>
    /// 当前剩余容量。
    /// 并不一定具有价值，因为多线程模型下查询容器的当前大小，它反映的总是一个旧值。
    /// </summary>
    /// <returns></returns>
    public abstract long RemainingCapacity();

    public virtual void Claim(long sequence) {
        // 生产者只可以调用一次claim
        if (!cursor.CompareAndSet(SequenceBarrier.INITIAL_SEQUENCE, sequence)) {
            throw new Exception("state error");
        }
    }

    #region sequencer

    public ProducerBarrier ProducerBarrier => this;
    public WaitStrategy WaitStrategy => waitStrategy;
    public SequenceBlocker? Blocker => blocker;

    public void SignalAllWhenBlocking() {
        if (blocker != null) {
            blocker.SignalAll();
        }
    }

    public long? TryNext(int n, TimeSpan timeout) {
        return Util.TryNext(n, timeout, this, spinIterations);
    }

    #endregion

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

    #region abstract

    public abstract bool HasAvailableCapacity(int requiredCapacity);

    public abstract long Next();

    public abstract long Next(int n);

    public abstract long? TryNext();

    public abstract long? TryNext(int n);

    public abstract void Publish(long sequence);

    public abstract void Publish(long lo, long hi);

    public abstract bool IsPublished(long sequence);

    public abstract long GetHighestPublishedSequence(long nextSequence, long availableSequence);

    public abstract long NextInterruptibly();

    public abstract long NextInterruptibly(int n);

    #endregion
}
}