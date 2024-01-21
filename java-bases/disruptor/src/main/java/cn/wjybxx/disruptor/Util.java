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

import cn.wjybxx.base.Preconditions;

import java.lang.invoke.VarHandle;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.LockSupport;

import static java.util.Arrays.copyOf;

/**
 * @author wjybxx
 * date - 2024/1/16
 */
public final class Util {

    /** 判断一个值是否是2的整次幂 */
    public static boolean isPowerOfTwo(int x) {
        return x > 0 && (x & (x - 1)) == 0;
    }

    /** 计算num最接近下一个整2次幂；如果自身是2的整次幂，则会返回自身 */
    public static int nextPowerOfTwo(int num) {
        if (num < 1) return 1;
        return 1 << (32 - Integer.numberOfLeadingZeros(num - 1));
    }

    public static long getMinimumSequence(final Sequence[] sequences) {
        int n = sequences.length;
        if (n == 1) {  // 1的概率极高
            return sequences[0].getVolatile();
        }
        long minimum = Long.MAX_VALUE;
        for (int i = 0; i < n; i++) {
            long value = sequences[i].getVolatile();
            minimum = Math.min(minimum, value);
        }
        return minimum;
    }

    public static long getMinimumSequence(final Sequence[] sequences, long minimum) {
        int n = sequences.length;
        if (n == 1) { // 1的概率极高
            return Math.min(minimum, sequences[0].getVolatile());
        }
        for (int i = 0; i < n; i++) {
            long value = sequences[i].getVolatile();
            minimum = Math.min(minimum, value);
        }
        return minimum;
    }

    public static long getMinimumSequence(final SequenceBarrier[] barriers) {
        int n = barriers.length;
        if (n == 1) {  // 1的概率极高
            return barriers[0].sequence();
        }
        long minimum = Long.MAX_VALUE;
        for (int i = 0; i < n; i++) {
            long value = barriers[i].sequence();
            minimum = Math.min(minimum, value);
        }
        return minimum;
    }

    public static long getMinimumSequence(final SequenceBarrier[] barriers, long minimum) {
        int n = barriers.length;
        if (n == 1) { // 1的概率极高
            return Math.min(minimum, barriers[0].sequence());
        }
        for (int i = 0; i < n; i++) {
            long value = barriers[i].sequence();
            minimum = Math.min(minimum, value);
        }
        return minimum;
    }

    /**
     * 原子方式添加屏障
     *
     * @param varHandle     数组字段自身的handle，不是数组元素的handle
     * @param current       用于初始化下游屏障
     * @param barriersToAdd 要追踪的屏障 -- 下游屏障
     */
    @SuppressWarnings("deprecation")
    public static <T extends SequenceBarrier> void addBarriers(VarHandle varHandle,
                                                               T current,
                                                               SequenceBarrier... barriersToAdd) {
        Preconditions.checkNullElements(barriersToAdd, "barriersToAdd");

        long cursorSequence;
        SequenceBarrier[] oldBarriers;
        SequenceBarrier[] newBarriers;
        do {
            oldBarriers = (SequenceBarrier[]) varHandle.get(current);
            newBarriers = copyOf(oldBarriers, oldBarriers.length + barriersToAdd.length);
            cursorSequence = current.sequence();

            // 这里对新的屏障进行初始化，仅用于避免阻塞当前屏障；
            // 否则一但更新成功，当前屏障必须等待新的屏障序号更新为最新值
            int index = oldBarriers.length;
            for (SequenceBarrier barrier : barriersToAdd) {
                barrier.claim(cursorSequence);
                newBarriers[index++] = barrier;
            }
        }
        while (!varHandle.compareAndSet(current, oldBarriers, newBarriers));

        // 在更新成功后，需要同步进度，这里的临时变量仅用于保证这些消费者同步
        cursorSequence = current.sequence();
        for (SequenceBarrier barrier : barriersToAdd) {
            barrier.claim(cursorSequence);
        }
    }

    /**
     * 原子方式删除屏障
     *
     * @param varHandle 数组字段自身的handle，不是数组元素的handle
     * @param current   当前屏障
     * @param barrier   要删除的屏障
     */
    public static <T extends SequenceBarrier> boolean removeBarrier(VarHandle varHandle,
                                                                    T current,
                                                                    SequenceBarrier barrier) {
        int numToRemove;
        SequenceBarrier[] oldBarriers;
        SequenceBarrier[] newBarriers;
        do {
            oldBarriers = (SequenceBarrier[]) varHandle.get(current);
            numToRemove = countMatching(oldBarriers, barrier);

            if (0 == numToRemove) {
                break;
            }

            final int oldSize = oldBarriers.length;
            newBarriers = new SequenceBarrier[oldSize - numToRemove];

            for (int i = 0, pos = 0; i < oldSize; i++) {
                final SequenceBarrier testSequence = oldBarriers[i];
                if (barrier != testSequence) {
                    newBarriers[pos++] = testSequence;
                }
            }
        }
        while (!varHandle.compareAndSet(current, oldBarriers, newBarriers));

        return numToRemove != 0;
    }

    private static <T> int countMatching(T[] values, final T toMatch) {
        int numToRemove = 0;
        for (T value : values) {
            if (value == toMatch) { // Specifically uses identity
                numToRemove++;
            }
        }
        return numToRemove;
    }

    /**
     * @param n          要申请的序号数量
     * @param timeout    超时时间
     * @param unit       时间单位
     * @param barrier    生产者屏障
     * @param sleepNanos 每次的挂起时间，纳秒
     * @return 申请成功则返回对应的序号，否则返回-1
     */
    public static long tryNext(int n, long timeout, TimeUnit unit,
                               ProducerBarrier barrier, long sleepNanos) {
        long sequence = barrier.tryNext(n);
        if (sequence != -1) {
            return sequence;
        }
        long nanoTime = System.nanoTime();
        final long deadline = nanoTime + unit.toNanos(timeout);
        if (sleepNanos <= 0) {
            do {
                Thread.onSpinWait();
                sequence = barrier.tryNext(n);
                if (sequence != -1) {
                    return sequence;
                }
            } while (System.nanoTime() < deadline);
        } else {
            boolean interrupted = false;
            do {
                long parkNanos = Math.min(sleepNanos, deadline - nanoTime);
                interrupted |= Thread.interrupted(); // 先清理中断
                LockSupport.parkNanos(parkNanos);

                sequence = barrier.tryNext(n);
                if (sequence != -1) {
                    return sequence;
                }
            } while ((nanoTime = System.nanoTime()) < deadline);
            if (interrupted) {
                Thread.currentThread().interrupt();
            }
        }
        return -1;
    }
}