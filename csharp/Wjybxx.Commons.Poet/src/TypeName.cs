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

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 类型名，TypeName用于表示对其它类型的引用 -- 不可包含Nullable以外的信息。
/// （这里的实现并不完整，只用于简单的代码生成）
/// （继承是为了节省内存，否则需要实现为标签类）
/// </summary>
public abstract class TypeName : IEquatable<TypeName>
{
    public readonly TypeNameAttributes attributes;
    private string? cachedString;

    internal TypeName(TypeNameAttributes attributes = TypeNameAttributes.None) {
        this.attributes = attributes;
    }

    /// <summary>
    /// 是否是基础类型
    /// </summary>
    /// <returns></returns>
    public bool IsPrimitive => (this is ClassName className) && primitiveTypeKeywords.Contains(className.keyword);

    /// <summary>
    /// 获取类型运行时的字符串名，可用于反射加载类型
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public abstract string ReflectionName();

    /// <summary>
    /// 注意：ToString影响Equals测试
    /// </summary>
    /// <returns></returns>
    public sealed override string ToString() {
        if (cachedString == null) {
            cachedString = ToStringImpl() + ", attrs: " + attributes;
        }
        return cachedString;
    }

    /** 注意：ToString影响Equals测试 */
    protected abstract string ToStringImpl();

    /// <summary>
    /// 增加约束<see cref="TypeNameAttributes"/>
    /// 
    /// </summary>
    /// <param name="attributes"></param>
    /// <returns>如果attributes和当前attributes相同，可返回自身</returns>
    public abstract TypeName WithAttributes(TypeNameAttributes attributes);

    /// <summary>
    /// 删除所有的Nullable信息
    ///
    /// 原因：<code>typeof(string?)</code>是非法的，禁止在typeof中使用Nullable注解。
    /// </summary>
    /// <returns>如果当前不包含nullable信息，可返回自身</returns>
    public abstract TypeName RemoveAllNullableAttribute();

    #region equals

    public bool Equals(TypeName? other) {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ToString() == other.ToString();
    }

    public sealed override bool Equals(object? obj) {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TypeName)obj);
    }

    public sealed override int GetHashCode() {
        return ToString().GetHashCode();
    }

    public static bool operator ==(TypeName? left, TypeName? right) {
        return Equals(left, right);
    }

    public static bool operator !=(TypeName? left, TypeName? right) {
        return !Equals(left, right);
    }

    #endregion

    #region get-parse

    /// <summary>
    /// 通过反射类型信息获取TypeName
    ///
    /// 注意：可空引用类型是C#8出的特性，netstandard2.0无法访问<code>NullableAttribute</code>注解。
    /// 因此，引用类型的默认的解析结果是不包含'?'的。
    /// </summary>
    public static TypeName Get(Type type) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        // 引用和指针 -- 无法直接拿到元素类型，通过name反射拿(去除末尾'&'或者'*')
        if (type.IsByRef || type.IsPointer) {
            // byRef也是GetElementType拿...
            Type elementType = type.GetElementType();
            if (elementType == null) throw new ArgumentException("unsupported type: " + type);
            return type.IsByRef ? ByRefTypeName.Get(elementType) : PointerTypeName.Get(elementType);
        }
        // 数组
        if (type.IsArray) {
            TypeName elementTypeName = Get(type.GetElementType()!);
            return ArrayTypeName.Get(elementTypeName);
        }
        // 泛型参数
        if (type.IsGenericParameter) {
            return TypeParameterName.Get(type);
        }
        return ClassName.Get(type);
    }

    #endregion

    #region consts

    // C#与Java不同，基础类型也是正常的类型；所以基础类型其实也应该使用ClassName
    public static readonly ClassName INT = new ClassName("System", "Int32", "int");
    public static readonly ClassName UINT = new ClassName("System", "UInt32", "uint");
    public static readonly ClassName LONG = new ClassName("System", "Int64", "long");
    public static readonly ClassName ULONG = new ClassName("System", "UInt64", "ulong");
    public static readonly ClassName FLOAT = new ClassName("System", "Single", "float");
    public static readonly ClassName DOUBLE = new ClassName("System", "Double", "double");

    public static readonly ClassName BOOL = new ClassName("System", "Bool", "bool");
    public static readonly ClassName BYTE = new ClassName("System", "Byte", "byte");
    public static readonly ClassName SBYTE = new ClassName("System", "SByte", "sbyte");
    public static readonly ClassName SHORT = new ClassName("System", "Int16", "short");
    public static readonly ClassName USHORT = new ClassName("System", "UInt16", "ushort");
    public static readonly ClassName CHAR = new ClassName("System", "Char", "char");
    public static readonly ClassName DECIMAL = new ClassName("System", "Decimal", "decimal");

    public static readonly ClassName STRING = new ClassName("System", "String", "string");
    public static readonly ClassName OBJECT = new ClassName("System", "Object", "object");
    public static readonly ClassName VOID = new ClassName("System", "Void", "void");

    /// <summary>
    /// 非基础类型的关键字
    /// </summary>
    private static readonly HashSet<string> primitiveTypeKeywords = new HashSet<string>()
    {
        INT.keyword,
        UINT.keyword,
        LONG.keyword,
        ULONG.keyword,
        FLOAT.keyword,
        DOUBLE.keyword,

        BOOL.keyword,
        BYTE.keyword,
        SBYTE.keyword,
        SHORT.keyword,
        USHORT.keyword,
        CHAR.keyword,
        DECIMAL.keyword
    };

    #endregion
}
}