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
public abstract class ConcurrentObjectPool
{
    private static readonly int SBP_MAX_CAPACITY = EnvironmentUtil.GetIntVar("Wjybxx.Commons.IO.SharedStringBuilderPool.MaxCapacity", 64 * 1024);
    private static readonly int SBP_SIZE = EnvironmentUtil.GetIntVar("Wjybxx.Commons.IO.SharedStringBuilderPool.PoolSize", 64);

    /** 默认的全局StringBuilderPool */
    public static readonly ConcurrentObjectPool<StringBuilder> SharedStringBuilderPool = new ConcurrentObjectPool<StringBuilder>(
        PoolableObjectHandlers.NewStringBuilderHandler(1024, SBP_MAX_CAPACITY), SBP_SIZE);

    /// <summary>
    /// 提供统一API清理对象池
    /// </summary>
    public abstract void Clear();
}

/// <summary>
/// 高性能固定大小对象池
/// (未鉴定归属，可归还外部对象)
/// </summary>
/// <typeparam name="T"></typeparam>
[ThreadSafe]
public class ConcurrentObjectPool<T> : ConcurrentObjectPool, IObjectPool<T> where T : class
{
    private readonly IPoolableObjectHandler<T> _handler;
    private readonly int _initCapacity;
    private readonly MpmcArrayQueue<T> _freeObjects;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="handler">对象创建工厂</param>
    /// <param name="poolSize">池大小</param>
    /// <param name="initCapacity">初始空间参数</param>
    public ConcurrentObjectPool(IPoolableObjectHandler<T> handler, int poolSize = 64, int initCapacity = 0) {
        if (initCapacity < 0) throw new ArgumentException(nameof(initCapacity));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _initCapacity = initCapacity;
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
        return _handler.Create(this, _initCapacity);
    }

    public void Release(T obj) {
        if (obj == null) throw new ArgumentNullException(nameof(obj));
        if (!_handler.Test(obj)) {
            _handler.Destroy(obj);
            return;
        }
        _handler.Reset(obj);
        if (!_freeObjects.Offer(obj)) {
            _handler.Destroy(obj);
        }
    }

    public override void Clear() {
        while (_freeObjects.Poll(out T obj)) {
            _handler.Destroy(obj);
        }
    }

    public void Fill(int count) {
        for (int i = 0; i < count; i++) {
            T obj = _handler.Create(this, _initCapacity);
            if (!_freeObjects.Offer(obj)) {
                _handler.Destroy(obj);
                return;
            }
        }
    }
}