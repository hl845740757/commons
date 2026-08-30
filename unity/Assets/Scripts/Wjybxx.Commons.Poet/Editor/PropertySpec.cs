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

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 属性
/// 暂不支持getter/setter上的独立注解
/// </summary>
public class PropertySpec : ISpecification
{
    public readonly TypeName type; // valueType
    public readonly string name; // 索引器为Item
    public readonly TypeName? indexType; // 索引类型 
    public readonly string? indexName; // 索引名字
    public readonly CodeBlock document;
    public readonly CodeBlock headerCode;
    public readonly IList<AttributeSpec> attributes;

    public readonly CodeBlock? initializer; // 自动属性的默认值
    public readonly CodeBlock? getter; // getter代码块（可选）
    public readonly CodeBlock? setter; // setter代码块（可选）
    public readonly Modifiers getterModifiers; // getter修饰符
    public readonly Modifiers setterModifiers; // setter修饰符

    public readonly bool hasGetter; // 是否有getter
    public readonly bool hasSetter; // 是否有setter

    private PropertySpec(Builder builder) {
        type = builder.type;
        name = builder.name;
        indexType = builder.indexType;
        indexName = builder.indexName;
        document = builder.document.Build();
        headerCode = builder.headerCode.Build();
        attributes = Util.ToImmutableList(builder.attributes);

        initializer = builder.initializer;
        getter = builder.getter;
        setter = builder.setter;
        getterModifiers = builder.getterModifiers;
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
        Builder builder = new Builder(type, name, indexType, indexName, getterModifiers)
            .AddDocument(document)
            .AddHeaderCode(headerCode)
            .AddAttributes(attributes);

        builder.initializer = initializer;
        builder.getter = getter;
        builder.setter = setter;
        builder.getterModifiers = getterModifiers;
        builder.setterModifiers = setterModifiers;

        builder.hasGetter = hasGetter;
        builder.hasSetter = hasSetter;
        return builder;
    }

    #endregion

    #region overriding

    /// <summary>
    /// 忘了属性也是可重写的...属性本质是方法
    /// </summary>
    public static Builder Overriding(PropertyInfo propertyInfo) {
        return CopyProperty(propertyInfo, true);
    }

    private static Builder CopyProperty(PropertyInfo propertyInfo, bool overriding) {
        Builder builder;
        if (Util.IsIndexerProperty(propertyInfo)) {
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

        Util.ParseModifiers(propertyInfo, out Modifiers getterModifiers, out Modifiers setterModifiers);
        if (overriding) {
            bool fromClass = propertyInfo.DeclaringType!.IsClass;
            getterModifiers = Util.AddOverrideModifiers(getterModifiers, fromClass);
            setterModifiers = Util.AddOverrideModifiers(setterModifiers, fromClass);
        }
        // 隐藏setter中包含的getter修饰符
        if (propertyInfo.CanRead && propertyInfo.CanWrite) {
            setterModifiers &= (~getterModifiers);
        }
        builder.AddGetterModifiers(getterModifiers);
        builder.AddSetterModifiers(setterModifiers);
        return builder;
    }

    #endregion

    public class Builder
    {
        public readonly TypeName type;
        public readonly string name;
        public readonly TypeName? indexType;
        public readonly string? indexName;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();
        public readonly CodeBlock.Builder headerCode = CodeBlock.NewBuilder();
        public readonly List<AttributeSpec> attributes = new List<AttributeSpec>();

        internal CodeBlock? initializer;
        internal CodeBlock? getter;
        internal CodeBlock? setter;
        public Modifiers getterModifiers;
        public Modifiers setterModifiers;

        public bool hasGetter = true;
        public bool hasSetter = true;

        internal Builder(TypeName type, string name, TypeName? indexType, string? indexName, Modifiers getterModifiers) {
            this.type = type ?? throw new ArgumentNullException(nameof(type));
            this.name = Util.CheckNotBlank(name, "name is blank");
            this.indexType = indexType;
            this.indexName = indexName;
            this.getterModifiers = getterModifiers;
        }

        public PropertySpec Build() {
            return new PropertySpec(this);
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
            this.hasGetter = true;
            return this;
        }

        public Builder Getter(string format, params object?[] args) {
            return Getter(CodeBlock.Of(format, args));
        }

        public Builder Getter(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.getter != null) throw new InvalidOperationException("getter was already set");
            this.getter = codeBlock;
            this.hasGetter = true;
            return this;
        }

        public Builder Setter(string format, params object?[] args) {
            return Setter(CodeBlock.Of(format, args));
        }

        public Builder Setter(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.setter != null) throw new InvalidOperationException("setter was already set");
            this.setter = codeBlock;
            this.hasSetter = true;
            return this;
        }

        public Builder RemoveGetter() {
            this.hasGetter = false;
            return this;
        }

        public Builder RemoveSetter() {
            this.hasSetter = false;
            return this;
        }

        public Builder AddGetterModifiers(Modifiers modifiers) {
            this.getterModifiers |= modifiers;
            return this;
        }

        public Builder AddSetterModifiers(Modifiers modifiers) {
            this.setterModifiers |= modifiers;
            return this;
        }
    }
}
}