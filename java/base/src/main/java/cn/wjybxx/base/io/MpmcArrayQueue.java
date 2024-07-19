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

package cn.wjybxx.base.io;

import cn.wjybxx.base.MathCommon;

import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Arrays;
import java.util.Objects;

/**
 * 这是一个特定实现的多生产者多消费者的数组队列（MpmcArrayQueue）
 * 这里没有强制数组大小必须是2的幂，因为要严格保证池的大小符合预期。
 * (这里的算法参照了Disruptor模块的实现，但针对数组池进行了特殊的修改，但没有做极致的优化)
 *
 * @author wjybxx
 * date - 2024/7/17
 */
final class MpmcArrayQueue<E> {

    // region padding
    private long p1, p2, p3, p4, p5, p6, p7, p8;
    // endregion

    /** 数组长度 -- 不一定为2的幂 */
    private final int length;
    /** 数组池 -- 每一个元素都是数组 */
    private final Object[] buffer;

    /** 已发布的数组元素 -- 存储的是对应的sequence */
    private final long[] published;
    /** 已消费的数组元素 -- 存储的是对应的sequence */
    private final long[] consumed;

    // region padding
    private long p11, p12, p13, p14, p15, p16, p17, p18;
    // endregion

    /** 生产者索引 */
    private volatile long producerIndex = -1;

    // region padding
    private long p21, p22, p23, p24, p25, p26, p27, p28;
    // endregion

    /** 消费者索引 */
    private volatile long consumerIndex = -1;

    // region padding
    private long p31, p32, p33, p34, p35, p36, p37, p38;
    // endregion

    public MpmcArrayQueue(int length) {
        this.length = length;

        this.buffer = new Object[length];
        this.published = new long[length];
        this.consumed = new long[length];

        // 需要初始化为-1，0是有效的sequence
        Arrays.fill(published, -1);
        Arrays.fill(consumed, -1);
    }

    /** 数组的长度 */
    public int getLength() {
        return length;
    }

    /** 当前元素数量 */
    public int size() {
        return MathCommon.clamp(lvProducerIndex() - lvConsumerIndex(), 0, length);
    }

    /** 尝试压入数组 */
    public boolean offer(E element) {
        if (length == 0) {
            return false;
        }
        Objects.requireNonNull(element);
        // 先更新生产者索引，然后设置元素，再标记为已生产 -- 实现可见性保证
        long current;
        long next;
        do {
            current = lvProducerIndex();
            next = current + 1;

            long wrapPoint = next - length;
            if (wrapPoint >= 0 && !isConsumed(wrapPoint)) {
                // 如果尚未被消费，则判断当前是否正在消费，如果正在消费则spin等待
                if (wrapPoint <= lvConsumerIndex()) {
                    next = current; // skip cas
                    continue;
                }
                return false;
            }
        }
        while ((next == current) || !casProducerIndex(current, next));

        int index = indexOfSequence(next);
        spElement(index, element);
        markPublished(next);
        return true;
    }

    /** 尝试弹出元素 */
    public E poll() {
        if (length == 0) {
            return null;
        }
        // 先更新消费者索引，然后设置元素为null，再标记为已消费 -- 实现可见性保证
        long current;
        long next;
        do {
            current = lvConsumerIndex();
            next = current + 1;

            if (!isPublished(next)) {
                // 如果尚未发布，则判断当前是否正在生产，如果正在生产者则spin等待
                if (next <= lvProducerIndex()) {
                    next = current; // skip cas
                    continue;
                }
                return null;
            }
        }
        while ((next == current) || !casConsumerIndex(current, next));

        int index = indexOfSequence(next);
        E element = lpElement(index);
        spElement(index, null);
        markConsumed(next);
        return element;
    }

    // region internal

    /** load volatile producerIndex */
    private long lvProducerIndex() {
        return producerIndex;
    }

    /** compare and set producerIndex */
    private boolean casProducerIndex(long expect, long newValue) {
        return VH_PRODUCER.compareAndSet(this, expect, newValue);
    }

    /** load volatile consumerIndex */
    private long lvConsumerIndex() {
        return consumerIndex;
    }

    /** compare and set consumerIndex */
    private boolean casConsumerIndex(long expect, long newValue) {
        return VH_CONSUMER.compareAndSet(this, expect, newValue);
    }

    /** store plain element -- 可见性由sequence的发布保证 */
    private void spElement(int index, E e) {
        VH_ELEMENTS.set(buffer, index, e);
    }

    /** store ordered element -- 可见性由自身的写入保证 */
    private void soElement(int index, E e) {
        VH_ELEMENTS.setRelease(buffer, index, e);
    }

    /** load volatile element */
    @SuppressWarnings("unchecked")
    private E lvElement(int index) {
        return (E) VH_ELEMENTS.getVolatile(buffer, index);
    }

    /** load plain element */
    @SuppressWarnings("unchecked")
    private E lpElement(int index) {
        return (E) VH_ELEMENTS.get(buffer, index);
    }

    /** 将指定槽位标记为已发布 */
    private void markPublished(long sequence) {
        int index = indexOfSequence(sequence);
        VH_LONG_ARRAY.setRelease(published, index, sequence);
    }

    /** 查询指定数据是否已发布 */
    private boolean isPublished(long sequence) {
        int index = indexOfSequence(sequence);
        long flag = (long) VH_LONG_ARRAY.getVolatile(published, index);
        return flag == sequence;
    }

    /** 将指定槽位标记为已消费 */
    private void markConsumed(long sequence) {
        int index = indexOfSequence(sequence);
        VH_LONG_ARRAY.setRelease(consumed, index, sequence);
    }

    private boolean isConsumed(long sequence) {
        int index = indexOfSequence(sequence);
        long flag = (long) VH_LONG_ARRAY.getVolatile(consumed, index);
        return flag == sequence;
    }

    /** 获取指定sequence对应的数组下标 */
    private int indexOfSequence(long sequence) {
        return (int) (sequence % length);
    }

    // endregion

    private static final VarHandle VH_ELEMENTS;
    private static final VarHandle VH_LONG_ARRAY;

    private static final VarHandle VH_PRODUCER;
    private static final VarHandle VH_CONSUMER;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_PRODUCER = l.findVarHandle(MpmcArrayQueue.class, "producerIndex", long.class);
            VH_CONSUMER = l.findVarHandle(MpmcArrayQueue.class, "consumerIndex", long.class);

            VH_ELEMENTS = MethodHandles.arrayElementVarHandle(Object[].class);
            VH_LONG_ARRAY = MethodHandles.arrayElementVarHandle(long[].class);
        } catch (Exception e) {
            throw new ExceptionInInitializerError(e);
        }
    }
}
