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
using System.Diagnostics;
using System.Threading;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons.Pool
{
/// <summary>
/// 只缓存单个对象对象池
///
/// 1.相比直接使用共享对象，使用该缓存池可避免递归调用带来的bug
/// 2.新版本支持多线程下调用
/// </summary>
/// <typeparam name="T"></typeparam>
[ThreadSafe]
public class SingleObjectPool<T> : IObjectPool<T> where T : class
{
    private readonly Func<T> _factory;
    private readonly Action<T> _cleaner;
    private readonly Func<T, bool>? _filter;
    private volatile T? _value;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory">对象创建工厂</param>
    /// <param name="cleaner">重置方法</param>
    /// <param name="filter">回收对象的过滤器</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SingleObjectPool(Func<T> factory, Action<T> cleaner, Func<T, bool>? filter = null) {
        this._factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this._cleaner = cleaner ?? throw new ArgumentNullException(nameof(cleaner));
        this._filter = filter;
    }

    public T Acquire() {
        T result = Interlocked.Exchange(ref _value, null);
        return result != null ? result : _factory();
    }

    public void Release(T obj) {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        // Debug.Assert(obj != this._value);
        _cleaner(obj);
        if (_filter == null || _filter.Invoke(obj)) {
            this._value = obj;
        }
    }

    public void Clear() {
        _value = null;
    }
}
}