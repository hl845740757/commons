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
internal static class Util
{
    /** 缓存行大小 */
    public const int CacheLineSize = 64;

    /** 判断一个值是否是2的整次幂 */
    public static bool IsPowerOfTwo(int x) {
        return x > 0 && (x & (x - 1)) == 0;
    }

    public static int NextPowerOfTwo(int num) {
        if (num < 1) {
            return 2; // disruptor模块特意修正为2
        }
        // https://acius2.blogspot.com/2007/11/calculating-next-power-of-2.html
        // C#未提供获取前导0数量的接口，因此我们选用该算法
        // 先减1，兼容自身已经是2的整次幂的情况；然后通过移位使得后续bit全部为1，再加1即获得结果
        num--;
        num = (num >> 1) | num;
        num = (num >> 2) | num;
        num = (num >> 4) | num;
        num = (num >> 8) | num;
        num = (num >> 16) | num;
        return ++num;
    }

    public static int Log2(int i) {
        int r = 0;
        while ((i >>= 1) != 0) {
            ++r;
        }
        return r;
    }

    public static long GetMinimumSequence(Sequence[] sequences) {
        int n = sequences.Length;
        if (n == 1) { // 1的概率极高
            return sequences[0].GetVolatile();
        }
        long minimum = long.MaxValue;
        for (int i = 0; i < n; i++) {
            long value = sequences[i].GetVolatile();
            minimum = Math.Min(minimum, value);
        }
        return minimum;
    }

    public static long GetMinimumSequence(Sequence[] sequences, long minimum) {
        int n = sequences.Length;
        if (n == 1) { // 1的概率极高
            return Math.Min(minimum, sequences[0].GetVolatile());
        }
        for (int i = 0; i < n; i++) {
            long value = sequences[i].GetVolatile();
            minimum = Math.Min(minimum, value);
        }
        return minimum;
    }

    public static long GetMinimumSequence(SequenceBarrier[] barriers) {
        int n = barriers.Length;
        if (n == 1) { // 1的概率极高
            return barriers[0].Sequence();
        }
        long minimum = long.MaxValue;
        for (int i = 0; i < n; i++) {
            long value = barriers[i].Sequence();
            minimum = Math.Min(minimum, value);
        }
        return minimum;
    }

    public static long GetMinimumSequence(SequenceBarrier[] barriers, long minimum) {
        int n = barriers.Length;
        if (n == 1) { // 1的概率极高
            return Math.Min(minimum, barriers[0].Sequence());
        }
        for (int i = 0; i < n; i++) {
            long value = barriers[i].Sequence();
            minimum = Math.Min(minimum, value);
        }
        return minimum;
    }

    /// <summary>
    /// 原子方式添加屏障
    /// </summary>
    /// <param name="location"></param>
    /// <param name="current">barrier的持有者</param>
    /// <param name="barriersToAdd"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public static void AddBarriers(ref SequenceBarrier[] location,
                                   SequenceBarrier current,
                                   SequenceBarrier[] barriersToAdd) {
        if (barriersToAdd == null) throw new ArgumentNullException(nameof(barriersToAdd));
        // 注意：C#使用ref传址后，内存语义需要显式控制
        long cursorSequence;
        SequenceBarrier[] oldBarriers;
        SequenceBarrier[] newBarriers;
        do {
            oldBarriers = Volatile.Read(ref location); // 这里使用volatile更容易成功
            newBarriers = CopyOf(oldBarriers!, oldBarriers!.Length + barriersToAdd.Length);
            cursorSequence = current.Sequence();

            // 这里对新的屏障进行初始化，仅用于避免阻塞当前屏障；
            // 否则一但更新成功，当前屏障必须等待新的屏障序号更新为最新值
            for (int index = oldBarriers.Length; index < barriersToAdd.Length; index++) {
                SequenceBarrier barrier = barriersToAdd[index];
                barrier.Claim(cursorSequence);
                newBarriers[index++] = barrier;
            }
        } while (Interlocked.CompareExchange(ref location, newBarriers, oldBarriers) != oldBarriers);
    }

    /// <summary>
    /// 原子方式删除屏障
    /// </summary>
    /// <param name="location"></param>
    /// <param name="current">barrier的持有者</param>
    /// <param name="barrier"></param>
    /// <returns></returns>
    public static bool RemoveBarrier(ref SequenceBarrier[] location,
                                     SequenceBarrier current,
                                     SequenceBarrier barrier) {
        int numToRemove;
        SequenceBarrier[] oldBarriers;
        SequenceBarrier[] newBarriers;
        do {
            oldBarriers = Volatile.Read(ref location); // 这里使用volatile更容易成功
            numToRemove = CountMatching(oldBarriers!, barrier);

            if (0 == numToRemove) {
                break;
            }

            int oldSize = oldBarriers.Length;
            newBarriers = new SequenceBarrier[oldSize - numToRemove];

            for (int i = 0, pos = 0; i < oldSize; i++) {
                SequenceBarrier testSequence = oldBarriers[i];
                if (!ReferenceEquals(barrier, testSequence)) {
                    newBarriers[pos++] = testSequence;
                }
            }
        } while (Interlocked.CompareExchange(ref location, newBarriers, oldBarriers) != oldBarriers);

        return numToRemove != 0;
    }

    private static int CountMatching(SequenceBarrier[] values, SequenceBarrier toMatch) {
        int numToRemove = 0;
        foreach (SequenceBarrier value in values) {
            if (ReferenceEquals(value, toMatch)) { // Specifically uses identity
                numToRemove++;
            }
        }
        return numToRemove;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="n">要申请的序号数量</param>
    /// <param name="timeout">超时时间</param>
    /// <param name="barrier">生产者屏障</param>
    /// <param name="spinIterations">生产者自旋参数</param>
    /// <returns></returns>
    public static long? TryNext(int n, TimeSpan timeout, ProducerBarrier barrier, int spinIterations) {
        long? sequence = barrier.TryNext(n);
        if (sequence.HasValue) {
            return sequence;
        }
        long current = SystemMillis();
        long deadline = current + (long)timeout.TotalMilliseconds;
        if (deadline <= current) {
            return null;
        }

        if (spinIterations > 0) {
            do {
                Thread.SpinWait(spinIterations);
                sequence = barrier.TryNext(n);
                if (sequence.HasValue) {
                    return sequence;
                }
            } while (SystemMillis() < deadline);
        } else {
            bool interrupted = false;
            do {
                try {
                    Thread.Sleep(1);
                }
                catch (ThreadInterruptedException) {
                    interrupted = true;
                }

                sequence = barrier.TryNext(n);
                if (sequence.HasValue) {
                    return sequence;
                }
            } while ((current = SystemMillis()) < deadline);
            if (interrupted) {
                Thread.CurrentThread.Interrupt();
            }
        }
        return null;
    }

    /// <summary>
    /// 系统毫秒时间戳
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long SystemMillis() => DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;

    /// <summary>
    /// TimeSpan转换毫秒时间
    /// </summary>
    /// <param name="timeout"></param>
    /// <param name="min"></param>
    /// <returns></returns>
    public static int ToTimeoutMilliseconds(TimeSpan timeout, int min = 0) {
        return (int)Math.Clamp(timeout.TotalMilliseconds, min, int.MaxValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static T[] CopyOf<T>(T[] src, int newLen) {
        T[] result = new T[newLen];
        Array.Copy(src, 0, result, 0, newLen);
        return result;
    }

    public static void CheckNullElements<T>(T[] array, string? name) {
        foreach (T element in array) {
            if (element == null) {
                throw new ArgumentException(($"{name ?? "array"} contains null elements"));
            }
        }
    }
}
}