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
/// 方法或构造函数
/// </summary>
public class MethodSpec : ISpecification
{
    public readonly Kind kind;
    public readonly string name;
    public readonly Modifiers modifiers;
    public readonly CodeBlock document;
    public readonly CodeBlock headerCode;
    public readonly IList<AttributeSpec> attributes;

    public readonly TypeName? explicitBaseType; // 显式实现的接口
    public readonly IList<TypeParameterSpec> typeParameters; // 泛型参数
    public readonly TypeName returnType; // 返回值类型
    public readonly IList<ParameterSpec> parameters; // 方法参数
    public readonly bool isVarargs; // 是否变长参数(params T[] args)
    public readonly bool isExtensionMethod; // 是否是扩展方法(this T type)

    public readonly CodeBlock? code; // 方法体(委托一定没有，接口可能有，也可能没有)
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
        typeParameters = Util.ToImmutableList(builder.typeParameters);
        parameters = Util.ToImmutableList(builder.parameters);
        isVarargs = builder.isVarargs;
        isExtensionMethod = builder.isExtensionMethod;

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
            .AddTypeParameters(typeParameters)
            .Returns(returnType)
            .AddParameters(parameters);
        builder.explicitBaseType = explicitBaseType;
        builder.isVarargs = isVarargs;
        builder.code = code;
        builder.constructorInvoker = constructorInvoker;
        return builder;
    }

    #endregion

    #region overriding

    /// <summary>
    /// 重写给定方法
    ///
    /// 1.如果是泛型类的方法，通常需要先构造目标泛型类以确定泛型参数。
    /// 2.默认会删除<see cref="Modifiers.Abstract"/>和<see cref="Modifiers.Virtual"/>，
    /// 如果方法来自于Class，则还会添加<see cref="Modifiers.Override"/>修饰符。
    /// </summary>
    public static Builder Overriding(MethodInfo methodInfo) {
        return CopyMethod(methodInfo, true);
    }

    private static Builder CopyMethod(MethodInfo methodInfo, bool overriding) {
        Modifiers modifiers = Util.ParseModifiers(methodInfo);
        if (overriding) {
            modifiers = Util.AddOverrideModifiers(modifiers, methodInfo.DeclaringType!.IsClass);
        }
        Builder builder = NewMethodBuilder(methodInfo.Name);
        builder.AddModifiers(modifiers);
        // 拷贝泛型参数
        CopyTypeVariables(builder, methodInfo);
        // 拷贝返回值
        TypeName returnType = TypeName.Get(methodInfo.ReturnType);
#if NET6_0_OR_GREATER
        // if (methodInfo.ReturnTypeCustomAttributes.IsDefined(typeof(NullableAttribute))) {
        //     returnType = returnType.AddAttributes(TypeNameAttributes.NullableReferenceType);
        // }
#endif
        builder.Returns(returnType);
        // 拷贝方法参数
        CopyParameters(builder, methodInfo.GetParameters());
        // 处理params参数和扩展方法
        builder.SetVarargs(Util.IsVarArgsMethod(methodInfo));
        builder.SetExtensionMethod(Util.IsExtensionMethod(methodInfo));
        return builder;
    }

    /// <summary>
    /// 拷贝泛型参数
    /// </summary>
    public static void CopyTypeVariables(Builder builder, MethodInfo methodInfo) {
        if (methodInfo.IsGenericMethodDefinition) {
            Type[] genericArguments = methodInfo.GetGenericArguments();
            foreach (Type genericArgument in genericArguments) {
                builder.AddTypeParameter(TypeParameterSpec.Get(genericArgument));
            }
        }
    }

    /// <summary>
    /// 拷贝方法参数
    /// </summary>
    public static void CopyParameters(Builder builder, IEnumerable<ParameterInfo> parameters) {
        foreach (ParameterInfo parameter in parameters) {
            builder.AddParameter(ParameterSpec.Get(parameter));
        }
    }

    #endregion

    public class Builder
    {
        public readonly Kind kind;
        public readonly string name;
        public Modifiers modifiers;
        public readonly CodeBlock.Builder document = CodeBlock.NewBuilder();
        public readonly CodeBlock.Builder headerCode = CodeBlock.NewBuilder();
        public readonly List<AttributeSpec> attributes = new();

        public TypeName? explicitBaseType;
        public readonly List<TypeParameterSpec> typeParameters = new();
        public TypeName returnType = TypeName.VOID;
        public readonly List<ParameterSpec> parameters = new();
        public bool isVarargs;
        public bool isExtensionMethod;

        /// <summary>
        /// 由于代码的的构建逻辑较多，Builder不进行完整的代理，外部直接访问该字段构建即可；
        /// </summary>
        public CodeBlock? code;
        public CodeBlock? constructorInvoker;
        /// <summary>
        /// 用于简化代码编写 -- 构建时如果code为null，而builder不为空(empty)，则自动构建为code。
        /// </summary>
        public readonly CodeBlock.Builder codeBuilder = CodeBlock.NewBuilder();

        internal Builder(Kind kind, string name, Modifiers modifiers = 0) {
            this.kind = kind;
            this.name = Util.CheckNotBlank(name, "name is blank");
            this.modifiers = modifiers;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="forceBuildCode">代码块为空的情况下是否也构建代码</param>
        /// <returns></returns>
        public MethodSpec Build(bool forceBuildCode = false) {
            if (code == null && (forceBuildCode || !codeBuilder.IsEmpty)) {
                code = codeBuilder.Build();
            }
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
        //

        public Builder AddTypeParameters(IEnumerable<TypeParameterSpec> typeParameters) {
            if (typeParameters == null) throw new ArgumentNullException(nameof(typeParameters));
            foreach (TypeParameterSpec typeParameter in typeParameters) {
                Util.CheckNotNull(typeParameter, "typeVariable");
                this.typeParameters.Add(typeParameter);
            }
            return this;
        }

        public Builder AddTypeParameter(TypeParameterSpec typeParameter) {
            if (typeParameter == null) throw new ArgumentNullException(nameof(typeParameter));
            typeParameters.Add(typeParameter);
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

        public Builder SetVarargs(bool varargs = true) {
            this.isVarargs = varargs;
            return this;
        }

        public Builder SetExtensionMethod(bool isExtensionMethod = true) {
            this.isExtensionMethod = isExtensionMethod;
            return this;
        }

        //
        public Builder ExplicitImpl(TypeName baseType) {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (this.explicitBaseType != null) throw new InvalidOperationException("explicitImpl was already set");
            this.explicitBaseType = baseType;
            return this;
        }

        public Builder Code(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.code != null) throw new InvalidOperationException("code was already set");
            this.code = codeBlock;
            return this;
        }

        public Builder ConstructorInvoker(CodeBlock codeBlock) {
            if (codeBlock == null) throw new ArgumentNullException(nameof(codeBlock));
            if (this.constructorInvoker != null) throw new InvalidOperationException("constructorInvoker was already set");
            this.constructorInvoker = codeBlock;
            return this;
        }
    }
}
}