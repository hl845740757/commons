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
using Microsoft.CodeAnalysis;
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Poet;

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// 
/// </summary>
internal class AptFieldProps
{
    public const string DEFAULT_NUMBER_STYLE = "Simple";
    public const string DEFAULT_STRING_STYLE = "Auto";

#nullable disable
    /** 字段序列化时的名字 */
    public string name = "";
    /** 取值方法 */
    public string getter = "";
    /** 赋值方法 */
    public string setter = "";

    /** 实现类 -- 会被替换（修正泛型参数） */
    private INamedTypeSymbol implType;
    public TypeName implTypeName;
    /** 写代理方法名 */
    public string writeProxy = "";
    /** 读代理方法名 */
    public string readProxy = "";

    /** 绑定类型 */
    public string dsonType = null; // 该属性只有显式声明才有效
    public int dsonSubType = 0;

    /** 绑定style -- 枚举取出来是数字 */
    public string numberStyle = DEFAULT_NUMBER_STYLE;
    public string stringStyle = DEFAULT_STRING_STYLE;
    public string? objectStyle = null; // 该属性只有显式声明才有效

    /** 是否忽略 -- 非null表示注解指定了值 */
    public bool? ignore;
#nullable enable

    /// <summary>
    /// 关联的自动属性（缓存）
    /// </summary>
    public IPropertySymbol? autoProperty;

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static AptFieldProps Parse(ISymbol fieldOrPropertySymbol, string attributeClassName,
                                      INamedTypeSymbol typeNumberStyle,
                                      INamedTypeSymbol typeStringStyle,
                                      INamedTypeSymbol typeObjectStyle) {
        AttributeData? attributeData = AptUtils.GetAttribute(fieldOrPropertySymbol.GetAttributes(), attributeClassName);
        if (attributeData == null) {
            return new AptFieldProps();
        }
        AptFieldProps props = new AptFieldProps();
        props.name = GetStringValue(attributeData, "Name", props.name);
        props.getter = GetStringValue(attributeData, "Getter", props.getter);
        props.setter = GetStringValue(attributeData, "Setter", props.setter);

        props.dsonType = GetStringValue(attributeData, "DsonType", props.dsonType);
        props.dsonSubType = GetIntValue(attributeData, "DsonSubType", props.dsonSubType);

        props.numberStyle = GetEnumStringValue(attributeData, "NumberStyle", props.numberStyle, typeNumberStyle);
        props.stringStyle = GetEnumStringValue(attributeData, "StringStyle", props.stringStyle, typeStringStyle);
        props.objectStyle = GetEnumStringValue(attributeData, "ObjectStyle", props.objectStyle, typeObjectStyle);

        props.writeProxy = GetStringValue(attributeData, "WriteProxy", props.writeProxy);
        props.readProxy = GetStringValue(attributeData, "ReadProxy", props.readProxy);
        // 需要将字段的泛型参数拷贝给Impl
        {
            if (AptUtils.GetAttributeValue(attributeData, "Impl", out TypedConstant typedConstant)) {
                INamedTypeSymbol? fieldType = BeanUtils.GetFieldType(fieldOrPropertySymbol) as INamedTypeSymbol;
                INamedTypeSymbol? implType = typedConstant.Value as INamedTypeSymbol;
                if (fieldType == null || implType == null) {
                    throw new InvalidOperationException();
                }
                if (implType.IsUnboundGenericType) {
                    implType = implType.ConstructedFrom.Construct(fieldType.TypeArguments.ToArray());
                }
                props.implType = implType;
                props.implTypeName = AptUtils.ParseType(implType);
            } else {
                props.implType = null;
            }
        }
        return props;
    }

    public void ParseIgnore(ISymbol memberInfo, string attributeClassName) {
        AttributeData? attributeData = AptUtils.GetAttribute(memberInfo.GetAttributes(), attributeClassName);
        if (attributeData == null) {
            return;
        }
        // 属性在构造函数中
        TypedConstant typedConstant = attributeData.ConstructorArguments[0];
        ignore = (bool?)typedConstant.Value;
    }

    private static string? GetStringValue(AttributeData attributeData, string propertyName, string? defValue) {
        if (AptUtils.GetAttributeValue(attributeData, propertyName, out TypedConstant typedConstant)) {
            return typedConstant.GetValueAsString() ?? defValue;
        }
        return defValue;
    }
    private static string? GetEnumStringValue(AttributeData attributeData, string propertyName, string? defValue,
                                              INamedTypeSymbol typeSymbol) {
        if (AptUtils.GetAttributeValue(attributeData, propertyName, out TypedConstant typedConstant)) {
            if (typedConstant.Value is int value) {
                return AptUtils.GetEnumName(typeSymbol, value);
            }
        }
        return defValue;
    }
    

    private static int GetIntValue(AttributeData attributeData, string propertyName, int defValue) {
        if (AptUtils.GetAttributeValue(attributeData, propertyName, out TypedConstant typedConstant)) {
            if (typedConstant.Value is int value) {
                return value;
            }
            string? stringValue = typedConstant.GetValueAsString();
            if (string.IsNullOrWhiteSpace(stringValue)) return defValue;
            if (int.TryParse(stringValue, out int r)) return r;
        }
        return defValue;
    }
}
}