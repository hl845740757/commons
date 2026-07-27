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

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 事件
///
/// 1.未设置访问器时输出为字段式事件，如：<code>public event EventHandler Changed;</code>
/// 2.设置访问器时输出add/remove块 -- C#要求add/remove必须成对实现，且不能再有初始化块。
/// 3.暂不支持add/remove访问器上的独立注解。
/// </summary>
public class EventSpec : ISpecification
{
    public readonly TypeName type;
    public readonly string name;
    public readonly Modifiers modifiers;
    public readonly CodeBlock document;
    public readonly CodeBlock headerCode;
    public readonly IList<AttributeSpec> attributes;

    public readonly CodeBlock? initializer; // 初始化块（仅字段式事件可用）
    public readonly CodeBlock? adder; // add访问器代码块
    public readonly CodeBlock? remover; // remove访问器代码块

    private EventSpec(Builder builder) {
        type = builder.type;
        name = builder.name;
        modifiers = builder.modifiers;
        document = builder.document.Build();
        headerCode = builder.headerCode.Build();
        attributes = Util.ToImmutableList(builder.attributes);

        initializer = builder.initializer;
        adder = builder.adder;
        remover = builder.remover;

        if (adder != null || remover != null) {
            if (adder == null || remover == null) {
                throw new InvalidOperationException("add and remove accessors must be paired");
            }
            if (initializer != null) {
                throw new InvalidOperationException("event with accessors cannot have an initializer");
            }
        }
    }

    public string Name => name;
    public SpecType SpecType => SpecType.Event;

    /// <summary>
    /// 是否包含自定义的add/remove访问器
    /// </summary>
    public bool HasAccessors => adder != null;

    #region builder

    public static Builder NewBuilder(TypeName type, string name, Modifiers modifiers = 0) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (name == null) throw new ArgumentNullException(nameof(name));
        return new Builder(type, name, modifiers);
    }

    public static Builder NewBuilder(Type type, string name, Modifiers modifiers = 0) {
        return NewBuilder(TypeName.Get(type), name, modifiers);
    }

    public Builder ToBuilder() {
        Builder builder = new Builder(type, name, modifiers);
        builder.document.Add(document);
        builder.headerCode.Add(headerCode);
        builder.attributes.AddRange(attributes);
        builder.initializer = initializer;
        builder.adder = adder;
        builder.remover = remover;
        return builder;
    }

    #endregion

    public class Builder
    {
        public readonly TypeName type;
        public readonly string name;
        public Modifiers modifiers;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();
        public readonly CodeBlock.Builder headerCode = CodeBlock.NewBuilder();
        public readonly List<AttributeSpec> attributes = new List<AttributeSpec>();

        internal CodeBlock? initializer;
        internal CodeBlock? adder;
        internal CodeBlock? remover;

        internal Builder(TypeName type, string name, Modifiers modifiers) {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.name = Util.CheckNotBlank(name, "name is blank");
            this.modifiers = modifiers;
        }

        public EventSpec Build() {
            return new EventSpec(this);
        }

        public Builder AddModifiers(Modifiers modifiers) {
            this.modifiers |= modifiers;
            return this;
        }

        public Builder RemModifiers(Modifiers modifiers) {
            this.modifiers &= ~modifiers;
            return this;
        }

        public Builder AddDocument(string format, params object?[] args) {
            document.Add(format, args);
            return this;
        }

        public Builder AddDocument(CodeBlock codeBlock) {
            document.Add(codeBlock);
            return this;
        }

        public Builder AddHeaderCode(string format, params object?[] args) {
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

        public Builder Initializer(string format, params object?[] args) {
            return Initializer(CodeBlock.Of(format, args));
        }

        public Builder Initializer(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.initializer != null) throw new InvalidOperationException("initializer was already set");
            this.initializer = codeBlock;
            return this;
        }

        public Builder Adder(string format, params object?[] args) {
            return Adder(CodeBlock.Of(format, args));
        }

        public Builder Adder(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.adder != null) throw new InvalidOperationException("adder was already set");
            this.adder = codeBlock;
            return this;
        }

        public Builder Remover(string format, params object?[] args) {
            return Remover(CodeBlock.Of(format, args));
        }

        public Builder Remover(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.remover != null) throw new InvalidOperationException("remover was already set");
            this.remover = codeBlock;
            return this;
        }
    }
}
}
