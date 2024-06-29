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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Attributes;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Apt;

/// <summary>
/// 方法或构造函数
/// </summary>
[Immutable]
public class MethodSpec : ISpecification
{
    public readonly Kind kind;
    public readonly string name;
    public readonly Modifiers modifiers;
    public readonly CodeBlock document;
    public readonly CodeBlock headerCode;
    public readonly IList<AttributeSpec> attributes;

    public readonly TypeName? explicitBaseType; // 显式实现的接口
    public readonly IList<TypeVariableName> typeVariables; // 泛型参数
    public readonly TypeName returnType; // 返回值类型
    public readonly IList<ParameterSpec> parameters; // 方法参数
    public readonly bool varargs; // 是否变长参数(params T[] args)

    public readonly CodeBlock? code; // 方法体(委托一定没有，接口也可能没有)
    public readonly CodeBlock? constructorInvoker; // 调用其它构造方法的代码

    public MethodSpec(Builder builder) {
        kind = builder.kind;
        name = builder.name;
        modifiers = builder.modifiers;
        document = builder.document.Build();
        headerCode = builder.headerCode.Build();
        attributes = Util.ToImmutableList(builder.attributes);

        explicitBaseType = builder.explicitBaseType;
        returnType = builder.returnType;
        typeVariables = Util.ToImmutableList(builder.typeVariables);
        parameters = Util.ToImmutableList(builder.parameters);
        varargs = builder.varargs;

        code = builder.code;
        constructorInvoker = builder.constructorInvoker;
    }

    public bool IsConstructor => kind == Kind.Constructor;

    public string Name => name;
    public SpecType SpecType => SpecType.Method;

    public enum Kind
    {
        /// <summary>
        /// 普通方法
        /// </summary>
        Method = 0,
        /// <summary>
        /// 构造函数(注意C#的静态构造函数)
        /// </summary>
        Constructor = 1,
    }

    #region builder

    public static Builder NewMethodBuilder(string name) {
        return new Builder(Kind.Method, name);
    }

    public static Builder NewConstructorBuilder() {
        return new Builder(Kind.Constructor, "<init>");
    }

    public Builder ToBuilder() {
        Builder builder = new Builder(kind, name, modifiers)
            .AddDocument(document)
            .AddAttributes(attributes)
            .AddTypeVariables(typeVariables)
            .Returns(returnType)
            .AddParameters(parameters);
        builder.explicitBaseType = explicitBaseType;
        builder.varargs = varargs;
        builder.code = code;
        builder.constructorInvoker = constructorInvoker;
        return builder;
    }

    public static Builder Overriding(MethodInfo methodInfo) {
        if (methodInfo.IsFinal || methodInfo.IsStatic || methodInfo.IsPrivate) {
            throw new ArgumentException("cannot override method with modifiers: " + methodInfo.Attributes);
        }
        Builder builder = NewMethodBuilder(methodInfo.Name);
        // MethodInfo.GetBaseDefinition() 可判断是否是重写方法
        Modifiers modifiers = Modifiers.None;
        if (methodInfo.IsStatic) modifiers |= Modifiers.Static;
        if (methodInfo.IsPublic) modifiers |= Modifiers.Public;
        if (methodInfo.IsAssembly) modifiers |= Modifiers.Internal;
        if (methodInfo.IsPrivate) modifiers |= Modifiers.Private;
        if (methodInfo.IsFamily) modifiers |= Modifiers.Protected;
        // async关键字是注解
        if (methodInfo.GetCustomAttributes()
            .Any(e => e is AsyncStateMachineAttribute)) {
            modifiers |= Modifiers.Async;
        }

        // 拷贝泛型参数
        if (methodInfo.IsGenericMethod) {
            foreach (Type genericArgument in methodInfo.GetGenericArguments()) {
                builder.AddTypeVariable(TypeVariableName.Get(genericArgument));
            }
        }
        // 拷贝返回值
        builder.Returns(TypeName.Get(methodInfo.ReturnType));
        // 拷贝方法参数
        ParameterInfo[] parameterInfos = methodInfo.GetParameters();
        if (parameterInfos.Length > 0) {
            builder.AddParameters(ParameterSpec.ParametersOf(methodInfo));
            // 处理params修饰符
            bool hasParamsModifier = parameterInfos[parameterInfos.Length - 1].GetCustomAttributes()
                .Any(e => e is ParamArrayAttribute);
            builder.Varargs(hasParamsModifier);
        }

        // 处理unsafe
        bool hasPointerType = methodInfo.ReturnType.IsPointer;
        if (!hasPointerType) {
            foreach (ParameterInfo parameterInfo in parameterInfos) {
                hasPointerType |= parameterInfo.ParameterType.IsPointer;
            }
        }
        if (hasPointerType) {
            modifiers |= Modifiers.Unsafe;
        }

        modifiers |= Modifiers.Override;
        builder.AddModifiers(modifiers);
        return builder;
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

        public TypeName? explicitBaseType;
        public readonly List<TypeVariableName> typeVariables = new List<TypeVariableName>();
        public TypeName returnType = TypeName.VOID;
        public readonly List<ParameterSpec> parameters = new List<ParameterSpec>();
        public bool varargs;

        /// <summary>
        /// 由于代码的的构建逻辑较多，Builder不进行完整的代理，外部直接访问该字段构建即可；
        /// </summary>
        public CodeBlock? code;
        public CodeBlock? constructorInvoker;

        internal Builder(Kind kind, string name, Modifiers modifiers = 0) {
            this.kind = kind;
            this.name = Util.CheckNotBlank(name, "name is blank");
            this.modifiers = modifiers;
        }

        public MethodSpec Build() {
            return new MethodSpec(this);
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
        //

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

        public Builder Returns(TypeName returnType) {
            Util.CheckState(kind != Kind.Constructor, "constructor cannot have return type.");
            this.returnType = returnType;
            return this;
        }

        public Builder Returns(Type returnType) {
            return Returns(TypeName.Get(returnType));
        }

        //
        public Builder AddParameters(IEnumerable<ParameterSpec?> parameterSpecs) {
            if (parameterSpecs == null) throw new ArgumentNullException(nameof(parameterSpecs));
            foreach (ParameterSpec? parameterSpec in parameterSpecs) {
                Util.CheckArgument(parameterSpec != null, "parameterSpec == null");
                this.parameters.Add(parameterSpec);
            }
            return this;
        }

        public Builder AddParameter(ParameterSpec parameterSpec) {
            if (parameterSpec == null) throw new ArgumentNullException(nameof(parameterSpec));
            this.parameters.Add(parameterSpec);
            return this;
        }

        public Builder AddParameter(TypeName type, string name, Modifiers modifiers = Modifiers.None) {
            return AddParameter(ParameterSpec.NewBuilder(type, name, modifiers).Build());
        }

        public Builder AddParameter(Type type, string name, Modifiers modifiers = Modifiers.None) {
            return AddParameter(TypeName.Get(type), name, modifiers);
        }

        public Builder Varargs(bool varargs = true) {
            this.varargs = varargs;
            return this;
        }

        //
        public Builder ExplicitImpl(TypeName baseType) {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (this.explicitBaseType != null) throw new IllegalStateException("explicitImpl was already set");
            this.explicitBaseType = baseType;
            return this;
        }

        public Builder Code(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.code != null) throw new IllegalStateException("code was already set");
            this.code = codeBlock;
            return this;
        }

        public Builder ConstructorInvoker(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.constructorInvoker != null) throw new IllegalStateException("constructorInvoker was already set");
            this.constructorInvoker = codeBlock;
            return this;
        }
    }
}