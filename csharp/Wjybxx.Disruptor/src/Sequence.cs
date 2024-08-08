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

using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS0169
namespace Wjybxx.Disruptor
{
/// <summary>
/// 序列，用于追踪RingBuffer和EventProcessor的进度，表示生产/消费进度。
/// </summary>
public sealed class Sequence
{
    private const long INITIAL_VALUE = -1L;

    // region padding
    private long p1, p2, p3, p4, p5, p6, p7;
    // endregion

    /** volatile读写 */
    private long _value;

    // region padding
    private long p11, p12, p13, p14, p15, p16, p17;
    // endregion

    public Sequence(long value = INITIAL_VALUE) {
        this._value = value;
    }

    /** volatile读 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetVolatile() {
        return Volatile.Read(ref _value);
    }

    /** volatile写 - 会插入写屏障，且尝试刷新缓存 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVolatile(long value) {
        Volatile.Write(ref _value, value);
    }

    /** acquire模式读 - 会插入读屏障 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetAcquire() {
        return Volatile.Read(ref _value); // C#暂无acquire和release内存语言支持
    }

    /** release模式写 - 会插入写屏障，但不立即刷新缓存 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRelease(long value) {
        Volatile.Write(ref _value, value); // C#暂无acquire和release内存语言支持
    }

    /** 无内存语义读 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetPlain() {
        return _value;
    }

    /** 无内存语义写 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPlain(long value) {
        _value = value;
    }

    /** 原子比较更新 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CompareAndSet(long expectedValue, long newValue) {
        return Interlocked.CompareExchange(ref _value, newValue, expectedValue) == expectedValue;
    }

    /** 原子+1 并返回+1 后的结果 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long IncrementAndGet() {
        return AddAndGet(1L);
    }

    /** 原子+1 并返回+1 前的结果 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetAndIncrement() {
        return GetAndAdd(1L);
    }

    /** 原子-1 并返回-1 后的结果 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long DecrementAndGet() {
        return AddAndGet(-1L);
    }

    /** 原子-1 并返回-1 前的结果 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetAndDecrement() {
        return GetAndAdd(-1L);
    }

    /** 原子加上给定数并返回增加后的值 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long AddAndGet(long increment) {
        return Interlocked.Add(ref _value, increment);
    }

    /** 原子加上给定数并返回增加前的值 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long GetAndAdd(long increment) {
        return Interlocked.Add(ref _value, increment) - increment;
    }

    public override string ToString() {
        return GetVolatile().ToString();
    }
}
}