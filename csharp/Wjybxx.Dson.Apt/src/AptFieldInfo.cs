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
using Microsoft.CodeAnalysis;
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Poet;

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// 该抽象用于统一反射和编译期数据
///
/// 注意：<see cref="fieldInfo"/>和<see cref="fieldSymbol"/>两者最多一个为null，可能同时有值。
/// </summary>
internal sealed class AptFieldInfo : IEquatable<AptFieldInfo>
{
#nullable disable
    /// <summary>
    /// 反射数据
    ///
    /// 1.如果是外部程序集类型的字段，则该字段有值
    /// 2.该字段只用于获取注解（Attribute），不能用于类型判断。
    /// </summary>
    public readonly FieldInfo? fieldInfo;
    /// <summary>
    /// 字段编译期数据
    /// 1.如果是当前程序集类型的字段，则该字段有值；
    /// 2.该字段只用于获取注解（Attribute），不能用于类型判断。
    /// </summary>
    public readonly IFieldSymbol? fieldSymbol;
    /// <summary>
    /// 字段关联的属性(缓存)
    /// 1.如果是当前程序集类型的属性，则该字段有值；
    /// 2.如果是外部程序集类型的public和protected属性，则该字段也有值；
    /// </summary>
    public readonly IPropertySymbol? propertySymbol;

    /// <summary>
    /// 字段的类型名缓存
    ///
    /// 注意：去除了NRT信息<see cref="TypeName.RemoveAllNullableAttribute"/>
    /// </summary>
    public TypeName typeName;
#nullable enable

    public AptFieldInfo(FieldInfo? fieldInfo, IFieldSymbol? fieldSymbol, IPropertySymbol? propertySymbol) {
        if (fieldInfo == null && fieldSymbol == null) {
            throw new ArgumentException("both fieldInfo and fieldSymbol are null");
        }
        this.fieldInfo = fieldInfo;
        this.fieldSymbol = fieldSymbol;
        this.propertySymbol = propertySymbol;
    }

    /// <summary>
    /// 字段的名字
    /// </summary>
    public string Name => fieldSymbol != null ? fieldSymbol.Name : fieldInfo!.Name;
    /// <summary>
    /// 是否是自动属性生成的字段
    /// </summary>
    public bool IsAutoPropertyField => Util.IsAutoPropertyField(Name);

    /// <summary>
    /// 是否是静态字段
    /// </summary>
    public bool IsStatic => fieldSymbol != null ? fieldSymbol.IsStatic : fieldInfo!.IsStatic;
    /// <summary>
    /// 是否是public
    /// </summary>
    public bool IsPublic => fieldSymbol != null ? fieldSymbol.IsPublic() : fieldInfo!.IsPublic;
    /// <summary>
    /// 是否是自读字段
    /// </summary>
    public bool IsReadOnly => fieldSymbol != null ? fieldSymbol.IsReadOnly : fieldInfo!.IsInitOnly;

    /// <summary>
    /// 是否有public的getter
    /// </summary>
    public bool HasPublicGetter => propertySymbol != null
                                   && propertySymbol.GetMethod != null
                                   && propertySymbol.GetMethod.IsPublic();
    /// <summary>
    /// 是否有public的setter
    /// </summary>
    public bool HasPublicSetter => propertySymbol != null
                                   && propertySymbol.SetMethod != null
                                   && propertySymbol.SetMethod.IsPublic();

    /// <summary>
    /// 字段的类型
    ///
    /// 1.由<see cref="fieldSymbol"/>或<see cref="propertySymbol"/>计算得到。
    /// 2.如果该值为null，则表示不支持序列化 -- 字段和其属性都是外部程序集不能访问的。
    /// </summary>
    public ITypeSymbol? FieldType {
        get {
            if (propertySymbol != null) return propertySymbol.Type;
            return fieldSymbol != null ? fieldSymbol.Type : null;
        }
    }

    /// <summary>
    /// 字段的键
    /// </summary>
    public FieldKey FieldKey {
        get {
            if (fieldSymbol != null) {
                return new FieldKey(fieldSymbol.ContainingType.Name, fieldSymbol.Name);
            }
            return new FieldKey(Util.GetSimpleName(fieldInfo!.DeclaringType!), fieldInfo.Name);
        }
    }

    /// <summary>
    /// 查找注解
    /// </summary>
    public AptAttributeData? GetAttribute(string className) {
        if (IsAutoPropertyField) {
            AttributeData? attributeData = AptUtils.GetAttribute(propertySymbol!.GetAttributes(), className);
            return attributeData == null ? null : new AptAttributeData(attributeData, null);
        }
        // 非自动属性，只能将注解添加到字段；编译期数据和反射数据都要查询
        if (fieldSymbol != null) {
            AttributeData? attributeData = AptUtils.GetAttribute(fieldSymbol!.GetAttributes(), className);
            if (attributeData != null) {
                return new AptAttributeData(attributeData, null);
            }
        }
        if (fieldInfo != null) {
            // 反射API未提供根据类型名查询注解的接口，只能笨方法测试Type的名字
            foreach (Attribute attribute in fieldInfo!.GetCustomAttributes(false)) {
                if (attribute.GetType().ToString() == className) {
                    return new AptAttributeData(null, attribute);
                }
            }
        }
        return null;
    }

    #region equals

    public bool Equals(AptFieldInfo other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (fieldInfo != null) {
            return fieldInfo.Equals(other.fieldInfo);
        }
        return fieldSymbol!.Equals(other.fieldSymbol, SymbolEqualityComparer.Default);
    }

    public override bool Equals(object obj) {
        return ReferenceEquals(this, obj) || obj is AptFieldInfo other && Equals(other);
    }

    public override int GetHashCode() {
        if (fieldInfo != null) {
            return fieldInfo.GetHashCode();
        }
        return SymbolEqualityComparer.Default.GetHashCode(fieldSymbol);
    }

    public static bool operator ==(AptFieldInfo left, AptFieldInfo right) {
        return Equals(left, right);
    }

    public static bool operator !=(AptFieldInfo left, AptFieldInfo right) {
        return !Equals(left, right);
    }

    public override string ToString() {
        return $"{nameof(fieldInfo)}: {fieldInfo}, {nameof(fieldSymbol)}: {fieldSymbol}";
    }

    #endregion
}
}