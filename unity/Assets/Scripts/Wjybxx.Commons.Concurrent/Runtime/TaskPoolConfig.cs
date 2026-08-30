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
using System.Collections.Concurrent;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于配置并发库中的对象池
/// 注：异步状态机方法使用<see cref="PooledTaskAttribute"/>进行配置。
/// </summary>
public static class TaskPoolConfig
{
    private static volatile Func<TaskPoolType, Type, int>? _handler;
    private static readonly ConcurrentDictionary<Key, Item> configDic = new();

    /// <summary>
    /// Type为目标类型的泛型参数
    /// </summary>
    public static Func<TaskPoolType, Type, int>? Handler {
        get => _handler;
        set => _handler = value;
    }

    /// <summary>
    /// 设置该类型的缺省池大小
    /// 本库统一使用int代替void，因此当T为int类型时，应当分配更大的池。
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <param name="poolSize">int和object类型的池大小</param>
    /// <param name="poolSize2">其它类型的池大小</param>
    public static void AddPoolConfig(TaskPoolType poolType, int poolSize, int? poolSize2 = null) {
        Key key = new Key(poolType, (string)null);
        Item item = new Item(poolSize, poolSize2 ?? poolSize / 4);
        configDic[key] = item;
    }

    /// <summary>
    /// 通过命名空间设置对象池的大小（暂不递归匹配）
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <param name="ns">结果命名空间或状态机顶层类的命名空间</param>
    /// <param name="poolSize">int和object类型的池大小</param>
    /// <param name="poolSize2">其它类型的池大小</param>
    public static void AddPoolConfig(TaskPoolType poolType, string ns, int poolSize, int? poolSize2 = null) {
        if (ns == null) throw new ArgumentNullException(nameof(ns));
        Key key = new Key(poolType, ns);
        Item item = new Item(poolSize, poolSize2 ?? poolSize / 4);
        configDic[key] = item;
    }

    /// <summary>
    /// 精确设置某类池的大小
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <param name="poolSize">对象池大小</param>
    /// <typeparam name="T">结果类型</typeparam>
    public static void AddPoolConfig<T>(TaskPoolType poolType, int poolSize) {
        Key key = new Key(poolType, typeof(T));
        Item item = new Item(poolSize, poolSize); // 此处是精确类型
        configDic[key] = item;
    }

    /// <summary>
    /// 获取对象池的缓存池大小。
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <param name="fallback">是否是handler回退</param>
    /// <typeparam name="T">对象的泛型参数类型</typeparam>
    public static int GetPoolSize<T>(TaskPoolType poolType, bool fallback = false) {
        Func<TaskPoolType, Type, int> func = _handler;
        if (func != null && !fallback) {
            return func.Invoke(poolType, typeof(T));
        }
        // 通常使用int代替void，而object适用装箱场景
        Type type = typeof(T);
        bool isIntOrObject = type == typeof(int) || type == typeof(object);
        if (GetItem(poolType, type, out Item item, out bool precise)) {
            return (precise || isIntOrObject) ? item.poolSize : item.poolSize2;
        }
        // 非int/object类型默认不再池化
        if (!isIntOrObject) {
            return 0;
        }
        // 保底方案
        return poolType switch {
            TaskPoolType.ValuePromise => 2000,
            TaskPoolType.PromiseTask => 1000,
            TaskPoolType.ScheduledPromiseTask => 1000,
            TaskPoolType.ManualResetPromise => 200,
            _ => 0
        };
    }

    private static bool GetItem(TaskPoolType poolType, Type resultType,
                                out Item item, out bool precise) {
        precise = false;
        if (configDic.TryGetValue(new Key(poolType, resultType), out item)) { // 精确查询
            precise = true;
            return true;
        }
        if (configDic.TryGetValue(new Key(poolType, resultType.Namespace), out item)) { // 模糊查询 - 暂不递归
            return true;
        }
        if (configDic.TryGetValue(new Key(poolType, (string)null), out item)) { // 模糊查询
            return true;
        }
        item = default;
        return false;
    }

    private readonly struct Item
    {
        public readonly int poolSize;
        public readonly int poolSize2;

        public Item(int poolSize, int poolSize2) {
            this.poolSize = poolSize;
            this.poolSize2 = poolSize2;
        }
    }

    private readonly struct Key : IEquatable<Key>
    {
        private readonly TaskPoolType _poolType;
        private readonly string? _ns; // 命名空间
        private readonly Type? _type;

        public Key(TaskPoolType poolType, Type? type) {
            _poolType = poolType;
            _type = type;
            _ns = null;
        }

        public Key(TaskPoolType poolType, string? ns) {
            _poolType = poolType;
            _ns = ns;
            _type = null;
        }

        public bool Equals(Key other) {
            return _poolType == other._poolType
                   && _ns == other._ns
                   && _type == other._type;
        }

        public override bool Equals(object? obj) {
            return obj is Key other && Equals(other);
        }

        public override int GetHashCode() {
            int hashCode = (int)_poolType;
            hashCode = (hashCode * 397) ^ (_ns != null ? _ns.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (_type != null ? _type.GetHashCode() : 0);
            return hashCode;
        }
    }
}

/// <summary>
/// 任务池类型
/// </summary>
public enum TaskPoolType
{
    /// <summary>
    /// 普通Future的状态机await回调
    /// 注：已不再使用，尽量使用<see cref="ValueFuture"/>。
    /// </summary>
    PromiseMoveNext,
    /// <summary>
    /// ValueFuture状态机任务
    /// 注：已不再使用，改为通过<see cref="TaskPoolConfig"/>在异步方法上配置。
    /// </summary>
    StateMachineTask,

    /// <summary>
    /// 最基础的<see cref="ValuePromise{T}"/>回调
    /// </summary>
    ValuePromise,
    /// <summary>
    /// 池化的普通任务
    /// <see cref="PromiseTask{T}"/>
    /// </summary>
    PromiseTask,
    /// <summary>
    /// 池化的定时任务
    /// <see cref="ScheduledPromiseTask{T}"/>
    /// </summary>
    ScheduledPromiseTask,

    /// <summary>
    /// 手动重置的Promise
    /// <see cref="ManualResetPromise{T}"/>
    /// </summary>
    ManualResetPromise,
}
}