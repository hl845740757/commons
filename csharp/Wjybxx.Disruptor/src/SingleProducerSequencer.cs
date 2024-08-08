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
using System.Diagnostics;
using System.Threading;

#pragma warning disable CS0169

namespace Wjybxx.Disruptor
{
/// <summary>
/// 单生产者序号分配器
/// （由用户保证不会并发的申请序号）
/// </summary>
public sealed class SingleProducerSequencer : RingBufferSequencer
{
    // region padding
    private long p1, p2, p3, p4, p5, p6, p7;
    // endregion

    /// <summary>
    /// 预分配的序号缓存，因为是单线程的生产者，不存在竞争，因此采用普通的long变量；
    /// 表示 <see cref="RingBufferSequencer.cursor"/> +1 ~  nextValue 这段空间被预分配出去了，但是可能还未填充数据。
    /// </summary>
    private long produced = SequenceBarrier.INITIAL_SEQUENCE;

    /// <summary>
    /// 网关序列的最小序号缓存。
    /// 因为是单线程的生产者，数据无竞争，因此使用普通的long变量即可。
    ///
    /// Q: 该缓存值的作用？
    /// A: 除了直观上的减少对<see cref="RingBufferSequencer.gatingBarriers"/>的遍历产生的volatile读以外，还可以提高缓存命中率。
    ///
    /// 由于消费者的<see cref="Sequence"/>变更较为频繁，因此消费者<see cref="Sequence"/>的缓存行极易失效。
    /// 如果生产者频繁读取消费者的<see cref="Sequence"/>，极易遇见缓存失效问题（伪共享），从而影响性能。
    /// 通过缓存一个值（在必要的时候更新），可以极大的减少对消费者序号的读操作，从而提高性能。
    /// PS: 使用一个变化频率较低的值代替一个变化频率较高的值，提高读效率。
    /// </summary>
    private long cachedGating = SequenceBarrier.INITIAL_SEQUENCE;

    // region padding
    private long p11, p12, p13, p14, p15, p16, p17;
    // endregion

    public SingleProducerSequencer(int bufferSize, int spinIterations, WaitStrategy waitStrategy, SequenceBlocker? blocker)
        : base(bufferSize, spinIterations, waitStrategy, blocker) {
    }

    public override void Claim(long sequence) {
        base.Claim(sequence);
        produced = sequence;
        cachedGating = sequence;
    }

    public override long RemainingCapacity() {
        // 查询尽量返回实时的数据
        long produced = this.produced;
        long consumed = Util.GetMinimumSequence(gatingBarriers, produced);
        return bufferSize - (produced - consumed);
    }

    public override bool HasAvailableCapacity(int requiredCapacity) {
        if (requiredCapacity < 0) {
            throw new ArgumentException("requiredCapacity: " + requiredCapacity);
        }
        return HasAvailableCapacity(requiredCapacity, false);
    }

    private bool HasAvailableCapacity(int requiredCapacity, bool doStore) {
        long produced = this.produced;
        long cachedGatingSequence = this.cachedGating;

        // 可能构成环路的点：环形缓冲区可能追尾的点 = 等于本次申请的序号 - 环形缓冲区大小
        long wrapPoint = (produced + requiredCapacity) - bufferSize;

        // wrapPoint > cachedGatingSequence 表示生产者追上消费者产生环路(追尾)，还需要更多的空间，上次看见的序号缓存无效，
        if (wrapPoint > cachedGatingSequence) {
            // 因为publish使用的是set()/putOrderedLong，并不保证消费者能及时看见发布的数据，
            // 当我再次申请更多的空间时，必须保证消费者能消费发布的数据（那么就需要进度对消费者立即可见，使用volatile写即可）
            if (doStore) {
                cursor.SetVolatile(produced); // StoreLoad fence
            }

            // 获取最新的消费者进度并缓存起来
            long minSequence = Util.GetMinimumSequence(gatingBarriers, produced);
            this.cachedGating = minSequence;

            // minSequence是已消费的序号，因此使用 == 判断
            return wrapPoint <= minSequence;
        }
        return true;
    }

    public override long Next() {
        return NextImpl(1, false);
    }

    public override long Next(int n) {
        return NextImpl(n, false);
    }

    public override long NextInterruptibly() {
        return NextImpl(1, true);
    }

    public override long NextInterruptibly(int n) {
        return NextImpl(n, true);
    }

    private long NextImpl(int n, bool interruptible) {
        Debug.Assert(this.produced == cursor.GetVolatile());
        if (n < 1 || n > bufferSize) {
            throw new ArgumentException("n: " + n);
        }

        long produced = this.produced;
        long cachedGatingSequence = this.cachedGating;

        long nextSequence = produced + n;
        long wrapPoint = nextSequence - bufferSize;

        // wrapPoint > cachedGatingSequence 表示生产者追上消费者产生环路(追尾)，还需要更多的空间，上次看见的序号缓存无效，
        if (wrapPoint > cachedGatingSequence) {
            // 因为publish使用的是set()/putOrderedLong，并不保证消费者能及时看见发布的数据，
            // 当我再次申请更多的空间时，必须保证消费者能消费发布的数据（那么就需要进度对消费者立即可见，使用volatile写即可）
            cursor.SetVolatile(produced); // StoreLoad fence

            long minSequence;
            bool interrupted = false;
            while (wrapPoint > (minSequence = Util.GetMinimumSequence(gatingBarriers, produced))) {
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
            }
            if (interrupted) {
                Thread.CurrentThread.Interrupt();
            }
            this.cachedGating = minSequence;
        }

        // publish后对消费者可见
        this.produced = nextSequence;
        return nextSequence;
    }

    public override long? TryNext() {
        return TryNext(1);
    }

    public override long? TryNext(int n) {
        if (n < 1 || n > bufferSize) {
            throw new ArgumentException("n: " + n);
        }
        if (!HasAvailableCapacity(n, true)) {
            return null;
        }
        long nextSequence = this.produced + n;
        this.produced = nextSequence;
        return nextSequence;
    }

    public override void Publish(long sequence) {
        // 非volatile写，并没有保证对其他线程立即可见(最终会看见)
        cursor.SetRelease(sequence);
        SignalAllWhenBlocking();
    }

    public override void Publish(long lo, long hi) {
        Publish(hi);
    }

    public override bool IsPublished(long sequence) {
        return sequence <= cursor.GetVolatile();
    }

    public override long GetHighestPublishedSequence(long nextSequence, long availableSequence) {
        return availableSequence; // 消费者看见的数据是连续的
    }
}
}