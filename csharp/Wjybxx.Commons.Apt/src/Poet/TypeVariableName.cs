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
using System.Reflection;
using System.Text;
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Poet;

/// <summary>
/// 类型变量名
/// (注意：并不是只有该类型才可以作为泛型参数，只是使用该类型时表示未构造泛型)
/// </summary>
[Immutable]
public class TypeVariableName : TypeName
{
    /// <summary>
    /// 泛型变量名
    /// </summary>
    public readonly string name;
    /// <summary>
    /// 泛型约束（类型上界）
    /// </summary>
    public readonly IList<TypeName> bounds;

    private TypeVariableName(string name, IList<TypeName>? bounds, TypeNameAttributes attributes)
        : base(attributes) {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.bounds = Util.ToImmutableList(bounds);
    }

    /// <summary>
    /// 反射名
    /// </summary>
    /// <returns></returns>
    public override string ReflectionName() => name;

    protected override string ToStringImpl() {
        StringBuilder sb = new StringBuilder(16);
        sb.Append(GetType().Name);
        sb.Append(", name: ");
        sb.Append(name);
        if (HasConstraints()) {
            sb.Append(", constraints: ");
            sb.Append(ConstraintsToString());
        }
        return sb.ToString();
    }

    public override TypeName WithAttributes(TypeNameAttributes attributes) {
        return new TypeVariableName(name, bounds, attributes);
    }

    /// <summary>
    /// 添加新的约束
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns>新的对象</returns>
    public TypeVariableName WithBounds(params TypeName[] bounds) {
        List<TypeName> newBounds = new List<TypeName>(this.bounds.Count + bounds.Length);
        newBounds.AddRange(this.bounds);
        newBounds.AddRange(bounds);
        return new TypeVariableName(name, newBounds, attributes);
    }

    /// <summary>
    /// 添加新的约束
    /// </summary>
    /// <param name="bounds"></param>
    /// <returns>新的对象</returns>
    public TypeVariableName WithBounds(IList<TypeName> bounds) {
        List<TypeName> newBounds = new List<TypeName>(this.bounds.Count + bounds.Count);
        newBounds.AddRange(this.bounds);
        newBounds.AddRange(bounds);
        return new TypeVariableName(name, newBounds, attributes);
    }

    #region parse/get

    /// <summary>
    /// 构建泛型变量名
    /// </summary>
    /// <param name="name">泛型名</param>
    /// <param name="attributes">泛型属性</param>
    /// <returns></returns>
    public static TypeVariableName Get(string name, TypeNameAttributes attributes = TypeNameAttributes.None) {
        return InternalGet(name, null, attributes);
    }

    /// <summary>
    /// 构建泛型变量名
    /// </summary>
    /// <param name="name">泛型名</param>
    /// <param name="bounds">泛型约束</param>
    /// <param name="attributes">泛型属性</param>
    /// <returns></returns>
    public static TypeVariableName Get(string name, IList<TypeName> bounds, TypeNameAttributes attributes = TypeNameAttributes.None) {
        return InternalGet(name, bounds, attributes);
    }

    /// <summary>
    /// 通过泛型变量Type实例解析信息
    /// 注意：
    /// 1. C#的泛型参数使用struct关键字约束时，会添加<see cref="ValueType"/>为上界，会自动去除。
    /// 2. 反射无法获取NotNull约束
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public new static TypeVariableName Get(Type type) {
        if (!type.IsGenericParameter) {
            throw new ArgumentException("type is not generic parameter");
        }
        // 理论上是可以做缓存的，子类和超类可能使用的同一个泛型变量，但我们先不优化，先保持代码简单
        Type[] constraints = type.GetGenericParameterConstraints();
        List<TypeName> bounds = new List<TypeName>(constraints.Length);
        foreach (Type constraint in constraints) {
            bounds.Add(TypeName.Get(constraint));
        }
        // 转换Attributes
        TypeNameAttributes attributes = TypeNameAttributes.None;
        if ((type.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0) {
            attributes |= TypeNameAttributes.ReferenceTypeConstraint;
        }
        if ((type.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0) {
            attributes |= TypeNameAttributes.NotNullableValueTypeConstraint;
        }
        if ((type.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0) {
            attributes |= TypeNameAttributes.DefaultConstructorConstraint;
        }
        return InternalGet(type.Name, bounds, attributes);
    }

    /// <summary>
    /// 其它工厂方法应当调用该方法统一处理约束
    /// </summary>
    /// <param name="name">泛型名</param>
    /// <param name="bounds">泛型约束</param>
    /// <param name="attributes">泛型属性</param>
    /// <returns></returns>
    private static TypeVariableName InternalGet(string name, IList<TypeName>? bounds, TypeNameAttributes attributes) {
        if (bounds == null || bounds.Count == 0) {
            return new TypeVariableName(name, bounds, attributes);
        } else {
            List<TypeName> visibleBounds = new List<TypeName>(bounds);
            // 统一去除ValueType和Object -- 不能直接作为泛型约束
            visibleBounds.Remove(TypeName.OBJECT);
            visibleBounds.Remove(ClassName.VALUE_TYPE);
            return new TypeVariableName(name, visibleBounds, attributes);
        }
    }

    #endregion

    /// <summary>
    /// 约束是否为值类型
    /// </summary>
    public bool IsValueTypeConstraint => (attributes & TypeNameAttributes.NotNullableValueTypeConstraint) != 0;

    /// <summary>
    /// 是否约束为引用类型
    /// </summary>
    public bool IsReferenceTypeConstraint => (attributes & TypeNameAttributes.ReferenceTypeConstraint) != 0;

    /// <summary>
    /// 是否包含约束条件 
    /// </summary>
    /// <returns></returns>
    public bool HasConstraints() => attributes != 0 || bounds.Count > 0;

    /// <summary>
    /// 输出字符串格式的约束条件，不包含where部分
    /// </summary>
    /// <returns></returns>
    public string ConstraintsToString() {
        if (attributes == 0 && bounds.Count == 0) {
            throw new IllegalStateException("none constraints");
        }
        if ((attributes & TypeNameAttributes.NotNullableValueTypeConstraint) != 0) {
            return "struct";
        }
        StringBuilder sb = new StringBuilder(32);
        int count = 0;
        if ((attributes & TypeNameAttributes.ReferenceTypeConstraint) != 0) {
            sb.Append("class");
            count++;
        }
        if ((attributes & TypeNameAttributes.NotNullableReferenceType) != 0) {
            sb.Append("notnull");
            count++;
        }
        if ((attributes & TypeNameAttributes.DefaultConstructorConstraint) != 0) {
            if (count++ > 0) sb.Append(", ");
            sb.Append("new()");
        }
        foreach (TypeName bound in bounds) {
            if (count++ > 0) sb.Append(", ");
            sb.Append(bound.ReflectionName());
        }
        return sb.ToString();
    }
}