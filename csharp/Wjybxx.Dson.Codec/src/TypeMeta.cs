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
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 类型元数据
///
/// 1.1个Class可以有多个ClassName(即允许别名)，以支持简写；但一个ClassName只能映射到一个Class。
/// 2.在文档型编解码中，可读性是比较重要的，因此不要一味追求简短。
/// </summary>
[Immutable]
public sealed class TypeMeta : IEquatable<TypeMeta>
{
    /// <summary>
    /// 关联的类型
    /// </summary>
    public readonly Type type;
    /// <summary>
    /// 文本编码时的输出格式。
    /// 当编码字段时，如果未指定样式，则使用类型的默认样式。
    /// </summary>
    public readonly ObjectStyle style;
    /// <summary>
    /// 支持的类型名。
    /// 如果是泛型，使用泛型原型的名字或别名，如：
    /// <code>
    /// Dictionary
    /// Dictionary`2
    /// List`1
    /// </code>
    /// </summary>
    public readonly IList<string> clsNames;

    private TypeMeta(Type type, ObjectStyle style, IList<string> clsNames) {
        if (clsNames.Count == 0) throw new ArgumentException("clsNames is empty");
        this.type = type;
        this.style = style;
        this.clsNames = clsNames.ToImmutableList2();
    }

    /// <summary>
    /// 类的主别名
    /// </summary>
    public string MainClsName => clsNames[0];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="clazz"></param>
    /// <param name="style">文本样式</param>
    /// <param name="clsName">类型名</param>
    /// <returns></returns>
    public static TypeMeta Of(Type clazz, ObjectStyle style, string clsName) {
        return new TypeMeta(clazz, style, ImmutableList<string>.Create(clsName));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="clazz"></param>
    /// <param name="style">文本样式</param>
    /// <param name="clsNames">类型名</param>
    /// <returns></returns>
    public static TypeMeta Of(Type clazz, ObjectStyle style, params string[] clsNames) {
        return new TypeMeta(clazz, style, ImmutableList<string>.CreateRange(clsNames));
    }

    public static TypeMeta Of(Type clazz, ObjectStyle style, List<string> clsNames) {
        return new TypeMeta(clazz, style, clsNames.ToImmutableList2());
    }

    public override string ToString() {
        return $"{nameof(type)}: {type}, {nameof(style)}: {style}, {nameof(clsNames)}: {CollectionUtil.ToString(clsNames)}";
    }

    public bool Equals(TypeMeta? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return type == other.type
               && style == other.style
               && clsNames.SequenceEqual(other.clsNames); // 序列相等
    }

    public override bool Equals(object? obj) {
        return ReferenceEquals(this, obj) || obj is TypeMeta other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(type, (int)style, clsNames);
    }

    public static bool operator ==(TypeMeta? left, TypeMeta? right) {
        return Equals(left, right);
    }

    public static bool operator !=(TypeMeta? left, TypeMeta? right) {
        return !Equals(left, right);
    }
}
}