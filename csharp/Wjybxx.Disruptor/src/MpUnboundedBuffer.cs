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
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS0169
namespace Wjybxx.Disruptor
{
public abstract class MpUnboundedBufferFields<E>
{
    /** 该引用表示生产者或消费者块正在执行更新 */
    internal static readonly MpUnboundedBufferChunk<E> ROTATION = new MpUnboundedBufferChunk<E>(0, -2, null);

    // region padding
    private long p1, p2, p3, p4, p5, p6, p7, p8;
    // endregion

    /**
     * 链表的首部
     * 1. 可能没有消费者在该块，但消费者都从该块开始查询 -- 消费者高频访问。
     * 2. 由【生产者】更新，生产者观察到消费者进入新块时，或自身进入新块时，尝试回收当前块。
     */
    private volatile MpUnboundedBufferChunk<E> headChunk;

    // region padding
    private long p11, p12, p13, p14, p15, p16, p17, p18;
    // endregion

    /** 用于竞争更新head */
    private volatile int headLock = 0;

    // region padding
    private long p21, p22, p23, p24, p25, p26, p27, p28;
    // endregion

    /**
     * 链表的末端
     * 1. 可能超出生产者块 -- 表示包含预先配的块或回收后的块。
     * 2. 由【生产者】更新，生产者需要新块时，或回收消费者块时更新。
     * 3. 为<see cref="ROTATION"/>时表示正在执行更新。
     */
    private volatile MpUnboundedBufferChunk<E> tailChunk;
    /**
     * 【最快的】生产者当前填充的块。
     * 1. 并非所有的生产者都在该块上，很可能有生产者还在填充旧的块，其它生产者通过prev获取前面的chunk。
     * 2. 插入新块时，先初始化新块，然后将其链接到当前块。
     * 3. 为<see cref="ROTATION"/>时表示正在执行更新。
     */
    private volatile MpUnboundedBufferChunk<E> producerChunk;

    // region padding
    private long p31, p32, p33, p34, p35, p36, p37, p38;
    // endregion


    /** loadVolatileProducerChunk */
    internal MpUnboundedBufferChunk<E> LvProducerChunk() {
        return this.producerChunk;
    }

    /** storeReleaseProducerChunk */
    internal void SoProducerChunk(MpUnboundedBufferChunk<E> chunk) {
        this.producerChunk = chunk;
    }

    /** cas更新生产者块 */
    internal bool CasProducerChunk(MpUnboundedBufferChunk<E> current, MpUnboundedBufferChunk<E> newChunk) {
        Debug.Assert(current != ROTATION);
        return Interlocked.CompareExchange(ref producerChunk, newChunk, current) == current;
    }

    /** loadVolatileHeadChunk */
    internal MpUnboundedBufferChunk<E> LvHeadChunk() {
        return this.headChunk;
    }

    /** storeReleaseHeadChunk */
    internal void SoHeadChunk(MpUnboundedBufferChunk<E> chunk) {
        this.headChunk = chunk;
    }

    /** 尝试锁定head的更新权限 */
    internal bool TryLockHead() {
        return Interlocked.CompareExchange(ref headLock, 1, 0) == 0;
    }

    /** 解除head的更新权限 */
    internal void UnlockHead() {
        this.headLock = 0;
    }

    /** loadVolatileTailChunk */
    internal MpUnboundedBufferChunk<E> LvTailChunk() {
        return this.tailChunk;
    }

    /** storeReleaseTailChunk */
    internal void SoTailChunk(MpUnboundedBufferChunk<E> chunk) {
        this.tailChunk = chunk;
    }

    /** cas更新Tail块 -- 注意！由生产者调用！ */
    internal bool CasTailChunk(MpUnboundedBufferChunk<E> current, MpUnboundedBufferChunk<E> newChunk) {
        Debug.Assert(current != ROTATION);
        return Interlocked.CompareExchange(ref tailChunk, newChunk, current) == current;
    }
}

/// <summary>
/// 多生产者的无界缓冲区
/// 注意：
/// 1. 该缓冲区不是为性能而设计的，它的主要是目的是避免死锁。该缓冲区应当用于内部系统交互，而不应该用于与外部系统交互，对外的缓冲区都应该是有界的。
/// 2. 该缓存不会自动回收和复用块，需要外部显式调用回收 -- Sequencer需要负责回收。
/// </summary>
/// <typeparam name="E"></typeparam>
public sealed class MpUnboundedBuffer<E> : MpUnboundedBufferFields<E>, DataProvider<E>
{
    /** 事件工厂 */
    private readonly Func<E> factory;
    /** chunkSize对应的掩码 */
    private readonly int chunkMask;
    /** chunk的size对应的右移偏移量 -- 用于快速计算sequence对应的chunk索引 */
    private readonly int chunkShift;
    /** 最大缓存块数 */
    private readonly int maxPooledChunks;

    public MpUnboundedBuffer(Func<E> factory, int chunkLength, int maxPooledChunks) {
        if (maxPooledChunks < 0) {
            throw new ArgumentException("Expecting a positive maxPooledChunks, but got:" + maxPooledChunks);
        }
        chunkLength = Util.NextPowerOfTwo(chunkLength);

        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this.chunkMask = chunkLength - 1;
        this.chunkShift = Util.Log2(chunkLength);
        this.maxPooledChunks = maxPooledChunks;

        MpUnboundedBufferChunk<E> firstChunk = new MpUnboundedBufferChunk<E>(chunkLength, 0, null);
        firstChunk.Fill(factory);

        SoTailChunk(firstChunk);
        SoHeadChunk(firstChunk);
        SoProducerChunk(firstChunk);
    }

    #region props

    /** 只能用在初始化的时候 */
    public void Claim(long sequence) {
        if (LvHeadChunk() != LvTailChunk()) {
            throw new Exception("state error");
        }
        long seqChunkIndex = sequence >> chunkShift;
        LvHeadChunk().SoChunkIndex(seqChunkIndex);
    }

    /** 单个块大小 */
    public int ChunkLength => chunkMask + 1;

    /** 缓存chunk数 */
    public int MaxPooledChunks => maxPooledChunks;

    /** 获取sequence对应的chunk的index */
    public long ChunkIndexForSequence(long sequence) {
        return sequence >> chunkShift;
    }

    /** 判断两个sequence是否落在同一个chunk */
    public bool InSameChunk(long seq1, long seq2) {
        return (seq1 >> chunkShift) == (seq2 >> chunkShift);
    }

    #endregion

    #region data-provider

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public E Get(long sequence) {
        return ConsumerGet(sequence); // 生产者会在竞争到序号的时候触发扩容
    }

    public E ProducerGet(long sequence) {
        if (sequence < 0) {
            throw new ArgumentException("sequence: " + sequence);
        }
        int seqChunkOffset = (int)(sequence & chunkMask);
        long seqChunkIndex = sequence >> chunkShift;

        MpUnboundedBufferChunk<E> pChunk = LvProducerChunk();
        if (pChunk.LvChunkIndex() != seqChunkIndex) {
            pChunk = ProducerChunkForIndex(pChunk, seqChunkIndex);
        }
        return pChunk.LpElement(seqChunkOffset);
    }

    public E ConsumerGet(long sequence) {
        if (sequence < 0) {
            throw new ArgumentException("sequence: " + sequence);
        }
        int seqChunkOffset = (int)(sequence & chunkMask);
        long seqChunkIndex = sequence >> chunkShift;

        MpUnboundedBufferChunk<E> cChunk = LvHeadChunk();
        if (cChunk.LvChunkIndex() != seqChunkIndex) {
            cChunk = ConsumerChunkForIndex(cChunk, seqChunkIndex);
        }
        return cChunk.LpElement(seqChunkOffset);
    }

    public void ProducerSet(long sequence, E data) {
        if (sequence < 0) {
            throw new ArgumentException("sequence: " + sequence);
        }
        int seqChunkOffset = (int)(sequence & chunkMask);
        long seqChunkIndex = sequence >> chunkShift;

        MpUnboundedBufferChunk<E> pChunk = LvProducerChunk();
        if (pChunk.LvChunkIndex() != seqChunkIndex) {
            pChunk = ProducerChunkForIndex(pChunk, seqChunkIndex);
        }
        pChunk.SpElement(seqChunkOffset, data);
    }

    public void ConsumerSet(long sequence, E data) {
        if (sequence < 0) {
            throw new ArgumentException("sequence: " + sequence);
        }
        int seqChunkOffset = (int)(sequence & chunkMask);
        long seqChunkIndex = sequence >> chunkShift;

        MpUnboundedBufferChunk<E> cChunk = LvHeadChunk();
        if (cChunk.LvChunkIndex() != seqChunkIndex) {
            cChunk = ConsumerChunkForIndex(cChunk, seqChunkIndex);
        }
        cChunk.SpElement(seqChunkOffset, data);
    }

    public ref E ProducerGetRef(long sequence) {
        if (sequence < 0) {
            throw new ArgumentException("sequence: " + sequence);
        }
        int seqChunkOffset = (int)(sequence & chunkMask);
        long seqChunkIndex = sequence >> chunkShift;

        MpUnboundedBufferChunk<E> pChunk = LvProducerChunk();
        if (pChunk.LvChunkIndex() != seqChunkIndex) {
            pChunk = ProducerChunkForIndex(pChunk, seqChunkIndex);
        }
        return ref pChunk.LpElementRef(seqChunkOffset);
    }

    public ref E ConsumerGetRef(long sequence) {
        if (sequence < 0) {
            throw new ArgumentException("sequence: " + sequence);
        }
        int seqChunkOffset = (int)(sequence & chunkMask);
        long seqChunkIndex = sequence >> chunkShift;

        MpUnboundedBufferChunk<E> cChunk = LvHeadChunk();
        if (cChunk.LvChunkIndex() != seqChunkIndex) {
            cChunk = ConsumerChunkForIndex(cChunk, seqChunkIndex);
        }
        return ref cChunk.LpElementRef(seqChunkOffset);
    }

    /** 获取生产者sequence对应的chunk -- 生产者再获得序号后应当调用该方法触发扩容 */
    public MpUnboundedBufferChunk<E> ProducerChunkForSequence(long sequence) {
        if (sequence < 0) {
            throw new ArgumentException("sequence: " + sequence);
        }
        long seqChunkIndex = sequence >> chunkShift;
        MpUnboundedBufferChunk<E> pChunk = LvProducerChunk();
        if (pChunk.LvChunkIndex() != seqChunkIndex) {
            pChunk = ProducerChunkForIndex(pChunk, seqChunkIndex);
        }
        return pChunk;
    }

    /** 获取消费者sequence对应的chunk */
    public MpUnboundedBufferChunk<E> ConsumerChunkForSequence(long sequence) {
        if (sequence < 0) {
            throw new ArgumentException("sequence: " + sequence);
        }
        long seqChunkIndex = sequence >> chunkShift;
        MpUnboundedBufferChunk<E> cChunk = LvHeadChunk();
        if (cChunk.LvChunkIndex() != seqChunkIndex) {
            cChunk = ConsumerChunkForIndex(cChunk, seqChunkIndex);
        }
        return cChunk;
    }

    /** 获取指定索引的消费者块 -- 当{@link #lvHeadChunk()}不是期望的块时调用。 */
    private MpUnboundedBufferChunk<E> ConsumerChunkForIndex(
        MpUnboundedBufferChunk<E> initialChunk,
        long requiredChunkIndex) {
        // 要保证这里的正确性，生产者在回收chunk后，一定要标记chunkIndex为-1或next为null。
        MpUnboundedBufferChunk<E> currentChunk = initialChunk;
        while (true) {
            if (currentChunk == null) {
                Thread.SpinWait(1);
                currentChunk = LvHeadChunk();
            }
            long currentChunkIndex = currentChunk.LvChunkIndex();
            if (currentChunkIndex == requiredChunkIndex) {
                return currentChunk;
            }
            if (currentChunkIndex < 0
                || currentChunkIndex > requiredChunkIndex) { // 当前块被回收复用
                currentChunk = null;
                continue;
            }
            currentChunk = currentChunk.LvNext(); // nullable；生产者尚未创建，或当前块被回收
        }
    }

    /** 获取指定索引的生产者块 -- 当{@link #lvProducerChunk()}不是期望的块时调用 */
    private MpUnboundedBufferChunk<E> ProducerChunkForIndex(
        MpUnboundedBufferChunk<E> initialChunk,
        long requiredChunkIndex) {
        MpUnboundedBufferChunk<E>? currentChunk = initialChunk;
        // 后跳步数 - 当生产者速度较快时，不同生产者可能处于不同的chunk，因此可能需要后跳
        long jumpBackward;
        while (true) {
            if (currentChunk == null) {
                currentChunk = LvProducerChunk();
            }
            if (currentChunk == ROTATION) { // 其它线程正在执行更新，等待其更新完成
                Thread.SpinWait(1);
                currentChunk = null;
                continue;
            }
            long currentChunkIndex = currentChunk.LvChunkIndex();
            jumpBackward = currentChunkIndex - requiredChunkIndex;
            if (jumpBackward >= 0) {
                break;
            }
            currentChunk = AppendNextChunks(currentChunk, currentChunkIndex, -jumpBackward);
        }
        for (long i = 0; i < jumpBackward; i++) {
            currentChunk = currentChunk.LvPrev()!;
        }
        Debug.Assert(currentChunk.LvChunkIndex() == requiredChunkIndex);
        return currentChunk;
    }

    private MpUnboundedBufferChunk<E>? AppendNextChunks(MpUnboundedBufferChunk<E> currentChunk,
                                                        long currentChunkIndex,
                                                        long chunksToAppend) {
        if (!CasProducerChunk(currentChunk, ROTATION)) {
            return null;
        }
        // 获得更新producerChunk权限，这期间其它生产者需要等待
        for (long i = 1; i <= chunksToAppend; i++) {
            MpUnboundedBufferChunk<E> newChunk = NewOrPooledChunk(currentChunk, currentChunkIndex + i);
            currentChunk.SoNext(newChunk);
            currentChunk = newChunk;
        }
        SoProducerChunk(currentChunk);
        return currentChunk;
    }

    private MpUnboundedBufferChunk<E> NewOrPooledChunk(MpUnboundedBufferChunk<E> prevChunk, long nextChunkIndex) {
        MpUnboundedBufferChunk<E> tailChunk;
        MpUnboundedBufferChunk<E> nextChunk;
        while (true) {
            tailChunk = LvTailChunk();
            // 其它生产者可能正在回收head
            if (tailChunk == ROTATION) {
                Thread.SpinWait(1);
                continue;
            }
            // tail可能包含预分配的块
            if (nextChunkIndex <= tailChunk.LvChunkIndex()) {
                nextChunk = prevChunk.LvNext();
                Debug.Assert(nextChunk != null && nextChunk.LvChunkIndex() == nextChunkIndex);
                return nextChunk;
            }
            // 其它生产者可能正在回收head
            if (!CasTailChunk(tailChunk, ROTATION)) {
                Thread.SpinWait(1);
                continue;
            }
            // 新增块到tail
            nextChunk = new MpUnboundedBufferChunk<E>(ChunkLength, nextChunkIndex, prevChunk);
            nextChunk.Fill(factory);

            nextChunk.SoPrev(tailChunk);
            tailChunk.SoNext(nextChunk);
            SoTailChunk(nextChunk);
            return nextChunk;
        }
    }

    #endregion

    #region recycle

    /**
     * 尝试将head更新到下一个chunk
     * (public以允许用户自行控制回收时机)
     *
     * @param gatingSequence 最慢的消费者进度(已消费)
     * @return 是否成功触发回收
     */
    public bool TryMoveHeadToNext(long gatingSequence) {
        MpUnboundedBufferChunk<E> headChunk = LvHeadChunk();
        MpUnboundedBufferChunk<E> producerChunk = LvProducerChunk();
        if (!IsRecyclable(headChunk, gatingSequence, producerChunk)) {
            return false;
        }
        if (!TryLockHead()) {
            return false;
        }
        // 注意：在竞争lock成功后，head可能是过期的！必须重新检查回收条件 -- 这期间producerChunk的索引不会变化
        headChunk = LvHeadChunk();
        producerChunk = LvProducerChunk();
        if (!IsRecyclable(headChunk, gatingSequence, producerChunk)) {
            UnlockHead();
            return false;
        }
        // 注意：观察到的消费者序号可能跨越了多个块，因此可能需要回收多个块
        MpUnboundedBufferChunk<E> nextChunk = headChunk.LvNext()!;
        nextChunk.SoPrev(null);
        while (IsRecyclable(nextChunk, gatingSequence, producerChunk)) {
            nextChunk = nextChunk.LvNext()!;
            nextChunk.SoPrev(null);
        }
        // 我们立即发布新的head，以允许消费者获取最新的数据
        SoHeadChunk(nextChunk);
        RecycleChunks(headChunk, nextChunk);
        UnlockHead();
        return true;
    }

    private static bool IsRecyclable(MpUnboundedBufferChunk<E> chunk, long gatingSequence,
                                     MpUnboundedBufferChunk<E> producerChunk) {
        // 不可以回收生产者当前块，否则会导致生产者append产生竞争
        return chunk.MaxSequence() <= gatingSequence
               && chunk.LvChunkIndex() < producerChunk.LvChunkIndex(); // ROTATION is ok
    }

    private void RecycleChunks(MpUnboundedBufferChunk<E> headChunk, MpUnboundedBufferChunk<E> nextChunk) {
        MpUnboundedBufferChunk<E> freeChunk = headChunk;
        while (true) {
            MpUnboundedBufferChunk<E> tailChunk = LvTailChunk();
            if (tailChunk == ROTATION) {
                Thread.SpinWait(1); // 生产者正在创建新的chunk
                continue;
            }
            long recyclable = maxPooledChunks - (tailChunk.LvChunkIndex() - nextChunk.LvChunkIndex()) - 1;
            if (recyclable <= 0) { // 这期间chunk只会越来越多，因此无需重试
                break;
            }
            if (!CasTailChunk(tailChunk, ROTATION)) {
                Thread.SpinWait(1); // 生产者正在创建新的chunk
                continue;
            }
            for (long i = 0; (i < recyclable && freeChunk != nextChunk); i++) {
                MpUnboundedBufferChunk<E> tempNext = freeChunk.LvNext()!; // next在前面并未先断开
                freeChunk.SoNext(null);

                freeChunk.SoChunkIndex(tailChunk.LvChunkIndex() + 1); // 消费者可能看见突然变大的id
                freeChunk.SoPrev(tailChunk);
                tailChunk.SoNext(freeChunk);

                tailChunk = freeChunk;
                freeChunk = tempNext;
            }
            SoTailChunk(tailChunk); // cas后不论是否触发回收，都需要写回
            break;
        }
        // 清理剩余块
        while (freeChunk != nextChunk) {
            MpUnboundedBufferChunk<E> tempNext = freeChunk.LvNext()!;
            freeChunk.SoNext(null); // 消费者需要感知到被清理
            freeChunk.Clear();
            freeChunk = tempNext;
        }
    }

    #endregion
}
}