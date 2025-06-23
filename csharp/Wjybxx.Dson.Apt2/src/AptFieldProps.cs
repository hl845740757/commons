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
using System.Linq;
using System.Reflection;
using Wjybxx.Commons;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson.Codec.Attributes;
using TypeName = Wjybxx.Commons.Poet.TypeName;

namespace Wjybxx.Dson.Apt2
{
/// <summary>
/// 我们不直接使用<code>Attribute类</code>，
/// 尽可能使两套生成器只在解析层有差异。
/// </summary>
internal class AptFieldProps
{
    public const string DEFAULT_NUMBER_STYLE = "Simple";
    public const string DEFAULT_STRING_STYLE = "Auto";

#nullable disable
    /** 字段序列化时的名字 */
    public string? name;
    /** 取值方法 */
    public string? getter;
    /** 赋值方法 */
    public string? setter;
    /** 是否不可变 */
    public bool isImmutable;

    /** 实现类 -- 会被替换（修正泛型参数） */
    private Type? implType;
    /** 实现类的TypeName缓存 */
    public TypeName? implTypeName;
    /** 写代理方法名 */
    public string? writeProxy;
    /** 读代理方法名 */
    public string? readProxy;

    /** 绑定style -- 需要正确初始化，可能没有注解 */
    public string numberStyle = DEFAULT_NUMBER_STYLE;
    public string stringStyle = DEFAULT_STRING_STYLE;
    public string? objectStyle = null; // 该属性只有显式声明才有效

    /// <summary>
    /// 是否不序列化 -- 非null表示注解指定了值
    /// </summary>
    public bool? ignore;
#nullable enable

    public static AptFieldProps Parse(AptFieldInfo fieldInfo) {
        DsonPropertyAttribute? attribute = fieldInfo.GetAttribute<DsonPropertyAttribute>();
        if (attribute == null) {
            return new AptFieldProps();
        }
        AptFieldProps props = new AptFieldProps();
        props.name = attribute.Name;
        props.getter = attribute.Getter;
        props.setter = attribute.Setter;
        props.isImmutable = attribute.IsImmutable;

        props.numberStyle = EnumUtil.GetName(attribute.NumberStyle)!;
        props.stringStyle = EnumUtil.GetName(attribute.StringStyle)!;
        // objectStyle必须显式声明
        if (attribute.HasObjectStyle) {
            props.objectStyle = EnumUtil.GetName(attribute.ObjectStyle)!;
        }

        props.writeProxy = attribute.WriteProxy;
        props.readProxy = attribute.ReadProxy;
        // 需要将字段的泛型参数拷贝给Impl
        {
            Type? implType = attribute.Impl;
            if (implType != null && implType.IsGenericType) {
                Type[] genericArguments = fieldInfo.FieldType.GetGenericArguments();
                implType = implType.GetGenericTypeDefinition().MakeGenericType(genericArguments);
            }
            if (implType != null) {
                props.implType = implType;
                props.implTypeName = ClassName.Get(implType);
            }
        }
        return props;
    }

    /// <summary>
    /// 解析DsonIgnore注解
    /// </summary>
    /// <param name="fieldInfo"></param>
    public void ParseIgnore(AptFieldInfo fieldInfo) {
        DsonIgnoreAttribute? ignoreAttribute = fieldInfo.GetAttribute<DsonIgnoreAttribute>();
        if (ignoreAttribute == null) {
            return;
        }
        ignore = ignoreAttribute.Value;
    }
}
}