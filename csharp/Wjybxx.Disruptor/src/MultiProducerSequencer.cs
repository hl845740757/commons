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
using System.Runtime.CompilerServices;
using System.Threading;

namespace Wjybxx.Disruptor
{
/// <summary>
/// 多生产者模型下的序号生成器
///
/// 注意: 使用该序号生成器时，在调用{@link WaitStrategy#waitFor(long, ProducerBarrier, ConsumerBarrier)}
/// 后必须调用{@link ProducerBarrier#getHighestPublishedSequence(long, long)}
/// 确定真正可用的序号。因为多生产者模型下，生产者之间是无锁的，预分配序号，那么真正填充的数据可能是非连续的。
/// </summary>
public sealed class MultiProducerSequencer : RingBufferSequencer
{
    /// <summary>
    /// 网关序列的最小序号缓存。
    /// 小心：多线程更新的情况下，有可能小于真实的gatingSequence -- 结果是良性的。
    /// 
    /// 由于消费者的<see cref="Sequence"/>变更较为频繁，因此消费者<see cref="Sequence"/>的缓存行极易失效。
    /// 如果生产者频繁读取消费者的<see cref="Sequence"/>，极易遇见缓存失效问题（伪共享），从而影响性能。
    /// 通过缓存一个值（在必要的时候更新），可以极大的减少对消费者序号的读操作，从而提高性能。
    /// PS: 使用一个变化频率较低的值代替一个变化频率较高的值，提高读效率。
    /// </summary>
    private readonly Sequence gatingSequenceCache = new Sequence(SequenceBarrier.INITIAL_SEQUENCE);

    /// <summary>
    /// 已发布的序号。
    /// 注意：与disruptor的解决方案不同，我存储的是槽位当前的序号 -- 这可以使用更久，也可避免额外的计算。
    /// </summary>
    private readonly long[] published;
    /** 用于快速的计算序号对应的下标 */
    private readonly int indexMask;

    public MultiProducerSequencer(int bufferSize, int spinIterations, WaitStrategy waitStrategy, SequenceBlocker? blocker)
        : base(bufferSize, spinIterations, waitStrategy, blocker) {
        this.indexMask = bufferSize - 1;
        this.published = new long[bufferSize];
        InitPublished(-1);
    }

    public override void Claim(long sequence) {
        base.Claim(sequence);
        InitPublished(sequence);
    }

    private void InitPublished(long value) {
        Array.Fill(published, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfSequence(long sequence, int indexMask) {
        return (int)(indexMask & sequence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetPublished(long sequence) {
        int index = IndexOfSequence(sequence, indexMask);
        Volatile.Write(ref published[index], sequence); // c#暂使用Volatile写
    }

    private void SetPublished(long lo, long hi) {
        long[] published = this.published;
        int indexMask = this.indexMask;

        int index = IndexOfSequence(lo, indexMask);
        Volatile.Write(ref published[index], lo); // store fence 确保数据填充的可见性
        if (lo < hi) {
            for (long seq = lo + 1; seq < hi; seq++) {
                index = IndexOfSequence(seq, indexMask);
                published[index] = seq; // store plain
            }
            index = IndexOfSequence(hi, indexMask);
            Volatile.Write(ref published[index], hi); // flush
        }
    }

    public override void Publish(long sequence) {
        SetPublished(sequence);
        SignalAllWhenBlocking();
    }

    public override void Publish(long lo, long hi) {
        SetPublished(lo, hi);
        SignalAllWhenBlocking();
    }

    public override bool IsPublished(long sequence) {
        int index = IndexOfSequence(sequence, indexMask);
        long flag = Volatile.Read(ref published[index]);
        return flag == sequence;
    }

    public override long GetHighestPublishedSequence(long lowerBound, long availableSequence) {
        long[] published = this.published;
        int indexMask = this.indexMask;
        // 这个方法的执行频率极高，值得我们重复编码减少调用
        for (long sequence = lowerBound; sequence <= availableSequence; sequence++) {
            int index = IndexOfSequence(sequence, indexMask);
            long flag = Volatile.Read(ref published[index]);
            if (flag != sequence) {
                return sequence - 1;
            }
        }
        return availableSequence;
    }

    #region sequencer

    public override long RemainingCapacity() {
        // 查询尽量返回实时的数据 - 不使用缓存
        long consumed = Util.GetMinimumSequence(gatingBarriers, cursor.GetVolatile());
        long produced = cursor.GetVolatile();
        return bufferSize - (produced - consumed);
    }

    public override bool HasAvailableCapacity(int requiredCapacity) {
        if (requiredCapacity < 0) throw new ArgumentException("requiredCapacity: " + requiredCapacity);
        if (requiredCapacity > bufferSize) return false;
        return hasAvailableCapacity(gatingBarriers, requiredCapacity, cursor.GetVolatile());
    }

    private bool hasAvailableCapacity(SequenceBarrier[] gatingBarriers, int requiredCapacity, long cursorValue) {
        // 可能构成环路的点/环形缓冲区可能追尾的点 = 请求的序号 - 环形缓冲区大小
        long wrapPoint = (cursorValue + requiredCapacity) - bufferSize;

        // 缓存的消费者们的最慢进度值，小于等于真实进度
        // 注意：对单个线程来说可能看见一个比该线程上次看见的更小的值 => 对另一个线程来说就可能看见一个比生产进度更大的值。
        long cachedGatingSequence = gatingSequenceCache.GetVolatile();

        // 1.wrapPoint > cachedGatingSequence 表示生产者追上消费者产生环路，上次看见的序号缓存无效，即缓冲区已满，此时需要获取消费者们最新的进度，以确定是否队列满。
        // 2.cachedGatingSequence > cursorValue  表示消费者的进度大于当前生产者进度，表示cursorValue无效，有以下可能：
        // 2.1 其它生产者发布了数据，并更新了gatingSequenceCache，并已被消费（当前线程进入该方法时可能被挂起，重新恢复调度时看见一个更大值）。
        // 2.2 claim的调用（建议忽略）
        if (wrapPoint > cachedGatingSequence || cachedGatingSequence > cursorValue) {
            // 获取最新的消费者进度并缓存起来
            // 这里存在竞态条件，多线程模式下，可能会被设置为多个线程看见的结果中的任意一个，可能比cachedGatingSequence更小，可能比cursorValue更大。
            // 但该竞争是良性的，产生的结果是可控的，不会导致错误（不会导致生产者覆盖未消费的数据）。
            long minSequence = Util.GetMinimumSequence(gatingBarriers, cursorValue);
            gatingSequenceCache.SetRelease(minSequence);

            return wrapPoint <= minSequence;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Next() {
        return NextImpl(1, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long Next(int n) {
        return NextImpl(n, false);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long NextInterruptibly() {
        return NextImpl(1, true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long NextInterruptibly(int n) {
        return NextImpl(n, true);
    }

    private long NextImpl(int n, bool interruptible) {
        if (n < 1 || n > bufferSize) {
            throw new ArgumentException("n: " + n);
        }
        long current;
        long next;
        bool interrupted = false;
        do {
            current = cursor.GetVolatile();
            next = current + n;

            // 可能构成环路的点/环形缓冲区可能追尾的点 = 请求的序号 - 环形缓冲区大小
            long wrapPoint = next - bufferSize;
            // 缓存的消费者们的最慢进度值，小于等于真实进度
            // 注意：对单个线程来说可能看见一个比该线程上次看见的更小的值 => 对另一个线程来说就可能看见一个比生产进度更大的值。
            long cachedGatingSequence = gatingSequenceCache.GetVolatile();

            // 第一步：空间不足时查看消费者的最新进度，如果最新进度仍不不满足就等待。
            // 1.wrapPoint > cachedGatingSequence 表示生产者追上消费者产生环路，上次看见的序号缓存无效，即缓冲区已满，此时需要获取消费者们最新的进度，以确定是否队列满。
            // 2.cachedGatingSequence > current 表示消费者的进度大于当前生产者进度，表示current无效，有以下可能：
            // 2.1 其它生产者发布了数据，并更新了gatingSequenceCache，并已被消费（当前线程进入该方法时可能被挂起，重新恢复调度时看见一个更大值）。
            // 2.2 claim的调用（建议忽略）
            if (wrapPoint > cachedGatingSequence || cachedGatingSequence > current) {
                // 获取最新的消费者进度并缓存起来 -- 如果缓存是有意义的
                long gatingSequence = Util.GetMinimumSequence(gatingBarriers, current);
                if (wrapPoint > gatingSequence) {
                    if (spinIterations > 0) { // 大于0时自旋 -- 不同于Java实现
                        Thread.SpinWait(spinIterations);
                        continue;
                    }
                    try {
                        Thread.Sleep(1);
                    }
                    catch (ThreadInterruptedException) {
                        if (interruptible) throw;
                        interrupted = true;
                    }
                    continue;
                }
                // 这里存在竞态条件，可能会被设置为多个线程看见的结果中的任意一个，可能会被设置为一个更小的值，从而小于当前的查询值
                gatingSequenceCache.SetRelease(gatingSequence);
                continue;
            }
            // 第二步：看见空间足够时尝试CAS竞争空间
            if (cursor.CompareAndSet(current, next)) {
                break;
            }
        } while (true);
        if (interrupted) {
            Thread.CurrentThread.Interrupt();
        }
        return next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override long? TryNext() {
        return TryNext(1);
    }

    public override long? TryNext(int n) {
        if (n < 1 || n > bufferSize) {
            throw new AggregateException("n: " + n);
        }
        long current;
        long next;
        do {
            current = cursor.GetVolatile();
            next = current + n;
            if (!hasAvailableCapacity(gatingBarriers, n, current)) {
                return null;
            }
        } while (!cursor.CompareAndSet(current, next));
        return next;
    }

    #endregion
}
}