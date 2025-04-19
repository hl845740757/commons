#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using System.Threading;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 依赖注入的Bean配置
/// </summary>
[Immutable]
public sealed class InjectBeanConfig : IEquatable<InjectBeanConfig>
{
    /// <summary>
    /// 用于为配置分配id
    /// </summary>
    private static volatile int sequencer = 0;

    /// <summary>
    /// 配置id
    ///
    /// 1.用于识别作用域，单例是绑定配置和<see cref="implType"/>的，
    /// 2.如果实现类是泛型类，则configId会关联多个<see cref="implType"/>。
    /// 3.<code>configId + implType</code>构成唯一键，用于hash和equals测试。
    /// </summary>
    public readonly int configId;
    /// <summary>
    /// 实现类型(class)
    ///
    /// 1.一个实现类型可以出现在BeanConfig中，但关联的接口不可以重复。
    /// 2.如果实现类是泛型定义类（原型类），且泛型参数不能直接从服务类中继承，则必须配置<see cref="implTypeMaker"/>
    /// 3.如果实现类是泛型定义类（原型类），则单例是隔离的
    /// </summary>
    public readonly Type implType;
    /// <summary>
    /// 绑定范围 -- 单例或多例
    ///
    /// 注意：单例是Config级别！如果实现类是泛型类，不同泛型类型之间也是隔离的。
    /// </summary>
    public readonly InjectScope scope;
    /// <summary>
    /// 绑定的实例，单例有效
    ///
    /// 如果提前指定了实例，则不需要框架层进行注入；如果仅仅是指定工厂，我们也进行字段和属性注入
    /// </summary>
    public readonly object? instance;
    /// <summary>
    /// 实例工厂，单例和多例都有效
    /// 参数：服务类型、实现类型 -- 都是已构造的泛型
    ///
    /// 当<see cref="scope"/>为单例时，工厂方法只会被调用一次，其结果会被缓存下来；
    /// 当<see cref="scope"/>为多例时，每次创建实例时都会调用工厂方法；
    /// </summary>
    public readonly Func<Type, Type, object>? factory;
    /// <summary>
    /// 实现类的类型构造器
    ///
    /// 1.用于解决服务类的泛型参数不能直接转移到实现类的情况
    /// 2.委托必须是无状态的，直接根据服务的泛型参数构建实现类 -- 以允许并发调用
    /// </summary>
    public readonly Func<Type, Type>? implTypeMaker;
    /// <summary>
    /// 绑定的服务信息
    /// </summary>
    public readonly ImmutableList<ServiceKey> serviceKeys;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    internal InjectBeanConfig(in InjectBeanConfigBuilder builder) {
        this.configId = Interlocked.Increment(ref sequencer);
        this.implType = builder.implType ?? throw new ArgumentException("invalid builder");
        this.scope = builder.scope;
        this.instance = builder.instance;
        this.factory = builder.factory;
        this.implTypeMaker = builder.implTypeMaker;

        if (CollectionUtil.IsNullOrEmpty(builder.serviceKeys)) {
            this.serviceKeys = ImmutableList<ServiceKey>.Empty;
        } else {
            this.serviceKeys = builder.serviceKeys.ToImmutableList2();
        }
    }

    #region equals

    public bool Equals(InjectBeanConfig? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return configId == other.configId && implType == other.implType;
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is InjectBeanConfig other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            return (configId * 397) ^ implType.GetHashCode();
        }
    }

    public static bool operator ==(InjectBeanConfig? left, InjectBeanConfig? right) {
        return Equals(left, right);
    }

    public static bool operator !=(InjectBeanConfig? left, InjectBeanConfig? right) {
        return !Equals(left, right);
    }

    public override string ToString() {
        return $"{nameof(configId)}: {configId}," +
               $" {nameof(implType)}: {implType}," +
               $" {nameof(scope)}: {scope}," +
               $" {nameof(serviceKeys)}: {CollectionUtil.ToString(serviceKeys)}";
    }

    #endregion
}
}