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

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 泛型变量
/// </summary>
public class TypeParameterSpec : ISpecification
{
    public readonly string name;
    public readonly TypeParameterConstraints constraints;
    public readonly IList<TypeName> bounds;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="constraints">泛型约束</param>
    /// <param name="bounds">注意：只包含代码可见的上界</param>
    public TypeParameterSpec(string name, TypeParameterConstraints constraints, IList<TypeName>? bounds = null) {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.constraints = constraints;
        this.bounds = Util.ToImmutableList(bounds);
    }

    public string? Name => name;
    public SpecType SpecType => SpecType.TypeParameter;

    /// <summary>
    /// 约束是否为值类型
    /// </summary>
    public bool HasValueTypeConstraint => (constraints & TypeParameterConstraints.ValueTypeConstraint) != 0;

    /// <summary>
    /// 是否约束为引用类型
    /// </summary>
    public bool HasReferenceTypeConstraint => (constraints & TypeParameterConstraints.ReferenceTypeConstraint) != 0;

    /// <summary>
    /// 是否包含约束条件 
    /// </summary>
    /// <value></value>
    public bool HasConstraints => constraints != 0 || bounds.Count > 0;

    /// <summary>
    /// 替换约束
    /// </summary>
    /// <param name="constraints">约束</param>
    /// <returns></returns>
    public TypeParameterSpec WithConstraints(TypeParameterConstraints constraints) {
        return new TypeParameterSpec(name, constraints, bounds);
    }

    /// <summary>
    /// 替换边界，需注意object和nullable
    /// </summary>
    /// <param name="bounds">边界</param>
    /// <returns>新的对象</returns>
    public TypeParameterSpec WithBounds(IList<TypeName> bounds) {
        return new TypeParameterSpec(name, constraints, bounds);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">变量名</param>
    /// <param name="constraints">泛型约束</param>
    /// <param name="bounds">注意：只包含代码可见的上界</param>
    public static TypeParameterSpec Get(string name, TypeParameterConstraints constraints = 0, IList<TypeName>? bounds = null) {
        return new TypeParameterSpec(name, constraints, bounds);
    }

    /// <summary>
    /// 通过泛型变量Type实例解析信息
    /// 注意：
    /// 1.C#的泛型参数使用struct关键字约束时，会添加<see cref="ValueType"/>为上界，会自动去除。
    /// 2.C#的泛型参数使用class关键字约束时，会添加<see cref="object"/>为上界，会自动去除。
    /// 3.反射无法获取notnull信息
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static TypeParameterSpec Get(Type type) {
        if (!type.IsGenericParameter) {
            throw new ArgumentException("type is not generic parameter");
        }
        Type[] boundTypes = type.GetGenericParameterConstraints();
        List<TypeName> visibleBounds = new List<TypeName>(boundTypes.Length);
        foreach (Type boundType in boundTypes) {
            if (boundType == typeof(object) || boundType == typeof(ValueType)) {
                continue;
            }
            visibleBounds.Add(TypeName.Get(boundType));
        }
        TypeParameterConstraints constraints = TypeParameterConstraints.None;
        if ((type.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0) {
            constraints |= TypeParameterConstraints.ReferenceTypeConstraint;
        }
        if ((type.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) {
            constraints |= TypeParameterConstraints.ValueTypeConstraint;
        }
        if ((type.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0) {
            constraints |= TypeParameterConstraints.DefaultConstructorConstraint;
        }
        return new TypeParameterSpec(type.Name, constraints, visibleBounds);
    }

    #region toString

    public override string ToString() {
        return $"{nameof(name)}: {name}," +
               $" {nameof(constraints)}: {constraints}," +
               $" {nameof(bounds)}: {Util.ToString(bounds)}";
    }

    #endregion
}
}