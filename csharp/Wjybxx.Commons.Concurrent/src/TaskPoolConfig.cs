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
/// 用于配置工具库中的对象池大小
/// </summary>
public static class TaskPoolConfig
{
    private static volatile Func<TaskPoolType, Type, int>? poolSizeCalculator;
    private static readonly ConcurrentDictionary<Key, Item> configDic = new();

    public static Func<TaskPoolType, Type, int>? PoolSizeCalculator {
        get => poolSizeCalculator;
        set => poolSizeCalculator = value;
    }

    /// <summary>
    /// 本库统一使用int代替void，因此当T为int类型时，应当分配更大的池。
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <param name="poolSize">int和object类型的池大小</param>
    /// <param name="poolSize2">其它类型的池大小</param>
    public static void AddPoolConfig(TaskPoolType poolType, int poolSize, int? poolSize2 = null) {
        Key key = new Key(poolType, null);
        Item item = new Item(poolSize, poolSize2 ?? poolSize / 4);
        configDic[key] = item;
    }

    /// <summary>
    /// 通过命名空间或类全限定名设置对象池的大小，适用于状态机
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <param name="ns">结果命名空间或状态机顶层类的命名空间</param>
    /// <param name="poolSize">int和object类型的池大小</param>
    /// <param name="poolSize2">其它类型的池大小</param>
    public static void AddPoolConfig(TaskPoolType poolType, string ns, int poolSize, int? poolSize2 = null) {
        if (ns == null) throw new ArgumentNullException(nameof(ns));
        Key key = new Key(poolType, ns, null);
        Item item = new Item(poolSize, poolSize2 ?? poolSize / 4);
        configDic[key] = item;
    }

    /// <summary>
    /// 精确设置某类池的大小
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <param name="poolSize">int和object类型的池大小</param>
    /// <param name="poolSize2">其它类型的池大小</param>
    /// <typeparam name="T">如果是状态机池，泛型参数为状态机的顶级类</typeparam>
    public static void AddPoolConfig<T>(TaskPoolType poolType, int poolSize, int? poolSize2 = null) {
        Type type = typeof(T);
        if (poolType == TaskPoolType.ValueFutureStateMachineTask) {
            while (type.IsNested) {
                type = type.DeclaringType!;
            }
            string ns = type.Namespace + "." + ObjectUtil.GetSimpleName(type);
            AddPoolConfig(poolType, ns, poolSize, poolSize2);
            return;
        }
        Key key = new Key(poolType, type);
        Item item = new Item(poolSize, 0);
        configDic[key] = item;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <param name="poolSize">对象池大小</param>
    /// <typeparam name="S">状态机归属的顶层类</typeparam>
    /// <typeparam name="T">状态机执行结果</typeparam>
    public static void AddPoolConfig<S, T>(TaskPoolType poolType, int poolSize) {
        Type topLevelType = typeof(S);
        while (topLevelType.IsNested) {
            topLevelType = topLevelType.DeclaringType!;
        }
        string ns = topLevelType.Namespace + "." + ObjectUtil.GetSimpleName(topLevelType);
        Key key = new Key(poolType, ns, typeof(T));
        Item item = new Item(poolSize, 0);
        configDic[key] = item;
    }

    /// <summary>
    /// 获取对象池的缓存池大小。
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <typeparam name="T">对象的泛型参数类型</typeparam>
    public static int GetPoolSize<T>(TaskPoolType poolType) {
        Func<TaskPoolType, Type, int> func = poolSizeCalculator;
        if (func != null) {
            return func.Invoke(poolType, typeof(T));
        }
        // 通常使用int代替void，而object适用装箱场景
        Type type = typeof(T);
        bool isIntOrObject = type == typeof(int) || type == typeof(object);
        if (GetItem(poolType, type, out Item item, out bool precise)) {
            return (precise || isIntOrObject) ? item.poolSize : item.poolSize2;
        }
        // 保底方案
        if (poolType == TaskPoolType.ValuePromise
            || poolType == TaskPoolType.PromiseMoveNext) { // await Future
            return isIntOrObject ? 2000 : 100;
        }
        if (poolType == TaskPoolType.CtsCompletion
            || poolType == TaskPoolType.Coroutine) {
            return 500; // 非泛型类
        }
        return isIntOrObject ? 200 : 50;
    }

    /// <summary>
    /// 获取对象池的缓存池大小。
    /// </summary>
    /// <param name="poolType">对象池类型</param>
    /// <typeparam name="S">状态机或状态机归属的顶层类</typeparam>
    /// <typeparam name="T">状态机执行结果</typeparam>
    /// <returns></returns>
    public static int GetPoolSize<S, T>(TaskPoolType poolType) {
        bool isIntOrObject = typeof(T) == typeof(int) || typeof(T) == typeof(object);
        if (poolType == TaskPoolType.ValueFutureStateMachineTask) { // await ValueFuture
            Type topLevelType = typeof(S);
            while (topLevelType.IsNested) {
                topLevelType = topLevelType.DeclaringType!;
            }
            if (GetItem(poolType, topLevelType, typeof(T), out Item item, out bool precise)) {
                return (precise || isIntOrObject) ? item.poolSize : item.poolSize2;
            }
            return isIntOrObject ? 200 : 50;
        }
        return isIntOrObject ? 100 : 20;
    }

    private static bool GetItem(TaskPoolType poolType, Type resultType,
                                out Item item, out bool precise) {
        precise = false;
        if (configDic.TryGetValue(new Key(poolType, resultType), out item)) { // 精确查询
            precise = true;
            return true;
        }
        if (configDic.TryGetValue(new Key(poolType, resultType.Namespace!, null), out item)) { // 模糊查询 - 暂不递归
            return true;
        }
        if (configDic.TryGetValue(new Key(poolType, null), out item)) { // 模糊查询
            return true;
        }
        item = default;
        return false;
    }

    private static bool GetItem(TaskPoolType poolType, Type topLevelType, Type resultType,
                                out Item item, out bool precise) {
        string fullName = topLevelType.Namespace + "." + ObjectUtil.GetSimpleName(topLevelType);
        precise = false;
        // 根据顶级类类名查询
        if (configDic.TryGetValue(new Key(poolType, fullName, resultType), out item)) { // 精确查询
            precise = true;
            return true;
        }
        if (configDic.TryGetValue(new Key(poolType, fullName, null), out item)) { // 模糊查询
            return true;
        }
        // 根据命名空间查询
        string ns = topLevelType.Namespace!;
        if (configDic.TryGetValue(new Key(poolType, ns, resultType), out item)) { // 精确查询
            precise = true;
            return true;
        }
        if (configDic.TryGetValue(new Key(poolType, ns, null), out item)) { // 模糊查询
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
        private readonly Type? _type;
        private readonly string? _name;

        public Key(TaskPoolType poolType, Type? type) : this() {
            _poolType = poolType;
            _type = type;
            _name = null;
        }

        public Key(TaskPoolType poolType, string name, Type? type) {
            _poolType = poolType;
            _name = name;
            _type = type;
        }

        public bool Equals(Key other) {
            return _poolType == other._poolType
                   && _type == other._type
                   && _name == other._name;
        }

        public override bool Equals(object? obj) {
            return obj is Key other && Equals(other);
        }

        public override int GetHashCode() {
            int hashCode = (int)_poolType;
            hashCode = (hashCode * 397) ^ (_type != null ? _type.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (_name != null ? _name.GetHashCode() : 0);
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
    /// </summary>
    PromiseMoveNext,
    /// <summary>
    /// ValueFuture状态机任务
    /// </summary>
    ValueFutureStateMachineTask,

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
    /// <see cref="ManualResetPromise{T}"/>
    /// </summary>
    ManualResetPromise,
    /// <summary>
    /// 取消令牌的监听器
    /// </summary>
    CtsCompletion,

    /// <summary>
    /// 协程任务(分时任务的新实现)
    /// </summary>
    Coroutine,
}
}