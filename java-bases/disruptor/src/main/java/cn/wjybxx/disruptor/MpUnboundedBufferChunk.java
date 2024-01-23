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

import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Arrays;
import java.util.concurrent.locks.LockSupport;

/**
 * @author wjybxx
 * date - 2024/1/16
 */
public final class MpUnboundedBufferChunk<E> {

    /** 未使用的块 */
    final static int NOT_USED = -1;

    /** 这里的buffer暂未使用缓存行填充机制 */
    private final Object[] buffer;
    /**
     * 已发布的槽位
     * 注意：与{@link MultiProducerSequencer}的方案不同，这里发布时是将其标记为{@link #chunkIndex} -- 可避免额外的计算。
     */
    private final long[] published;
    /** 该chunk的索引 */
    private volatile long chunkIndex;

    private volatile MpUnboundedBufferChunk<E> prev;
    private volatile MpUnboundedBufferChunk<E> next;

    /**
     * @param length     chunk的长度
     * @param chunkIndex chunk的索引
     * @param prev       前置节点
     */
    public MpUnboundedBufferChunk(int length,
                                  long chunkIndex, MpUnboundedBufferChunk<E> prev) {
        this.buffer = new Object[length];
        this.published = new long[length];

        if (chunkIndex == 0) { // 其它情况下0可以表示未发布
            Arrays.fill(published, -1);
        }
        spChunkIndex(chunkIndex);
        soPrev(prev);
    }

    // region internal

    /** load plain index */
    private long lpChunkIndex() {
        return (long) VH_INDEX.get(this);
    }

    /** store plain index */
    private void spChunkIndex(long index) {
        VH_INDEX.set(this, index);
    }

    /** load volatile index */
    final long lvChunkIndex() {
        return chunkIndex;
    }

    /** store ordered index */
    final void soChunkIndex(long index) {
        VH_INDEX.setRelease(this, index);
    }

    /** load volatile next */
    final MpUnboundedBufferChunk<E> lvNext() {
        return next;
    }

    /** store ordered next */
    final void soNext(MpUnboundedBufferChunk<E> value) {
        VH_NEXT.setRelease(this, value);
    }

    /** load volatile prev */
    final MpUnboundedBufferChunk<E> lvPrev() {
        return prev;
    }

    /** store ordered prev */
    final void soPrev(MpUnboundedBufferChunk<?> value) {
        VH_PREV.setRelease(this, value);
    }

    // endregion

    /** store plain element */
    public final void spElement(int index, E e) {
        VH_ELEMENTS.set(buffer, index, e);
    }

    /** store ordered element */
    public final void soElement(int index, E e) {
        VH_ELEMENTS.setRelease(buffer, index, e);
    }

    /** load volatile element */
    @SuppressWarnings("unchecked")
    public final E lvElement(int index) {
        return (E) VH_ELEMENTS.getVolatile(buffer, index);
    }

    /**
     * 将指定槽位标记为已发布
     */
    public final void publish(int index) {
        VH_PUBLISHED.setRelease(published, index, lpChunkIndex());
    }

    /**
     * 批量发布数据
     *
     * @param low  起始下标(inclusive)
     * @param high 结束下标(inclusive)
     */
    public final void publish(int low, int high) {
        final long[] published = this.published;
        final long chunkIndex = lpChunkIndex();
        while (low <= high) {
            VH_PUBLISHED.setRelease(published, low++, chunkIndex);
        }
    }

    public final boolean isPublished(int index) {
        long flag = (long) VH_PUBLISHED.getVolatile(published, index);
        return flag == lpChunkIndex();
    }

    /** @return highest index */
    public final int getHighestPublishedSequence(final int low, final int high) {
        final long[] published = this.published;
        final long chunkIndex = lpChunkIndex();
        for (int index = low; index <= high; index++) {
            long flag = (long) VH_PUBLISHED.getVolatile(published, index);
            if (flag != chunkIndex) {
                return index - 1;
            }
        }
        return high;
    }

    /** 获取chunk上数据的最小sequence -- plain内存语义 */
    public final long minSequence() {
        int length = buffer.length;
        return (long) VH_INDEX.get(this) * length;
    }

    /** 获取chunk上数据的最大sequence -- plain内存语义 */
    public final long maxSequence() {
        int length = buffer.length;
        return (long) VH_INDEX.get(this) * length + (length - 1);
    }

    /** buffer的长度 */
    public final int length() {
        return buffer.length;
    }

    //

    /** 填充chunk - 使用Plain内存语义 */
    public final void fill(EventFactory<? extends E> factory) {
        for (int i = 0; i < buffer.length; i++) {
            buffer[i] = factory.newInstance();
        }
    }

    /** 清理chunk - 使用Plain内存语义 */
    public final void clear() {
        Arrays.fill(buffer, null);
    }

    private static final VarHandle VH_INDEX;
    private static final VarHandle VH_PREV;
    private static final VarHandle VH_NEXT;
    private static final VarHandle VH_ELEMENTS;
    private static final VarHandle VH_PUBLISHED;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_INDEX = l.findVarHandle(MpUnboundedBufferChunk.class, "chunkIndex", long.class);
            VH_PREV = l.findVarHandle(MpUnboundedBufferChunk.class, "prev", MpUnboundedBufferChunk.class);
            VH_NEXT = l.findVarHandle(MpUnboundedBufferChunk.class, "next", MpUnboundedBufferChunk.class);
            VH_ELEMENTS = MethodHandles.arrayElementVarHandle(Object[].class);
            VH_PUBLISHED = MethodHandles.arrayElementVarHandle(long[].class);
        } catch (Exception e) {
            throw new ExceptionInInitializerError(e);
        }

        // Reduce the risk of rare disastrous classloading in first call to
        // LockSupport.park: https://bugs.openjdk.java.net/browse/JDK-8074773
        Class<?> ensureLoaded = LockSupport.class;
    }

}
