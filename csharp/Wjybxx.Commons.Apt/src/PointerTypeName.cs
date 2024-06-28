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
/// 指针类型(不建议真的使用)
/// <code>int*</code>
/// </summary>
[Immutable]
public class PointerTypeName : TypeName
{
    /// <summary>
    /// 原始类型
    /// </summary>
    public readonly TypeName targetType;

    private PointerTypeName(TypeName targetType, TypeNameAttributes attributes)
        : base(attributes) {
        // 指针排在ref前System.Int32*&
        if (targetType is ByRefTypeName) throw new ArgumentException("targetType cant be ref");
        this.targetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
    }

    /// <summary>
    /// 反射名追加星号
    /// </summary>
    /// <returns></returns>
    public override string ReflectionName() => targetType.ReflectionName() + "*";

    protected override string ToStringImpl() {
        return $"{GetType().Name}, {nameof(targetType)}: {targetType}";
    }

    public override PointerTypeName WithAttributes(TypeNameAttributes attributes) {
        return new PointerTypeName(targetType, attributes);
    }

    public static PointerTypeName Of(TypeName targetType, TypeNameAttributes attributes = TypeNameAttributes.None) {
        return new PointerTypeName(targetType, attributes);
    }

    public static PointerTypeName Of(Type targetType, TypeNameAttributes attributes = TypeNameAttributes.None) {
        return new PointerTypeName(TypeName.Get(targetType), attributes);
    }

    /// <summary>
    /// 获取指针指向的最终类型
    /// </summary>
    /// <returns></returns>
    public TypeName GetRootTargetType() {
        TypeName root = targetType;
        while (root is PointerTypeName nested) {
            root = nested.targetType;
        }
        return root;
    }

    public int GetPointerRank() {
        int r = 1;
        TypeName root = targetType;
        while (root is PointerTypeName nested) {
            root = nested.targetType;
            r++;
        }
        return r;
    }
}