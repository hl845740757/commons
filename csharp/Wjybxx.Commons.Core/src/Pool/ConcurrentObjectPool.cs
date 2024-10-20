﻿#region LICENSE

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

namespace Wjybxx.Commons.Pool
{
/// <summary>
/// 提供全局常量支持
/// </summary>
public abstract class ConcurrentObjectPool
{
    private static readonly int SBP_MAX_CAPACITY = EnvironmentUtil.GetIntVar("Wjybxx.Commons.IO.SharedStringBuilderPool.MaxCapacity", 64 * 1024);
    private static readonly int SBP_SIZE = EnvironmentUtil.GetIntVar("Wjybxx.Commons.IO.SharedStringBuilderPool.PoolSize", 64);

    /** 默认的全局StringBuilderPool */
    public static readonly ConcurrentObjectPool<StringBuilder> SharedStringBuilderPool = new ConcurrentObjectPool<StringBuilder>(
        () => new StringBuilder(1024),
        sb => sb.Clear(),
        SBP_SIZE,
        sb => sb.Capacity >= 1024 && sb.Capacity <= SBP_MAX_CAPACITY);

    /// <summary>
    /// 提供统一API清理对象池
    /// </summary>
    public abstract void Clear();
}

/// <summary>
/// 高性能固定大小对象池
/// (未鉴定归属，可归还外部对象，适用简单场景)
/// </summary>
/// <typeparam name="T"></typeparam>
[ThreadSafe]
public class ConcurrentObjectPool<T> : ConcurrentObjectPool, IObjectPool<T> where T : class
{
    private static readonly Action<T> DO_NOTHING = _ => { };

    private readonly Func<T> _factory;
    private readonly Action<T> _resetHandler;
    private readonly Func<T, bool>? _filter;
    private readonly MpmcObjectBucket<T> _freeObjects;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory">对象创建工厂</param>
    /// <param name="resetHandler">重置方法</param>
    /// <param name="poolSize">池大小；0表示不缓存对象</param>
    /// <param name="filter">回收对象的过滤器</param>
    public ConcurrentObjectPool(Func<T> factory, Action<T>? resetHandler, int poolSize = 64, Func<T, bool>? filter = null) {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _resetHandler = resetHandler ?? DO_NOTHING;
        _filter = filter;
        _freeObjects = new MpmcObjectBucket<T>(poolSize);
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
        _resetHandler.Invoke(obj);
        if (_filter == null || _filter.Invoke(obj)) {
            _freeObjects.Offer(obj);
        }
    }

    public override void Clear() {
        while (_freeObjects.Poll(out T _)) {
        }
    }
}
}