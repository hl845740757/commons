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
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 表示一个class或struct的类型名。
/// 
/// 注意：
/// 1.无法通过名字判断是否是结构体或引用类型。
/// 2.ClassName始终通过<see cref="WithAttributes"/>方法设置属性，避免工厂方法参数过多。
/// 3.要想输出未构造泛型的typeof，可使用空名字的<see cref="TypeVariableName"/>。
/// </summary>
[Immutable]
public class ClassName : TypeName
{
    // 一些常用ClassName
    public static readonly ClassName VALUE_TYPE = InternalGet(typeof(ValueType));
    public static readonly ClassName ATTRIBUTE = InternalGet(typeof(Attribute));
    public static readonly ClassName NULLABLE = InternalGet(typeof(Nullable<>));
    public static readonly ClassName SPAN = InternalGet(typeof(Span<>));
    public static readonly ClassName VOID_CLASS = InternalGet(typeof(VoidClass));

    /// <summary>
    /// namespace
    /// (虽然c#可以不指定命名空间，但我选择必须指定...)
    /// </summary>
    public readonly string ns;
    /// <summary>
    /// 外部类类名<see cref="Type.DeclaringType"/>
    /// </summary>
    public readonly ClassName? enclosingClassName;
    /// <summary>
    /// 类简单名。
    /// 简单名是我们编码时的名字，不包含反引号和泛型参数个数信息。
    /// <code>Dictionary</code>
    /// </summary>
    public readonly string simpleName;
    /// <summary>
    /// 所有泛型参数（包含从外部类拷贝来的）
    /// </summary>
    public readonly IList<TypeName> typeArguments;
    /// <summary>
    /// 当前类声明的泛型参数（生成代码时只使用这部分）
    /// </summary>
    public readonly IList<TypeName> declaredTypeArguments;

    private ClassName(string ns, ClassName? enclosingClassName, string simpleName, IList<TypeName>? typeArguments,
                      TypeNameAttributes attributes = TypeNameAttributes.None)
        : base(attributes) {
        if (string.IsNullOrWhiteSpace(ns)) throw new ArgumentException("namespace cant be blank");
        if (simpleName.EndsWith("[]")) throw new ArgumentException("SimpleName cant be array name");

        this.ns = ns;
        this.enclosingClassName = enclosingClassName;
        this.simpleName = simpleName ?? throw new ArgumentNullException(nameof(simpleName));
        this.typeArguments = Util.ToImmutableList(typeArguments);
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

    /// <summary>
    /// 获取类型变量个数（未指定类型个数）
    /// </summary>
    /// <returns></returns>
    private int GetTypeVariableCount() {
        int c = 0;
        foreach (TypeName typeArgument in typeArguments) {
            if (typeArgument is TypeVariableName) c++;
        }
        return c;
    }

    /// <summary>
    /// 是否是泛型类
    /// </summary>
    public bool IsGenericType => typeArguments.Count > 0;

    /// <summary>
    /// 是否是泛型定义类
    /// </summary>
    public bool IsGenericTypeDefinition => IsGenericType && GetTypeVariableCount() > 0;

    /// <summary>
    /// 是否是已构造泛型（具体泛型）
    /// </summary>
    public bool IsConstructedGenericType => IsGenericType && GetTypeVariableCount() == 0;

    /// <summary>
    /// 是否是系统的<see cref="Nullable{T}"/>结构体
    /// </summary>
    public new bool IsNullableStruct => simpleName == "Nullable" && ns == "System";

    /// <summary>
    /// 获取顶层类类名
    /// </summary>
    /// <returns></returns>
    public ClassName ToTopLevelClassName() {
        return enclosingClassName != null ? enclosingClassName.ToTopLevelClassName() : this;
    }

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
    /// 对于未构造泛型，ToString会在反射名的基础上追加泛型变量的约束，以确保唯一性
    /// <code>System.Nullable`1[System.Int32]</code>
    /// <code>System.Nullable`1[T] where T : struct</code>
    /// </summary>
    /// <returns></returns>
    protected override string ToStringImpl() {
        StringBuilder sb = new StringBuilder();
        sb.Append(GetType().Name);
        sb.Append(", reflectionName: ");
        sb.Append(ReflectionName()); // 避免ToString外部类信息
        // 追加泛型约束
        for (int i = 0; i < typeArguments.Count; i++) {
            if (typeArguments[i] is TypeVariableName typeVariableName && typeVariableName.HasConstraints()) {
                sb.Append(" where ");
                sb.Append(typeVariableName.name);
                sb.Append(" : ");
                sb.Append(typeVariableName.ConstraintsToString());
            }
        }
        return sb.ToString();
    }

#if NET5_0_OR_GREATER
    public override ClassName WithAttributes(TypeNameAttributes attributes) {
#else
    public override TypeName WithAttributes(TypeNameAttributes attributes) {
#endif
        return new ClassName(ns, enclosingClassName, simpleName, typeArguments, attributes);
    }

    /// <summary>
    /// 创建一个同级类类名
    /// </summary>
    /// <param name="name">类简单名</param>
    /// <param name="typeArguments">泛型参数</param>
    /// <returns></returns>
    public ClassName PeerClass(string name, IList<TypeName>? typeArguments = null) {
        return new ClassName(ns, enclosingClassName, name, typeArguments);
    }

    /// <summary>
    /// 创建一个嵌套类类名。
    /// 注意：默认情况下会继承当前类的泛型参数。
    /// </summary>
    /// <param name="name">类简单名</param>
    /// <param name="typeArguments">泛型参数</param>
    /// <param name="inheritTypeArguments">是否继承泛型参数</param>
    /// <returns></returns>
    public ClassName NestedClass(string name, IList<TypeName>? typeArguments = null, bool inheritTypeArguments = true) {
        if (inheritTypeArguments) {
            typeArguments = CollectionUtil.Concat(this.typeArguments, typeArguments);
        }
        return new ClassName(ns, this, name, typeArguments);
    }

    /// <summary>
    /// 替换所有的泛型参数（长度必须一致）。
    /// </summary>
    /// <param name="typeArguments">长度必须等于类显式</param>
    /// <returns></returns>
    public ClassName WithTypeVariables(params TypeName[] typeArguments) {
        if (typeArguments.Length != this.typeArguments.Count) {
            throw new ArgumentException();
        }
        return new ClassName(ns, enclosingClassName, simpleName, typeArguments, attributes); // 需保留attributes
    }

    /// <summary>
    /// 构建真实泛型。
    /// 注意：必须从外部类开始构造，参数只接收该类显式定义的泛型参数。
    /// </summary>
    /// <param name="actualTypeArguments">长度必须等于类显式</param>
    /// <returns></returns>
    public ClassName WithActualTypeVariables(params TypeName[] actualTypeArguments) {
        if (actualTypeArguments.Length != declaredTypeArguments.Count) {
            throw new ArgumentException();
        }
        if (enclosingClassName == null || enclosingClassName.typeArguments.Count == 0) {
            return new ClassName(ns, enclosingClassName, simpleName, actualTypeArguments, attributes); // 需保留attributes
        } else {
            List<TypeName> typeArguments = CollectionUtil.Concat(enclosingClassName.typeArguments, actualTypeArguments);
            return new ClassName(ns, enclosingClassName, simpleName, typeArguments, attributes);
        }
    }

    #region Get/Parse

    /// <summary>
    /// 创建一个ClassName
    /// </summary>
    /// <param name="ns">命名空间</param>
    /// <param name="simpleName">类简单名</param>
    /// <param name="typeArguments">泛型参数</param>
    /// <returns></returns>
    public static ClassName Get(string ns, string simpleName, IList<TypeName>? typeArguments = null) {
        return new ClassName(ns, null, simpleName, typeArguments);
    }

    /// <summary>
    /// 通过类型信息解析
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public new static ClassName Get(Type type) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (type.Namespace == null || type.IsArray || type.IsGenericParameter || type.IsPrimitive) {
            throw new ArgumentException("invalid type: " + type);
        }
        if (type == typeof(ValueType)) return VALUE_TYPE;
        if (type == typeof(Attribute)) return ATTRIBUTE;
        if (type == typeof(Nullable<>)) return NULLABLE;
        if (type == typeof(Span<>)) return SPAN;
        if (type == typeof(VoidClass)) return VOID_CLASS;
        return InternalGet(type);
    }

    private static ClassName InternalGet(Type type) {
        // 处理泛型类
        string name = type.Name;
        List<TypeName>? genericArgumentNames = null;
        if (type.IsGenericType) {
            Type[] genericArguments = type.GetGenericArguments();
            genericArgumentNames = new List<TypeName>(genericArguments.Length);
            foreach (Type genericArgument in genericArguments) {
                genericArgumentNames.Add(TypeName.Get(genericArgument));
            }
            int idx = name.LastIndexOf('`');
            if (idx > 0) {
                name = name.Substring(0, idx);
            }
        }
        // 暂不处理匿名类
        if (type.IsNested) {
            return ClassName.Get(type.DeclaringType!).NestedClass(name, genericArgumentNames, false);
        } else {
            return new ClassName(type.Namespace!, null, name, genericArgumentNames);
        }
    }

    #endregion

    #region emit

    // import问题
    // 对于Java来说，导入的粒度是类，因此在写文件时要根据ClassName判断是否是某个类的内部类，从而决定是否import
    // 但对C#来说，导入没有问题

    /// <summary>
    /// 判断目标类型是否是当前类的直接内部类。
    ///
    /// 虽然C#没有导入问题，但和Java一样，对于直接内部类，访问时最好是去除外部类前缀。
    /// 这是个可选优化项，不影响代码的正确性。
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    public bool IsDirectNestedClass(ClassName className) {
        if (className.enclosingClassName == null) {
            return false;
        }
        return Equals(className.enclosingClassName);
    }

    private List<ClassName> EnclosingClasses() {
        List<ClassName> result = new List<ClassName>();
        for (ClassName c = this; c != null; c = c.enclosingClassName) {
            result.Add(c);
        }
        result.Reverse();
        return result;
    }

    #endregion
}
}