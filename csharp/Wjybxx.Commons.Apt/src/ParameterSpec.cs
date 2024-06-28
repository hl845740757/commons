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
using System.Reflection;
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Apt;

/// <summary>
/// 方法参数
/// 注意：方法参数的 ref/in/out 不是单纯的修饰符，而是修改了字段的类型。
/// </summary>
[Immutable]
public class ParameterSpec : ISpecification
{
    public readonly TypeName type;
    public readonly string name;
    public readonly Modifiers modifiers; // c#其实没有 -- ref/in/out其实是修改了type
    public readonly CodeBlock document; // 暂时可能不生成
    public readonly IList<AttributeSpec> attributes; // 暂时不想支持代码生成

    public readonly CodeBlock? defaultValue; // 默认值

    private ParameterSpec(Builder builder) {
        type = builder.type;
        name = builder.name;
        modifiers = builder.modifiers;
        attributes = Util.ToImmutableList(builder.attributes);
        document = builder.document.Build();

        defaultValue = builder.defaultValue;
    }

    public string Name => name;
    public SpecType SpecType => SpecType.Parameter;

    #region builder

    public static Builder NewBuilder(TypeName type, string name, Modifiers modifiers = 0) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (name == null) throw new ArgumentNullException(nameof(name));
        return new Builder(type, name, modifiers);
    }

    public static Builder NewBuilder(Type type, string name, Modifiers modifiers = 0) {
        return NewBuilder(TypeName.Get(type), name, modifiers);
    }

    public static ParameterSpec Get(ParameterInfo parameterInfo) {
        return NewBuilder(TypeName.Get(parameterInfo.ParameterType), parameterInfo.Name!)
            .Build();
    }

    public static List<ParameterSpec> ParametersOf(MethodBase method) {
        ParameterInfo[] parameterInfos = method.GetParameters();
        List<ParameterSpec> result = new List<ParameterSpec>(parameterInfos.Length);
        foreach (var parameterInfo in parameterInfos) {
            result.Add(Get(parameterInfo));
        }
        return result;
    }

    /// <summary>
    /// 转为builder，不继承文档和默认值
    /// </summary>
    /// <returns></returns>
    public Builder ToBuilder() {
        Builder builder = new Builder(type, name, modifiers);
        builder.attributes.AddRange(attributes);
        return builder;
    }

    #endregion

    public class Builder
    {
        public readonly TypeName type;
        public readonly string name;
        public Modifiers modifiers;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();
        public readonly List<AttributeSpec> attributes = new List<AttributeSpec>();

        public CodeBlock? defaultValue;

        internal Builder(TypeName type, string name, Modifiers modifiers) {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.name = Util.CheckNotBlank(name, "name is blank");
            this.modifiers = modifiers;
        }

        public ParameterSpec Build() {
            return new ParameterSpec(this);
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

        public Builder DefaultValue(string format, params object[] args) {
            return DefaultValue(CodeBlock.Of(format, args));
        }

        public Builder DefaultValue(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.defaultValue != null) throw new IllegalStateException("defaultValue was already set");
            this.defaultValue = codeBlock;
            return this;
        }
    }
}