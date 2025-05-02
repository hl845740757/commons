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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Poet
{
public static class TypeNameExtensions
{
    #region make

    /// <summary>
    /// 构建一个数组类型
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayTypeName MakeArrayType(this TypeName typeName) {
        return ArrayTypeName.Get(typeName);
    }

    /// <summary>
    /// 构建引用传值类型
    /// </summary>
    /// <param name="typeName">this</param>
    /// <param name="kind">引用类型</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ByRefTypeName MakeByRefType(this TypeName typeName, ByRefTypeName.Kind kind = ByRefTypeName.Kind.Ref) {
        if (typeName is ByRefTypeName) {
            throw new InvalidOperationException();
        }
        return ByRefTypeName.Get(typeName, kind);
    }

    /// <summary>
    /// 构造一个引用类型
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointerTypeName MakePointerType(this TypeName typeName) {
        return PointerTypeName.Get(typeName);
    }

    /// <summary>
    /// 构造一个Nullable结构体,。
    /// <see cref="Nullable{T}"/>
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClassName MakeNullableType(this TypeName typeName) {
        return ClassName.NULLABLE.WithActualTypeVariables(typeName);
    }

    #endregion

    /// <summary>
    /// 增加属性
    /// (如果已包含目标属性，则返回原始对象)
    /// </summary>
    /// <param name="typeName"></param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeName AddAttributes(this TypeName typeName, TypeNameAttributes attributes) {
        if (attributes == TypeNameAttributes.None || (typeName.attributes & attributes) == attributes) {
            return typeName;
        }
        return typeName.WithAttributes(typeName.attributes | attributes);
    }

    /// <summary>
    /// 删除属性
    /// (如果不包含目标属性，则返回原始对象)
    /// </summary>
    /// <param name="typeName"></param>
    /// <param name="attributes"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeName RemoveAttributes(this TypeName typeName, TypeNameAttributes attributes) {
        if (attributes == TypeNameAttributes.None || (typeName.attributes & attributes) == 0) {
            return typeName;
        }
        return typeName.WithAttributes(typeName.attributes & ~attributes);
    }

    /// <summary>
    /// 是否是<see cref="Nullable{T}"/>结构体
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullableType(this TypeName typeName) {
        return typeName is ClassName className && className.IsGenericType && className.IsNullableType;
    }

    /// <summary>
    /// 获取引用或指针的最终目标类型
    /// </summary>
    /// <param name="typeName"></param>
    /// <param name="includeArray"></param>
    /// <returns></returns>
    public static TypeName GetRootTargetType(this TypeName typeName, bool includeArray = true) {
        // System.String[]*[]&
        if (typeName is ByRefTypeName refTypeName) { // ref总是在末尾
            typeName = refTypeName.targetType;
        }
        if (includeArray && typeName is ArrayTypeName arrayTypeName) { // 考虑指针的数组...
            typeName = arrayTypeName.GetRootElementType();
        }
        if (typeName is PointerTypeName pointerTypeName) {
            typeName = pointerTypeName.GetRootTargetType();
        }
        if (includeArray && typeName is ArrayTypeName arrayTypeName2) { // 考虑数组的指针
            typeName = arrayTypeName2.GetRootElementType();
        }
        return typeName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIntersect(this TypeNameAttributes self, TypeNameAttributes other) {
        return (self & other) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsSet(this TypeNameAttributes self, TypeNameAttributes other) {
        return (self & other) == other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeNameAttributes Set(this TypeNameAttributes self, TypeNameAttributes other) {
        return self | other;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeNameAttributes Unset(this TypeNameAttributes self, TypeNameAttributes other) {
        return self & ~other;
    }
}
}