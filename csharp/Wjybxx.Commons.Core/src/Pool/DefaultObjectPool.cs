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
using System.Collections.Generic;
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Pool;

/// <summary>
/// 默认缓存池实现
///
/// <h3>队列 OR 栈</h3>
/// 主要区别：栈结构会频繁使用栈顶元素，而队列结构的元素是平等的。
/// 因此栈结构有以下特性：
/// 1.如果复用对象存在bug，更容易发现。
/// 2.如果池化的对象是List这类会扩容的对象，则只有栈顶部分的对象会扩容较大。
/// </summary>
/// <typeparam name="T"></typeparam>
[NotThreadSafe]
public class DefaultObjectPool<T> : IObjectPool<T>
{
    /** 默认不无限缓存 */
    private const int DefaultPoolSize = 1024;

    private readonly Func<T> _factory;
    private readonly Action<T> _resetHandler;
    private readonly Func<T, bool>? _filter;

    private readonly int _poolSize;
    private readonly Stack<T> _freeObjects;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory">对象创建工厂</param>
    /// <param name="resetHandler">重置方法</param>
    /// <param name="poolSize">池大小；0表示不缓存对象</param>
    /// <param name="filter">回收对象的过滤器</param>
    public DefaultObjectPool(Func<T> factory, Action<T> resetHandler, int poolSize = DefaultPoolSize, Func<T, bool>? filter = null) {
        this._factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this._resetHandler = resetHandler ?? throw new ArgumentNullException(nameof(resetHandler));
        this._poolSize = poolSize;
        this._filter = filter;
        this._freeObjects = new Stack<T>(Math.Clamp(poolSize, 0, 10));
    }

    public int PoolSize => _poolSize;

    public T Acquire() {
        if (_freeObjects.TryPop(out T result)) {
            return result;
        }
        return _factory();
    }

    public void Release(T obj) {
        if (obj == null) {
            throw new ArgumentException("object cannot be null.");
        }
        // 先调用reset，避免reset出现异常导致添加脏对象到缓存池中 -- 断言是否在池中还是有较大开销
        _resetHandler(obj);
        if (_freeObjects.Count < _poolSize && (_filter == null || _filter.Invoke(obj))) {
            _freeObjects.Push(obj);
        }
    }

    public void ReleaseAll(IEnumerable<T?> objects) {
        if (objects == null) {
            throw new ArgumentException("objects cannot be null.");
        }
        Stack<T> freeObjects = this._freeObjects;
        Action<T> resetPolicy = this._resetHandler;
        Func<T, bool>? filter = this._filter;
        int maxCapacity = this._poolSize;
        if (objects is List<T> arrayList) {
            for (int i = 0, n = arrayList.Count; i < n; i++) {
                T obj = arrayList[i];
                if (null == obj) {
                    continue;
                }
                resetPolicy(obj);
                if (freeObjects.Count < maxCapacity && (filter == null || filter.Invoke(obj))) {
                    freeObjects.Push(obj);
                }
            }
        } else {
            foreach (T obj in objects) {
                if (null == obj) {
                    continue;
                }
                resetPolicy(obj);
                if (freeObjects.Count < maxCapacity && (filter == null || filter.Invoke(obj))) {
                    freeObjects.Push(obj);
                }
            }
        }
    }

    public void Clear() {
        _freeObjects.Clear();
    }
}