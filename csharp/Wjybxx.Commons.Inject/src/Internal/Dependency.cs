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
using System.Reflection;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Inject.Attributes;

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 该数据相当于各类注解解析后的合并数据
/// </summary>
internal sealed class Dependency
{
#nullable disable
    /// <summary>
    /// 关联的依赖注入点，延迟赋值
    /// </summary>
    internal InjectionPoint injectionPoint;
#nullable enable
    /// <summary>
    /// 字段/属性/参数类型，未拆解的；可能是List或字典
    /// </summary>
    public readonly Type fieldType;
    /// <summary>
    /// 依赖的类型
    /// 如果字段/属性/方法参数是<see cref="IList{T}"/>或<see cref="IDictionary{K,V}"/>，服务类型为Value的类型
    /// </summary>
    public readonly Type serviceType;
    /// <summary>
    /// 依赖的服务信息，单注入和多注入都有效
    /// </summary>
    public readonly ImmutableList<InjectServiceAttribute> serviceAttributes;
    /// <summary>
    /// 构造函数中的下标
    /// </summary>
    public readonly int parameterIndex;

    /// <summary>
    /// 如果字段类型是<see cref="IList{T}"/>类型，则该字段有值
    /// </summary>
    public readonly Type? listType;
    /// <summary>
    /// 如果字段是<see cref="IDictionary{TKey,TValue}"/>类型，则该字段有值
    /// </summary>
    public readonly Type? dictionaryType;
    /// <summary>
    /// list或字典的add方法
    /// </summary>
    public readonly MethodInfo? addMethod;

    public Dependency(Type fieldType, ImmutableList<InjectServiceAttribute> serviceAttributes, int parameterIndex) {
        this.fieldType = fieldType ?? throw new ArgumentNullException(nameof(fieldType));
        this.serviceType = Util.GetServiceType(fieldType);
        this.serviceAttributes = serviceAttributes;
        this.parameterIndex = parameterIndex;

        // 缓存对应的类型
        if (Util.IsList(fieldType)) {
            listType = typeof(List<>).MakeGenericType(fieldType.GetGenericArguments());
            dictionaryType = null;
            addMethod = Util.GetAddMethod(listType);
        } else if (Util.IsDictionary(fieldType)) {
            listType = null;
            dictionaryType = typeof(Dictionary<,>).MakeGenericType(fieldType.GetGenericArguments());
            addMethod = Util.GetAddMethod(dictionaryType);
        } else {
            listType = null;
            dictionaryType = null;
            addMethod = null;
        }
    }

    public bool IsList => listType != null;
    public bool IsDictionary => dictionaryType != null;
}
}