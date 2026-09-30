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
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Poet;
using TypeName = Wjybxx.Commons.Poet.TypeName;

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// 生成Codec的常量字段和反射读写方法
/// </summary>
internal class SchemaGenerator
{
    private readonly CodecProcessor processor;
    private readonly Context context;

    public SchemaGenerator(CodecProcessor processor, Context context) {
        this.processor = processor;
        this.context = context;
    }

    public void Execute() {
        context.typeBuilder.AddFields(GenNameFields());
        GenAccessMethods();
    }

    private List<FieldSpec> GenNameFields() {
        List<FieldSpec> result = new List<FieldSpec>();
        HashSet<string> dsonNameSet = new HashSet<string>();

        foreach (AptFieldInfo fieldInfo in context.serialFields) {
            AptFieldProps props = context.fieldPropsMap[fieldInfo];
            string fieldName = fieldInfo.Name;
            string serialName;
            if (!string.IsNullOrWhiteSpace(props.name)) {
                serialName = props.name.Trim();
            } else {
                if (fieldInfo.IsAutoPropertyField) {
                    serialName = fieldInfo.propertySymbol!.Name;
                } else {
                    serialName = fieldName;
                }
                // 显式字段名不参与样式转换
                serialName = context.aptClassProps.nameStyle switch
                {
                    1 => Util.FirstCharToLowerCase(serialName),
                    2 => BeanUtils.ToSnakeCase(serialName),
                    3 => ToCamelCaseNoPrefix(fieldName),
                    _ => serialName
                };
            }
            if (!dsonNameSet.Add(serialName)) {
                throw new Exception($"SerialName {serialName} is duplicate, Type: {context.type}");
            }

            FieldSpec fieldSpec = FieldSpec.NewBuilder(TypeName.STRING, GetNameFieldName(fieldName), Modifiers.Private | Modifiers.Const)
                .Initializer(CodeBlock.Of("$S", serialName))
                .Build();
            result.Add(fieldSpec);
        }
        return result;
    }

    private static string ToCamelCaseNoPrefix(string fieldName) {
        if (fieldName[0] == '_') {
            fieldName = fieldName.Substring(1);
        }
        return Util.FirstCharToLowerCase(fieldName);
    }

    internal static string GetNameFieldName(string rawFieldName) {
        return "names" + GetAccessFieldName(rawFieldName);
    }

    internal static string GetGetValueMethodName(string rawFieldName) {
        return "set" + GetAccessFieldName(rawFieldName);
    }

    internal static string GetSetValueMethodName(string rawFieldName) {
        return "get" + GetAccessFieldName(rawFieldName);
    }

    private static string GetAccessFieldName(string rawFieldName) {
        string val = rawFieldName[0] == '<' // 自动属性字段
            ? rawFieldName.Substring2(1, rawFieldName.IndexOf('>'))
            : rawFieldName;
        return val[0] != '_' ? "_" + val : val;
    }

    private void GenAccessMethods() {
        HashSet<string> memberNames = new HashSet<string>();
        foreach (AptFieldInfo fieldInfo in context.serialFields) {
            bool genGetter = !processor.CanGetDirectly(fieldInfo) && !fieldInfo.HasPublicGetter;
            bool genSetter = !processor.CanSetDirectly(fieldInfo) && !fieldInfo.HasPublicSetter;
            if (!genGetter && !genSetter) {
                continue;
            }
            // 属性必须同时包含getter/setter，读写才统一走属性反射
            bool useProperty = fieldInfo.propertySymbol?.GetMethod != null
                               && fieldInfo.propertySymbol.SetMethod != null;
            string refName = (useProperty ? "refp" : "ref") + GetAccessFieldName(fieldInfo.Name);
            CheckMemberName(memberNames, refName);
            context.typeBuilder.AddField(FieldSpec.NewBuilder(
                    TypeName.Get(useProperty ? typeof(PropertyInfo) : typeof(FieldInfo)),
                    refName, Modifiers.Private | Modifiers.Static | Modifiers.ReadOnly)
                .Initializer(CodeBlock.Of("$L($S)!",
                    useProperty ? "InternalGetProperty" : "InternalGetField",
                    useProperty ? fieldInfo.propertySymbol!.Name : fieldInfo.Name))
                .Build());

            if (genGetter) {
                string methodName = GetGetValueMethodName(fieldInfo.Name);
                CheckMemberName(memberNames, methodName);
                context.typeBuilder.AddMethod(MethodSpec.NewMethodBuilder(methodName)
                    .AddModifiers(Modifiers.Private | Modifiers.Static)
                    .Returns(fieldInfo.typeName)
                    .AddParameter(context.rawTypeName, "inst")
                    .Code(CodeBlock.NewBuilder()
                        .AddStatement("return ($T)$L.GetValue(inst)!", fieldInfo.typeName, refName)
                        .Build())
                    .Build());
            }
            if (genSetter) {
                string methodName = GetSetValueMethodName(fieldInfo.Name);
                CheckMemberName(memberNames, methodName);
                context.typeBuilder.AddMethod(MethodSpec.NewMethodBuilder(methodName)
                    .AddModifiers(Modifiers.Private | Modifiers.Static)
                    .Returns(TypeName.VOID)
                    .AddParameter(context.type.IsValueType ? context.rawTypeName.MakeByRefType() : context.rawTypeName, "inst")
                    .AddParameter(fieldInfo.typeName, "value")
                    .Code(CodeBlock.NewBuilder()
                        .AddStatement("$L.SetValue(inst, value)", refName)
                        .Build())
                    .Build());
            }
        }
    }

    private void CheckMemberName(HashSet<string> memberNames, string name) {
        if (!memberNames.Add(name)) {
            throw new Exception($"reflection member {name} is duplicate, Type: {context.type}");
        }
    }
}
}