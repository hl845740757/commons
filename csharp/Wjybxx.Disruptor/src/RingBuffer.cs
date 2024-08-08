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

#pragma warning disable CS0169

namespace Wjybxx.Disruptor
{
/// <summary>
/// 环形缓冲区
/// </summary>
/// <typeparam name="E"></typeparam>
public sealed class RingBuffer<E> : DataProvider<E>
{
    /** 前后缓存行填充的元素元素 */
    private const int BUFFER_PAD = 16;

    // region padding
    private long p1, p2, p3, p4, p5, p6, p7;
    // endregion

    /**
     * 索引掩码，表示后X位是有效数字(截断)。位运算代替取余快速计算插槽索引
     * (需要放在数组前面充当缓存行填充)
     */
    private readonly long indexMask;
    /**
     * 事件对象数组，大于真正需要的容量，采用了缓存行填充减少伪共享。
     */
    private readonly E[] entries;
    /**
     * 缓存有效空间大小(必须是2的整次幂，-1就是掩码)
     * (使用long类型充当填充)
     */
    private readonly long bufferLength;

    // region padding
    private long p11, p12, p13, p14, p15, p16, p17;
    // endregion

    public RingBuffer(Func<E> eventFactory, int bufferLength) {
        if (eventFactory == null) throw new ArgumentNullException(nameof(eventFactory));
        if (!Util.IsPowerOfTwo(bufferLength)) {
            throw new ArgumentException("bufferSize must be a power of 2");
        }
        // 前16和后16个用于缓存行填充 -- 32位VM上Object4字节，泛型使用sizeof不安全
        this.entries = new E[bufferLength + BUFFER_PAD * 2];
        this.bufferLength = bufferLength;
        this.indexMask = bufferLength - 1;
        // 预填充数据
        Fill(eventFactory);
    }

    private void Fill(Func<E> eventFactory) {
        for (int i = 0; i < bufferLength; i++) {
            entries[BUFFER_PAD + i] = eventFactory();
        }
    }

    #region internal

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal E GetElement(long sequence) {
        int index = (int)(sequence & indexMask);
        return entries[BUFFER_PAD + index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void SetElement(long sequence, E element) {
        int index = (int)(sequence & indexMask);
        entries[BUFFER_PAD + index] = element;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ref E GetElementRef(long sequence) {
        int index = (int)(sequence & indexMask);
        return ref entries[BUFFER_PAD + index];
    }

    #endregion

    public int BufferLength => (int)bufferLength;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public E Get(long sequence) {
        return GetElement(sequence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public E ProducerGet(long sequence) {
        return GetElement(sequence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public E ConsumerGet(long sequence) {
        return GetElement(sequence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ProducerSet(long sequence, E data) {
        SetElement(sequence, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ConsumerSet(long sequence, E data) {
        SetElement(sequence, data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref E ProducerGetRef(long sequence) {
        return ref GetElementRef(sequence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref E ConsumerGetRef(long sequence) {
        return ref GetElementRef(sequence);
    }
}
}