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
using Wjybxx.Commons.Poet;
using ClassName = Wjybxx.Commons.Poet.ClassName;
using TypeName = Wjybxx.Commons.Poet.TypeName;

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// 生成Codec的常量字段
/// </summary>
internal class SchemaGenerator
{
    // 新实现下，工厂对象已约定为Func<object>
    private static readonly ClassName className_Func = ClassName.Get(typeof(Func<>));
    private static readonly ClassName factoryTypeName = ClassName.Get(typeof(Func<object>));

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
        foreach (AptFieldInfo fieldInfo in context.serialFields) {
            AptFieldProps props = context.fieldPropsMap[fieldInfo];
            if (props.implTypeName != null) {
                result.Add(GenFactoryField(fieldInfo, props));
            }
        }
        return result;
    }

    // 不能在编译时生成过多的factory，因为即使字段的声明类型是具体类型，其运行时类型仍可能是子类型，因此默认分配factory不安全
    private FieldSpec GenFactoryField(AptFieldInfo fieldInfo, AptFieldProps props) {
        // dotnet 6泛型不支持协变 -- 现在的工厂统一为了Func<object>
        return FieldSpec.NewBuilder(factoryTypeName, GetFactoryFieldName(fieldInfo.Name),
                Modifiers.Public | Modifiers.Static | Modifiers.ReadOnly)
            .Initializer(CodeBlock.Of("() => new $T()", props.implTypeName))
            .Build();
    }

    private List<FieldSpec> GenNameFields() {
        List<FieldSpec> result = new List<FieldSpec>();
        HashSet<string> dsonNameSet = new HashSet<string>();

        foreach (AptFieldInfo fieldInfo in context.serialFields) {
            AptFieldProps props = context.fieldPropsMap[fieldInfo];
            string fieldName = fieldInfo.Name;
            string dsonName;
            if (!string.IsNullOrWhiteSpace(props.name)) {
                dsonName = props.name.Trim();
            } else if (fieldInfo.IsAutoPropertyField) {
                // 自动属性字段使用属性名
                dsonName = fieldInfo.propertySymbol!.Name;
            } else {
                dsonName = fieldName;
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