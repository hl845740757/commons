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
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Poet;

/// <summary>
/// 表示一个类型
///
/// 1.C#没有静态代码块，取而代之的是静态构造函数。
/// 2.C#由于有宏，因此字段和方法等可能有多个同名定义。。。
/// 3.C#由于有宏，using/import也不能安全自动推导 -- 可以设定开关。
/// 4.C#由于有宏，我们需要保持所有元素的插入顺序，不能分开存储。
/// 5.C#由于有宏，带代码进行校验也是较为困难的，由用户自行保证吧。
///
/// ps:命名空间由<see cref="NamespaceSpec"/>指定。
/// </summary>
[Immutable]
public class TypeSpec : ISpecification
{
    public readonly Kind kind;
    public readonly string name;
    public readonly Modifiers modifiers;
    public readonly CodeBlock document;
    public readonly CodeBlock headerCode; // 文档后，类定义前的自定义代码
    public readonly IList<AttributeSpec> attributes; // 这里仅表示非宏管理注解

    public readonly IList<TypeVariableName> typeVariables; // 泛型参数
    public readonly IList<TypeName> baseClasses; // 超类和接口
    public readonly IList<ISpecification> nestedSpecs; // 所有的嵌套元素

    public TypeSpec(Builder builder) {
        kind = builder.kind;
        name = builder.name;
        modifiers = builder.modifiers;
        document = builder.document.Build();
        headerCode = builder.headerCode.Build();
        attributes = Util.ToImmutableList(builder.attributes);

        typeVariables = Util.ToImmutableList(builder.typeVariables);
        baseClasses = Util.ToImmutableList(builder.baseClasses);
        nestedSpecs = Util.ToImmutableList(builder.nestedSpecs);

        // 委托检查
        if (kind == Kind.Delegator) {
            if (nestedSpecs.Count != 1 || nestedSpecs[0].SpecType != SpecType.Method) {
                throw new IllegalStateException("Delegator method is absent");
            }
        }
    }

    public string Name => name;
    public SpecType SpecType => SpecType.Type;

    public enum Kind
    {
        Class,
        Struct,
        Interface,
        Enum,
        /// <summary>
        /// C#委托是Type，不是Method；每一个委托都定义了一个类型。
        /// C#委托只允许有一个嵌套成员，即方法定义。
        /// </summary>
        Delegator,
    }

    #region builder

    public static Builder NewClassBuilder(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        return new Builder(Kind.Class, name);
    }

    public static Builder NewClassBuilder(ClassName className) {
        if (className == null) throw new ArgumentNullException(nameof(className));
        return NewClassBuilder(className.simpleName);
    }

    public static Builder NewStructBuilder(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        return new Builder(Kind.Struct, name);
    }

    public static Builder NewStructBuilder(ClassName className) {
        if (className == null) throw new ArgumentNullException(nameof(className));
        return NewStructBuilder(className.simpleName);
    }

    public static Builder NewInterfaceBuilder(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        return new Builder(Kind.Interface, name);
    }

    public static Builder NewInterfaceBuilder(ClassName className) {
        if (className == null) throw new ArgumentNullException(nameof(className));
        return NewInterfaceBuilder(className.simpleName);
    }

    public static Builder NewEnumBuilder(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        return new Builder(Kind.Enum, name);
    }

    public static Builder NewEnumBuilder(ClassName className) {
        if (className == null) throw new ArgumentNullException(nameof(className));
        return NewEnumBuilder(className.simpleName);
    }

    public static Builder NewDelegatorBuilder(MethodSpec methodSpec) {
        if (methodSpec == null) throw new ArgumentNullException(nameof(methodSpec));
        return new Builder(Kind.Delegator, methodSpec.name)
            .AddMethod(methodSpec);
    }

    public static TypeSpec NewDelegator(MethodSpec methodSpec) {
        return NewDelegatorBuilder(methodSpec).Build();
    }

    public Builder ToBuilder() {
        return new Builder(kind, name)
            .AddModifiers(modifiers)
            .AddDocument(document)
            .AddHeaderCode(headerCode)
            .AddAttributes(attributes)
            .AddTypeVariables(typeVariables)
            .AddBaseClasses(baseClasses)
            .AddSpecs(nestedSpecs);
    }

    #endregion

    public class Builder
    {
        public readonly Kind kind;
        public readonly string name;
        public Modifiers modifiers;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();
        public readonly CodeBlock.Builder headerCode = CodeBlock.NewBuilder();
        public readonly List<AttributeSpec> attributes = new List<AttributeSpec>();

        public readonly List<TypeName> baseClasses = new List<TypeName>();
        public readonly List<TypeVariableName> typeVariables = new List<TypeVariableName>();
        public readonly List<ISpecification> nestedSpecs = new List<ISpecification>();

        internal Builder(Kind kind, string name) {
            this.kind = kind;
            this.name = Util.CheckNotBlank(name, "name is blank");
        }

        public TypeSpec Build() {
            return new TypeSpec(this);
        }

        public Builder AddModifiers(Modifiers modifiers) {
            this.modifiers |= modifiers;
            return this;
        }

        public Builder AddDocument(string format, params object[] args) {
            document.Add(format, args);
            return this;
        }

        public Builder AddDocument(CodeBlock codeBlock) {
            document.Add(codeBlock);
            return this;
        }

        public Builder AddHeaderCode(string format, params object[] args) {
            headerCode.Add(format, args);
            return this;
        }

        public Builder AddHeaderCode(CodeBlock codeBlock) {
            headerCode.Add(codeBlock);
            return this;
        }

        public Builder AddAttribute(AttributeSpec attributeSpec) {
            if (attributeSpec == null) throw new ArgumentNullException(nameof(attributeSpec));
            this.attributes.Add(attributeSpec);
            return this;
        }

        public Builder AddAttribute(ClassName attributeSpec) {
            if (attributeSpec == null) throw new ArgumentNullException(nameof(attributeSpec));
            this.attributes.Add(AttributeSpec.NewBuilder(attributeSpec).Build());
            return this;
        }

        public Builder AddAttributes(IEnumerable<AttributeSpec> attributeSpecs) {
            if (attributeSpecs == null) throw new ArgumentNullException(nameof(attributeSpecs));
            foreach (AttributeSpec spec in attributeSpecs) {
                if (spec == null) throw new ArgumentException("attribute == null");
                this.attributes.Add(spec);
            }
            return this;
        }

        public Builder AddSpecs(IEnumerable<ISpecification> specs) {
            if (specs == null) throw new ArgumentNullException(nameof(specs));
            foreach (ISpecification spec in specs) {
                if (spec == null) throw new ArgumentException("spec == null");
                this.nestedSpecs.Add(spec);
            }
            return this;
        }

        public Builder AddSpec(ISpecification spec) {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            this.nestedSpecs.Add(spec);
            return this;
        }

        #region base-classes

        public Builder AddTypeVariables(IEnumerable<TypeVariableName?> typeVariables) {
            if (typeVariables == null) throw new ArgumentNullException(nameof(typeVariables));
            foreach (TypeVariableName? typeVariable in typeVariables) {
                Util.CheckArgument(typeVariable != null, "typeVariable == null");
                this.typeVariables.Add(typeVariable);
            }
            return this;
        }

        public Builder AddTypeVariable(TypeVariableName typeVariable) {
            if (typeVariable == null) throw new ArgumentNullException(nameof(typeVariable));
            typeVariables.Add(typeVariable);
            return this;
        }

        public Builder AddBaseClasses(IEnumerable<TypeName?> baseClasses) {
            if (baseClasses == null) throw new ArgumentNullException(nameof(baseClasses));
            foreach (TypeName? parameterSpec in baseClasses) {
                Util.CheckArgument(parameterSpec != null, "parameterSpec == null");
                this.baseClasses.Add(parameterSpec);
            }
            return this;
        }

        public Builder AddBaseClass(TypeName baseClass) {
            if (baseClass == null) throw new ArgumentNullException(nameof(baseClass));
            this.baseClasses.Add(baseClass);
            return this;
        }

        public Builder AddBaseClass(Type type) {
            this.baseClasses.Add(TypeName.Get(type));
            return this;
        }

        #endregion

        #region enum

        public Builder AddEnumValues(IEnumerable<EnumValueSpec> enumValues) {
            this.nestedSpecs.AddRange(enumValues);
            return this;
        }

        public Builder AddEnumValue(EnumValueSpec enumValue) {
            this.nestedSpecs.Add(enumValue);
            return this;
        }

        public Builder AddEnumValue(string name) {
            this.nestedSpecs.Add(new EnumValueSpec(name));
            return this;
        }

        public Builder AddEnumValue(string name, int value) {
            this.nestedSpecs.Add(new EnumValueSpec(name, value));
            return this;
        }

        #endregion

        #region field

        public Builder AddFields(IEnumerable<FieldSpec> fieldSpecs) {
            if (fieldSpecs == null) throw new ArgumentNullException(nameof(fieldSpecs));
            foreach (FieldSpec fieldSpec in fieldSpecs) {
                AddField(fieldSpec);
            }
            return this;
        }

        public Builder AddField(FieldSpec fieldSpec) {
            nestedSpecs.Add(fieldSpec);
            return this;
        }

        public Builder AddField(TypeName type, string name, Modifiers modifiers = 0) {
            return AddField(FieldSpec.NewBuilder(type, name, modifiers).Build());
        }

        public Builder AddField(Type type, string name, Modifiers modifiers = 0) {
            return AddField(TypeName.Get(type), name, modifiers);
        }

        #endregion

        #region props

        public Builder AddProperties(IEnumerable<PropertySpec> propertySpecs) {
            if (propertySpecs == null) throw new ArgumentNullException(nameof(propertySpecs));
            foreach (PropertySpec propertySpec in propertySpecs) {
                AddProperty(propertySpec);
            }
            return this;
        }

        public Builder AddProperty(PropertySpec fieldSpec) {
            nestedSpecs.Add(fieldSpec);
            return this;
        }

        public Builder AddProperty(TypeName type, string name, Modifiers modifiers = 0) {
            return AddProperty(PropertySpec.NewBuilder(type, name, modifiers).Build());
        }

        public Builder AddProperty(Type type, string name, Modifiers modifiers = 0) {
            return AddProperty(TypeName.Get(type), name, modifiers);
        }

        #endregion

        #region methods

        public Builder AddMethods(IEnumerable<MethodSpec> propertySpecs) {
            if (propertySpecs == null) throw new ArgumentNullException(nameof(propertySpecs));
            foreach (MethodSpec methodSpec in propertySpecs) {
                AddMethod(methodSpec);
            }
            return this;
        }

        public Builder AddMethod(MethodSpec methodSpec) {
            nestedSpecs.Add(methodSpec);
            return this;
        }

        #endregion

        #region nestedTypes

        public Builder AddTypes(IEnumerable<TypeSpec> nestedTypes) {
            if (nestedTypes == null) throw new ArgumentNullException(nameof(nestedTypes));
            foreach (TypeSpec methodSpec in nestedTypes) {
                AddType(methodSpec);
            }
            return this;
        }

        public Builder AddType(TypeSpec typeSpec) {
            nestedSpecs.Add(typeSpec);
            return this;
        }

        #endregion
    }
}