#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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

using System.Threading;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 
/// </summary>
public sealed class AtomicLong
{
    /// <summary>
    /// C#的volatile不能直接修饰64位的值，简直太坑
    /// </summary>
    private long _value;

    public AtomicLong(long value) {
        _value = value;
    }

    public long Value {
        get => Volatile.Read(ref _value);
        set => Volatile.Write(ref _value, value);
    }

    public long Increment() {
        return Interlocked.Increment(ref _value);
    }

    public long Decrement() {
        return Interlocked.Decrement(ref _value);
    }

    public long GetAndIncrement() {
        return Interlocked.Increment(ref _value) - 1;
    }

    public long GetAndDecrement() {
        return Interlocked.Decrement(ref _value) + 1;
    }

    public long GetAndAdd(long delta) {
        return Interlocked.Add(ref _value, delta) - delta;
    }

    public long AddAndGet(long delta) {
        return Interlocked.Add(ref _value, delta);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value">新值</param>
    /// <returns>地址上的旧值</returns>
    public long GetAndSet(long value) {
        return Interlocked.Exchange(ref _value, value);
    }

    /// <summary>
    /// </summary>
    /// <param name="value">要设置的值</param>
    /// <param name="comparand">比较数</param>
    /// <returns>如果更新成功则返回true</returns>
    public bool CompareAndSet(long value, long comparand) {
        return Interlocked.CompareExchange(ref _value, value, comparand) == comparand;
    }

    /// <summary>
    /// 按照C#的编程习惯，比较数放在末；唯一的好处可能就是进行==比较时，两个值是挨着的。
    /// <code>
    /// CompareAndExchange(newValue, expectedValue) == expectedValue
    /// </code>
    /// </summary>
    /// <param name="value">要设置的值</param>
    /// <param name="comparand">比较数</param>
    /// <returns>地址上的旧值</returns>
    public long CompareAndExchange(long value, long comparand) {
        return Interlocked.CompareExchange(ref _value, value, comparand);
    }
}
}