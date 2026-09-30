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
#nullable disable
    /** 字段序列化时的名字 */
    public string? name;
    /** 取值方法 */
    public string? getter;
    /** 赋值方法 */
    public string? setter;
    /** 序列化特征值 */
    public int encodeFeatures;
    /** 反序列化特征值 */
    public int decodeFeatures;
    /** 向量分量的名字 */
    public string? elementNames;

    /** 目标类型 -- 会被替换（修正泛型参数） */
    public INamedTypeSymbol? targetType;
    /** 目标类型的TypeName缓存 */
    public TypeName? targetTypeName;
    /** 写代理方法名 */
    public string? writeProxy;
    /** 读代理方法名 */
    public string? readProxy;

    /** 是否忽略 -- 非null表示注解指定了值 */
    public bool? ignore;
    /** 是否序列化为引用 */
    public bool? serializeReference;
#nullable restore

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static AptFieldProps Parse(AptFieldInfo fieldInfo, string attributeClassName,
                                      Compilation compilation) {
        AptAttributeData? attributeData = fieldInfo.GetAttribute(attributeClassName);
        if (attributeData == null) {
            return new AptFieldProps();
        }
        if (attributeData.CompilationData != null) {
            return ParseByCompilationData(fieldInfo, attributeData.CompilationData, compilation);
        }
        return ParseByReflectionData(fieldInfo, attributeData.ReflectionData, compilation);
    }

    private void ParseTargetType(AptFieldInfo fieldInfo, INamedTypeSymbol? targetType,
                                 Compilation compilation) {
        ITypeSymbol? fieldType = fieldInfo.FieldType;
        if (fieldType == null || targetType == null) {
            throw new InvalidOperationException($"无法解析字段'{fieldInfo.Name}'的TargetType或声明类型。");
        }
        // 生成器使用typed null传递目标类型，只支持引用目标；不要求无参构造函数
        if (!targetType.IsReferenceType) {
            throw new InvalidOperationException($"字段'{fieldInfo.Name}'的TargetType '{targetType}'必须为引用类型。");
        }
        // 不测试IsUnboundGenericType，保留将声明类型的泛型参数拷贝给TargetType的约定
        if (targetType.IsGenericType) {
            INamedTypeSymbol? namedFieldType = fieldType as INamedTypeSymbol;
            int argumentCount = namedFieldType?.TypeArguments.Length ?? 0;
            if (namedFieldType == null || targetType.Arity != argumentCount) {
                throw new InvalidOperationException($"字段'{fieldInfo.Name}'的TargetType '{targetType}'需要{targetType.Arity}个泛型参数，但声明类型'{fieldType}'提供了{argumentCount}个。");
            }
            targetType = targetType.OriginalDefinition.Construct(namedFieldType.TypeArguments.ToArray());
        }
        this.targetType = targetType;
        this.targetTypeName = AptUtils.ParseType(targetType).RemoveAllNullableAttribute();
    }

    #region parse-compilation

    private static AptFieldProps ParseByCompilationData(AptFieldInfo fieldInfo, AttributeData attributeData,
                                                        Compilation compilation) {
        AptFieldProps props = new AptFieldProps();
        props.name = GetStringValue(attributeData, "Name", props.name);
        props.getter = GetStringValue(attributeData, "Getter", props.getter);
        props.setter = GetStringValue(attributeData, "Setter", props.setter);
        props.encodeFeatures = GetIntValue(attributeData, "EncodeFeatures", props.encodeFeatures);
        props.decodeFeatures = GetIntValue(attributeData, "DecodeFeatures", props.decodeFeatures);
        props.elementNames = GetStringValue(attributeData, "ElementNames", props.elementNames);

        props.writeProxy = GetStringValue(attributeData, "WriteProxy", props.writeProxy);
        props.readProxy = GetStringValue(attributeData, "ReadProxy", props.readProxy);

        if (AptUtils.GetAttributeValue(attributeData, "TargetType", out TypedConstant typedConstant)) {
            props.ParseTargetType(fieldInfo, typedConstant.Value as INamedTypeSymbol, compilation);
        }
        return props;
    }

    private static string? GetStringValue(AttributeData attributeData, string propertyName, string? defValue) {
        if (AptUtils.GetAttributeValue(attributeData, propertyName, out TypedConstant typedConstant)) {
            return typedConstant.GetValueAsString() ?? defValue;
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
    private static PropertyInfo refPropertyEncodeFeatures;
    private static PropertyInfo refPropertyDecodeFeatures;
    private static PropertyInfo refPropertyElementNames;

    private static PropertyInfo refPropertyTargetType;
    private static PropertyInfo refPropertyWriteProxy;
    private static PropertyInfo refPropertyReadProxy;
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
            Type type = Type.GetType(CodecProcessor.CNAME_DSON_PROPERTY);
            if (type == null) {
                throw new Exception($"load type {CodecProcessor.CNAME_DSON_PROPERTY} failed");
            }
            refPropertyName = type.GetProperty("Name");
            refPropertyGetter = type.GetProperty("Getter");
            refPropertySetter = type.GetProperty("Setter");
            refPropertyEncodeFeatures = type.GetProperty("EncodeFeatures");
            refPropertyDecodeFeatures = type.GetProperty("DecodeFeatures");
            refPropertyElementNames = type.GetProperty("ElementNames");

            refPropertyTargetType = type.GetProperty("TargetType");
            refPropertyWriteProxy = type.GetProperty("WriteProxy");
            refPropertyReadProxy = type.GetProperty("ReadProxy");
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
                                                       Compilation compilation) {
        InitReflectEnv();
        //
        AptFieldProps props = new AptFieldProps();
        props.name = (string)refPropertyName.GetValue(attribute);
        props.getter = (string)refPropertyGetter.GetValue(attribute);
        props.setter = (string)refPropertySetter.GetValue(attribute);
        props.encodeFeatures = (int)refPropertyEncodeFeatures.GetValue(attribute);
        props.decodeFeatures = (int)refPropertyDecodeFeatures.GetValue(attribute);
        props.elementNames = (string)refPropertyElementNames.GetValue(attribute);

        props.writeProxy = (string)refPropertyWriteProxy.GetValue(attribute);
        props.readProxy = (string)refPropertyReadProxy.GetValue(attribute);

        // 反射类型先还原为定义，再统一按声明类型闭合泛型参数
        if (refPropertyTargetType.GetValue(attribute) is Type targetTyp0) {
            INamedTypeSymbol? targetType = compilation.GetTypeByMetadataName(targetTyp0.ToString());
            props.ParseTargetType(fieldInfo, targetType, compilation);
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

    public void ParseSerializeReference(AptFieldInfo fieldInfo, string attributeClassName) {
        AptAttributeData? attributeData = fieldInfo.GetAttribute(attributeClassName);
        if (attributeData == null) {
            return;
        }
        serializeReference = true;
        encodeFeatures |= 0x01; // 序列化引用
        decodeFeatures |= 0x01;
    }

    #endregion
}
}