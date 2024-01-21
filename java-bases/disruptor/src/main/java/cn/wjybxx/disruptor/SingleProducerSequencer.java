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


import javax.annotation.Nullable;
import java.util.concurrent.locks.LockSupport;

abstract class SingleProducerSequencerPad extends RingBufferSequencer {

    /**
     * 缓冲行填充，保护 {@link SingleProducerSequencerFields#produced}、{@link SingleProducerSequencerFields#cachedGating}
     */
    private long p1, p2, p3, p4, p5, p6, p7;
    private long p8, p9, p10, p11, p12, p13, p14, p15;

    public SingleProducerSequencerPad(int bufferSize, long sleepNanos,
                                      WaitStrategy waitStrategy, @Nullable SequenceBlocker blocker) {
        super(bufferSize, sleepNanos, waitStrategy, blocker);
    }
}

abstract class SingleProducerSequencerFields extends SingleProducerSequencerPad {

    public SingleProducerSequencerFields(int bufferSize, long sleepNanos,
                                         WaitStrategy waitStrategy, @Nullable SequenceBlocker blocker) {
        super(bufferSize, sleepNanos, waitStrategy, blocker);
    }

    /**
     * 预分配的序号缓存，因为是单线程的生产者，不存在竞争，因此采用普通的long变量
     * 表示 {@link #cursor} +1 ~  nextValue 这段空间被预分配出去了，但是可能还未填充数据。
     */
    long produced = SequenceBarrier.INITIAL_SEQUENCE;
    /**
     * 网关序列的最小序号缓存。
     * 因为是单线程的生产者，数据无竞争，因此使用普通的long变量即可。
     * <p>
     * Q: 该缓存值的作用？
     * A: 除了直观上的减少对{@link #gatingBarriers}的遍历产生的volatile读以外，还可以提高缓存命中率。
     * <p>
     * 由于消费者的{@link Sequence}变更较为频繁，因此消费者的{@link Sequence}的缓存极易失效。
     * 如果生产者频繁读取消费者的{@link Sequence}，极易遇见缓存失效问题（伪共享），从而影响性能。
     * 通过缓存一个值（在必要的时候更新），可以极大的减少对消费者的{@link Sequence}的读操作，从而提高性能。
     * PS: 使用一个变化频率较低的值代替一个变化频率较高的值，提高读效率。
     */
    long cachedGating = SequenceBarrier.INITIAL_SEQUENCE;
}

/**
 * 单生产者序号分配器
 * （由用户保证不会并发的申请序号）
 *
 * @author wjybxx
 * date - 2024/1/17
 */
public class SingleProducerSequencer extends SingleProducerSequencerFields {

    /**
     * 缓冲行填充，保护 {@link SingleProducerSequencerFields#produced}、{@link SingleProducerSequencerFields#cachedGating}
     */
    private long p1, p2, p3, p4, p5, p6, p7;
    private long p8, p9, p10, p11, p12, p13, p14, p15;

    /**
     * @param bufferSize   RingBuffer大小
     * @param sleepNanos   单步等待时间 - 0则使用自旋
     * @param waitStrategy 默认等待策略
     * @param blocker      用于唤醒消费者的锁
     */
    public SingleProducerSequencer(int bufferSize, long sleepNanos, WaitStrategy waitStrategy, @Nullable SequenceBlocker blocker) {
        super(bufferSize, sleepNanos, waitStrategy, blocker);
    }

    @SuppressWarnings("deprecation")
    @Override
    public void claim(long sequence) {
        super.claim(sequence);
        produced = sequence;
        cachedGating = sequence;
    }

    @Override
    public long remainingCapacity() {
        // 查询尽量返回实时的数据
        long produced = this.produced;
        long consumed = Util.getMinimumSequence(gatingBarriers, produced);
        return bufferSize - (produced - consumed);
    }

    @Override
    public boolean hasAvailableCapacity(int requiredCapacity) {
        if (requiredCapacity < 0) {
            throw new IllegalArgumentException();
        }
        return hasAvailableCapacity(requiredCapacity, false);
    }

    private boolean hasAvailableCapacity(int requiredCapacity, boolean doStore) {
        final long produced = this.produced;
        final long cachedGatingSequence = this.cachedGating;

        // 可能构成环路的点：环形缓冲区可能追尾的点 = 等于本次申请的序号 - 环形缓冲区大小
        long wrapPoint = (produced + requiredCapacity) - bufferSize;

        // wrapPoint > cachedGatingSequence 表示生产者追上消费者产生环路(追尾)，还需要更多的空间，上次看见的序号缓存无效，
        if (wrapPoint > cachedGatingSequence) {
            // 因为publish使用的是set()/putOrderedLong，并不保证消费者能及时看见发布的数据，
            // 当我再次申请更多的空间时，必须保证消费者能消费发布的数据（那么就需要进度对消费者立即可见，使用volatile写即可）
            if (doStore) {
                cursor.setVolatile(produced);  // StoreLoad fence
            }

            // 获取最新的消费者进度并缓存起来
            long minSequence = Util.getMinimumSequence(gatingBarriers, produced);
            this.cachedGating = minSequence;

            // minSequence是已消费的序号，因此使用 == 判断
            return wrapPoint <= minSequence;
        }
        return true;
    }

    @Override
    public long next() {
        try {
            return nextImpl(1, false);
        } catch (InterruptedException e) {
            throw new AssertionError(e);
        }
    }

    @Override
    public long next(int n) {
        try {
            return nextImpl(n, false);
        } catch (InterruptedException e) {
            throw new AssertionError(e);
        }
    }

    @Override
    public long nextInterruptibly() throws InterruptedException {
        return nextImpl(1, true);
    }

    @Override
    public long nextInterruptibly(int n) throws InterruptedException {
        return nextImpl(n, true);
    }

    private long nextImpl(int n, final boolean interruptible) throws InterruptedException {
        assert produced == cursor.getVolatile() : "Unpublished";
        if (n < 1 || n > bufferSize) {
            throw new IllegalArgumentException("n: " + n);
        }

        long produced = this.produced;
        long cachedGatingSequence = this.cachedGating;

        long nextSequence = produced + n;
        long wrapPoint = nextSequence - bufferSize;

        // wrapPoint > cachedGatingSequence 表示生产者追上消费者产生环路(追尾)，还需要更多的空间，上次看见的序号缓存无效，
        if (wrapPoint > cachedGatingSequence) {
            // 因为publish使用的是set()/putOrderedLong，并不保证消费者能及时看见发布的数据，
            // 当我再次申请更多的空间时，必须保证消费者能消费发布的数据（那么就需要进度对消费者立即可见，使用volatile写即可）
            cursor.setVolatile(produced);  // StoreLoad fence

            long minSequence;
            boolean interrupted = false;
            while (wrapPoint > (minSequence = Util.getMinimumSequence(gatingBarriers, produced))) {
                if (sleepNanos <= 0) { // 为0时自旋
                    Thread.onSpinWait();
                    continue;
                }
                if (interruptible) {
                    if (Thread.interrupted()) throw new InterruptedException();
                } else {
                    interrupted |= Thread.interrupted();
                }
                LockSupport.parkNanos(sleepNanos);
            }
            if (interrupted) {
                Thread.currentThread().interrupt();
            }
            this.cachedGating = minSequence;
        }

        // publish后对消费者可见
        this.produced = nextSequence;
        return nextSequence;
    }

    @Override
    public long tryNext() {
        return tryNext(1);
    }

    @Override
    public long tryNext(int n) {
        assert produced == cursor.getVolatile() : "Unpublished";
        if (n < 1 || n > bufferSize) {
            throw new IllegalArgumentException("n: " + n);
        }
        if (!hasAvailableCapacity(n, true)) {
            return -1;
        }
        long nextSequence = this.produced + n;
        this.produced = nextSequence;
        return nextSequence;
    }

    @Override
    public void publish(long sequence) {
        // 非volatile写，并没有保证对其他线程立即可见(最终会看见)
        cursor.setRelease(sequence);
        signalAllWhenBlocking();
    }

    @Override
    public void publish(long lo, long hi) {
        publish(hi);
    }

    @Override
    public boolean isPublished(long sequence) {
        return sequence <= cursor.getVolatile();
    }

    @Override
    public long getHighestPublishedSequence(long nextSequence, long availableSequence) {
        return availableSequence; // 消费者看见的数据是连续的
    }
}
