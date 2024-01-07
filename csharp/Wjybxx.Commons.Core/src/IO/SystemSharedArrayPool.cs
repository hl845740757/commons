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
/// 基于对于<see cref="ArrayPool{T}"/>中的共享池进行封装的数组池。
/// </summary>
/// <typeparam name="T"></typeparam>
[ThreadSafe]
public class SystemSharedArrayPool<T> : IArrayPool<T>
{
    public static readonly IArrayPool<T> Shared = new SystemSharedArrayPool<T>(4096);
    private readonly int _initCapacity;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="initCapacity">默认分配空间</param>
    /// <exception cref="ArgumentException"></exception>
    public SystemSharedArrayPool(int initCapacity) {
        if (initCapacity < 0) {
            throw new ArgumentException($"{nameof(initCapacity)}: {initCapacity}");
        }
        _initCapacity = initCapacity;
    }

    public T[] Rent() {
        return ArrayPool<T>.Shared.Rent(_initCapacity);
    }

    public T[] Rent(int minimumLength, bool clear = false) {
        T[] array = ArrayPool<T>.Shared.Rent(minimumLength);
        if (clear) {
            Array.Clear(array);
        }
        return array;
    }

    public void ReturnOne(T[] obj) {
        ArrayPool<T>.Shared.Return(obj);
    }

    public void ReturnOne(T[] obj, bool clear) {
        ArrayPool<T>.Shared.Return(obj, clear);
    }

    public void FreeAll() {
    }
}