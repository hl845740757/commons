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

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 该Builder放在顶层，方便使用
/// </summary>
public struct InjectBeanConfigBuilder
{
    /// <summary>
    /// 实现类型(class)
    ///
    /// 1.一个实现类型可以出现在BeanConfig中，但关联的接口不可以重复。
    /// 2.如果实现类是泛型定义类（原型类），且泛型参数不能直接从服务类中继承，则必须配置<see cref="implTypeMaker"/>
    /// 3.如果实现类是泛型定义类（原型类），则单例是隔离的
    /// </summary>
    public Type implType;
    /// <summary>
    /// 绑定范围 -- 单例或多例
    /// </summary>
    public InjectScope scope;
    /// <summary>
    /// 绑定的服务信息
    /// </summary>
    public List<ServiceKey>? serviceKeys;

    /// <summary>
    /// 绑定的实例，单例有效
    /// </summary>
    public object? instance;
    /// <summary>
    /// 实例工厂，单例和多例都有效
    ///
    /// 参数：服务类型、实现类型 -- 都是已构造的泛型
    /// </summary>
    public Func<Type, Type, object>? factory;
    /// <summary>
    /// 实现类的类型解析器
    /// 用于解决服务类的泛型参数不能直接转移到实现类的情况
    /// </summary>
    public Func<Type, Type>? implTypeMaker;

    public InjectBeanConfigBuilder(Type implType) : this() {
        this.implType = implType ?? throw new ArgumentNullException(nameof(implType));
        this.scope = InjectScope.Singleton;
    }

    public InjectBeanConfig Build() {
        Check();
        return new InjectBeanConfig(in this);
    }

    private void Check() {
        if (serviceKeys == null || serviceKeys.Count == 0) {
            throw new ArgumentException("serviceKeys is empty");
        }
        if (implType.IsAbstract) {
            throw new ArgumentException($"implType is abstract, {implType}");
        }
        if (!implType.IsGenericTypeDefinition) {
            foreach (var key in serviceKeys) {
                if (!key.serviceType.IsAssignableFrom(implType)) {
                    throw new ArgumentException($"service is not assignable from implType: {key.serviceType}-{key.serviceName}-{implType}");
                }
            }
            return;
        }
        // 如果是泛型接口的话，这里好像还无法检测是否可赋值，也无法检测是否可继承服务类的泛型参数 -- 只有运行时才可以检测...
        // 能检查多少检查多少吧，尽量提前发现错误
        if (instance != null) {
            throw new ArgumentException($"generic implType cant bind to instance, {implType}");
        }
        if (implTypeMaker != null) {
            return;
        }
        // 检查泛型参数个数是否相同
        foreach (ServiceKey key in serviceKeys) {
            if (key.serviceType.GetGenericArguments().Length != implType.GetGenericArguments().Length) {
                throw new ArgumentException($"service is not assignable from implType:, {key.serviceType}-{key.serviceName}-{implType}");
            }
        }
    }
}
}