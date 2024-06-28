#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Apt;

/// <summary>
/// 传递对象的引用
/// <code>ref int, ref int* , in int, out int</code>
/// </summary>
[Immutable]
public class ByRefTypeName : TypeName
{
    /// <summary>
    /// 原始类型
    /// </summary>
    public readonly TypeName targetType;
    /// <summary>
    /// 引用的修饰符
    /// </summary>
    public readonly Kind kind;

    private ByRefTypeName(TypeName targetType, Kind kind, TypeNameAttributes attributes)
        : base(attributes) {
        // 引用不能出现嵌套，但引用的目标类型可能是指针
        if (targetType is ByRefTypeName) throw new ArgumentException("targetType cant be ref");
        this.targetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        this.kind = kind;
    }

    public enum Kind
    {
        Ref,
        In,
        Out,
    }

    /// <summary>
    /// 反射名追加与符号
    /// </summary>
    /// <returns></returns>
    public override string ReflectionName() => targetType.ReflectionName() + "&";

    protected override string ToStringImpl() {
        return $"{GetType().Name}, {nameof(targetType)}: {targetType}";
    }

    public override ByRefTypeName WithAttributes(TypeNameAttributes attributes) {
        return new ByRefTypeName(targetType, kind, attributes);
    }

    public static ByRefTypeName Of(TypeName targetType, Kind kind = Kind.Ref, TypeNameAttributes attributes = TypeNameAttributes.None) {
        return new ByRefTypeName(targetType, kind, attributes);
    }

    public static ByRefTypeName Of(Type targetType, Kind kind = Kind.Ref, TypeNameAttributes attributes = TypeNameAttributes.None) {
        return new ByRefTypeName(TypeName.Get(targetType), kind, attributes);
    }
}