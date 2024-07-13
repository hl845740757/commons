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

#pragma warning disable CS1591

namespace Wjybxx.Commons;

/// <summary>
/// 常量对象池
///
/// ps: 常量池的接口使用频率并不是太高，使用 lock + Dictionary 足够。
/// </summary>
/// <typeparam name="TConstant"></typeparam>
public class ConstantPool<TConstant> where TConstant : class, IConstant
{
    /** ConstantPool不需要多高的查询性能，因此使用普通的字典更合适，可以保证id分配的连续性 */
    private readonly Dictionary<string, TConstant> _constantMap = new();
    private readonly ConstantFactory<TConstant>? _factory;
    /** 下一个要分配的常量Id，总是分配 --由lock保护 */
    private int _nextId;
    /**
     * 下一个要分配的缓存索引，只有builder显式申请的情况下分配。
     * cacheIndex和id是独立的，通常用于为特殊的<see cref="IConstant"/>建立高速索引。
     * 由lock保护
     */
    private int _nextIndex = 0;

    private ConstantPool(ConstantFactory<TConstant>? factory, int nextId) {
        _factory = factory;
        _nextId = nextId;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="factory">简单工厂，如果为null，则不能通过<see cref="ValueOf"/>等创建实例</param>
    /// <param name="nextId">初始id</param>
    /// <returns></returns>
    public static ConstantPool<TConstant> NewPool(ConstantFactory<TConstant>? factory, int nextId = 0) {
        return new ConstantPool<TConstant>(factory, nextId);
    }

    /// <summary>
    /// 获取给定名字对应的常量
    ///
    /// 1.如果给定的常量存在，则返回存在的常量。
    /// 2.如果关联的常量不存在，但可以默认创建，则创建一个新的常量并返回，否则抛出异常。
    /// </summary>
    /// <param name="name">常量名</param>
    /// <returns></returns>
    public TConstant ValueOf(string name) {
        IConstant.CheckName(name);
        return GetOrCreate(name);
    }

    /// <summary>
    /// 创建一个常量，如果已存在关联的常量，则抛出异常。
    /// </summary>
    /// <param name="name">常量名</param>
    public TConstant NewInstance(string name) {
        IConstant.CheckName(name);
        if (_factory == null) {
            throw new IllegalStateException("builder required");
        }
        return CreateOrThrow(new SimpleBuilder(name, _factory));
    }

    /// <summary>
    /// 创建一个常量，如果已存在关联的常量，则抛出异常。
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public TConstant NewInstance(IConstant.Builder builder) {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        return CreateOrThrow(builder);
    }

    /// <summary>
    /// 判断对应的常量是否存在
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool Exists(string name) {
        IConstant.CheckName(name);
        lock (_constantMap) {
            return _constantMap.ContainsKey(name);
        }
    }

    /// <summary>
    /// 获取对应的常量，若不存在关联的常量则返回null
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public TConstant? Get(string name) {
        IConstant.CheckName(name);
        lock (_constantMap) {
            return _constantMap.TryGetValue(name, out var constant) ? constant : null;
        }
    }

    /// <summary>
    /// 获取对应的常量，若不存在关联的常量则抛出异常
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public TConstant GetOrThrow(string name) {
        IConstant.CheckName(name);
        lock (_constantMap) {
            if (_constantMap.TryGetValue(name, out var constant)) {
                return constant;
            }
            throw new ArgumentException(name + " does not exist");
        }
    }

    /// <summary>
    /// 获取当前池中的所有常量对象
    ///
    /// 注意：
    /// 1.该操作是个高开销的操作。
    /// 2.如果存在竞态条件，那么每次返回的结果可能并不一致。
    /// 3.返回值是当前数据的一个快照
    /// 4.默认我们是按照声明顺序排序的
    /// </summary>
    public List<TConstant> Values {
        get {
            lock (_constantMap) {
                List<TConstant> r = new List<TConstant>(_constantMap.Values);
                r.Sort();
                return r;
            }
        }
    }

    /// <summary>
    /// 获取常量池中的常量数量
    /// </summary>
    public int Count {
        get {
            lock (_constantMap) {
                return _constantMap.Count;
            }
        }
    }

    /// <summary>
    /// 创建一个当前常量池的快照
    /// </summary>
    /// <returns></returns>
    public ConstantMap<TConstant> NewConstantMap() => new ConstantMap<TConstant>(this);

    #region internal

    private TConstant GetOrCreate(string name) {
        IConstant.CheckName(name);
        lock (_constantMap) {
            if (_constantMap.TryGetValue(name, out var constant)) {
                return constant;
            }
            constant = NewConstant(new SimpleBuilder(name, _factory!));
            _constantMap.Add(constant.Name, constant);
            return constant;
        }
    }

    private TConstant CreateOrThrow(IConstant.Builder builder) {
        string name = builder.Name;
        lock (_constantMap) {
            if (_constantMap.ContainsKey(name)) {
                throw new ArgumentException($"'{name}' is already in use");
            }
            TConstant constant = NewConstant(builder);
            _constantMap.Add(constant.Name, constant);
            return constant;
        }
    }

    /// <summary>
    /// 该方法必须在加锁的情况下调用
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    private TConstant NewConstant(IConstant.Builder builder) {
        string name = builder.Name;
        int nextId = _nextId++;
        builder.SetId(this, nextId);
        if (builder.RequireCacheIndex) {
            builder.SetCacheIndex(_nextIndex++);
        }

        TConstant constant = (TConstant)builder.Build();
        if (constant.Name != name
            || constant.Id != nextId
            || constant.DeclaringPool != this) {
            throw new IllegalStateException($"expected id: {nextId}, name: {name}, but found id: {constant.Id}, name: {constant.Name}");
        }
        return constant;
    }

    #endregion

    private class SimpleBuilder : IConstant.Builder
    {
        private readonly ConstantFactory<TConstant> _factory;

        public SimpleBuilder(string name, ConstantFactory<TConstant> factory) : base(name) {
            _factory = factory;
        }

        public override TConstant Build() {
            return _factory(this);
        }
    }
}