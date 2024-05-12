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
namespace Wjybxx.Commons.Pool;

/// <summary>
/// 基于<see cref="ArrayPool{T}"/>封装实现的对象池，获得了系统库的高性能和线程安全性。
/// </summary>
/// <typeparam name="T"></typeparam>
[Beta]
[ThreadSafe]
public class ConcurrentObjectPool<T> : IObjectPool<RecycleHandle<T>> where T : class
{
    private readonly Func<T> _factory;
    private readonly Action<T> _resetPolicy;
    private readonly Func<T, bool>? _filter;
    private readonly ArrayPool<T> _arrayPool = ArrayPool<T>.Create(2, 64);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory">对象创建工厂</param>
    /// <param name="resetPolicy">重置方法</param>
    /// <param name="filter">回收对象的过滤器</param>
    public ConcurrentObjectPool(Func<T> factory, Action<T> resetPolicy, Func<T, bool>? filter = null) {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _resetPolicy = resetPolicy ?? throw new ArgumentNullException(nameof(resetPolicy));
        _filter = filter;
    }

    public RecycleHandle<T> Rent() {
        T[] array = _arrayPool.Rent(1);
        if (array[0] == null) {
            array[0] = _factory.Invoke();
        }
        return new RecycleHandle<T>(array[0], array, this);
    }

    public void ReturnOne(RecycleHandle<T> obj) {
        var array = (T[])obj.ctx;
        if (_filter == null || _filter.Invoke(obj.value)) {
            _resetPolicy.Invoke(obj.value);
        } else {
            array[0] = null; // 清理对象
        }
        // 数组必须归还
        _arrayPool.Return(array);
    }

    public void FreeAll() {
    }
}