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
using System.Runtime.InteropServices;
using System.Text;

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 表示一个class或struct的类型名。
/// 
/// 注意：
/// 1.无法通过名字判断是否是结构体或引用类型。
/// 2.ClassName始终通过<see cref="WithAttributes"/>方法设置属性，避免工厂方法参数过多。
/// 3.要想输出未构造泛型的typeof，可使用空名字的<see cref="TypeParameterName"/>。
/// </summary>
public class ClassName : TypeName
{
    // 一些常用ClassName
    public static readonly ClassName ENUM = InternalGet(typeof(Enum));
    public static readonly ClassName VALUE_TYPE = InternalGet(typeof(ValueType));
    public static readonly ClassName NULLABLE = InternalGet(typeof(Nullable<>));
    public static readonly ClassName INT_PTR = InternalGet(typeof(IntPtr));
    public static readonly ClassName UINT_PTR = InternalGet(typeof(UIntPtr));
    public static readonly ClassName DATETIME = InternalGet(typeof(DateTime));
    public static readonly ClassName DELEGATE = InternalGet(typeof(Delegate));

    public static readonly ClassName ATTRIBUTE = InternalGet(typeof(Attribute));
    public static readonly ClassName SERIALIZABLE = InternalGet(typeof(SerializableAttribute));
    public static readonly ClassName NON_SERIALIZED = InternalGet(typeof(NonSerializedAttribute));
    public static readonly ClassName OPTIONAL = InternalGet(typeof(OptionalAttribute));

    /// <summary>
    /// 外部类类名<see cref="Type.DeclaringType"/>
    /// </summary>
    public readonly ClassName? enclosingClassName;

    /// <summary>
    /// namespace
    /// (虽然c#可以不指定命名空间，但我选择必须指定...)
    /// </summary>
    public readonly string ns;
    /// <summary>
    /// 类简单名。
    /// 简单名是我们编码时的名字，不包含反引号和泛型参数个数信息。
    /// <code>Dictionary</code>
    /// </summary>
    public readonly string simpleName;
    /// <summary>
    /// 类型关键字
    /// </summary>
    internal readonly string? keyword;

    /// <summary>
    /// 所有泛型参数（包含从外部类拷贝来的）
    /// </summary>
    public readonly IList<TypeName> typeArguments;
    /// <summary>
    /// 当前类声明的泛型参数（生成代码时只使用这部分）
    /// </summary>
    public readonly IList<TypeName> declaredTypeArguments;

    /// <summary>
    /// 用于构建系统内建类型
    /// </summary>
    /// <param name="ns"></param>
    /// <param name="simpleName"></param>
    /// <param name="keyword"></param>
    internal ClassName(string ns, string simpleName, string keyword) {
        this.ns = ns;
        this.simpleName = simpleName;
        this.keyword = keyword;
        this.typeArguments = ImmutableList<TypeName>.Empty;
        this.declaredTypeArguments = ImmutableList<TypeName>.Empty;
    }

    private ClassName(in Builder builder)
        : base(builder.Attributes) {
        string ns = builder.Namespace;
        string simpleName = builder.Name;
        if (string.IsNullOrWhiteSpace(ns)) throw new ArgumentException("namespace cant be blank");
        if (string.IsNullOrWhiteSpace(simpleName)) throw new ArgumentException("simpleName cant be blank");

        this.enclosingClassName = builder.EnclosingClassName;
        this.ns = ns;
        this.simpleName = simpleName;
        this.keyword = builder.Keyword;
        this.typeArguments = Util.ToImmutableList(builder.TypeArguments);
        // 节选当前类定义的泛型参数个数
        if (this.typeArguments.Count == 0
            || this.enclosingClassName == null
            || this.enclosingClassName.typeArguments.Count == 0) {
            this.declaredTypeArguments = this.typeArguments;
        } else {
            int enclosingCount = this.enclosingClassName.typeArguments.Count;
            int declaredCount = this.typeArguments.Count - enclosingCount;
            if (declaredCount == 0) {
                this.declaredTypeArguments = Util.EmptyList<TypeName>();
            } else {
                List<TypeName> typeNames = new List<TypeName>(this.typeArguments);
                this.declaredTypeArguments = Util.ToImmutableList(typeNames.GetRange(enclosingCount, declaredCount));
            }
        }
    }

    #region props

    /// <summary>
    /// 是否是系统的<see cref="Nullable{T}"/>结构体
    /// </summary>
    public bool IsNullableType => simpleName == "Nullable" && ns == "System";

    /// <summary>
    /// 是否是泛型类
    /// </summary>
    public bool IsGenericType => typeArguments.Count > 0;

    /// <summary>
    /// 是否是未绑定泛型(typeof未指定泛型参数 -- 所有泛型参数为空)
    /// </summary>
    public bool IsUnboundedGenericType {
        get {
            if (!IsGenericType) return false;
            foreach (TypeName typeArgument in typeArguments) {
                if (typeArgument is not TypeParameterName typeParameterName
                    || !string.IsNullOrEmpty(typeParameterName.name)) {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// 获取顶层类类名
    /// 如果当前类是顶层类，则返回自己
    /// </summary>
    public ClassName TopLevelClassName {
        get {
            ClassName r = this;
            while (r.enclosingClassName != null) {
                r = r.enclosingClassName;
            }
            return r;
        }
    }

    #endregion

    #region overrides

    /// <summary>
    /// 类型关键字
    /// </summary>
    public string? Keyword => keyword;

    /// <summary>
    /// 获取运行时的反射类型名
    /// 未构造泛型：<code>System.Collections.Generic.Dictionary`2+Enumerator[TKey,TValue]</code>
    /// 已构造泛型：<code>System.Collections.Generic.Dictionary`2+Enumerator[System.String,System.Object]</code>
    /// </summary>
    /// <returns></returns>
    public override string ReflectionName() {
        return ReflectionName(true);
    }

    /// <summary>
    /// 获取运行时的反射类型名，可用于<see cref="Type.GetType(string)"/>加载类型。
    /// 泛型原型(可反射加载)：<code>System.Collections.Generic.Dictionary`2+Enumerator</code>
    /// 未构造泛型(不可反射加载)：<code>System.Collections.Generic.Dictionary`2+Enumerator[TKey,TValue]</code>
    /// 已构造泛型(可反射加载)：<code>System.Collections.Generic.Dictionary`2+Enumerator[System.String,System.Object]</code>
    /// </summary>
    /// <param name="includeTypeArguments">输出是否包含泛型参数，不包含时可用于加载泛型原型</param>
    /// <returns></returns>
    public string ReflectionName(bool includeTypeArguments) {
        // 需要处理泛型参数
        string name = simpleName;
        if (typeArguments.Count > 0) {
            StringBuilder sb = new StringBuilder(simpleName);
            // 追加反引号和泛型参数个数 -- 个数只是当前类新增的泛型个数
            if (declaredTypeArguments.Count > 0) {
                sb.Append('`');
                sb.Append(declaredTypeArguments.Count);
            }
            // 追加泛型参数详情 -- 这里包含外部类的泛型
            if (includeTypeArguments) {
                sb.Append('[');
                for (int i = 0; i < typeArguments.Count; i++) {
                    if (i > 0) sb.Append(',');
                    sb.Append(typeArguments[i].ReflectionName());
                }
                sb.Append(']');
            }
            name = sb.ToString();
        }
        return enclosingClassName != null
            ? enclosingClassName.ReflectionName(false) + "+" + name // c#内部类使用'+'连接
            : ns + "." + name;
    }

    /// <summary>
    /// 对于未构造泛型，ToString会在反射名的基础上追加泛型变量的属性，以确保唯一性
    /// <code>System.Nullable`1[System.Int32]</code>
    /// <code>System.Nullable`1[T], typeArgumentAttrs: [None]</code>
    /// </summary>
    /// <returns></returns>
    protected override string ToStringImpl() {
        StringBuilder sb = new StringBuilder();
        sb.Append(GetType().Name);
        sb.Append(", reflectionName: ");
        sb.Append(ReflectionName()); // 避免ToString外部类信息
        // 追加泛型变量的attributes
        sb.Append(", typeArgumentAttrs: [");
        for (int i = 0; i < typeArguments.Count; i++) {
            TypeName typeArgument = typeArguments[i];
            if (i > 0) {
                sb.Append(',');
            }
            sb.Append(typeArgument.attributes);
        }
        sb.Append(']');
        return sb.ToString();
    }

#if NET6_0_OR_GREATER
    public override ClassName WithAttributes(TypeNameAttributes attributes) {
#else
    public override TypeName WithAttributes(TypeNameAttributes attributes) {
#endif
        if (this.attributes == attributes) return this;
        return new Builder()
        {
            EnclosingClassName = enclosingClassName,
            Namespace = ns,
            Name = simpleName,
            TypeArguments = typeArguments,
            Attributes = attributes,
            Keyword = Keyword // 需要保留关键字
        }.Build();
    }

#if NET6_0_OR_GREATER
    public override ClassName RemoveAllNullableAttribute() {
#else
    public override TypeName RemoveAllNullableAttribute() {
#endif
        if (typeArguments.Count == 0) {
            if (!attributes.IsIntersect(TypeNameAttributes.NullableReferenceType)) return this;
            return new Builder()
            {
                EnclosingClassName = enclosingClassName,
                Namespace = ns,
                Name = simpleName,
                TypeArguments = typeArguments,
                Attributes = attributes.Unset(TypeNameAttributes.NullableReferenceType),
                Keyword = Keyword // 需要保留关键字
            }.Build();
        }
        // 不再做过多测试，直接构建新对象
        List<TypeName> tempTypeArguments = new List<TypeName>(typeArguments.Count);
        foreach (TypeName typeArgument in typeArguments) {
            tempTypeArguments.Add(typeArgument.RemoveAllNullableAttribute());
        }
        return new Builder()
        {
            EnclosingClassName = enclosingClassName,
            Namespace = ns,
            Name = simpleName,
            TypeArguments = tempTypeArguments,
            Attributes = attributes.Unset(TypeNameAttributes.NullableReferenceType),
            Keyword = Keyword // 需要保留关键字
        }.Build();
    }

    #endregion

    /// <summary>
    /// 增加泛型参数--用于泛型定义类型
    /// </summary>
    /// <param name="typeArguments">新的泛型参数列表</param>
    /// <returns></returns>
    public ClassName AddTypeArguments(params TypeName[] typeArguments) {
        return new Builder()
        {
            EnclosingClassName = enclosingClassName,
            Namespace = ns,
            Name = simpleName,
            TypeArguments = Util.Concat(this.typeArguments, typeArguments),
            Attributes = attributes
        }.Build();
    }

    /// <summary>
    /// 替换所有的泛型参数（长度必须一致）。
    /// </summary>
    /// <param name="typeArguments">新的泛型参数列表</param>
    /// <returns></returns>
    public ClassName WithTypeArguments(params TypeName[] typeArguments) {
        if (typeArguments.Length != this.typeArguments.Count) {
            throw new ArgumentException();
        }
        return new Builder()
        {
            EnclosingClassName = enclosingClassName,
            Namespace = ns,
            Name = simpleName,
            TypeArguments = typeArguments,
            Attributes = attributes
        }.Build();
    }

    /// <summary>
    /// 替换当前类声明的泛型参数。
    /// 注意：必须从外部类开始构造，参数只接收该类显式定义的泛型参数。
    /// </summary>
    /// <param name="typeArguments">长度必须等于类显式声明的泛型变量个数</param>
    /// <returns></returns>
    public ClassName WithDeclaredTypeArguments(params TypeName[] typeArguments) {
        if (typeArguments.Length != declaredTypeArguments.Count) {
            throw new ArgumentException();
        }
        return new Builder()
        {
            EnclosingClassName = enclosingClassName,
            Namespace = ns,
            Name = simpleName,
            TypeArguments = Util.Concat(enclosingClassName?.typeArguments, typeArguments),
            Attributes = attributes
        }.Build();
    }

    /// <summary>
    /// 创建一个同级类类名
    /// </summary>
    /// <param name="name">类简单名</param>
    /// <param name="typeArguments">泛型参数</param>
    /// <param name="inheritTypeArguments">是否继承外部类泛型参数</param>
    /// <param name="attributes">额外属性</param>
    /// <returns></returns>
    public ClassName PeerClass(string name, IList<TypeName>? typeArguments = null,
                               bool inheritTypeArguments = true,
                               TypeNameAttributes attributes = TypeNameAttributes.None) {
        if (enclosingClassName != null) {
            return enclosingClassName.NestedClass(name, typeArguments, inheritTypeArguments, attributes);
        }
        return new Builder()
        {
            Namespace = ns,
            Name = name,
            TypeArguments = typeArguments,
            Attributes = attributes
        }.Build();
    }

    /// <summary>
    /// 创建一个嵌套类类名。
    /// 注意：默认情况下会继承当前类的泛型参数。
    /// </summary>
    /// <param name="name">类简单名</param>
    /// <param name="typeArguments">子类的泛型参数</param>
    /// <param name="inheritTypeArguments">是否继承外部类泛型参数</param>
    /// <param name="attributes">嵌套类的属性</param>
    /// <returns></returns>
    public ClassName NestedClass(string name, IList<TypeName>? typeArguments = null,
                                 bool inheritTypeArguments = true,
                                 TypeNameAttributes attributes = TypeNameAttributes.None) {
        if (inheritTypeArguments) {
            typeArguments = Util.Concat(this.typeArguments, typeArguments);
        }
        return new Builder()
        {
            EnclosingClassName = this,
            Namespace = ns,
            Name = name,
            TypeArguments = typeArguments,
            Attributes = attributes
        }.Build();
    }

    #region Get/Parse

    /// <summary>
    /// 创建一个ClassName
    ///(更复杂的构建请使用Builder)
    /// </summary>
    /// <param name="ns">命名空间</param>
    /// <param name="simpleName">类简单名</param>
    /// <param name="typeArguments">泛型参数</param>
    /// <returns></returns>
    public static ClassName Get(string ns, string simpleName, params TypeName[] typeArguments) {
        return new Builder()
        {
            Namespace = ns,
            Name = simpleName,
            TypeArguments = typeArguments,
        }.Build();
    }

    /// <summary>
    /// 创建一个ClassName
    /// (更复杂的构建请使用Builder)
    /// </summary>
    /// <param name="ns">命名空间</param>
    /// <param name="simpleName">类简单名</param>
    /// <param name="typeArguments">泛型参数</param>
    /// <param name="attributes">属性</param>
    /// <returns></returns>
    public static ClassName Get(string ns, string simpleName,
                                IList<TypeName>? typeArguments = null,
                                TypeNameAttributes attributes = TypeNameAttributes.None) {
        return new Builder()
        {
            Namespace = ns,
            Name = simpleName,
            TypeArguments = typeArguments,
            Attributes = attributes
        }.Build();
    }

    /// <summary>
    /// 通过类型信息解析
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public new static ClassName Get(Type type) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (type.Namespace == null || type.IsArray || type.IsGenericParameter) {
            throw new ArgumentException("invalid type: " + type);
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

        if (type == typeof(Enum)) return ENUM;
        if (type == typeof(ValueType)) return VALUE_TYPE;
        if (type == typeof(Nullable<>)) return NULLABLE;
        if (type == typeof(IntPtr)) return INT_PTR;
        if (type == typeof(UIntPtr)) return UINT_PTR;
        if (type == typeof(DateTime)) return DATETIME;
        if (type == typeof(Delegate)) return DELEGATE;
        return InternalGet(type);
    }

    private static ClassName InternalGet(Type type) {
        string name = type.Name;
        List<TypeName>? genericArgumentNames = null;
        if (type.IsGenericType) {
            Type[] genericArguments = type.GetGenericArguments();
            genericArgumentNames = new List<TypeName>(genericArguments.Length);
            foreach (Type genericArgument in genericArguments) {
                genericArgumentNames.Add(TypeName.Get(genericArgument));
            }
            // Name去掉反引号
            int idx = name.LastIndexOf('`');
            if (idx > 0) {
                name = name.Substring(0, idx);
            }
        }
        // 暂不处理匿名类
        if (type.IsNested) {
            ClassName outerClassName = Get(type.DeclaringType!);
            return outerClassName.NestedClass(name, genericArgumentNames, false);
        }
        return new Builder()
        {
            EnclosingClassName = null,
            Namespace = type.Namespace!,
            Name = name,
            TypeArguments = genericArgumentNames
        }.Build();
    }

    #endregion

    public struct Builder
    {
        /// <summary>
        /// 外部类类名<see cref="Type.DeclaringType"/>
        /// </summary>
        public ClassName? EnclosingClassName { get; set; }
        /// <summary>
        /// 命名空间
        /// </summary>
        public string Namespace { get; set; }
        /// <summary>
        /// 简单名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 泛型参数，需要包含外部类的泛型参数
        /// </summary>
        public IList<TypeName>? TypeArguments { get; set; }
        /// <summary>
        /// 属性
        /// </summary>
        public TypeNameAttributes Attributes { get; set; }

        /// <summary>
        /// 关键字
        /// </summary>
        internal string? Keyword { get; set; }

        public ClassName Build() {
            return new ClassName(in this);
        }
    }
}
}