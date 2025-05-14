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
using System.Text;

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 泛型变量引用
/// 
/// 注意：
/// 1.并不是只有该类型才可以作为泛型参数，只是使用该类型时表示未构造泛型。
/// 2.它只是目标泛型变量的引用，因此不包含边界谢谢，但可以追加Nullable注解信息。
/// </summary>
public class TypeParameterName : TypeName
{
    /// <summary>
    /// 空类型变量仅用于输出泛型定义类时
    /// (也用于未绑定泛型)
    /// </summary>
    public static readonly TypeParameterName Empty = new TypeParameterName("", TypeNameAttributes.None);

    /// <summary>
    /// 泛型变量名(允许空字符串表示泛型定义类)
    /// </summary>
    public readonly string name;

    private TypeParameterName(string name, TypeNameAttributes attributes)
        : base(attributes) {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
    }

    #region overrides

    /// <summary>
    /// 反射名
    /// </summary>
    /// <returns></returns>
    public override string ReflectionName() => name;

    protected override string ToStringImpl() {
        StringBuilder sb = new StringBuilder(16);
        sb.Append(GetType().Name);
        sb.Append(", name: ");
        sb.Append(name);
        return sb.ToString();
    }

#if NET6_0_OR_GREATER
    public override TypeParameterName WithAttributes(TypeNameAttributes attributes) {
#else
    public override TypeName WithAttributes(TypeNameAttributes attributes) {
#endif
        if (this.attributes == attributes) return this;
        return new TypeParameterName(name, attributes);
    }

#if NET6_0_OR_GREATER
    public override TypeParameterName RemoveAllNullableAttribute() {
#else
    public override TypeName RemoveAllNullableAttribute() {
#endif
        if (!attributes.IsIntersect(TypeNameAttributes.NullableReferenceType)) return this;
        return Get(name, attributes.Unset(TypeNameAttributes.NullableReferenceType));
    }

    #endregion

    #region parse/get

    /// <summary>
    /// 构建泛型变量名
    /// </summary>
    /// <param name="name">泛型名</param>
    /// <param name="attributes">泛型属性</param>
    /// <returns></returns>
    public static TypeParameterName Get(string name, TypeNameAttributes attributes = TypeNameAttributes.None) {
        return new TypeParameterName(name, attributes);
    }

    /// <summary>
    /// 通过泛型变量Type实例解析信息
    /// 注意：
    /// 1. C#的泛型参数使用struct关键字约束时，会添加<see cref="ValueType"/>为上界，会自动去除。
    /// 2. 反射无法获取NotNull约束
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public new static TypeParameterName Get(Type type) {
        if (!type.IsGenericParameter) {
            throw new ArgumentException("type is not generic parameter");
        }
        // 转换Attributes
        TypeNameAttributes attributes = TypeNameAttributes.None;
#if NET6_0_OR_GREATER
        // if (type.IsDefined(typeof(NullableAttribute))) {
        // attributes |= TypeNameAttributes.NullableReferenceType;
        // }
#endif
        return new TypeParameterName(type.Name, attributes);
    }

    #endregion
}
}