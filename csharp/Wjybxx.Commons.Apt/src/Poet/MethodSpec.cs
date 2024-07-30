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

namespace Wjybxx.Commons.Poet
{
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

    /// <summary>
    /// 重写给定方法
    /// （注意：如果是泛型类的方法，通常需要先构造目标泛型类以确定泛型参数）
    /// </summary>
    public static Builder Overriding(MethodInfo methodInfo) {
        if (methodInfo.IsFinal || methodInfo.IsStatic || methodInfo.IsPrivate) {
            throw new ArgumentException("cannot override method with modifiers: " + methodInfo.Attributes);
        }
        return CopyMethod(methodInfo, true);
    }

    /// <summary>
    /// 拷贝方法信息
    /// </summary>
    public static Builder CopyMethod(MethodInfo methodInfo) {
        return CopyMethod(methodInfo, false);
    }

    private static Builder CopyMethod(MethodInfo methodInfo, bool overriding) {
        Builder builder = NewMethodBuilder(methodInfo.Name);
        // MethodInfo.GetBaseDefinition() 可判断是否是重写方法
        Modifiers modifiers = ParseModifiers(methodInfo, overriding);
        builder.AddModifiers(modifiers);
        // 拷贝泛型参数
        CopyTypeVariables(builder, methodInfo);
        // 拷贝返回值
        builder.Returns(TypeName.Get(methodInfo.ReturnType));
        // 拷贝方法参数
        CopyParameters(builder, methodInfo.GetParameters());
        // 处理params修饰符
        builder.Varargs(IsVarArgsMethod(methodInfo));
        return builder;
    }

    /// <summary>
    /// 解析方法的修饰符
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
    public static Modifiers ParseModifiers(MethodInfo methodInfo) {
        return ParseModifiers(methodInfo, false);
    }

    internal static Modifiers ParseModifiers(MethodInfo methodInfo, bool overriding) {
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
        // 处理unsafe
        bool hasPointerType = methodInfo.ReturnType.IsPointer;
        if (!hasPointerType) {
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            foreach (ParameterInfo parameterInfo in parameterInfos) {
                hasPointerType |= parameterInfo.ParameterType.IsPointer;
            }
        }
        if (hasPointerType) {
            modifiers |= Modifiers.Unsafe;
        }
        // 处理override -- 接口方法不需要override关键字，不论方法有没有默认实现；抽象类的抽象方法也不需要override关键字
        if (overriding && methodInfo.IsVirtual && methodInfo.DeclaringType!.IsClass) {
            modifiers |= Modifiers.Override;
        }
        return modifiers;
    }

    /// <summary>
    /// 拷贝泛型参数
    /// </summary>
    public static void CopyTypeVariables(Builder builder, MethodInfo methodInfo) {
        if (methodInfo.IsGenericMethodDefinition) {
            Type[] genericArguments = methodInfo.GetGenericArguments();
            foreach (Type genericArgument in genericArguments) {
                builder.AddTypeVariable(TypeVariableName.Get(genericArgument));
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

    /// <summary>
    /// 是否是变长参数方法
    /// </summary>
    public static bool IsVarArgsMethod(MethodInfo methodInfo) {
        ParameterInfo[] parameterInfos = methodInfo.GetParameters();
        if (parameterInfos.Length > 0) {
            return parameterInfos[parameterInfos.Length - 1].IsDefined(typeof(ParamArrayAttribute));
        }
        return false;
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
}