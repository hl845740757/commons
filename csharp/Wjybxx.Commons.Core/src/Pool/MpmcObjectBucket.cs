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
#pragma warning disable CS1591

#pragma warning disable CS0169

namespace Wjybxx.Commons.Pool;

/// <summary>
/// 这是一个特定实现的多生产者多消费者的数组队列（MpmcArrayQueue）
///  这里没有强制数组大小必须是2的幂，因为要严格保证池的大小符合预期。
///  (这里的算法参照了Disruptor模块的实现，但针对数组池进行了特殊的修改，但没有做极致的优化)
/// </summary>
public sealed class MpmcObjectBucket<T>
{
    // region padding
    private long p1, p2, p3, p4, p5, p6, p7, p8;
    // endregion

    /** 数组长度 -- 不一定为2的幂 */
    private readonly int length;
    /** 数组池 -- 每一个元素都是数组 */
    private readonly T[] buffer;

    /** 已发布的数组元素 -- 存储的是对应的sequence */
    private readonly long[] published;

    /** 已消费的数组元素 -- 存储的是对应的sequence */
    private readonly long[] consumed;

    // region padding
    private long p11, p12, p13, p14, p15, p16, p17, p18;
    // endregion

    /** 生产者索引 -- volatile读写 */
    private long producerIndex = -1;

    // region padding
    private long p21, p22, p23, p24, p25, p26, p27, p28;
    // endregion

    /** 消费者索引 -- volatile读写 */
    private long consumerIndex = -1;

    // region padding
    private long p31, p32, p33, p34, p35, p36, p37, p38;
    // endregion

    public MpmcObjectBucket(int length) {
        this.length = length;

        this.buffer = new T[length];
        this.published = new long[length];
        this.consumed = new long[length];

        // 需要初始化为-1，0是有效的sequence
        Array.Fill(published, -1);
        Array.Fill(consumed, -1);
    }

    /// <summary>
    /// 桶的大小
    /// </summary>
    public int Length => length;

    /// <summary>
    /// 当前元素数量
    /// </summary>
    public int Count => MathCommon.Clamp(LvProducerIndex() - LvConsumerIndex(), 0, length);

    /** 尝试压入数组 */
    public bool Offer(T element) {
        if (element == null) throw new ArgumentNullException(nameof(element));
        if (length == 0) {
            return false;
        }
        // 先更新生产者索引，然后设置元素，再标记为已生产 -- 实现可见性保证
        long current;
        long next;
        do {
            current = LvProducerIndex();
            next = current + 1;

            long wrapPoint = next - length;
            if (wrapPoint >= 0 && !IsConsumed(wrapPoint)) {
                // 如果尚未被消费，则判断当前是否正在消费，如果正在消费则spin等待
                if (wrapPoint <= LvConsumerIndex()) {
                    next = current; // skip cas
                    continue;
                }
                return false;
            }
        } while ((next == current) || !CasProducerIndex(current, next));

        int index = IndexOfSequence(next);
        SpElement(index, element);
        MarkPublished(next);
        return true;
    }

    /** 尝试弹出元素 */
    public bool Poll(out T result) {
        if (length == 0) {
            result = default;
            return false;
        }
        // 先更新消费者索引，然后设置元素为null，再标记为已消费 -- 实现可见性保证
        long current;
        long next;
        do {
            current = LvConsumerIndex();
            next = current + 1;

            if (!IsPublished(next)) {
                // 如果尚未发布，则判断当前是否正在生产，如果正在生产者则spin等待
                if (next <= LvProducerIndex()) {
                    next = current; // skip cas
                    continue;
                }
                result = default;
                return false;
            }
        } while ((next == current) || !CasConsumerIndex(current, next));

        int index = IndexOfSequence(next);
        result = LpElement(index);
        SpElement(index, default);
        MarkConsumed(next);
        return true;
    }

    #region internal

    /** load volatile producerIndex */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long LvProducerIndex() {
        return Volatile.Read(ref producerIndex);
    }

    /** compare and set producerIndex */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CasProducerIndex(long expect, long newValue) {
        return Interlocked.CompareExchange(ref producerIndex, newValue, expect) == expect;
    }

    /** load volatile consumerIndex */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long LvConsumerIndex() {
        return Volatile.Read(ref consumerIndex);
    }

    /** compare and set consumerIndex */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CasConsumerIndex(long expect, long newValue) {
        return Interlocked.CompareExchange(ref consumerIndex, newValue, expect) == expect;
    }

    /** store plain element -- 可见性由sequence的发布保证 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SpElement(int index, T e) {
        buffer[index] = e;
    }

    /** load plain element */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private T LpElement(int index) {
        return buffer[index];
    }

    /** 将指定槽位标记为已发布 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MarkPublished(long sequence) {
        int index = IndexOfSequence(sequence);
        Volatile.Write(ref published[index], sequence);
    }

    /** 查询指定数据是否已发布 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsPublished(long sequence) {
        int index = IndexOfSequence(sequence);
        long flag = Volatile.Read(ref published[index]);
        return flag == sequence;
    }

    /** 将指定槽位标记为已消费 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void MarkConsumed(long sequence) {
        int index = IndexOfSequence(sequence);
        Volatile.Write(ref consumed[index], sequence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsConsumed(long sequence) {
        int index = IndexOfSequence(sequence);
        long flag = Volatile.Read(ref consumed[index]);
        return flag == sequence;
    }

    /** 获取指定sequence对应的数组下标 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int IndexOfSequence(long sequence) {
        return (int)(sequence % length);
    }

    #endregion
}