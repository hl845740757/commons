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
    public string? name;
    /** 取值方法 */
    public string? getter;
    /** 赋值方法 */
    public string? setter;

    /** 实现类 -- 会被替换（修正泛型参数） */
    private INamedTypeSymbol? implType;
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

    /** 是否忽略 -- 非null表示注解指定了值 */
    public bool? ignore;
#nullable restore

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static AptFieldProps Parse(AptFieldInfo fieldInfo, string attributeClassName,
                                      INamedTypeSymbol typeNumberStyle,
                                      INamedTypeSymbol typeStringStyle,
                                      INamedTypeSymbol typeObjectStyle,
                                      Compilation compilation) {
        AptAttributeData? attributeData = fieldInfo.GetAttribute(attributeClassName);
        if (attributeData == null) {
            return new AptFieldProps();
        }
        if (attributeData.CompilationData != null) {
            return ParseByCompilationData(fieldInfo, attributeData.CompilationData,
                typeNumberStyle, typeStringStyle, typeObjectStyle);
        }
        return ParseByReflectionData(fieldInfo, attributeData.ReflectionData,
            typeNumberStyle, typeStringStyle, typeObjectStyle,
            compilation);
    }

    #region parse-compilation

    private static AptFieldProps ParseByCompilationData(AptFieldInfo fieldInfo, AttributeData attributeData,
                                                        INamedTypeSymbol typeNumberStyle,
                                                        INamedTypeSymbol typeStringStyle,
                                                        INamedTypeSymbol typeObjectStyle) {
        AptFieldProps props = new AptFieldProps();
        props.name = GetStringValue(attributeData, "Name", props.name);
        props.getter = GetStringValue(attributeData, "Getter", props.getter);
        props.setter = GetStringValue(attributeData, "Setter", props.setter);

        // props.dsonType = GetStringValue(attributeData, "DsonType", props.dsonType);
        // props.dsonSubType = GetIntValue(attributeData, "DsonSubType", props.dsonSubType);

        props.numberStyle = GetEnumStringValue(attributeData, "NumberStyle", props.numberStyle, typeNumberStyle);
        props.stringStyle = GetEnumStringValue(attributeData, "StringStyle", props.stringStyle, typeStringStyle);
        props.objectStyle = GetEnumStringValue(attributeData, "ObjectStyle", null, typeObjectStyle);

        props.writeProxy = GetStringValue(attributeData, "WriteProxy", props.writeProxy);
        props.readProxy = GetStringValue(attributeData, "ReadProxy", props.readProxy);
        // 需要将字段的泛型参数拷贝给Impl
        {
            if (AptUtils.GetAttributeValue(attributeData, "Impl", out TypedConstant typedConstant)) {
                INamedTypeSymbol? fieldType = fieldInfo.FieldType as INamedTypeSymbol;
                INamedTypeSymbol? implType = typedConstant.Value as INamedTypeSymbol;
                if (fieldType == null || implType == null) {
                    throw new InvalidOperationException("fieldType == null || implType == null");
                }
                // 不测试IsUnboundGenericType，因为可能绑定了一部分...
                if (implType.IsGenericType) {
                    implType = implType.OriginalDefinition.Construct(fieldType.TypeArguments.ToArray());
                }
                props.implType = implType;
                props.implTypeName = AptUtils.ParseType(implType).RemoveAllNullableAttribute();
            } else {
                props.implType = null;
                props.implTypeName = null;
            }
        }
        return props;
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

    private static bool GetBoolValue(AttributeData attributeData, string propertyName, bool defValue) {
        if (AptUtils.GetAttributeValue(attributeData, propertyName, out TypedConstant typedConstant)) {
            if (typedConstant.Value is bool value) {
                return value;
            }
        }
        return defValue;
    }

    #endregion

    #region parse-reflection

#nullable disable
    // DsonProperty
    private static PropertyInfo refPropertyName;
    private static PropertyInfo refPropertyGetter;
    private static PropertyInfo refPropertySetter;

    private static PropertyInfo refPropertyNumberStyle;
    private static PropertyInfo refPropertyStringStyle;
    private static PropertyInfo refPropertyObjectStyle;
    private static PropertyInfo refPropertyHasStyle;

    private static PropertyInfo refPropertyWriteProxy;
    private static PropertyInfo refPropertyReadProxy;
    private static PropertyInfo refPropertyImpl;
    // DsonIgnore
    private static PropertyInfo refPropertyIgnoreValue;

    /// <summary>
    /// 该方法只有出现反射数据的时候才可调用
    /// </summary>
    private static void InitReflectEnv() {
        if (refPropertyIgnoreValue != null) {
            return;
        }
        {
            Type type = Type.GetType(CodecProcessor.CNAME_PROPERTY);
            if (type == null) {
                throw new Exception($"load type {CodecProcessor.CNAME_PROPERTY} failed");
            }
            refPropertyName = type.GetProperty("Name");
            refPropertyGetter = type.GetProperty("Getter");
            refPropertySetter = type.GetProperty("Setter");

            refPropertyNumberStyle = type.GetProperty("NumberStyle");
            refPropertyStringStyle = type.GetProperty("StringStyle");
            refPropertyObjectStyle = type.GetProperty("ObjectStyle");
            refPropertyHasStyle = type.GetProperty("HasObjectStyle");

            refPropertyWriteProxy = type.GetProperty("WriteProxy");
            refPropertyReadProxy = type.GetProperty("ReadProxy");
            refPropertyImpl = type.GetProperty("Impl");
        }
        {
            Type type = Type.GetType(CodecProcessor.CNAME_DSON_IGNORE);
            if (type == null) {
                throw new Exception($"load type {CodecProcessor.CNAME_DSON_IGNORE} failed");
            }
            refPropertyIgnoreValue = type.GetProperty("Value");
        }
    }
#nullable restore

    /// <summary>
    /// 走到这里，证明是解析第三方程序集中某Class的字段，证明第三方程序集引用了Dson-Codec库，
    /// 在编译的过程中由于反射的原因，会导致Dson-Codec程序集被加载到内存，此时我们才能使用反射获取数据。
    /// </summary>
    private static AptFieldProps ParseByReflectionData(AptFieldInfo fieldInfo, Attribute attribute,
                                                       INamedTypeSymbol typeNumberStyle,
                                                       INamedTypeSymbol typeStringStyle,
                                                       INamedTypeSymbol typeObjectStyle,
                                                       Compilation compilation) {
        InitReflectEnv();
        //
        AptFieldProps props = new AptFieldProps();
        props.name = (string)refPropertyName.GetValue(attribute);
        props.getter = (string)refPropertyGetter.GetValue(attribute);
        props.setter = (string)refPropertySetter.GetValue(attribute);

        props.numberStyle = AptUtils.GetEnumName(typeNumberStyle, (int)refPropertyNumberStyle.GetValue(attribute));
        props.stringStyle = AptUtils.GetEnumName(typeStringStyle, (int)refPropertyStringStyle.GetValue(attribute));
        if ((bool)refPropertyHasStyle.GetValue(attribute)) { // objectStyle必须显式声明
            props.objectStyle = AptUtils.GetEnumName(typeObjectStyle, (int)refPropertyObjectStyle.GetValue(attribute));
        }
        props.writeProxy = (string)refPropertyWriteProxy.GetValue(attribute);
        props.readProxy = (string)refPropertyReadProxy.GetValue(attribute);

        // 修正实现类的泛型参数
        Type? implType = refPropertyImpl.GetValue(attribute) as Type;
        if (implType != null) {
            if (implType.IsGenericType) {
                implType = implType.GetGenericTypeDefinition();
            }
            INamedTypeSymbol? fieldType = fieldInfo.FieldType as INamedTypeSymbol;
            INamedTypeSymbol? implTypeSymbol = compilation.GetTypeByMetadataName(implType.ToString());
            if (fieldType == null || implTypeSymbol == null) {
                throw new InvalidOperationException("implTypeSymbol == null");
            }
            if (implTypeSymbol.IsGenericType) {
                implTypeSymbol = implTypeSymbol.OriginalDefinition.Construct(fieldType.TypeArguments.ToArray());
            }
            props.implType = implTypeSymbol;
            props.implTypeName = AptUtils.ParseType(implTypeSymbol).RemoveAllNullableAttribute();
        }
        return props;
    }

    #endregion

    #region parse-ignore

    public void ParseIgnore(AptFieldInfo fieldInfo, string attributeClassName) {
        AptAttributeData? attributeData = fieldInfo.GetAttribute(attributeClassName);
        if (attributeData == null) {
            return;
        }
        if (attributeData.CompilationData != null) {
            // 属性在构造函数中
            TypedConstant typedConstant = attributeData.CompilationData.ConstructorArguments[0];
            ignore = (bool)typedConstant.Value!;
        } else if (attributeData.ReflectionData != null) {
            // Value属性
            ignore = (bool)refPropertyIgnoreValue.GetValue(attributeData.ReflectionData);
        }
    }

    #endregion
}
}