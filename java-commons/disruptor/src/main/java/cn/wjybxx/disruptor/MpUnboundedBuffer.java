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
import java.util.Objects;


abstract class MpUnboundedBufferFields<E> {

    /** 该引用表示生产者或消费者块正在执行更新 */
    protected static final MpUnboundedBufferChunk<?> ROTATION = new MpUnboundedBufferChunk<>(0, -2, null);

    // region pad
    private long p1, p2, p3, p4, p5, p6, p7, p8;
    private long p11, p12, p13, p14, p15, p16, p17, p18;
    // endregion

    /**
     * 链表的首部
     * 1. 可能没有消费者在该块，但消费者都从该块开始查询 -- 消费者高频访问。
     * 2. 由【生产者】更新，生产者观察到消费者进入新块时，或自身进入新块时，尝试回收当前块。
     */
    private volatile MpUnboundedBufferChunk<E> headChunk;

    // region pad
    private long p21, p22, p23, p24, p25, p26, p27, p28;
    private long p31, p32, p33, p34, p35, p36, p37, p38;
    // endregion

    /** 用于竞争更新head */
    private volatile int headLock = 0;
    /**
     * 链表的末端
     * 1. 可能超出生产者块 -- 表示包含预先配的块或回收后的块。
     * 2. 由【生产者】更新，生产者需要新块时，或回收消费者块时更新。
     * 3. 为{@link #ROTATION}时表示正在执行更新。
     */
    private volatile MpUnboundedBufferChunk<E> tailChunk;
    /**
     * 【最快的】生产者当前填充的块。
     * 1. 并非所有的生产者都在该块上，很可能有生产者还在填充旧的块，其它生产者通过prev获取前面的chunk。
     * 2. 插入新块时，先初始化新块，然后将其链接到当前块。
     * 3. 为{@link #ROTATION}时表示正在执行更新。
     */
    private volatile MpUnboundedBufferChunk<E> producerChunk;

    // region pad
    private long p41, p42, p43, p44, p45, p46, p47, p48;
    private long p51, p52, p53, p54, p55, p56, p57, p58;
    // endregion

    // region producer

    /** loadVolatileProducerChunk */
    final MpUnboundedBufferChunk<E> lvProducerChunk() {
        return this.producerChunk;
    }

    /** storeReleaseProducerChunk */
    final void soProducerChunk(MpUnboundedBufferChunk<E> chunk) {
        VH_PRODUCER_CHUNK.setRelease(this, chunk);
    }

    /** cas更新生产者块 */
    final boolean casProducerChunk(MpUnboundedBufferChunk<E> current, MpUnboundedBufferChunk<?> newChunk) {
        assert current != ROTATION;
        return VH_PRODUCER_CHUNK.compareAndSet(this, current, newChunk);
    }

    /** loadVolatileHeadChunk */
    final MpUnboundedBufferChunk<E> lvHeadChunk() {
        return this.headChunk;
    }

    /** storeReleaseHeadChunk */
    final void soHeadChunk(MpUnboundedBufferChunk<E> chunk) {
        VH_HEAD_CHUNK.setRelease(this, chunk);
    }

    /** 尝试锁定head的更新权限 */
    final boolean tryLockHead() {
        return VH_HEAD_LOCK.compareAndSet(this, 0, 1);
    }

    /** 解除head的更新权限 */
    final void unlockHead() {
        VH_HEAD_LOCK.setRelease(this, 0);
    }

    /** loadVolatileTailChunk */
    final MpUnboundedBufferChunk<E> lvTailChunk() {
        return this.tailChunk;
    }

    /** storeReleaseTailChunk */
    final void soTailChunk(MpUnboundedBufferChunk<E> chunk) {
        VH_TAIL_CHUNK.setRelease(this, chunk);
    }

    /** cas更新Tail块 -- 注意！由生产者调用！ */
    final boolean casTailChunk(MpUnboundedBufferChunk<E> current, MpUnboundedBufferChunk<?> newChunk) {
        assert current != ROTATION;
        return VH_TAIL_CHUNK.compareAndSet(this, current, newChunk);
    }

    // endregion
    private static final VarHandle VH_HEAD_LOCK;
    private static final VarHandle VH_HEAD_CHUNK;
    private static final VarHandle VH_PRODUCER_CHUNK;
    private static final VarHandle VH_TAIL_CHUNK;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_HEAD_LOCK = l.findVarHandle(MpUnboundedBufferFields.class, "headLock", int.class);
            VH_HEAD_CHUNK = l.findVarHandle(MpUnboundedBufferFields.class, "headChunk", MpUnboundedBufferChunk.class);
            VH_PRODUCER_CHUNK = l.findVarHandle(MpUnboundedBufferFields.class, "producerChunk", MpUnboundedBufferChunk.class);
            VH_TAIL_CHUNK = l.findVarHandle(MpUnboundedBufferFields.class, "tailChunk", MpUnboundedBufferChunk.class);
        } catch (Exception e) {
            throw new ExceptionInInitializerError(e);
        }
    }
}

/**
 * 多生产者的无界缓冲区
 * 注意：
 * 1. 该缓冲区不是为性能而设计的，它的主要是目的是避免死锁。该缓冲区应当用于内部系统交互，
 * 而不应该用于与外部系统交互，对外的缓冲区都应该是有界的。<br>
 * 2. 该缓存不会自动回收和复用块，需要外部显式调用回收 -- Sequencer需要负责回收。
 *
 * @author wjybxx
 * date - 2024/1/18
 */
public final class MpUnboundedBuffer<E> extends MpUnboundedBufferFields<E> implements DataProvider<E> {

    /** chunkSize对应的掩码 */
    private final int chunkMask;
    /** chunk的size对应的右移偏移量 -- 用于快速计算sequence对应的chunk索引 */
    private final int chunkShift;
    /** 最大缓存块数 */
    private final int maxPooledChunks;
    /** 事件工厂 */
    private final EventFactory<? extends E> factory;

    /**
     * @param chunkSize       单个块大小
     * @param maxPooledChunks 缓存块数量
     * @param factory         事件工厂
     */
    public MpUnboundedBuffer(int chunkSize,
                             int maxPooledChunks,
                             EventFactory<? extends E> factory) {
        if (maxPooledChunks < 0) {
            throw new IllegalArgumentException("Expecting a positive maxPooledChunks, but got:" + maxPooledChunks);
        }
        chunkSize = Util.nextPowerOfTwo(chunkSize);
        this.chunkMask = chunkSize - 1;
        this.chunkShift = Integer.numberOfTrailingZeros(chunkSize);
        this.maxPooledChunks = maxPooledChunks;
        this.factory = Objects.requireNonNull(factory, "factory");

        MpUnboundedBufferChunk<E> firstChunk = new MpUnboundedBufferChunk<>(chunkSize, 0, null);
        firstChunk.fill(factory);

        soTailChunk(firstChunk);
        soHeadChunk(firstChunk);
        soProducerChunk(firstChunk);
    }

    /** 只能用在初始化的时候 */
    public void claim(long sequence) {
        if (lvHeadChunk() != lvTailChunk()) {
            throw new IllegalStateException();
        }
        final long seqChunkIndex = sequence >> chunkShift;
        lvHeadChunk().soChunkIndex(seqChunkIndex);
    }

    /** 单个块大小 */
    public int chunkSize() {
        return chunkMask + 1;
    }

    /** 缓存chunk数 */
    public int maxPooledChunks() {
        return maxPooledChunks;
    }

    /** 获取sequence对应的chunk的index */
    public long chunkIndexForSequence(long sequence) {
        assert sequence >= 0;
        return sequence >> chunkShift;
    }

    /** 判断两个sequence是否落在同一个chunk */
    public boolean inSameChunk(long seq1, long seq2) {
        final int chunkShift = this.chunkShift;
        return (seq1 >> chunkShift) == (seq2 >> chunkShift);
    }

    @Override
    public E get(long sequence) {
        return consumerGet(sequence); // 生产者会在竞争到序号的时候触发扩容
    }

    @Override
    public E producerGet(long sequence) {
        if (sequence < 0) {
            throw new IllegalArgumentException();
        }
        final int seqChunkOffset = (int) (sequence & chunkMask);
        final long seqChunkIndex = sequence >> chunkShift;

        MpUnboundedBufferChunk<E> pChunk = lvProducerChunk();
        if (pChunk.lvChunkIndex() != seqChunkIndex) {
            pChunk = producerChunkForIndex(pChunk, seqChunkIndex);
        }
        return pChunk.lvElement(seqChunkOffset);
    }

    @Override
    public E consumerGet(long sequence) {
        if (sequence < 0) {
            throw new IllegalArgumentException();
        }
        final int seqChunkOffset = (int) (sequence & chunkMask);
        final long seqChunkIndex = sequence >> chunkShift;

        MpUnboundedBufferChunk<E> cChunk = lvHeadChunk();
        if (cChunk.lvChunkIndex() != seqChunkIndex) {
            cChunk = consumerChunkForIndex(cChunk, seqChunkIndex);
        }
        return cChunk.lvElement(seqChunkOffset);
    }

    @Override
    public void producerSet(long sequence, E data) {
        Objects.requireNonNull(data);
        if (sequence < 0) {
            throw new IllegalArgumentException();
        }
        final int seqChunkOffset = (int) (sequence & chunkMask);
        final long seqChunkIndex = sequence >> chunkShift;

        MpUnboundedBufferChunk<E> pChunk = lvProducerChunk();
        if (pChunk.lvChunkIndex() != seqChunkIndex) {
            pChunk = producerChunkForIndex(pChunk, seqChunkIndex);
        }
        pChunk.spElement(seqChunkOffset, data);
    }

    @Override
    public void consumerSet(long sequence, E data) {
        if (sequence < 0) {
            throw new IllegalArgumentException();
        }
        final int seqChunkOffset = (int) (sequence & chunkMask);
        final long seqChunkIndex = sequence >> chunkShift;

        MpUnboundedBufferChunk<E> cChunk = lvHeadChunk();
        if (cChunk.lvChunkIndex() != seqChunkIndex) {
            cChunk = consumerChunkForIndex(cChunk, seqChunkIndex);
        }
        cChunk.spElement(seqChunkOffset, data);
    }

    /** 获取生产者sequence对应的chunk -- 生产者再获得序号后应当调用该方法触发扩容 */
    public MpUnboundedBufferChunk<E> producerChunkForSequence(long sequence) {
        if (sequence < 0) {
            throw new IllegalArgumentException();
        }
        final long seqChunkIndex = sequence >> chunkShift;
        MpUnboundedBufferChunk<E> pChunk = lvProducerChunk();
        if (pChunk.lvChunkIndex() != seqChunkIndex) {
            pChunk = producerChunkForIndex(pChunk, seqChunkIndex);
        }
        return pChunk;
    }

    /** 获取消费者sequence对应的chunk */
    public MpUnboundedBufferChunk<E> consumerChunkForSequence(long sequence) {
        if (sequence < 0) {
            throw new IllegalArgumentException();
        }
        final long seqChunkIndex = sequence >> chunkShift;
        MpUnboundedBufferChunk<E> cChunk = lvHeadChunk();
        if (cChunk.lvChunkIndex() != seqChunkIndex) {
            cChunk = consumerChunkForIndex(cChunk, seqChunkIndex);
        }
        return cChunk;
    }

    /** 获取指定索引的消费者块 -- 当{@link #lvHeadChunk()}不是期望的块时调用。 */
    private MpUnboundedBufferChunk<E> consumerChunkForIndex(
            final MpUnboundedBufferChunk<E> initialChunk,
            final long requiredChunkIndex) {
        // 要保证这里的正确性，生产者在回收chunk后，一定要标记chunkIndex为-1或next为null。
        MpUnboundedBufferChunk<E> currentChunk = initialChunk;
        while (true) {
            if (currentChunk == null) {
                Thread.onSpinWait();
                currentChunk = lvHeadChunk();
            }
            final long currentChunkIndex = currentChunk.lvChunkIndex();
            if (currentChunkIndex == requiredChunkIndex) {
                return currentChunk;
            }
            if (currentChunkIndex < 0
                    || currentChunkIndex > requiredChunkIndex) { // 当前块被回收复用
                currentChunk = null;
                continue;
            }
            currentChunk = currentChunk.lvNext(); // nullable；生产者尚未创建，或当前块被回收
        }
    }

    /** 获取指定索引的生产者块 -- 当{@link #lvProducerChunk()}不是期望的块时调用 */
    private MpUnboundedBufferChunk<E> producerChunkForIndex(
            final MpUnboundedBufferChunk<E> initialChunk,
            final long requiredChunkIndex) {
        MpUnboundedBufferChunk<E> currentChunk = initialChunk;
        // 后跳步数 - 当生产者速度较快时，不同生产者可能处于不同的chunk，因此可能需要后跳
        long jumpBackward;
        while (true) {
            if (currentChunk == null) {
                currentChunk = lvProducerChunk();
            }
            if (currentChunk == ROTATION) { // 其它线程正在执行更新，等待其更新完成
                Thread.onSpinWait();
                currentChunk = null;
                continue;
            }
            final long currentChunkIndex = currentChunk.lvChunkIndex();
            jumpBackward = currentChunkIndex - requiredChunkIndex;
            if (jumpBackward >= 0) {
                break;
            }
            currentChunk = appendNextChunks(currentChunk, currentChunkIndex, -jumpBackward);
        }
        for (long i = 0; i < jumpBackward; i++) {
            currentChunk = currentChunk.lvPrev();
        }
        assert currentChunk.lvChunkIndex() == requiredChunkIndex;
        return currentChunk;
    }

    private MpUnboundedBufferChunk<E> appendNextChunks(MpUnboundedBufferChunk<E> currentChunk,
                                                       long currentChunkIndex,
                                                       long chunksToAppend) {
        if (!casProducerChunk(currentChunk, ROTATION)) {
            return null;
        }
        // 获得更新producerChunk权限，这期间其它生产者需要等待
        for (long i = 1; i <= chunksToAppend; i++) {
            MpUnboundedBufferChunk<E> newChunk = newOrPooledChunk(currentChunk, currentChunkIndex + i);
            currentChunk.soNext(newChunk);
            currentChunk = newChunk;
        }
        soProducerChunk(currentChunk);
        return currentChunk;
    }

    private MpUnboundedBufferChunk<E> newOrPooledChunk(MpUnboundedBufferChunk<E> prevChunk, long nextChunkIndex) {
        MpUnboundedBufferChunk<E> tailChunk;
        while (true) {
            tailChunk = lvTailChunk();
            // 其它生产者可能正在回收head
            if (tailChunk == ROTATION) {
                Thread.onSpinWait();
                continue;
            }
            // tail可能包含预分配的块
            if (nextChunkIndex <= tailChunk.lvChunkIndex()) {
                MpUnboundedBufferChunk<E> nextChunk = prevChunk.lvNext();
                assert nextChunk != null && nextChunk.lvChunkIndex() == nextChunkIndex;
                return nextChunk;
            }
            // 其它生产者可能正在回收head
            if (!casTailChunk(tailChunk, ROTATION)) {
                Thread.onSpinWait();
                continue;
            }
            // 新增块到tail
            MpUnboundedBufferChunk<E> nextChunk = new MpUnboundedBufferChunk<>(chunkSize(),
                    nextChunkIndex, prevChunk);
            nextChunk.fill(factory);

            nextChunk.soPrev(tailChunk);
            tailChunk.soNext(nextChunk);
            soTailChunk(nextChunk);
            return nextChunk;
        }
    }

    /**
     * 尝试将head更新到下一个chunk
     * (public以允许用户自行控制回收时机)
     *
     * @param gatingSequence 最慢的消费者进度(已消费)
     * @return 是否成功触发回收
     */
    public boolean tryMoveHeadToNext(long gatingSequence) {
        MpUnboundedBufferChunk<E> headChunk = lvHeadChunk();
        MpUnboundedBufferChunk<E> producerChunk = lvProducerChunk();
        if (!isRecyclable(headChunk, gatingSequence, producerChunk)) {
            return false;
        }
        if (!tryLockHead()) {
            return false;
        }
        // 注意：在竞争lock成功后，head可能是过期的！必须重新检查回收条件 -- 这期间producerChunk的索引不会变化
        headChunk = lvHeadChunk();
        producerChunk = lvProducerChunk();
        if (!isRecyclable(headChunk, gatingSequence, producerChunk)) {
            unlockHead();
            return false;
        }
        // 注意：观察到的消费者序号可能跨越了多个块，因此可能需要回收多个块
        MpUnboundedBufferChunk<E> nextChunk = headChunk.lvNext();
        nextChunk.soPrev(null);
        while (isRecyclable(nextChunk, gatingSequence, producerChunk)) {
            nextChunk = nextChunk.lvNext();
            nextChunk.soPrev(null);
        }
        // 我们立即发布新的head，以允许消费者获取最新的数据
        soHeadChunk(nextChunk);
        recycleChunks(headChunk, nextChunk);
        unlockHead();
        return true;
    }

    private static boolean isRecyclable(MpUnboundedBufferChunk<?> chunk, long gatingSequence,
                                        MpUnboundedBufferChunk<?> producerChunk) {
        // 不可以回收生产者当前块，否则会导致生产者append产生竞争
        return chunk.maxSequence() <= gatingSequence
                && chunk.lvChunkIndex() < producerChunk.lvChunkIndex(); // ROTATION is ok
    }

    private void recycleChunks(MpUnboundedBufferChunk<E> headChunk, MpUnboundedBufferChunk<E> nextChunk) {
        MpUnboundedBufferChunk<E> freeChunk = headChunk;
        while (true) {
            MpUnboundedBufferChunk<E> tailChunk = lvTailChunk();
            if (tailChunk == ROTATION) {
                Thread.onSpinWait(); // 生产者正在创建新的chunk
                continue;
            }
            long recyclable = maxPooledChunks - (tailChunk.lvChunkIndex() - nextChunk.lvChunkIndex()) - 1;
            if (recyclable <= 0) { // 这期间chunk只会越来越多，因此无需重试
                break;
            }
            if (!casTailChunk(tailChunk, ROTATION)) {
                Thread.onSpinWait(); // 生产者正在创建新的chunk
                continue;
            }
            for (long i = 0; (i < recyclable && freeChunk != nextChunk); i++) {
                MpUnboundedBufferChunk<E> tempNext = freeChunk.lvNext(); // next在前面并未先断开
                freeChunk.soNext(null);

                freeChunk.soChunkIndex(tailChunk.lvChunkIndex() + 1); // 消费者可能看见突然变大的id
                freeChunk.soPrev(tailChunk);
                tailChunk.soNext(freeChunk);

                tailChunk = freeChunk;
                freeChunk = tempNext;
            }
            soTailChunk(tailChunk); // cas后不论是否触发回收，都需要写回
            break;
        }
        // 清理剩余块
        while (freeChunk != nextChunk) {
            MpUnboundedBufferChunk<E> tempNext = freeChunk.lvNext();
            freeChunk.soNext(null); // 消费者需要感知到被清理
            freeChunk.clear();
            freeChunk = tempNext;
        }
    }

}
