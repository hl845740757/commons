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
using NUnit.Framework;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;

namespace Commons.Tests.Apt;

public class MyCodeAttribute : Attribute
{
    public string Name { get; set; }
    public int Age { get; set; }
}

public class GeneratorTest
{
    private static List<ISpecification> NewLicenseRegion() {
        List<ISpecification> result = new List<ISpecification>(3);
        result.Add(new MacroSpec("region", "LICENSE"));
        result.Add(new CodeBlockSpec(CodeBlock.Of("\n")));
        result.Add(new CodeBlockSpec(CodeBlock.NewBuilder()
            .AddLiteral("Copyright 2024 wjybxx(845740757@qq.com)")
            .AddNewLine(2)
            .AddLiteral("Licensed under the Apache License, Version 2.0 (the \"License\");")
            .Build(), CodeBlockSpec.Kind.Comment));
        result.Add(new MacroSpec("endregion", "LICENSE"));
        return result;
    }

    [Test]
    public void GenerateBean() {
        TypeSpec classType = BuildClassType();
        TypeSpec delegatorType = BuildDelegatorType(); //测试委托打印
        TypeSpec recordType = BuildRecordType();
        TypeSpec indexerType = BuildIndexerType(); //测试索引器属性

        CsharpFile csharpFile = CsharpFile.NewBuilder("ClassBean")
            .AddSpecs(NewLicenseRegion())
            .AddSpec(NamespaceSpec.Of("Wjybxx.Commons.Apt", classType, delegatorType, recordType, indexerType))
            .Build();

        CodeWriter codeWriter = new CodeWriter();
        codeWriter.EnableFileScopedNamespace = true;
        string fileString = codeWriter.Write(csharpFile);
        Console.WriteLine(fileString);
    }

    private static TypeSpec BuildIndexerType() {
        return TypeSpec.NewClassBuilder("MyDictionary")
            .AddModifiers(Modifiers.Public)
            .AddTypeParameter(TypeParameterSpec.Get("TKey"))
            .AddTypeParameter(TypeParameterSpec.Get("TValue"))
            .AddProperty(PropertySpec.Overriding(typeof(IDictionary<,>).GetProperties()[0])
                .Build())
            .Build();
    }
    
    private static TypeSpec BuildRecordType() {
        // public record MinMax<T>(T min, T max);
        return TypeSpec.NewRecordBuilder("MinMax")
            .AddModifiers(Modifiers.Public)
            .AddTypeParameter(TypeParameterSpec.Get("T"))
            .AddSpec(FieldSpec.NewBuilder(TypeParameterName.Get("T"), "min").Build())
            .AddSpec(FieldSpec.NewBuilder(TypeParameterName.Get("T"), "max").Build())
            .Build();
    }
    
    private static TypeSpec BuildDelegatorType() {
        // public delegate int Apply<T>(T obj);
        return TypeSpec.NewDelegator(MethodSpec.NewMethodBuilder("Apply")
            .AddModifiers(Modifiers.Public)
            .Returns(TypeName.INT)
            .AddTypeParameter(TypeParameterSpec.Get("T"))
            .AddParameter(TypeParameterName.Get("T"), "obj")
            .Build());
    }

    private static TypeSpec BuildClassType() {
        TypeName dictionaryTypeName = TypeName.Get(typeof(LinkedDictionary<string, object>));
        AttributeSpec processorAttribute = AttributeSpec.NewBuilder(ClassName.Get(typeof(GeneratedAttribute)))
            .Constructor(CodeBlock.Of("$S", "GeneratorTest")) // 字符串$S
            .Build();

        AttributeSpec attributeSpec = AttributeSpec.NewBuilder(ClassName.Get(typeof(MyCodeAttribute)))
            .AddMember("Name", CodeBlock.Of("$S", "wjybxx"))
            .AddMember("Age", CodeBlock.Of("29"))
            .Build();

        TypeSpec classType = TypeSpec.NewClassBuilder("ClassBean")
            .AddModifiers(Modifiers.Public)
            .AddAttribute(processorAttribute)
            .AddAttribute(attributeSpec)
            // 字段
            .AddField(TypeName.INT, "age", Modifiers.Private)
            .AddField(TypeName.STRING, "name", Modifiers.Private)
            .AddSpec(FieldSpec.NewBuilder(dictionaryTypeName, "blackboard", Modifiers.Public | Modifiers.ReadOnly)
                .Initializer("new $T()", dictionaryTypeName)
                .Build())
            // 构造函数
            .AddSpec(MethodSpec.NewConstructorBuilder()
                .AddModifiers(Modifiers.Public)
                .ConstructorInvoker(CodeBlock.Of("this($L, $S)", 29, "wjybxx"))
                .Build())
            .AddSpec(MethodSpec.NewConstructorBuilder()
                .AddModifiers(Modifiers.Public)
                .AddParameter(TypeName.INT, "age")
                .AddParameter(TypeName.STRING, "name")
                .Code(CodeBlock.NewBuilder()
                    .AddStatement("this.age = age")
                    .AddStatement("this.name = name")
                    .Build())
                .Build())
            // 属性
            .AddSpec(PropertySpec.NewBuilder(TypeName.INT, "Age", Modifiers.Public)
                .Getter(CodeBlock.Of("age").WithExpressionStyle(true))
                .Setter(CodeBlock.Of("age = value").WithExpressionStyle(true))
                .Build())
            .AddSpec(PropertySpec.NewBuilder(TypeName.BOOL, "IsOnline", Modifiers.Private)
                .Initializer("$L", false)
                .Build()
            )
            // 普通方法
            .AddSpec(MethodSpec.NewMethodBuilder("Sum")
                .AddDocument("求int的和")
                .AddModifiers(Modifiers.Public)
                .Returns(TypeName.INT)
                .AddParameter(TypeName.INT, "a")
                .AddParameter(TypeName.INT, "b")
                .Code(CodeBlock.NewBuilder()
                    .AddStatement("return a + b")
                    .Build())
                .Build())
            .AddSpec(MethodSpec.NewMethodBuilder("SumNullable")
                .AddDocument("求空int的和")
                .AddModifiers(Modifiers.Public | Modifiers.Extern)
                .Returns(TypeName.INT.MakeNullableType())
                .AddParameter(TypeName.INT.MakeNullableType(), "a")
                .AddParameter(TypeName.INT, "b")
                .Build())
            .AddSpec(MethodSpec.NewMethodBuilder("SumRef")
                .AddDocument("求ref int的和")
                .AddModifiers(Modifiers.Public | Modifiers.Extern)
                .Returns(TypeName.INT)
                .AddParameter(TypeName.INT.MakeByRefType(), "a")
                .AddParameter(TypeName.INT.MakeByRefType(ByRefTypeName.Kind.In), "b")
                .Build())
            .AddSpec(MethodSpec.NewMethodBuilder("MinMax")
                .AddDocument("测试返回元组")
                .AddModifiers(Modifiers.Public)
                .Returns(TupleTypeName.Get(new List<TupleElement>()
                {
                    new TupleElement(TypeName.INT, "min"),
                    new TupleElement(TypeName.INT, "max")
                }))
                .AddParameter(TypeName.INT, "a")
                .AddParameter(TypeName.INT, "b")
                .Code(CodeBlock.Of("a < b ? (a, b) : (b, a)").WithExpressionStyle())
                .Build())
            .Build();
        return classType;
    }
}