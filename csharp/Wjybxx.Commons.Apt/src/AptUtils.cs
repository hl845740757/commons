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
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Wjybxx.Commons.Poet;
using static Wjybxx.Commons.Poet.TypeName;

namespace Wjybxx.Commons.Apt
{
/// <summary>
/// 该工具类用于为Poet提供更多用户层的API
///
/// <h3>TypeSymbol解析</h3>
/// 该文件集中写TypeName的解析逻辑，可避免对CodeAnalyzer的依赖散步到其它文件。
///
/// <h3>NRT和可空结构体</h3>
/// NRT是注解，而<see cref="Nullable{T}"/>是类型；如果泛型参数被声明为<code>struct</code>，
/// 那么实际上是转换了泛型参数的类型的，类型的泛型参数不是<code>T</code>，而是<code>Nullable{T}</code>，
/// 因此并不会出现<see cref="NullableAnnotation"/>。
/// </summary>
public static class AptUtils
{
    private static readonly ClassName clsName_GeneratedAttribute = ClassName.Get("Wjybxx.Commons.Attributes", "GeneratedAttribute");
    private static readonly ClassName clsName_SourceFileRef = ClassName.Get("Wjybxx.Commons.Attributes", "SourceFileRefAttribute");

    /// <summary>
    /// 为生成代码的注解处理器创建一个通用注解
    /// </summary>
    public static AttributeSpec NewProcessorInfoAnnotation(Type type) {
        return AttributeSpec.NewBuilder(clsName_GeneratedAttribute)
            .Constructor(CodeBlock.Of("$S", type.ToString()))
            .Build();
    }

    /// <summary>
    /// 添加指向源代码文件的引用，方便查看文件依赖
    /// </summary>
    /// <param name="sourceFileTypeName"></param>
    /// <returns></returns>
    public static AttributeSpec NewSourceFileRefAnnotation(TypeName sourceFileTypeName) {
        return AttributeSpec.NewBuilder(clsName_SourceFileRef)
            .Constructor(CodeBlock.Of("typeof($T)", sourceFileTypeName))
            .Build();
    }

    /** @param cname 类的标准名，import语句格式 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClassName ClassNameOfCanonicalName(string cname) {
        int index = cname.LastIndexOf('.');
        return ClassName.Get(cname.Substring2(0, index), cname.Substring2(index + 1));
    }

    /// <summary>
    /// 根据原类型，计算对应的辅助类的类名
    /// 对于内部类，生成的类为：外部类名_内部类名
    /// </summary>
    /// <param name="type">type</param>
    /// <param name="suffix">后缀</param>
    /// <returns></returns>
    public static string GetProxyClassName(Type type, string? suffix = null) {
        if (suffix == null) suffix = "";

        string proxyName;
        if (type.DeclaringType == null) {
            proxyName = type.Name + suffix; // TopLevel
        } else {
            // 内部类，避免与其它的内部类冲突，不能使用简单名
            // Q: 为什么不使用$符合?
            // A: 因为生成的工具类都是外部类，不是内部类。
            List<string> simpleNames = new List<string>(3);
            simpleNames.Add(type.Name);
            while ((type = type.DeclaringType) != null) {
                simpleNames.Add(type.Name);
            }
            simpleNames.Reverse();
            proxyName = string.Join("_", simpleNames) + suffix;
        }
        // Type.Name 会包含反引号，我们需要去除
        if (proxyName.IndexOf('`') >= 0) {
            StringBuilder builder = new StringBuilder(proxyName.Length);
            for (var i = 0; i < proxyName.Length; i++) {
                if (proxyName[i] != '`') {
                    builder.Append(proxyName[i]);
                }
            }
            proxyName = builder.ToString();
        }
        return proxyName;
    }

    /// <summary>
    /// 根据原类型，计算对应的辅助类的类名
    /// 对于内部类，生成的类为：外部类名_内部类名
    /// </summary>
    /// <param name="type">type</param>
    /// <param name="suffix">后缀</param>
    /// <returns></returns>
    public static string GetProxyClassName(INamedTypeSymbol type, string? suffix = null) {
        if (suffix == null) suffix = "";

        string proxyName;
        if (type.ContainingType == null) {
            proxyName = type.Name + suffix; // TopLevel
        } else {
            // 内部类，避免与其它的内部类冲突，不能使用简单名
            // Q: 为什么不使用$符合?
            // A: 因为生成的工具类都是外部类，不是内部类。
            List<string> simpleNames = new List<string>(3);
            simpleNames.Add(type.Name);
            while ((type = type.ContainingType) != null) {
                simpleNames.Add(type.Name);
            }
            simpleNames.Reverse();
            proxyName = string.Join("_", simpleNames) + suffix;
        }
        // TypeSymbol.Name 不包含反引号(MetadataName会包含反引号)
        return proxyName;
    }

    #region flat

    /**
     * 将继承体系展开，不包含实现的接口。
     * （超类在后，包含object）
     */
    public static List<Type> FlatInherit(Type typeElement) {
        List<Type> result = new List<Type>(4);
        result.Add(typeElement);
        while ((typeElement = typeElement.BaseType) != null) {
            result.Add(typeElement);
        }
        return result;
    }

    /**
     * 将继承体系展开，并逆序返回，不包含实现的接口。
     * （超类在前，包含object）
     */
    public static List<Type> FlatInheritAndReverse(Type typeElement) {
        List<Type> result = FlatInherit(typeElement);
        result.Reverse();
        return result;
    }

    /**
     * 将继承体系展开，不包含实现的接口。
     * （超类在后，包含object）
     */
    public static List<INamedTypeSymbol> FlatInherit(INamedTypeSymbol typeElement) {
        List<INamedTypeSymbol> result = new List<INamedTypeSymbol>(4);
        result.Add(typeElement);
        while ((typeElement = typeElement.BaseType) != null) {
            result.Add(typeElement);
        }
        return result;
    }

    /**
     * 将继承体系展开，并逆序返回，不包含实现的接口。
     * （超类在前，包含object）
     */
    public static List<INamedTypeSymbol> FlatInheritAndReverse(INamedTypeSymbol typeElement) {
        List<INamedTypeSymbol> result = FlatInherit(typeElement);
        result.Reverse();
        return result;
    }

    #endregion

    #region baisc

    /// <summary>
    /// 是否出现了Nullable注解（NRT）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNullableAnnotated(this ITypeSymbol typeSymbol) {
        return typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;
    }

    /// <summary>
    /// 将Nullable注解转换为TypeName的属性
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeNameAttributes ToTypeNameAttributes(this NullableAnnotation annotation) {
        return annotation == NullableAnnotation.Annotated
            ? TypeNameAttributes.NullableReferenceType
            : TypeNameAttributes.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPublic(this ISymbol symbol) {
        return symbol.DeclaredAccessibility == Accessibility.Public;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPrivate(this ISymbol symbol) {
        return symbol.DeclaredAccessibility == Accessibility.Private;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsProtected(this ISymbol symbol) {
        return symbol.DeclaredAccessibility == Accessibility.Protected
               || symbol.DeclaredAccessibility == Accessibility.ProtectedAndInternal;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInternal(this ISymbol symbol) {
        return symbol.DeclaredAccessibility == Accessibility.Internal
               || symbol.DeclaredAccessibility == Accessibility.ProtectedAndInternal;
    }

    /// <summary>
    /// 是否是变长参数方法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsVarArgsMethod(IMethodSymbol methodInfo) {
        ImmutableArray<IParameterSymbol> parameters = methodInfo.Parameters;
        if (parameters.Length == 0) return false;
        // c#12支持params collection.... Span<int>
        return parameters[parameters.Length - 1].IsParams;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeSymbol">枚举类型</param>
    /// <param name="value">枚举的数组</param>
    /// <returns></returns>
    public static string? GetEnumName(INamedTypeSymbol typeSymbol, int value) {
        foreach (ISymbol member in typeSymbol.GetMembers()) {
            if (!member.IsStatic || member.Kind != SymbolKind.Field) continue;

            IFieldSymbol fieldSymbol = (IFieldSymbol)member;
            if (fieldSymbol.ConstantValue is int constantValue && constantValue == value) {
                return fieldSymbol.Name;
            }
        }
        return null;
    }

    #endregion

    #region attribute-data

    /// <summary>
    /// 查找指定的属性
    /// </summary>
    public static AttributeData? GetAttribute(ImmutableArray<AttributeData> attributeDataArray, INamedTypeSymbol attributeClass) {
        SymbolEqualityComparer comparer = SymbolEqualityComparer.Default;
        foreach (AttributeData attributeData in attributeDataArray) {
            if (attributeData.AttributeClass == null) continue;
            if (attributeData.AttributeClass.Equals(attributeClass, comparer)) return attributeData;
        }
        return null;
    }

    /// <summary>
    /// 查找指定的属性
    /// </summary>
    /// <param name="attributeDataArray"></param>
    /// <param name="attributeClassName">属性类的全限定名</param>
    /// <returns></returns>
    public static AttributeData? GetAttribute(ImmutableArray<AttributeData> attributeDataArray, string attributeClassName) {
        foreach (AttributeData attributeData in attributeDataArray) {
            if (attributeData.AttributeClass == null) continue;
            if (attributeData.AttributeClass.ToDisplayString() == attributeClassName) return attributeData;
        }
        return null;
    }

    /// <summary>
    /// 查找属性的指定值
    /// </summary>
    /// <returns></returns>
    public static bool GetAttributeValue(AttributeData attributeData, string propertyName, out TypedConstant typedConstant) {
        foreach (var pair in attributeData.NamedArguments) {
            if (pair.Key == propertyName) {
                typedConstant = pair.Value;
                return true;
            }
        }
        typedConstant = default;
        return false;
    }

    /// <summary>
    /// 注意：不适用数组
    /// </summary>
    /// <param name="typedConstant"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string? GetValueAsString(this TypedConstant typedConstant) {
        // string, object 归属在 TypedConstantKind.Primitive 下
        object value = typedConstant.Value;
        return value == null ? null : value.ToString();
    }

    #endregion

    #region 类型测试

    /// <summary>
    /// 测试是否是同一个类型
    /// </summary>
    /// <param name="self"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool IsSameType(this ITypeSymbol self, ITypeSymbol target) {
        return self.Equals(target, SymbolEqualityComparer.Default);
    }

    /// <summary>
    /// 测试是是否是目标类型的子类型
    /// </summary>
    /// <param name="self"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static bool IsSubTypeOf(this ITypeSymbol self, ITypeSymbol target) {
        SymbolEqualityComparer comparer = SymbolEqualityComparer.Default;
        if (self.Equals(target, comparer)) {
            return true;
        }
        // 结构体也可以实现接口
        if (target.TypeKind == TypeKind.Interface) {
            foreach (INamedTypeSymbol baseType in self.AllInterfaces) {
                if (baseType.Equals(target, comparer)) return true;
            }
            return false;
        }
        if (target.TypeKind != TypeKind.Class) {
            return false;
        }
        // 现在目标类型只能是class
        {
            INamedTypeSymbol baseType = self.BaseType;
            while (baseType != null) {
                if (baseType.Equals(target, comparer)) return true;
                baseType = baseType.BaseType;
            }
        }
        return false;
    }

    /// <summary>
    /// 是否是基础类型的数字类型
    /// </summary>
    /// <param name="self"></param>
    /// <param name="includeDecimal"></param>
    /// <returns></returns>
    public static bool IsPrimitiveNumber(this ITypeSymbol self, bool includeDecimal = false) {
        switch (self.SpecialType) {
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double: {
                return true;
            }
            case SpecialType.System_Decimal: {
                return includeDecimal;
            }
            default: {
                return false;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsByteArray(this ITypeSymbol typeSymbol) {
        return typeSymbol is IArrayTypeSymbol arrayTypeSymbol
               && arrayTypeSymbol.ElementType.SpecialType == SpecialType.System_Byte;
    }

    #endregion

    #region Parse-TypeSymbol

    /// <summary>
    /// 解析编译时的<see cref="ITypeSymbol"/>为<see cref="TypeName"/>
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TypeName ParseType(ITypeSymbol typeSymbol) {
        return typeSymbol.TypeKind switch
        {
            TypeKind.Pointer => ParsePointerType((IPointerTypeSymbol)typeSymbol),
            TypeKind.Array => ParseArrayType((IArrayTypeSymbol)typeSymbol),
            TypeKind.TypeParameter => ParseTypeParameter((ITypeParameterSymbol)typeSymbol),
            _ => ParseNamedType((INamedTypeSymbol)typeSymbol)
        };
    }

    /// <summary>
    /// 解析普通类型
    /// 
    /// 注：该方法开放没有意义，因为解析结果不一定是<see cref="ClassName"/>，用户调用<see cref="ParseType"/>即可。
    /// </summary>
    private static TypeName ParseNamedType(INamedTypeSymbol typeSymbol) {
        // 先测试特殊类型
        switch (typeSymbol.SpecialType) {
            case SpecialType.System_Int32: return INT;
            case SpecialType.System_UInt32: return UINT;
            case SpecialType.System_Int64: return LONG;
            case SpecialType.System_UInt64: return ULONG;
            case SpecialType.System_Single: return FLOAT;
            case SpecialType.System_Double: return DOUBLE;

            case SpecialType.System_Boolean: return BOOL;
            case SpecialType.System_Byte: return BYTE;
            case SpecialType.System_SByte: return SBYTE;
            case SpecialType.System_Int16: return SHORT;
            case SpecialType.System_UInt16: return USHORT;
            case SpecialType.System_Char: return USHORT;
            case SpecialType.System_Decimal: return DECIMAL;

            case SpecialType.System_String: return typeSymbol.IsNullableAnnotated() ? STRING.MakeNullableType() : STRING;
            case SpecialType.System_Object: return typeSymbol.IsNullableAnnotated() ? OBJECT.MakeNullableType() : OBJECT;
            case SpecialType.System_Void: return VOID;

            case SpecialType.System_ValueType: return ClassName.VALUE_TYPE;
            case SpecialType.System_Nullable_T: return ClassName.NULLABLE;
            case SpecialType.System_IntPtr: return ClassName.INT_PTR;
            case SpecialType.System_UIntPtr: return ClassName.UINT_PTR;
        }

        List<TypeName>? genericArgumentNames = null;
        if (typeSymbol.IsUnboundGenericType) {
            // typeof(IDictionary<,>)
            int typeArgumentsLength = typeSymbol.ConstructedFrom.TypeArguments.Length;
            genericArgumentNames = new List<TypeName>(typeArgumentsLength);
            for (int i = 0; i < typeArgumentsLength; i++) {
                genericArgumentNames.Add(TypeVariableName.Empty);
            }
        } else if (typeSymbol.IsGenericType) {
            ImmutableArray<ITypeSymbol> genericArguments = typeSymbol.TypeArguments;
            ImmutableArray<NullableAnnotation> nullableAnnotations = typeSymbol.TypeArgumentNullableAnnotations;
            genericArgumentNames = new List<TypeName>(genericArguments.Length);
            for (int index = 0; index < genericArguments.Length; index++) {
                ITypeSymbol genericArgument = genericArguments[index];
                TypeNameAttributes attributes = nullableAnnotations[index].ToTypeNameAttributes();
                genericArgumentNames.Add(ParseType(genericArgument).AddAttributes(attributes));
            }
        }
        if (typeSymbol.ContainingType != null) {
            ClassName outerClassName = (ClassName)ParseNamedType(typeSymbol.ContainingType);
            return outerClassName.NestedClass(typeSymbol.Name, genericArgumentNames, false,
                typeSymbol.NullableAnnotation.ToTypeNameAttributes());
        }
        return ClassName.Get(typeSymbol.ContainingNamespace.ToDisplayString(),
            typeSymbol.Name, genericArgumentNames,
            typeSymbol.NullableAnnotation.ToTypeNameAttributes());
    }

    /// <summary>
    /// 解析数组类型
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ArrayTypeName ParseArrayType(IArrayTypeSymbol arrayTypeSymbol) {
        // NRT -- 不清楚ElementNullableAnnotation是否和ElementType.NullableAnnotation是否有区别，但不处理是没影响的
        TypeNameAttributes attributes = arrayTypeSymbol.NullableAnnotation.ToTypeNameAttributes();
        return ArrayTypeName.Get(ParseType(arrayTypeSymbol.ElementType), attributes);
    }

    /// <summary>
    /// 解析指针类型
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PointerTypeName ParsePointerType(IPointerTypeSymbol pointerTypeSymbol) {
        // NRT
        TypeNameAttributes attributes = pointerTypeSymbol.NullableAnnotation.ToTypeNameAttributes();
        return PointerTypeName.Of(ParseType(pointerTypeSymbol.PointedAtType), attributes);
    }

    /// <summary>
    /// 解析泛型变量
    /// </summary>
    public static TypeVariableName ParseTypeParameter(ITypeParameterSymbol typeParameterSymbol) {
        TypeNameAttributes attributes = TypeNameAttributes.None;
        // In Out
        if (typeParameterSymbol.Variance == VarianceKind.In) {
            attributes |= TypeNameAttributes.VarianceIn;
        } else if (typeParameterSymbol.Variance == VarianceKind.Out) {
            attributes |= TypeNameAttributes.VarianceOut;
        }
        // Nullable
        if (typeParameterSymbol.HasNotNullConstraint) {
            attributes |= TypeNameAttributes.NotNullableReferenceType;
        } else if (typeParameterSymbol.IsNullableAnnotated()) {
            attributes |= TypeNameAttributes.NullableReferenceType;
        }
        // 类型约束
        if (typeParameterSymbol.HasReferenceTypeConstraint) {
            attributes |= TypeNameAttributes.ReferenceTypeConstraint;
        }
        if (typeParameterSymbol.HasValueTypeConstraint) {
            attributes |= TypeNameAttributes.ValueTypeConstraint;
        }
        if (typeParameterSymbol.HasConstructorConstraint) {
            attributes |= TypeNameAttributes.DefaultConstructorConstraint;
        }
        // 上界
        ImmutableArray<ITypeSymbol> constraintTypes = typeParameterSymbol.ConstraintTypes;
        ImmutableArray<NullableAnnotation> nullableAnnotations = typeParameterSymbol.ConstraintNullableAnnotations;
        if (constraintTypes.Length == 0) {
            return TypeVariableName.Get(typeParameterSymbol.Name, attributes);
        }
        List<TypeName> bounds = new List<TypeName>(constraintTypes.Length);
        for (int index = 0; index < constraintTypes.Length; index++) {
            ITypeSymbol constraintType = constraintTypes[index];
            TypeName bound = ParseType(constraintType).AddAttributes(nullableAnnotations[index].ToTypeNameAttributes());
            bounds.Add(bound);
        }
        return TypeVariableName.Get(typeParameterSymbol.Name, bounds, attributes);
    }

    #endregion

    #region parse-method

    /// <summary>
    /// 获取方法返回值的类型名
    /// </summary>
    public static TypeName ParseMethodReturnType(IMethodSymbol methodSymbol) {
        TypeName typeName = ParseType(methodSymbol.ReturnType);
        if (methodSymbol.RefKind == RefKind.Ref) {
            typeName = typeName.MakeByRefType();
        } else if (methodSymbol.RefKind == RefKind.RefReadOnly) {
            typeName = typeName.MakeByRefType(ByRefTypeName.Kind.RefReadOnly);
        }
        // NRT
        if (methodSymbol.ReturnNullableAnnotation == NullableAnnotation.Annotated) {
            return typeName.AddAttributes(TypeNameAttributes.NullableReferenceType);
        }
        return typeName;
    }

    /// <summary>
    /// 获取方法参数的类型名
    /// </summary>
    public static TypeName ParseMethodParameterType(IParameterSymbol parameterSymbol) {
        TypeName typeName = ParseType(parameterSymbol.Type);
        // 修正ref
        switch (parameterSymbol.RefKind) {
            case RefKind.Ref:
                typeName = typeName.MakeByRefType();
                break;
            case RefKind.Out:
                typeName = typeName.MakeByRefType(ByRefTypeName.Kind.Out);
                break;
            case RefKind.In:
                typeName = typeName.MakeByRefType(ByRefTypeName.Kind.In);
                break;
        }
        // NRT
        if (parameterSymbol.NullableAnnotation == NullableAnnotation.Annotated) {
            return typeName.AddAttributes(TypeNameAttributes.NullableReferenceType);
        }
        return typeName;
    }

    #endregion

    #region overriding-roslyn

    /// <summary>
    /// 重写给定方法
    /// （注意：如果是泛型类的方法，通常需要先构造目标泛型类以确定泛型参数）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodSpec.Builder Overriding(IMethodSymbol methodInfo) {
        if (methodInfo.IsStatic || methodInfo.IsPrivate() || methodInfo.IsSealed) {
            throw new ArgumentException("cannot override target method: " + methodInfo);
        }
        return CopyMethod(methodInfo, true);
    }

    private static MethodSpec.Builder CopyMethod(IMethodSymbol methodInfo, bool overriding = false) {
        Modifiers modifiers = ParseModifiers(methodInfo, overriding);
        MethodSpec.Builder builder;
        if (methodInfo.MethodKind == MethodKind.Constructor) {
            builder = MethodSpec.NewConstructorBuilder();
        } else {
            builder = MethodSpec.NewMethodBuilder(methodInfo.Name);
        }
        builder.AddModifiers(modifiers);
        // 拷贝泛型参数
        CopyTypeVariables(builder, methodInfo);
        // 拷贝返回值
        builder.Returns(ParseMethodReturnType(methodInfo));
        // 拷贝方法参数
        CopyParameters(builder, methodInfo);
        // 处理params参数和扩展方法
        builder.SetVarargs(IsVarArgsMethod(methodInfo));
        builder.SetExtensionMethod(methodInfo.IsExtensionMethod);
        return builder;
    }

    /// <summary>
    /// 拷贝泛型参数
    /// </summary>
    public static void CopyTypeVariables(MethodSpec.Builder builder, IMethodSymbol methodInfo) {
        ImmutableArray<ITypeParameterSymbol> typeParameterSymbols = methodInfo.TypeParameters;
        foreach (ITypeParameterSymbol typeParameterSymbol in typeParameterSymbols) {
            builder.AddTypeVariable(ParseTypeParameter(typeParameterSymbol));
        }
    }

    /// <summary>
    /// 拷贝方法参数
    ///
    /// ImmutableArray是struct，因此我们传入<see cref="IMethodSymbol"/>
    /// </summary>
    public static void CopyParameters(MethodSpec.Builder builder, IMethodSymbol methodInfo) {
        foreach (IParameterSymbol parameter in methodInfo.Parameters) {
            builder.AddParameter(ParameterSpec.NewBuilder(ParseMethodParameterType(parameter), parameter.Name).Build());
        }
    }

    /// <summary>
    /// 解析方法的修饰符
    /// </summary>
    /// <param name="methodInfo">方法信息</param>
    /// <param name="overriding">是否用于重写</param>
    /// <returns></returns>
    public static Modifiers ParseModifiers(IMethodSymbol methodInfo, bool overriding = false) {
        Modifiers modifiers = methodInfo.DeclaredAccessibility switch
        {
            Accessibility.Public => Modifiers.Public,
            Accessibility.Private => Modifiers.Private,
            Accessibility.ProtectedAndInternal => Modifiers.Protected | Modifiers.Internal,
            Accessibility.Protected => Modifiers.Protected,
            Accessibility.Internal => Modifiers.Internal,
            _ => Modifiers.None // 不能识别一律置空
        };
        if (methodInfo.IsStatic) modifiers |= Modifiers.Static;
        if (methodInfo.IsAsync) modifiers |= Modifiers.Async;
        // 重写相关
        if (methodInfo.IsSealed) modifiers |= Modifiers.Sealed;
        if (!overriding && methodInfo.ContainingType.TypeKind == TypeKind.Class) {
            if (methodInfo.IsAbstract) modifiers |= Modifiers.Abstract;
            if (methodInfo.IsVirtual) modifiers |= Modifiers.Virtual;
        }
        // 处理unsafe
        bool hasPointerType = methodInfo.ReturnType.Kind == SymbolKind.PointerType;
        if (!hasPointerType) {
            ImmutableArray<IParameterSymbol> parameterInfos = methodInfo.Parameters;
            foreach (IParameterSymbol parameterInfo in parameterInfos) {
                hasPointerType |= parameterInfo.Type.Kind == SymbolKind.PointerType;
            }
        }
        if (hasPointerType) {
            modifiers |= Modifiers.Unsafe;
        }
        // 处理override -- 接口方法不需要override关键字，不论方法有没有默认实现
        if (overriding && methodInfo.ContainingType.TypeKind == TypeKind.Class) {
            modifiers |= Modifiers.Override;
        }
        return modifiers;
    }

    #endregion
}
}