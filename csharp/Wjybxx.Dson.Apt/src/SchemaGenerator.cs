#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using System.Reflection;
using Wjybxx.Commons;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson.Codec;
using ClassName = Wjybxx.Commons.Poet.ClassName;

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// 生成Codec的常量字段
/// </summary>
internal class SchemaGenerator
{
    private static readonly ClassName className_Func = ClassName.Get(typeof(Func<>));

    private readonly CodecProcessor processor;
    private readonly Context context;

    public SchemaGenerator(CodecProcessor processor, Context context) {
        this.processor = processor;
        this.context = context;
    }

    public void Execute() {
        context.typeBuilder
            .AddFields(GenNameFields())
            .AddFields(GenFactoryFields());
    }

    internal static string GetNameFieldName(string rawFieldName) {
        if (rawFieldName[0] == '<') { // 自动属性字段
            rawFieldName = rawFieldName.Substring2(1, rawFieldName.IndexOf('>'));
        }
        string nameFieldName = rawFieldName[0] == '_'
            ? "names" + rawFieldName
            : "names_" + rawFieldName;
        return nameFieldName;
    }

    internal static string GetFactoryFieldName(string rawFieldName) {
        if (rawFieldName[0] == '<') { // 自动属性字段
            rawFieldName = rawFieldName.Substring2(1, rawFieldName.IndexOf('>'));
        }
        string factoryFieldName = rawFieldName[0] == '_'
            ? "factories" + rawFieldName
            : "factories_" + rawFieldName;
        return factoryFieldName;
    }

    private List<FieldSpec> GenFactoryFields() {
        List<FieldSpec> result = new List<FieldSpec>();
        foreach (FieldInfo fieldInfo in context.serialFields) {
            AptFieldProps props = context.fieldPropsMap[fieldInfo];
            // 集合类型默认指定实现类
            if (props.implType == null && IsConcreteCollection(fieldInfo.FieldType)) {
                props.implType = fieldInfo.FieldType;
            }
            if (props!.implType != null) {
                result.Add(GenFactoryField(fieldInfo, props));
            }
        }
        return result;
    }

    private static bool IsConcreteCollection(Type type) {
        return type.IsClass
               && !type.IsAbstract
               && DsonConverterUtils.IsCollection(type, true)
               && type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, 
                   binder: null, Array.Empty<Type>(), modifiers: null) != null;
    }

    private FieldSpec GenFactoryField(MemberInfo memberInfo, AptFieldProps props) {
        // Type declaredType;
        // if (memberInfo is FieldInfo fieldInfo) {
        //     declaredType = fieldInfo.FieldType;
        // } else {
        //     PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
        //     declaredType = propertyInfo.PropertyType;
        // }
        // C#6泛型不支持协变
        ClassName factoryFieldType = className_Func.WithActualTypeVariables(TypeName.Get(props.implType!));
        return FieldSpec.NewBuilder(factoryFieldType, GetFactoryFieldName(memberInfo.Name),
                Modifiers.Public | Modifiers.Static | Modifiers.Readonly)
            .Initializer(CodeBlock.Of("() => new $T()", props.implType!))
            .Build();
    }

    private List<FieldSpec> GenNameFields() {
        List<FieldSpec> result = new List<FieldSpec>();
        HashSet<string> dsonNameSet = new HashSet<string>();

        foreach (FieldInfo fieldInfo in context.serialFields) {
            AptFieldProps props = context.fieldPropsMap[fieldInfo];
            string fieldName = fieldInfo.Name;
            string dsonName;
            if (!string.IsNullOrWhiteSpace(props.attribute.Name)) {
                dsonName = props.attribute.Name.Trim();
            } else if (props.autoProperty != null) {
                // 自动属性使用属性名
                dsonName = props.autoProperty.Name;
            } else {
                // 普通私有字段去除下划线
                dsonName = (fieldName[0] == '_' && fieldName[1] != '_')
                    ? fieldName.Substring(1)
                    : fieldName;
            }
            if (!dsonNameSet.Add(dsonName)) {
                throw new Exception($"dsonName {dsonName} is duplicate, Type: {context.type}");
            }

            FieldSpec fieldSpec = FieldSpec.NewBuilder(TypeName.STRING, GetNameFieldName(fieldName), Modifiers.Public | Modifiers.Const)
                .Initializer(CodeBlock.Of("$S", dsonName))
                .Build();
            result.Add(fieldSpec);
        }
        return result;
    }
}
}