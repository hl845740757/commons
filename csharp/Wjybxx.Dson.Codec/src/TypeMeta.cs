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
using System.Linq;
using Wjybxx.Commons;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 类型元数据
/// 不使用Schema这样的东西，是因为Schema包含的信息太多，难以手动维护。
///
/// 1.1个Class可以有多个ClassName(即允许别名)，以支持简写；但一个ClassName只能映射到一个Class。
/// 2.在文档型编解码中，可读性是比较重要的，因此不要一味追求简短。
/// 3.支持为特定泛型预先设置name和style。
/// </summary>
[Immutable]
public sealed class TypeMeta : IEquatable<TypeMeta>
{
    /// <summary>
    /// 关联的类型
    /// </summary>
    public readonly Type type;
    /// <summary>
    /// 支持的类型名。
    /// 如果是泛型，使用泛型原型的名字或别名，如：
    /// <code>
    /// Dictionary
    /// Dictionary`2
    /// List`1
    /// </code>
    /// </summary>
    public readonly ImmutableList<string> clsNames;

    /// <summary>
    /// 序列化特征值
    /// </summary>
    public readonly SerializeFeatures encodeFeatures;
    /// <summary>
    /// 反序列化特征值
    /// </summary>
    public readonly DeserializeFeatures decodeFeatures;

    public TypeMeta(Type type, IList<string> clsNames,
                    SerializeFeatures encodeFeatures = default,
                    DeserializeFeatures decodeFeatures = default) {
        this.type = type ?? throw new ArgumentNullException(nameof(type));
        this.clsNames = clsNames.ToImmutableList2();
        this.encodeFeatures = encodeFeatures;
        this.decodeFeatures = decodeFeatures;
    }

    /** 类的主别名 */
    public string MainClsName => clsNames[0];

    /** 替换特征值 */
    public TypeMeta WithFeatures(SerializeFeatures encodeFeatures, DeserializeFeatures decodeFeatures) {
        return new TypeMeta(type, clsNames, encodeFeatures, decodeFeatures);
    }

    /** 替换clsNames */
    public TypeMeta WithClsNames(IList<string> clsNames) {
        return new TypeMeta(type, clsNames, encodeFeatures, decodeFeatures);
    }

    #region factory

    public static TypeMeta Of(Type clazz, string clsName) {
        return new TypeMeta(clazz, ImmutableList<string>.Create(clsName));
    }

    public static TypeMeta Of(Type clazz, params string[] clsNames) {
        return new TypeMeta(clazz, ImmutableList<string>.CreateRange(clsNames));
    }

    public static TypeMeta Of(Type clazz,
                              SerializeFeatures encodeFeatures,
                              DeserializeFeatures decodeFeatures) {
        return new TypeMeta(clazz, ImmutableList<string>.Create(clazz.Name), encodeFeatures, decodeFeatures);
    }

    public static TypeMeta Of(Type clazz,
                              SerializeFeatures encodeFeatures,
                              DeserializeFeatures decodeFeatures,
                              string clsName) {
        return new TypeMeta(clazz, ImmutableList<string>.Create(clsName), encodeFeatures, decodeFeatures);
    }

    public static TypeMeta Of(Type clazz,
                              SerializeFeatures encodeFeatures,
                              DeserializeFeatures decodeFeatures,
                              params string[] clsNames) {
        return new TypeMeta(clazz, clsNames.ToImmutableList2(), encodeFeatures, decodeFeatures);
    }

    public static TypeMeta Of(Type clazz,
                              SerializeFeatures encodeFeatures,
                              DeserializeFeatures decodeFeatures,
                              List<string> clsNames) {
        return new TypeMeta(clazz, clsNames.ToImmutableList2(), encodeFeatures, decodeFeatures);
    }

    //
    public static TypeMeta Of(Type clazz,
                              SerializeFeatures encodeFeatures,
                              string clsName) {
        return new TypeMeta(clazz, ImmutableList<string>.Create(clsName), encodeFeatures);
    }

    public static TypeMeta Of(Type clazz,
                              SerializeFeatures encodeFeatures,
                              params string[] clsNames) {
        return new TypeMeta(clazz, clsNames.ToImmutableList2(), encodeFeatures);
    }

    #endregion

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is TypeMeta other && Equals(other);
    }

    public bool Equals(TypeMeta? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return type == other.type
               && encodeFeatures == other.encodeFeatures
               && clsNames.SequenceEqual(other.clsNames);
    }

    public override int GetHashCode() {
        int hashCode = type.GetHashCode();
        hashCode = hashCode * 31 + encodeFeatures.GetHashCode();
        hashCode = hashCode * 31 + CollectionUtil.HashCode(clsNames);
        return hashCode;
    }

    public static bool operator ==(TypeMeta? left, TypeMeta? right) {
        return Equals(left, right);
    }

    public static bool operator !=(TypeMeta? left, TypeMeta? right) {
        return !Equals(left, right);
    }

    public override string ToString() {
        return $"{nameof(type)}: {type}, {nameof(encodeFeatures)}: {encodeFeatures}, {nameof(clsNames)}: {CollectionUtil.ToString(clsNames)}";
    }
}
}