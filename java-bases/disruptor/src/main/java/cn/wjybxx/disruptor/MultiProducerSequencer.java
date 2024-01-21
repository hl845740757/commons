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
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Arrays;
import java.util.concurrent.locks.LockSupport;

/**
 * 多生产者模型下的序号生成器
 * <p>
 * 注意: 使用该序号生成器时，在调用{@link WaitStrategy#waitFor(long, Sequencer, ConsumerBarrier)}
 * 后必须调用{@link ProducerBarrier#getHighestPublishedSequence(long, long)}
 * 确定真正可用的序号。因为多生产者模型下，生产者之间是无锁的，预分配序号，那么真正填充的数据可能是非连续的。
 * <p>
 * 建议阅读我多年前注释的disruptor源码 <a href="https://github.com/hl845740757/disruptor-translation">Disruptor源码注释</a>
 *
 * @author wjybxx
 * date - 2024/1/17
 */
public class MultiProducerSequencer extends RingBufferSequencer {

    /** {@link #published}元素的读写句柄 */
    private static final VarHandle VH_PUBLISHED_ELEMENTS = MethodHandles.arrayElementVarHandle(long[].class);

    /**
     * 网关序列的最小序号缓存。
     * 小心：多线程更新的情况下，有可能小于真实的gatingSequence -- 结果是良性的。
     * <p>
     * 由于消费者的{@link Sequence}变更较为频繁，因此消费者的{@link Sequence}的缓存极易失效。
     * 如果生产者频繁读取消费者的{@link Sequence}，极易遇见缓存失效问题（伪共享），从而影响性能。
     * 通过缓存一个值（在必要的时候更新），可以极大的减少对消费者的{@link Sequence}的读操作，从而提高性能。
     * PS: 使用一个变化频率较低的值代替一个变化频率较高的值，提高读效率。
     */
    protected final Sequence gatingSequenceCache = new Sequence(SequenceBarrier.INITIAL_SEQUENCE);
    /**
     * 已发布的序号。
     * 注意：与disruptor的解决方案不同，我存储的是槽位当前的序号 -- 这可以使用更久，也可避免额外的计算。
     */
    private final long[] published;
    /** 用于快速的计算序号对应的下标 */
    private final int indexMask;

    public MultiProducerSequencer(int bufferSize, long sleepNanos, WaitStrategy waitStrategy, @Nullable SequenceBlocker blocker) {
        super(bufferSize, sleepNanos, waitStrategy, blocker);

        this.indexMask = bufferSize - 1;
        this.published = new long[bufferSize];
        initPublished(-1);
    }

    @SuppressWarnings("deprecation")
    @Override
    public void claim(long sequence) {
        super.claim(sequence);
        initPublished(sequence);
    }

    private void initPublished(long value) {
        Arrays.fill(published, value);
    }

    private static int indexOfSequence(long sequence, int indexMask) {
        return (int) (indexMask & sequence);
    }

    protected final void setPublished(long sequence) {
        int index = indexOfSequence(sequence, indexMask);
        VH_PUBLISHED_ELEMENTS.setRelease(published, index, sequence);
    }

    protected final void setPublished(long lo, long hi) {
        final long[] published = this.published;
        final int indexMask = this.indexMask;
        final VarHandle varHandle = VH_PUBLISHED_ELEMENTS;
        for (long seq = lo; seq <= hi; seq++) {
            int index = indexOfSequence(seq, indexMask);
            varHandle.setRelease(published, index, seq);
        }
    }

    @Override
    public void publish(long sequence) {
        setPublished(sequence);
        signalAllWhenBlocking();
    }

    @Override
    public void publish(long lo, long hi) {
        setPublished(lo, hi);
        signalAllWhenBlocking();
    }

    @Override
    public boolean isPublished(long sequence) {
        int index = indexOfSequence(sequence, indexMask);
        long flag = (long) VH_PUBLISHED_ELEMENTS.getVolatile(published, index);
        return flag == sequence;
    }

    @Override
    public long getHighestPublishedSequence(long lowerBound, long availableSequence) {
        final long[] published = this.published;
        final int indexMask = this.indexMask;
        final VarHandle varHandle = VH_PUBLISHED_ELEMENTS;
        // 这个方法的执行频率极高，值得我们重复编码减少调用
        for (long sequence = lowerBound; sequence <= availableSequence; sequence++) {
            int index = indexOfSequence(sequence, indexMask);
            long flag = (long) varHandle.getVolatile(published, index);
            if (flag != sequence) {
                return sequence - 1;
            }
        }
        return availableSequence;
    }

    // region sequencer

    @Override
    public long remainingCapacity() {
        // 查询尽量返回实时的数据 - 不使用缓存
        long consumed = Util.getMinimumSequence(gatingBarriers, cursor.getVolatile());
        long produced = cursor.getVolatile();
        return getBufferSize() - (produced - consumed);
    }

    @Override
    public boolean hasAvailableCapacity(int requiredCapacity) {
        if (requiredCapacity < 0) {
            throw new IllegalArgumentException();
        }
        return hasAvailableCapacity(gatingBarriers, requiredCapacity, cursor.getVolatile());
    }

    private boolean hasAvailableCapacity(final SequenceBarrier[] gatingBarriers, final int requiredCapacity, long cursorValue) {
        // 可能构成环路的点/环形缓冲区可能追尾的点 = 请求的序号 - 环形缓冲区大小
        long wrapPoint = (cursorValue + requiredCapacity) - bufferSize;

        // 缓存的消费者们的最慢进度值，小于等于真实进度
        // 注意：对单个线程来说可能看见一个比该线程上次看见的更小的值 => 对另一个线程来说就可能看见一个比生产进度更大的值。
        long cachedGatingSequence = gatingSequenceCache.getVolatile();

        // 1.wrapPoint > cachedGatingSequence 表示生产者追上消费者产生环路，上次看见的序号缓存无效，即缓冲区已满，此时需要获取消费者们最新的进度，以确定是否队列满。
        // 2.cachedGatingSequence > cursorValue  表示消费者的进度大于当前生产者进度，表示cursorValue无效，有以下可能：
        // 2.1 其它生产者发布了数据，并更新了gatingSequenceCache，并已被消费（当前线程进入该方法时可能被挂起，重新恢复调度时看见一个更大值）。
        // 2.2 claim的调用（建议忽略）
        if (wrapPoint > cachedGatingSequence || cachedGatingSequence > cursorValue) {
            // 获取最新的消费者进度并缓存起来
            // 这里存在竞态条件，多线程模式下，可能会被设置为多个线程看见的结果中的任意一个，可能比cachedGatingSequence更小，可能比cursorValue更大。
            // 但该竞争是良性的，产生的结果是可控的，不会导致错误（不会导致生产者覆盖未消费的数据）。
            long minSequence = Util.getMinimumSequence(gatingBarriers, cursorValue);
            gatingSequenceCache.setRelease(minSequence);

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
        if (n < 1 || n > bufferSize) {
            throw new IllegalArgumentException("n: " + n);
        }
        long current;
        long next;
        boolean interrupted = false;
        do {
            current = cursor.getVolatile();
            next = current + n;

            // 可能构成环路的点/环形缓冲区可能追尾的点 = 请求的序号 - 环形缓冲区大小
            long wrapPoint = next - bufferSize;
            // 缓存的消费者们的最慢进度值，小于等于真实进度
            // 注意：对单个线程来说可能看见一个比该线程上次看见的更小的值 => 对另一个线程来说就可能看见一个比生产进度更大的值。
            long cachedGatingSequence = gatingSequenceCache.getVolatile();

            // 第一步：空间不足时查看消费者的最新进度，如果最新进度仍不不满足就等待。
            // 1.wrapPoint > cachedGatingSequence 表示生产者追上消费者产生环路，上次看见的序号缓存无效，即缓冲区已满，此时需要获取消费者们最新的进度，以确定是否队列满。
            // 2.cachedGatingSequence > current 表示消费者的进度大于当前生产者进度，表示current无效，有以下可能：
            // 2.1 其它生产者发布了数据，并更新了gatingSequenceCache，并已被消费（当前线程进入该方法时可能被挂起，重新恢复调度时看见一个更大值）。
            // 2.2 claim的调用（建议忽略）
            if (wrapPoint > cachedGatingSequence || cachedGatingSequence > current) {
                // 获取最新的消费者进度并缓存起来 -- 如果缓存是有意义的
                long gatingSequence = Util.getMinimumSequence(gatingBarriers, current);
                if (wrapPoint > gatingSequence) {
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
                    continue;
                }
                // 这里存在竞态条件，可能会被设置为多个线程看见的结果中的任意一个，可能会被设置为一个更小的值，从而小于当前的查询值
                gatingSequenceCache.setRelease(gatingSequence);
                continue;
            }
            // 第二步：看见空间足够时尝试CAS竞争空间
            if (cursor.compareAndSet(current, next)) {
                break;
            }
        }
        while (true);
        if (interrupted) {
            Thread.currentThread().interrupt();
        }
        return next;
    }

    @Override
    public long tryNext() {
        return tryNext(1);
    }

    @Override
    public long tryNext(int n) {
        if (n < 1 || n > bufferSize) {
            throw new IllegalArgumentException("n: " + n);
        }
        long current;
        long next;
        do {
            current = cursor.getVolatile();
            next = current + n;
            if (!hasAvailableCapacity(gatingBarriers, n, current)) {
                return -1;
            }
        }
        while (!cursor.compareAndSet(current, next));
        return next;
    }

    // endregion

}