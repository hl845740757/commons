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
using System.Reflection;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 实现类的Bean信息
/// <code>configId + implType</code> 指向该信息
/// </summary>
internal sealed class BeanInfo
{
    /// <summary>
    /// 关联的用户配置
    /// </summary>
    public readonly InjectBeanConfig config;

    /// <summary>
    /// 实现类型(class)
    /// 
    /// 如果是泛型类，则是已构造泛型
    /// </summary>
    public readonly Type implType;
    /// <summary>
    /// 绑定的实例，单例有效
    ///
    /// 0.单例是绑定config和implType的
    /// 1.工厂创建对象后也会存储在这里
    /// 2.volatile读；lock写，lock的对象即该Info对象
    /// </summary>
    public volatile object? instance;
    /// <summary>
    /// 当前正在创建的服务类型
    /// </summary>
    public Type? creatingServiceType;

    /// <summary>
    /// 实现类的注入点
    /// </summary>
    public readonly ImmutableList<InjectionPoint> injectionPoints;
    /// <summary>
    /// 实体被创建后的钩子方法
    /// </summary>
    public readonly MethodInfo? onCreateHook;

    public BeanInfo(InjectBeanConfig config, Type implType, object? instance) {
        if (implType.IsGenericTypeDefinition) {
            throw new ArgumentException("implType.IsGenericTypeDefinition");
        }
        this.config = config;
        this.implType = implType;
        this.instance = instance;

        if (instance != null) {
            this.injectionPoints = ImmutableList<InjectionPoint>.Empty;
            this.onCreateHook = null;
        } else {
            this.injectionPoints = Util.GetInjectPoints(implType);
            this.onCreateHook = Util.FindOnActiveMethod(implType);
        }
    }
}
}