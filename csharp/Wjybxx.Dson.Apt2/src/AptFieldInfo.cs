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
using Wjybxx.Commons.Poet;

namespace Wjybxx.Dson.Apt2
{
internal class AptFieldInfo : IEquatable<AptFieldInfo>
{
#nullable disable
    /// <summary>
    /// 字段信息
    /// </summary>
    public readonly FieldInfo fieldInfo;
    /// <summary>
    /// 字段关联的属性
    /// </summary>
    public readonly PropertyInfo? propertyInfo;

    /// <summary>
    /// 字段的类型名缓存
    ///
    /// 注意：去除了NRT信息<see cref="TypeName.RemoveAllNullableAttribute"/>
    /// </summary>
    public TypeName typeName;
#nullable enable

    public AptFieldInfo(FieldInfo fieldInfo, PropertyInfo? propertyInfo) {
        this.fieldInfo = fieldInfo;
        this.propertyInfo = propertyInfo;
    }

    /// <summary>
    /// 字段的名字
    /// </summary>
    public string Name => fieldInfo.Name;
    /// <summary>
    /// 是否是自动属性生成的字段
    /// </summary>
    public bool IsAutoPropertyField => Util.IsAutoPropertyField(Name);

    /// <summary>
    /// 是否是静态字段
    /// </summary>
    public bool IsStatic => fieldInfo.IsStatic;
    /// <summary>
    /// 是否是public
    /// </summary>
    public bool IsPublic => fieldInfo.IsPublic;
    /// <summary>
    /// 是否是自读字段
    /// </summary>
    public bool IsReadOnly => fieldInfo.IsInitOnly;

    /// <summary>
    /// 是否有public的getter
    /// </summary>
    public bool HasPublicGetter => propertyInfo != null
                                   && propertyInfo.GetMethod != null
                                   && propertyInfo.GetMethod.IsPublic;
    /// <summary>
    /// 是否有public的setter
    /// </summary>
    public bool HasPublicSetter => propertyInfo != null
                                   && propertyInfo.SetMethod != null
                                   && propertyInfo.SetMethod.IsPublic;

    /// <summary>
    /// 字段的类型
    /// </summary>
    public Type FieldType => fieldInfo.FieldType;

    /// <summary>
    /// 字段的键
    /// </summary>
    public FieldKey FieldKey => new FieldKey(Util.GetSimpleName(fieldInfo.DeclaringType!), fieldInfo.Name);

    public Attribute? GetAttribute(Type type) {
        return fieldInfo.GetCustomAttribute(type);
    }

    public T? GetAttribute<T>() where T : Attribute {
        return fieldInfo.GetCustomAttribute<T>();
    }

    #region equals

    public bool Equals(AptFieldInfo? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(fieldInfo, other.fieldInfo);
    }

    public override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((AptFieldInfo)obj);
    }

    public override int GetHashCode() {
        return (fieldInfo != null ? fieldInfo.GetHashCode() : 0);
    }

    public static bool operator ==(AptFieldInfo? left, AptFieldInfo? right) {
        return Equals(left, right);
    }

    public static bool operator !=(AptFieldInfo? left, AptFieldInfo? right) {
        return !Equals(left, right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(fieldInfo)}: {fieldInfo}";
    }
}
}