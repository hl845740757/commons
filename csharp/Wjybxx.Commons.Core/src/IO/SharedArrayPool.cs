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
using Wjybxx.Commons.Pool;

#pragma warning disable CS1591
namespace Wjybxx.Commons.IO;

/// <summary>
/// 基于对于<see cref="ArrayPool{T}"/>进行适配的数组池。
/// </summary>
/// <typeparam name="T"></typeparam>
public class SharedArrayPool<T> : IObjectPool<T[]>
{
    private static readonly ArrayPool<T> Shared = ArrayPool<T>.Shared;
    private readonly int _minimumLength;

    public SharedArrayPool(int minimumLength) {
        if (minimumLength < 0) {
            throw new ArgumentException(nameof(minimumLength));
        }
        this._minimumLength = minimumLength;
    }

    public T[] Rent(int minimumLength) {
        return Shared.Rent(minimumLength);
    }

    public T[] Rent() {
        return Shared.Rent(_minimumLength);
    }

    public void ReturnOne(T[] obj) {
        Shared.Return(obj);
    }

    public void Clear() {
    }
}