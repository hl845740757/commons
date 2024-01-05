#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Pool;

#pragma warning disable CS1591
namespace Wjybxx.Commons.IO;

/// <summary>
/// 简单的StringBuilder池实现，非线程安全
/// </summary>
[NotThreadSafe]
public class StringBuilderPool : IObjectPool<StringBuilder>
{
    private readonly int _poolSize;
    private readonly int _initCapacity;
    private readonly int _maxCapacity;
    private readonly Stack<StringBuilder> _freeBuilders;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="poolSize">池大小</param>
    /// <param name="initCapacity">Builder的初始空间</param>
    /// <param name="maxCapacity">Builder的最大空间，超过空间的Builder不会被回收复用</param>
    public StringBuilderPool(int poolSize, int initCapacity, int maxCapacity = int.MaxValue) {
        if (poolSize < 0 || initCapacity < 0 || maxCapacity < 0) {
            throw new ArgumentException(
                $"{nameof(poolSize)}: {poolSize}, {nameof(initCapacity)}: {initCapacity}, {nameof(maxCapacity)}: {maxCapacity}");
        }
        this._poolSize = poolSize;
        this._initCapacity = initCapacity;
        this._maxCapacity = maxCapacity;
        this._freeBuilders = new Stack<StringBuilder>(Math.Clamp(poolSize, 0, 10));
    }

    public StringBuilder Rent() {
        if (_freeBuilders.TryPop(out StringBuilder sb)) {
            return sb;
        }
        return new StringBuilder(_initCapacity);
    }

    public void ReturnOne(StringBuilder sb) {
        if (sb == null) throw new ArgumentNullException(nameof(sb));
        if (_freeBuilders.Count < _poolSize && sb.Length <= _maxCapacity) {
            sb.Length = 0;
            _freeBuilders.Push(sb);
        }
    }

    public void Clear() {
        _freeBuilders.Clear();
    }
}