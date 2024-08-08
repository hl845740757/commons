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
/// 
/// </summary>
/// <typeparam name="E"></typeparam>
public sealed class MpUnboundedBufferChunk<E>
{
    private readonly E[] buffer;
    /// <summary>
    /// 已发布的槽位
    /// 注意：与<see cref="MultiProducerSequencer"/>的方案不同，这里发布时是将其标记为<see cref="chunkIndex"/> -- 可避免额外的计算。
    /// </summary>
    private readonly long[] published;

    /** 该chunk的索引 -- volatile读写 */
    private long chunkIndex;
    private volatile MpUnboundedBufferChunk<E>? prev;
    private volatile MpUnboundedBufferChunk<E>? next;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="length"></param>
    /// <param name="chunkIndex"></param>
    /// <param name="prev"></param>
    public MpUnboundedBufferChunk(int length, long chunkIndex, MpUnboundedBufferChunk<E>? prev) {
        this.buffer = new E[length];
        this.published = new long[length];

        if (chunkIndex == 0) { // 其它情况下0可以表示未发布
            Array.Fill(published, -1);
        }

        this.chunkIndex = chunkIndex;
        this.prev = prev;
    }

    #region internal

    /** load plain index */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long LpChunkIndex() {
        return chunkIndex;
    }

    /** store plain index */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SpChunkIndex(long index) {
        chunkIndex = index;
    }

    /** load volatile index */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal long LvChunkIndex() {
        return Volatile.Read(ref chunkIndex);
    }

    /** store ordered index */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SoChunkIndex(long index) {
        Volatile.Write(ref chunkIndex, index);
    }

    /** load volatile next */
    internal MpUnboundedBufferChunk<E>? LvNext() {
        return next;
    }

    /** store ordered next */
    internal void SoNext(MpUnboundedBufferChunk<E>? value) {
        this.next = value;
    }

    /** load volatile prev */
    internal MpUnboundedBufferChunk<E>? LvPrev() {
        return prev;
    }

    /** store ordered prev */
    internal void SoPrev(MpUnboundedBufferChunk<E>? value) {
        this.prev = value;
    }

    #endregion

    /** load plain element ref */
    public ref E LpElementRef(int index) {
        return ref buffer[index];
    }

    /** store plain element */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SpElement(int index, E e) {
        buffer[index] = e;
    }

    /** load plain element */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public E LpElement(int index) {
        return buffer[index];
    }

    /** 将指定槽位标记为已发布 */
    public void Publish(int index) {
        Volatile.Write(ref published[index], LpChunkIndex());
    }

    /** 批量发布数据 */
    public void Publish(int low, int high) {
        long[] published = this.published;
        long chunkIndex = LpChunkIndex();
        while (low < high) {
            published[low++] = chunkIndex;
        }
        Volatile.Write(ref published[high], chunkIndex);
    }

    public bool IsPublished(int index) {
        long flag = Volatile.Read(ref published[index]);
        return flag == LpChunkIndex();
    }

    public int GetHighestPublishedSequence(int low, int high) {
        long[] published = this.published;
        long chunkIndex = LpChunkIndex();
        for (int index = low; index <= high; index++) {
            long flag = Volatile.Read(ref published[index]);
            if (flag != chunkIndex) {
                return index - 1;
            }
        }
        return high;
    }

    /** 获取chunk上数据的最小sequence -- plain内存语义 */
    public long MinSequence() {
        int length = buffer.Length;
        return LpChunkIndex() * length;
    }

    /** 获取chunk上数据的最大sequence -- plain内存语义 */
    public long MaxSequence() {
        int length = buffer.Length;
        return LpChunkIndex() * length + (length - 1);
    }

    /** buffer的长度 */
    public int Length => buffer.Length;

    /** 填充chunk - 使用Plain内存语义 */
    public void Fill(Func<E> factory) {
        for (int i = 0; i < buffer.Length; i++) {
            buffer[i] = factory();
        }
    }

    /** 清理chunk - 使用Plain内存语义 */
    public void Clear() {
        if (RuntimeHelpers.IsReferenceOrContainsReferences<E>()) {
            Array.Fill(buffer, default);
        }
    }
}
}