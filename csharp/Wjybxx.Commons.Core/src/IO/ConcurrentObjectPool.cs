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
using System.Text;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Pool;

#pragma warning disable CS1591
namespace Wjybxx.Commons.IO;

/// <summary>
/// 提供全局常量支持
/// </summary>
public class ConcurrentObjectPool
{
    /** 默认的全局StringBuilderPool */
    public static readonly ConcurrentObjectPool<StringBuilder> SharedStringBuilderPool = new ConcurrentObjectPool<StringBuilder>(
        () => new StringBuilder(1024),
        sb => sb.Clear(),
        64,
        sb => sb.Capacity >= 1024 && sb.Capacity <= 64 * 1024
    );
}

/// <summary>
/// 高性能固定大小对象池
/// (未鉴定归属，可归还外部对象)
/// </summary>
/// <typeparam name="T"></typeparam>
[ThreadSafe]
public class ConcurrentObjectPool<T> : IObjectPool<T> where T : class
{
    private static readonly Action<T> DoNothing = obj => { };

    private readonly Func<T> _factory;
    private readonly Action<T> _resetPolicy;
    private readonly Func<T, bool>? _filter;
    private readonly MpmcArrayQueue<T> _freeObjects;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory">对象创建工厂</param>
    /// <param name="resetPolicy">重置方法</param>
    /// <param name="poolSize">池大小</param>
    /// <param name="filter">回收对象的过滤器</param>
    public ConcurrentObjectPool(Func<T> factory, Action<T>? resetPolicy, int poolSize = 64, Func<T, bool>? filter = null) {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _resetPolicy = resetPolicy ?? DoNothing;
        _filter = filter;
        _freeObjects = new MpmcArrayQueue<T>(poolSize);
    }

    /// <summary>
    /// 对象池大小
    /// </summary>
    public int PoolSize => _freeObjects.Length;

    /// <summary>
    /// 可用对象数
    /// (注意：这只是一个估值，通常仅用于debug和测试用例)
    /// </summary>
    /// <returns></returns>
    public int AvailableCount() => _freeObjects.Count;

    public T Acquire() {
        if (_freeObjects.Poll(out T result)) {
            return result;
        }
        return _factory.Invoke();
    }

    public void Release(T obj) {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        _resetPolicy.Invoke(obj);
        if (_filter == null || _filter.Invoke(obj)) {
            _freeObjects.Offer(obj);
        }
    }

    public void Clear() {
        while (_freeObjects.Poll(out T _)) {
        }
    }

    public void Fill(int count) {
        for (int i = 0; i < count; i++) {
            if (!_freeObjects.Offer(_factory.Invoke())) {
                return;
            }
        }
    }
}