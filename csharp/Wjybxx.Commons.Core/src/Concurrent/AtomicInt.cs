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
/// 封装原子更新，适配Java开发习惯
/// </summary>
public sealed class AtomicInt
{
    private volatile int _value;

    public AtomicInt() {
    }

    public AtomicInt(int value) {
        _value = value;
    }

    public int Value {
        get => _value;
        set => _value = value;
    }

    public int Increment() {
        return Interlocked.Increment(ref _value);
    }

    public int Decrement() {
        return Interlocked.Decrement(ref _value);
    }

    public int GetAndIncrement() {
        return Interlocked.Increment(ref _value) - 1;
    }

    public int GetAndDecrement() {
        return Interlocked.Decrement(ref _value) + 1;
    }

    public int GetAndAdd(int delta) {
        return Interlocked.Add(ref _value, delta) - delta;
    }

    public int AddAndGet(int delta) {
        return Interlocked.Add(ref _value, delta);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value">新值</param>
    /// <returns>地址上的旧值</returns>
    public int GetAndSet(int value) {
        return Interlocked.Exchange(ref _value, value);
    }

    /// <summary>
    /// </summary>
    /// <param name="value">要设置的值</param>
    /// <param name="comparand">比较数</param>
    /// <returns>如果更新成功则返回true</returns>
    public bool CompareAndSet(int value, int comparand) {
        return Interlocked.CompareExchange(ref _value, value, comparand) == comparand;
    }

    /// <summary>
    /// 按照C#的编程习惯，比较数放在末尾；唯一的好处可能就是进行==比较时，两个值是挨着的。
    /// <code>
    /// CompareAndExchange(newValue, expectedValue) == expectedValue
    /// </code>
    /// </summary>
    /// <param name="value">要设置的值</param>
    /// <param name="comparand">比较数</param>
    /// <returns>地址上的旧值</returns>
    public int CompareAndExchange(int value, int comparand) {
        return Interlocked.CompareExchange(ref _value, value, comparand);
    }
}
}