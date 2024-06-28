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
/// 字段(或事件)
/// 注意：C#的事件本身并不是字段，但实在不想整那么复杂了...
/// </summary>
[Immutable]
public class FieldSpec : ISpecification
{
    public readonly Kind kind;
    public readonly TypeName type;
    public readonly string name;
    public readonly Modifiers modifiers;
    public readonly CodeBlock document;
    public readonly CodeBlock headerCode;
    public readonly IList<AttributeSpec> attributes;

    public readonly CodeBlock? initializer; // 初始化块

    private FieldSpec(Builder builder) {
        kind = builder.kind;
        type = builder.type;
        name = builder.name;
        modifiers = builder.modifiers;
        attributes = Util.ToImmutableList(builder.attributes);
        document = builder.document.Build();
        headerCode = builder.headerCode.Build();

        initializer = builder.initializer;
    }

    public bool IsEvent => kind == Kind.Event;

    public string Name => name;
    public SpecType SpecType => SpecType.Field;

    public enum Kind
    {
        /// <summary>
        /// 普通字段
        /// </summary>
        Field,
        /// <summary>
        /// 事件字段（C#的大量语法糖现在都是坑）
        /// </summary>
        Event,
    }

    #region builder

    public static Builder NewBuilder(TypeName type, string name, Modifiers modifiers = 0) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (name == null) throw new ArgumentNullException(nameof(name));
        return new Builder(Kind.Field, type, name, modifiers);
    }

    public static Builder NewBuilder(Type type, string name, Modifiers modifiers = 0) {
        return NewBuilder(TypeName.Get(type), name, modifiers);
    }

    public static Builder NewEventBuilder(TypeName type, string name, Modifiers modifiers = 0) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (name == null) throw new ArgumentNullException(nameof(name));
        return new Builder(Kind.Event, type, name, modifiers);
    }

    public static Builder NewEventBuilder(Type type, string name, Modifiers modifiers = 0) {
        return NewEventBuilder(TypeName.Get(type), name, modifiers);
    }

    public Builder ToBuilder() {
        Builder builder = new Builder(kind, type, name, modifiers);
        builder.document.Add(document);
        builder.attributes.AddRange(attributes);
        builder.initializer = initializer;
        return builder;
    }

    #endregion

    public class Builder
    {
        public readonly Kind kind;
        public readonly TypeName type;
        public readonly string name;
        public Modifiers modifiers;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();
        public readonly CodeBlock.Builder headerCode = CodeBlock.NewBuilder();
        public readonly List<AttributeSpec> attributes = new List<AttributeSpec>();

        internal CodeBlock? initializer;

        internal Builder(Kind kind, TypeName type, string name, Modifiers modifiers) {
            this.kind = kind;
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.name = Util.CheckNotBlank(name, "name is blank");
            this.modifiers = modifiers;
        }

        public FieldSpec Build() {
            return new FieldSpec(this);
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
    }
}