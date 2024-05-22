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
using System.Buffers;
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591
namespace Wjybxx.Commons.IO;

/// <summary>
/// 基于对于<see cref="ArrayPool{T}"/>进行封装的数组池。
/// 注意：扩容管理需要用户自行实现。
/// </summary>
/// <typeparam name="T"></typeparam>
[ThreadSafe]
public class DefaultArrayPool<T> : IArrayPool<T>
{
    private readonly int _defCapacity;
    private readonly bool _clear;
    private readonly ArrayPool<T> _arrayPool;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="defCapacity">默认分配空间</param>
    /// <param name="maxCapacity">最近数组长度；超过大小的数组不会放入池中</param>
    /// <param name="clear">数组归还到池中时是否清理</param>
    /// <exception cref="ArgumentException"></exception>
    public DefaultArrayPool(int defCapacity, int maxCapacity, bool clear = false) {
        if (defCapacity < 0 || maxCapacity < 0) {
            throw new ArgumentException($"{nameof(defCapacity)}: {defCapacity}, {nameof(maxCapacity)}: {maxCapacity}");
        }
        _defCapacity = defCapacity;
        _clear = clear;
        _arrayPool = ArrayPool<T>.Create(maxCapacity, 16);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="arrayPool">被代理的池</param>
    /// <param name="defCapacity">默认分配空间</param>
    /// <param name="clear">数组归还到池中时是否清理</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public DefaultArrayPool(ArrayPool<T> arrayPool, int defCapacity, bool clear = false) {
        if (defCapacity < 0) {
            throw new ArgumentException($"{nameof(defCapacity)}: {defCapacity}");
        }
        _defCapacity = defCapacity;
        _clear = clear;
        _arrayPool = arrayPool ?? throw new ArgumentNullException(nameof(arrayPool));
    }

    public T[] Acquire() {
        return _arrayPool.Rent(_defCapacity);
    }

    public T[] Acquire(int minimumLength, bool clear = false) {
        T[] array = _arrayPool.Rent(minimumLength);
        if (!this._clear && clear) { // 默认不清理的情况下用户请求有效
            Array.Clear(array);
        }
        return array;
    }

    public void Release(T[] obj) {
        _arrayPool.Return(obj, _clear);
    }

    public void Release(T[] obj, bool clear) {
        _arrayPool.Return(obj, _clear || clear); // 默认不清理的情况下用户请求有效
    }

    public void Clear() {
    }
}