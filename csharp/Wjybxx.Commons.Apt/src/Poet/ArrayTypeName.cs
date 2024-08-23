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
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 表示一个数组类型
/// </summary>
[Immutable]
public class ArrayTypeName : TypeName
{
    public static readonly ArrayTypeName INT_ARRAY = new ArrayTypeName(TypeName.INT);
    public static readonly ArrayTypeName UINT_ARRAY = new ArrayTypeName(TypeName.UINT);
    public static readonly ArrayTypeName LONG_ARRAY = new ArrayTypeName(TypeName.LONG);
    public static readonly ArrayTypeName ULONG_ARRAY = new ArrayTypeName(TypeName.ULONG);
    public static readonly ArrayTypeName FLOAT_ARRAY = new ArrayTypeName(TypeName.FLOAT);
    public static readonly ArrayTypeName DOUBLE_ARRAY = new ArrayTypeName(TypeName.DOUBLE);
    public static readonly ArrayTypeName BYTE_ARRAY = new ArrayTypeName(TypeName.BYTE);
    public static readonly ArrayTypeName CHAR_ARRAY = new ArrayTypeName(TypeName.CHAR);

    public static readonly ArrayTypeName STRING_ARRAY = new ArrayTypeName(TypeName.STRING);
    public static readonly ArrayTypeName OBJECT_ARRAY = new ArrayTypeName(TypeName.OBJECT);

    /// <summary>
    /// 数组元素类型
    /// </summary>
    public readonly TypeName elementType;

    private ArrayTypeName(TypeName elementType, TypeNameAttributes attributes = TypeNameAttributes.None)
        : base(attributes) {
        this.elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));
    }

    /// <summary>
    /// 数组的反射名
    /// </summary>
    /// <returns></returns>
    public override string ReflectionName() => elementType.ReflectionName() + "[]";

    protected override string ToStringImpl() {
        return $"{GetType().Name}, {nameof(elementType)}: {elementType}";
    }

#if NET5_0_OR_GREATER
    public override ArrayTypeName WithAttributes(TypeNameAttributes attributes) {
#else
    public override TypeName WithAttributes(TypeNameAttributes attributes) {
#endif
        return new ArrayTypeName(elementType, attributes);
    }

    public static ArrayTypeName Of(TypeName elementType, TypeNameAttributes attributes = TypeNameAttributes.None) {
        return new ArrayTypeName(elementType, attributes);
    }

    public static ArrayTypeName Of(Type elementType, TypeNameAttributes attributes = TypeNameAttributes.None) {
        return new ArrayTypeName(TypeName.Get(elementType), attributes);
    }

    /// <summary>
    /// 获取数组的顶层元素类型
    /// </summary>
    /// <returns></returns>
    public TypeName GetRootElementType() {
        TypeName root = elementType;
        while (root is ArrayTypeName nested) {
            root = nested.elementType;
        }
        return root;
    }

    /// <summary>
    /// 获取数组的阶数
    /// </summary>
    /// <returns></returns>
    public int GetArrayRank() {
        int r = 1;
        TypeName root = elementType;
        while (root is ArrayTypeName nested) {
            root = nested.elementType;
            r++;
        }
        return r;
    }
}
}