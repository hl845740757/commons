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
/// 属性
/// 暂不支持getter/setter上的独立注解
/// </summary>
[Immutable]
public class PropertySpec : ISpecification
{
    public readonly TypeName type; // valueType
    public readonly string name; // 索引器为Item
    public readonly TypeName? indexType; // 索引类型 
    public readonly string? indexName; // 索引名字
    public readonly Modifiers modifiers;
    public readonly CodeBlock document;
    public readonly CodeBlock headerCode;
    public readonly IList<AttributeSpec> attributes;

    public readonly CodeBlock? initializer; // 自动属性的默认值
    public readonly CodeBlock? getter; // getter代码块（可选）
    public readonly CodeBlock? setter; // setter代码块（可选）
    public readonly Modifiers setterModifiers; // setter修饰符

    public readonly bool hasGetter; // 是否有getter
    public readonly bool hasSetter; // 是否有setter

    public PropertySpec(Builder builder) {
        type = builder.type;
        name = builder.name;
        indexType = builder.indexType;
        indexName = builder.indexName;
        modifiers = builder.modifiers;
        document = builder.document.Build();
        headerCode = builder.headerCode.Build();
        attributes = Util.ToImmutableList(builder.attributes);

        initializer = builder.initializer;
        getter = builder.getter;
        setter = builder.setter;
        setterModifiers = builder.setterModifiers;

        hasGetter = builder.hasGetter;
        hasSetter = builder.hasSetter;
    }

    public string Name => name;
    public SpecType SpecType => SpecType.Property;

    /// <summary>
    /// 是否是索引器属性
    /// </summary>
    public bool IsIndexer => indexType != null;

    #region builder

    public static Builder NewBuilder(Type type, string name, Modifiers modifiers = 0) {
        return NewBuilder(TypeName.Get(type), name, modifiers);
    }

    public static Builder NewBuilder(TypeName type, string name, Modifiers modifiers = 0) {
        return new Builder(type, name, null, null, modifiers);
    }

    public static Builder NewIndexerBuilder(Type type, Type indexerType,
                                            string indexerName = "index",
                                            Modifiers modifiers = 0) {
        return NewIndexerBuilder(TypeName.Get(type), TypeName.Get(indexerType), indexerName, modifiers);
    }

    public static Builder NewIndexerBuilder(TypeName type, TypeName indexerType,
                                            string indexerName = "index",
                                            Modifiers modifiers = 0) {
        return new Builder(type, "Item", indexerType, indexerName, modifiers);
    }

    public Builder ToBuilder() {
        Builder builder = new Builder(type, name, indexType, indexName, modifiers)
            .AddDocument(document)
            .AddHeaderCode(headerCode)
            .AddAttributes(attributes);

        builder.initializer = initializer;
        builder.getter = getter;
        builder.setter = setter;
        builder.setterModifiers = setterModifiers;

        builder.hasGetter = hasGetter;
        builder.hasSetter = hasSetter;
        return builder;
    }

    private static bool IsIndexerProperty(PropertyInfo propertyInfo) {
        if (!propertyInfo.Name.Equals("Item")) return false;
        if (propertyInfo.CanRead) {
            MethodInfo getMethod = propertyInfo.GetGetMethod(true)!;
            return getMethod.GetParameters().Length > 0;
        }
        if (propertyInfo.CanWrite) {
            MethodInfo setMethod = propertyInfo.GetSetMethod(true)!;
            return setMethod.GetParameters().Length > 1;
        }
        return false;
    }

    // 忘了属性也是可重写的...属性本质是方法
    public static Builder Overriding(PropertyInfo propertyInfo) {
        Builder builder;
        if (IsIndexerProperty(propertyInfo)) {
            ParameterInfo parameterInfo;
            if (propertyInfo.CanRead) {
                parameterInfo = propertyInfo.GetGetMethod(true)!.GetParameters()[0];
            } else {
                parameterInfo = propertyInfo.GetSetMethod(true)!.GetParameters()[0];
            }
            TypeName indexType = TypeName.Get(parameterInfo.ParameterType);
            string indexName = parameterInfo.Name;
            builder = NewIndexerBuilder(TypeName.Get(propertyInfo.PropertyType), indexType, indexName);
        } else {
            builder = NewBuilder(propertyInfo.PropertyType, propertyInfo.Name);
        }

        builder.hasGetter = propertyInfo.CanRead;
        builder.hasSetter = propertyInfo.CanWrite;
        // MethodInfo.GetBaseDefinition() 可判断是否是重写方法
        Modifiers modifiers = Modifiers.None;
        if (propertyInfo.CanRead) {
            MethodInfo getMethod = propertyInfo.GetGetMethod(true)!;
            modifiers = MethodSpec.ParseModifiers(getMethod, true);

            MethodInfo setMethod = propertyInfo.GetSetMethod(true);
            if (setMethod != null) {
                builder.setterModifiers = MethodSpec.ParseModifiers(setMethod, true);
                // 隐藏setter中包含的getter修饰符
                builder.setterModifiers &= (~modifiers);
            }
        } else {
            MethodInfo setMethod = propertyInfo.GetSetMethod(true)!;
            modifiers = MethodSpec.ParseModifiers(setMethod, true);
        }
        builder.AddModifiers(modifiers);
        return builder;
    }

    #endregion

    public class Builder
    {
        public readonly TypeName type;
        public readonly string name;
        public readonly TypeName? indexType;
        public readonly string? indexName;
        public Modifiers modifiers;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();
        public readonly CodeBlock.Builder headerCode = CodeBlock.NewBuilder();
        public readonly List<AttributeSpec> attributes = new List<AttributeSpec>();

        internal CodeBlock? initializer;
        internal CodeBlock? getter;
        internal CodeBlock? setter;
        public Modifiers setterModifiers;

        public bool hasGetter = true;
        public bool hasSetter = true;

        internal Builder(TypeName type, string name, TypeName? indexType, string? indexName, Modifiers modifiers) {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.name = Util.CheckNotBlank(name, "name is blank");
            this.indexType = indexType;
            this.indexName = indexName;
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

        public Builder Getter(string format, params object[] args) {
            return Getter(CodeBlock.Of(format, args));
        }

        public Builder Getter(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.getter != null) throw new IllegalStateException("getter was already set");
            this.getter = codeBlock;
            this.hasGetter = true;
            return this;
        }

        public Builder Setter(string format, params object[] args) {
            return Setter(CodeBlock.Of(format, args));
        }

        public Builder Setter(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.setter != null) throw new IllegalStateException("setter was already set");
            this.setter = codeBlock;
            this.hasSetter = true;
            return this;
        }

        public Builder HasGetter(bool value = true) {
            this.hasGetter = value;
            return this;
        }

        public Builder HasSetter(bool value = true) {
            this.hasSetter = value;
            return this;
        }

        public Builder AddSetterModifiers(Modifiers modifiers) {
            this.setterModifiers |= modifiers;
            return this;
        }
    }
}