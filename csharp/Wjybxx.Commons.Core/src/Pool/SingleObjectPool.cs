#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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
using System.Collections.ObjectModel;
using System.Diagnostics;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Pool;

/// <summary>
/// 只缓存单个对象对象池
/// 相比直接使用共享对象，使用该缓存池可避免递归调用带来的bug
/// </summary>
/// <typeparam name="T"></typeparam>
public class SingleObjectPool<T> : IObjectPool<T> where T : class
{
    private readonly Func<T> _factory;
    private readonly Action<T> _resetPolicy;
    private T? _value;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory">对象创建工厂</param>
    /// <param name="resetPolicy">重置方法</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SingleObjectPool(Func<T> factory, Action<T> resetPolicy) {
        this._factory = factory ?? throw new ArgumentNullException(nameof(factory));
        this._resetPolicy = resetPolicy ?? throw new ArgumentNullException(nameof(resetPolicy));
    }

    public T Rent() {
        T result = this._value;
        if (result != null) {
            this._value = null;
        } else {
            result = _factory();
        }
        return result;
    }

    public void ReturnOne(T obj) {
        if (obj == null) {
            throw new ArgumentException("object cannot be null.");
        }
        Debug.Assert(obj != this._value);
        _resetPolicy(obj);
        this._value = obj;
    }

    public void ReturnAll(Collection<T?> objects) {
        if (objects == null) {
            throw new ArgumentException("objects cannot be null.");
        }
        foreach (T obj in objects) {
            if (null == obj) {
                continue;
            }
            Debug.Assert(obj != this._value);
            _resetPolicy(obj);
            this._value = obj;
        }
    }

    public int MaxCount => 1;

    public int IdleCount => _value == null ? 0 : 1;

    public void Clear() {
        _value = null;
    }
}