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
/// </summary>
[Immutable]
public class ParameterSpec : ISpecification
{
    public readonly TypeName type;
    public readonly string name;
    public readonly Modifiers modifiers;
    public readonly CodeBlock document;
    public readonly IList<TypeSpec> attributes; // 暂时不想支持代码生成

    public readonly CodeBlock? defaultValue; // 默认值

    private ParameterSpec(Builder builder) {
        this.type = builder.type;
        this.name = builder.name;
        this.modifiers = builder.modifiers;
        this.attributes = Util.ToImmutableList(builder.attributes);
        this.document = builder.document.Build();
        this.defaultValue = builder.defaultValue;
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
        // 这里存在引用类型的问题。..
        TypeName typeName = TypeName.Get(parameterInfo.ParameterType);
        if (typeName is RefTypeName refTypeName) {
            typeName = refTypeName.targetType;
        }
        return NewBuilder(typeName, parameterInfo.Name!)
            .AddModifiers(InRefOut(parameterInfo))
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

    private static Modifiers InRefOut(ParameterInfo parameter) {
        // in、ref、out ，类型名后面会带有 & 符号
        if (parameter.ParameterType.IsByRef) {
            return parameter.IsIn ? Modifiers.In
                : parameter.IsOut ? Modifiers.Out
                : Modifiers.Ref;
        }
        return Modifiers.None;
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
        public readonly List<TypeSpec> attributes = new List<TypeSpec>();

        public CodeBlock? defaultValue;

        internal Builder(TypeName type, string name, Modifiers modifiers) {
            this.type = type;
            this.name = name;
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