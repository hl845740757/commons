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
using System.Text;
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Apt;

/// <summary>
/// 类型名
/// （这里的实现并不完整，只用于简单的代码生成）
/// （继承是为了节省内存，否则需要实现为标签类）
/// </summary>
[Immutable]
public class TypeName : IEquatable<TypeName>
{
    public static readonly TypeName INT = new TypeName("int");
    public static readonly TypeName UINT = new TypeName("uint");
    public static readonly TypeName LONG = new TypeName("long");
    public static readonly TypeName ULONG = new TypeName("ulong");
    public static readonly TypeName FLOAT = new TypeName("float");
    public static readonly TypeName DOUBLE = new TypeName("double");

    public static readonly TypeName BOOL = new TypeName("bool");
    public static readonly TypeName BYTE = new TypeName("byte");
    public static readonly TypeName SBYTE = new TypeName("sbyte");
    public static readonly TypeName SHORT = new TypeName("short");
    public static readonly TypeName USHORT = new TypeName("ushort");
    public static readonly TypeName CHAR = new TypeName("char");
    public static readonly TypeName DECIMAL = new TypeName("decimal");

    public static readonly TypeName STRING = new TypeName("string");
    public static readonly TypeName OBJECT = new TypeName("object");
    public static readonly TypeName VOID = new TypeName("void"); // void不可作为泛型参数

    private readonly string? keyword;
    /** toString缓存 */
    private string? cachedString;

    private TypeName(string keyword) {
        this.keyword = keyword;
    }

    internal TypeName() {
        this.keyword = null;
    }

    /// <summary>
    /// 非基础类型的关键字
    /// </summary>
    private static readonly HashSet<string> nonPrimitiveTypeKeywords = new HashSet<string>(4)
    {
        STRING.keyword,
        OBJECT.keyword,
        VOID.keyword,
    };

    /// <summary>
    /// 是否是基础类型
    /// </summary>
    /// <returns></returns>
    public bool IsPrimitive() => keyword != null && !nonPrimitiveTypeKeywords.Contains(keyword);

    /// <summary>
    /// 获取类型运行时的字符串名
    /// </summary>
    /// <returns></returns>
    /// <exception cref="IllegalStateException"></exception>
    public virtual string ReflectionName() {
        return keyword switch
        {
            "int" => typeof(int).ToString(),
            "uint" => typeof(uint).ToString(),
            "long" => typeof(long).ToString(),
            "ulong" => typeof(ulong).ToString(),
            "float" => typeof(float).ToString(),
            "double" => typeof(double).ToString(),

            "bool" => typeof(bool).ToString(),
            "byte" => typeof(byte).ToString(),
            "sbyte" => typeof(sbyte).ToString(),
            "short" => typeof(short).ToString(),
            "ushort" => typeof(ushort).ToString(),
            "char" => typeof(char).ToString(),
            "decimal" => typeof(decimal).ToString(),

            "string" => typeof(string).ToString(),
            "object" => typeof(object).ToString(),
            "void" => typeof(void).ToString(),
            _ => throw new AssertionError()
        };
    }

    /// <summary>
    /// 注意：ToString影响Equals测试
    /// </summary>
    /// <returns></returns>
    public sealed override string ToString() {
        if (cachedString == null) {
            cachedString = ToStringImpl();
        }
        return cachedString;
    }

    /** 注意：ToString影响Equals测试 */
    protected virtual string ToStringImpl() {
        if (keyword == null) throw new AssertionError();
        return $"{GetType().Name}, {nameof(keyword)}: {keyword}";
    }

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

    #endregion

    /// <summary>
    /// 通过反射类型信息获取TypeName
    /// </summary>
    public static TypeName Get(Type type) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        // 引用和指针 -- 无法直接拿到元素类型，通过name反射拿(去除末尾'&'或者'*')
        if (type.IsByRef || type.IsPointer) {
            string typeName = type.ToString();
            Type targetType = Type.GetType(typeName.Substring(0, typeName.Length - 1));
            if (targetType == null) throw new ArgumentException("unsupported ref type: " + type);
            return type.IsByRef ? RefTypeName.Of(targetType) : PointerTypeName.Of(targetType);
        }
        // 数组
        if (type.IsArray) {
            TypeName elementTypeName = Get(type.GetElementType()!);
            return ArrayTypeName.Of(elementTypeName);
        }
        // 泛型参数
        if (type.IsGenericParameter) {
            return TypeVariableName.Get(type);
        }
        // 基础类型
        if (type == typeof(void)) return VOID;
        if (type.IsPrimitive) {
            if (type == typeof(int)) return INT;
            if (type == typeof(uint)) return UINT;
            if (type == typeof(long)) return LONG;
            if (type == typeof(ulong)) return ULONG;
            if (type == typeof(float)) return FLOAT;
            if (type == typeof(double)) return DOUBLE;

            if (type == typeof(bool)) return BOOL;
            if (type == typeof(byte)) return BYTE;
            if (type == typeof(sbyte)) return SBYTE;
            if (type == typeof(short)) return SHORT;
            if (type == typeof(ushort)) return USHORT;
            if (type == typeof(char)) return CHAR;
            if (type == typeof(decimal)) return DECIMAL;
            throw new ArgumentException("unsupported primitive type: " + type);
        }
        // 特殊引用类型
        if (type == typeof(string)) return STRING;
        if (type == typeof(object)) return OBJECT;
        if (type == typeof(ValueType)) return ClassName.VALUE_TYPE;
        if (type == typeof(Nullable<>)) return ClassName.NULLABLE;
        if (type == typeof(Span<>)) return ClassName.SPAN;
        return ClassName.Get(type);
    }

    /// <summary>
    /// 获取引用或指针的最终目标类型
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public static TypeName GetRootTargetType(TypeName typeName) {
        if (typeName is RefTypeName refTypeName) {
            typeName = refTypeName.targetType; // 引用类型的目标类型可能是指针
        }
        if (typeName is PointerTypeName pointerTypeName) {
            return pointerTypeName.GetRootTargetType();
        }
        return typeName;
    }
}