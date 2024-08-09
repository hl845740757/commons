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

import java.util.Arrays;
import java.util.Objects;
import java.util.stream.Stream;


/**
 * 与Disruptor的设计不同，我将RingBuffer类仅仅设计为数据结构。
 *
 * @author wjybxx
 * date - 2024/1/16
 */
public final class RingBuffer<E> implements DataProvider<E> {

    /** 前后缓存行填充的元素元素 */
    private static final int BUFFER_PAD = 16;

    // region padding
    @SuppressWarnings("unused")
    private long p1, p2, p3, p4, p5, p6, p7;
    // endregion

    /**
     * 索引掩码，表示后X位是有效数字(截断)。位运算代替取余快速计算插槽索引
     * (需要放在数组前面充当缓存行填充)
     */
    private final long indexMask;
    /**
     * 事件对象数组，大于真正需要的容量，采用了缓存行填充减少伪共享。
     */
    private final Object[] entries;
    /**
     * 缓存有效空间大小(必须是2的整次幂，-1就是掩码)
     * (使用long类型充当填充)
     */
    private final long bufferSize;

    // region padding
    @SuppressWarnings("unused")
    private long p11, p12, p13, p14, p15, p16, p17;
    // endregion

    public RingBuffer(EventFactory<? extends E> eventFactory, int bufferSize) {
        Objects.requireNonNull(eventFactory, "eventFactory");
        if (!Util.isPowerOfTwo(bufferSize)) {
            throw new IllegalArgumentException("bufferSize must be a power of 2");
        }
        // 前16和后16个用于缓存行填充 -- 32位JVM上Object4字节
        this.entries = new Object[bufferSize + BUFFER_PAD * 2];
        this.bufferSize = bufferSize;
        this.indexMask = bufferSize - 1;
        // 预填充数据
        fill(eventFactory);
    }

    private void fill(EventFactory<? extends E> eventFactory) {
        for (int i = 0; i < bufferSize; i++) {
            entries[BUFFER_PAD + i] = eventFactory.newInstance();
        }
    }

    // region internal

    @SuppressWarnings("unchecked")
    E getElement(long sequence) {
        if (sequence < 0) {
            throw new IllegalArgumentException("sequence: " + sequence);
        }
        int index = (int) (sequence & indexMask);
        return (E) entries[BUFFER_PAD + index];
    }

    void setElement(long sequence, E event) {
        if (sequence < 0) {
            throw new IllegalArgumentException("sequence: " + sequence);
        }
        int index = (int) (sequence & indexMask);
        entries[BUFFER_PAD + index] = event;
    }

    // endregion

    /** 获取buffer大小 */
    public int getBufferSize() {
        return (int) bufferSize;
    }

    /** 用于测试 */
    public Stream<E> stream() {
        @SuppressWarnings("unchecked") E[] elements = (E[]) entries;
        return Arrays.asList(elements)
                .subList(BUFFER_PAD, BUFFER_PAD + (int) bufferSize)
                .stream();
    }

    @Override
    public E get(long sequence) {
        return getElement(sequence);
    }

    @Override
    public E producerGet(long sequence) {
        return getElement(sequence);
    }

    @Override
    public E consumerGet(long sequence) {
        return getElement(sequence);
    }

    @Override
    public void producerSet(long sequence, E data) {
        Objects.requireNonNull(data);
        setElement(sequence, data);
    }

    @Override
    public void consumerSet(long sequence, E data) {
        setElement(sequence, data);
    }
}