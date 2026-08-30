#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using System.Runtime.InteropServices;
using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于多线程下
/// </summary>
[StructLayout(LayoutKind.Explicit)]
public struct PaddedInt32
{
    [FieldOffset(0)]
    private readonly long lhsPadding;

    [FieldOffset(64 - 4)] // 前后各填充60个字节
    private int _value;

    [FieldOffset(124 - 8)]
    private readonly long rhsPadding;

    public PaddedInt32(int value) {
        _value = value;
        lhsPadding = 0;
        rhsPadding = 0;
    }

    /** volatile读 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetVolatile() {
        return Volatile.Read(ref _value);
    }

    /** volatile写 - 会插入写屏障，且尝试刷新缓存 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetVolatile(int value) {
        Volatile.Write(ref _value, value);
    }

    /** acquire模式读 - 会插入读屏障 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetAcquire() {
        return Volatile.Read(ref _value); // C#暂无acquire和release内存语言支持
    }

    /** release模式写 - 会插入写屏障，但不立即刷新缓存 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetRelease(int value) {
        Volatile.Write(ref _value, value); // C#暂无acquire和release内存语言支持
    }

    /** 无内存语义读 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetPlain() {
        return _value;
    }

    /** 无内存语义写 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetPlain(int value) {
        _value = value;
    }

    /** 原子+1 并返回+1 后的结果 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IncrementAndGet() {
        return AddAndGet(1);
    }

    /** 原子+1 并返回+1 前的结果 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetAndIncrement() {
        return GetAndAdd(1);
    }

    /** 原子-1 并返回-1 后的结果 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DecrementAndGet() {
        return AddAndGet(-1);
    }

    /** 原子-1 并返回-1 前的结果 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetAndDecrement() {
        return GetAndAdd(-1);
    }

    /** 原子加上给定数并返回增加后的值 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int AddAndGet(int increment) {
        return Interlocked.Add(ref _value, increment);
    }

    /** 原子加上给定数并返回增加前的值 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetAndAdd(int increment) {
        return Interlocked.Add(ref _value, increment) - increment;
    }

    /// <summary>
    /// 交换内存地址的值
    /// </summary>
    /// <param name="value">新值</param>
    /// <returns>地址上的旧值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Exchange(int value) {
        return Interlocked.Exchange(ref _value, value);
    }

    /// <summary>
    /// 原子比较更新
    /// </summary>
    /// <param name="expectedValue"></param>
    /// <param name="newValue"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool CompareAndSet(int expectedValue, int newValue) {
        return Interlocked.CompareExchange(ref _value, newValue, expectedValue) == expectedValue;
    }

    /// <summary>
    /// 比较并交换
    /// </summary>
    /// <param name="expectedValue">期望值</param>
    /// <param name="newValue">要设置的值</param>
    /// <returns>地址上的旧值</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareAndExchange(int expectedValue, int newValue) {
        // 按照C#的编程习惯，比较数放在末；唯一的好处可能就是进行==比较时，两个值是挨着的。
        // CompareAndExchange(newValue, expectedValue) == expectedValue
        return Interlocked.CompareExchange(ref _value, newValue, expectedValue);
    }

    public override string ToString() {
        return GetVolatile().ToString();
    }
}
}