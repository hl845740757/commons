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

namespace Wjybxx.Commons.Apt;

/// <summary>
/// 属性
/// 暂不支持getter/setter上的独立注解
/// </summary>
[Immutable]
public class PropertySpec : ISpecification
{
    public readonly TypeName type;
    public readonly string name;
    public readonly Modifiers modifiers;
    public readonly CodeBlock document;
    public readonly IList<TypeSpec> attributes;

    public readonly CodeBlock? initializer; // 自动属性的默认值
    public readonly CodeBlock? getter; // getter代码块（可选）
    public readonly CodeBlock? setter; // setter代码块（可选）
    public readonly Modifiers setterModifiers; // setter修饰符

    public PropertySpec(Builder builder) {
        this.type = builder.type;
        this.name = builder.name;
        this.modifiers = builder.modifiers;
        this.attributes = Util.ToImmutableList(builder.attributes);
        this.document = builder.document.Build();
        this.initializer = builder.initializer;
        this.getter = builder.setter;
        this.setter = builder.setter;
        this.setterModifiers = builder.setterModifiers;
    }

    public string Name => name;
    public SpecType SpecType => SpecType.Property;

    #region builder

    public static Builder NewBuilder(TypeName type, string name, Modifiers modifiers = 0) {
        return new Builder(type, name, modifiers);
    }

    #endregion

    public class Builder
    {
        public readonly TypeName type;
        public readonly string name;
        public Modifiers modifiers;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();
        public readonly List<TypeSpec> attributes = new List<TypeSpec>();

        internal CodeBlock? initializer;
        internal CodeBlock? getter;
        internal CodeBlock? setter;
        public Modifiers setterModifiers;

        internal Builder(TypeName type, string name, Modifiers modifiers) {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.modifiers = modifiers;
        }

        public PropertySpec Build() {
            return new PropertySpec(this);
        }

        public Builder AddModifiers(Modifiers modifiers) {
            this.modifiers |= modifiers;
            return this;
        }

        public Builder RemModifiers(Modifiers modifiers) {
            this.modifiers &= ~modifiers;
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

        public Builder AddAttribute(TypeSpec attributeSpec) {
            if (attributeSpec == null) throw new ArgumentNullException(nameof(attributeSpec));
            this.attributes.Add(attributeSpec);
            return this;
        }

        public Builder AddAttribute(ClassName attributeSpec) {
            if (attributeSpec == null) throw new ArgumentNullException(nameof(attributeSpec));
            this.attributes.Add(TypeSpec.NewAttributeBuilder(attributeSpec).Build());
            return this;
        }

        public Builder AddAttributes(IEnumerable<TypeSpec> attributeSpecs) {
            if (attributeSpecs == null) throw new ArgumentNullException(nameof(attributeSpecs));
            foreach (TypeSpec spec in attributeSpecs) {
                if (spec == null) throw new ArgumentException("null element");
                this.attributes.Add(spec);
            }
            return this;
        }

        public Builder Initializer(string format, params object[] args) {
            return Initializer(CodeBlock.Of(format, args));
        }

        public Builder Initializer(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.initializer != null) throw new IllegalStateException("initializer was already set");
            this.initializer = codeBlock;
            return this;
        }

        public Builder Getter(string format, params object[] args) {
            return Getter(CodeBlock.Of(format, args));
        }

        public Builder Getter(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.getter != null) throw new IllegalStateException("getter was already set");
            this.getter = codeBlock;
            return this;
        }

        public Builder Setter(string format, params object[] args) {
            return Setter(CodeBlock.Of(format, args));
        }

        public Builder Setter(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.setter != null) throw new IllegalStateException("setter was already set");
            this.setter = codeBlock;
            return this;
        }

        public Builder AddSetterModifiers(Modifiers modifiers) {
            this.setterModifiers |= modifiers;
            return this;
        }
    }
}