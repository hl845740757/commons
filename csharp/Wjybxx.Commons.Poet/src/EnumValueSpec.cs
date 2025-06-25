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

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 表示一个枚举值定义
/// </summary>
public class EnumValueSpec : ISpecification
{
    /** 枚举名 */
    public readonly string name;
    /** 枚举关联的数字 -- 可能未定义 */
    public readonly int? number;
    /** 枚举的注释 */
    public readonly CodeBlock document;
    /** 枚举的注解 */
    public readonly IList<AttributeSpec> attributes;

    public EnumValueSpec(string name, int? number = null, CodeBlock? document = null) {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.number = number;
        this.document = document ?? CodeBlock.Empty;
        this.attributes = Util.EmptyList<AttributeSpec>();
    }

    private EnumValueSpec(Builder builder) {
        this.name = builder.name;
        this.number = builder.number;
        this.document = builder.document.Build();
        this.attributes = Util.ToImmutableList(builder.attributes);
    }

    public string Name => name;
    public SpecType SpecType => SpecType.EnumValue;

    public override string ToString() {
        return $"{nameof(name)}: {name}, {nameof(number)}: {number}";
    }

    #region builder

    public static EnumValueSpec Get(string name, int? number = null, CodeBlock? document = null) {
        return new EnumValueSpec(name, number, document);
    }

    public static Builder NewBuilder(string name, int? number = null) {
        return new Builder(name, number);
    }

    public Builder ToBuilder() {
        return new Builder(name, number)
            .AddDocument(document)
            .AddAttributes(attributes);
    }

    #endregion

    public class Builder
    {
        public readonly string name;
        public readonly int? number;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();
        public readonly List<AttributeSpec> attributes = new List<AttributeSpec>();

        internal Builder(string name, int? number) {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            this.number = number;
        }

        public EnumValueSpec Build() {
            return new EnumValueSpec(this);
        }

        public Builder AddDocument(string format, params object?[] args) {
            document.Add(format, args);
            return this;
        }

        public Builder AddDocument(CodeBlock codeBlock) {
            document.Add(codeBlock);
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
    }
}
}