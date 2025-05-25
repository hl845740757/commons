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
using System.Collections.Concurrent;
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
    public const string CNAME_UsedForReflectionAttribute = "Wjybxx.Commons.Attributes.UsedForReflectionBasedGeneratorAttribute";

    /// <summary>
    /// 为生成代码的注解处理器创建一个通用注解
    /// </summary>
    /// <param name="type">生成器的类型信息</param>
    /// <param name="version">生成器的版本</param>
    /// <param name="assembly">归属的程序集</param>
    /// <param name="dateTime">执行时间</param>
    /// <returns></returns>
    public static AttributeSpec NewProcessorInfoAnnotation(Type type,
                                                           string? version = null,
                                                           string? assembly = null,
                                                           DateTime? dateTime = null) {
        var builder = AttributeSpec.NewBuilder(clsName_GeneratedAttribute)
            .Constructor(CodeBlock.Of("$S", type.ToString()));
        if (assembly != null) {
            builder.AddMember("Assembly", "$S", assembly);
        }
        if (version != null) {
            builder.AddMember("Version", "$S", version);
        }
        if (dateTime != null) {
            builder.AddMember("DateTime", "$S", dateTime.Value.ToString("s"));
        }
        return builder.Build();
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
            proxyName = Util.GetSimpleName(type) + suffix; // TopLevel
        } else {
            // 内部类，避免与其它的内部类冲突，不能使用简单名
            // Q: 为什么不使用$符合?
            // A: 因为生成的工具类都是外部类，不是内部类。
            List<string> simpleNames = new List<string>(3);
            simpleNames.Add(Util.GetSimpleName(type));
            while ((type = type.DeclaringType) != null) {
                simpleNames.Add(Util.GetSimpleName(type));
            }
            simpleNames.Reverse();
            proxyName = string.Join("_", simpleNames) + suffix;
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
    public static List<Type> FlatInherit(Type type) {
        List<Type> result = new List<Type>(4);
        result.Add(type);
        while ((type = type.BaseType) != null) {
            result.Add(type);
        }
        return result;
    }

    /**
     * 将继承体系展开，并逆序返回，不包含实现的接口。
     * （超类在前，包含object）
     */
    public static List<Type> FlatInheritAndReverse(Type type) {
        List<Type> result = FlatInherit(type);
        result.Reverse();
        return result;
    }

    /**
     * 将继承体系展开，不包含实现的接口。
     * （超类在后，包含object）
     */
    public static List<INamedTypeSymbol> FlatInherit(INamedTypeSymbol typeSymbol) {
        List<INamedTypeSymbol> result = new List<INamedTypeSymbol>(4);
        result.Add(typeSymbol);
        while ((typeSymbol = typeSymbol.BaseType) != null) {
            result.Add(typeSymbol);
        }
        return result;
    }

    /**
     * 将继承体系展开，并逆序返回，不包含实现的接口。
     * （超类在前，包含object）
     */
    public static List<INamedTypeSymbol> FlatInheritAndReverse(INamedTypeSymbol typeSymbol) {
        List<INamedTypeSymbol> result = FlatInherit(typeSymbol);
        result.Reverse();
        return result;
    }

    #endregion

    #region baisc

    /// <summary>
    /// 获取symbol的第一个位置
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public static Location? GetFirstLocation(this ISymbol symbol) {
        ImmutableArray<Location> locations = symbol.Locations;
        return locations.Length == 0 ? null : locations[0];
    }

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
    /// 属性是否可读
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanRead(this IPropertySymbol symbol) {
        return !symbol.IsWriteOnly;
    }

    /// <summary>
    /// 属性是否可写
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanWrite(this IPropertySymbol symbol) {
        return !symbol.IsReadOnly;
    }

    /// <summary>
    /// 获取类型的完整MetadataName
    /// <code>System.Collections.Generic.Dictionary`2+Enumerator</code>
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <returns></returns>
    public static string GetFullMetadataName(INamedTypeSymbol typeSymbol) {
        string ns = typeSymbol.ContainingNamespace.ToDisplayString();
        if (typeSymbol.ContainingType == null) {
            return ns + "." + typeSymbol.MetadataName;
        }
        List<INamedTypeSymbol> containingTypes = new List<INamedTypeSymbol>();
        INamedTypeSymbol temp = typeSymbol.ContainingType;
        while (temp != null) {
            containingTypes.Add(temp);
            temp = temp.ContainingType;
        }
        containingTypes.Reverse();

        StringBuilder sb = new StringBuilder(32);
        sb.Append(sb).Append('.');
        foreach (INamedTypeSymbol containingType in containingTypes) {
            sb.Append(containingType.MetadataName);
            sb.Append('+');
        }
        sb.Append(typeSymbol.MetadataName);
        return sb.ToString();
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
    /// 是否包含用于反射时的注解
    /// </summary>
    /// <param name="attributeDataArray"></param>
    /// <returns></returns>
    public static bool HasUsedForReflectionAttribute(ImmutableArray<AttributeData> attributeDataArray) {
        return GetAttribute(attributeDataArray, CNAME_UsedForReflectionAttribute) != null;
    }

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
    /// 查找属性的指定值
    /// </summary>
    /// <param name="attributeData">注解</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    public static int GetAttributeValueValue(AttributeData attributeData, string propertyName, int def) {
        foreach (var pair in attributeData.NamedArguments) {
            if (pair.Key == propertyName) {
                return (int)pair.Value.Value!;
            }
        }
        return def;
    }

    /// <summary>
    /// 查找属性的指定值
    /// </summary>
    /// <param name="attributeData">注解</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    public static bool GetAttributeValueValue(AttributeData attributeData, string propertyName, bool def) {
        foreach (var pair in attributeData.NamedArguments) {
            if (pair.Key == propertyName) {
                return (bool)pair.Value.Value!;
            }
        }
        return def;
    }

    /// <summary>
    /// 查找属性的指定值
    /// </summary>
    /// <param name="attributeData">注解</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    public static object? GetAttributeValueValue(AttributeData attributeData, string propertyName, object? def) {
        foreach (var pair in attributeData.NamedArguments) {
            if (pair.Key == propertyName) {
                return pair.Value.Value;
            }
        }
        return def;
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
    /// name解析缓存--避免频繁解析Symbol
    /// 注意：需要包含Nullable注解信息
    /// </summary>
    private static readonly ConcurrentDictionary<INamedTypeSymbol, TypeName> typeSymbol2NameCache = new(SymbolEqualityComparer.IncludeNullability);

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

            case SpecialType.System_String: return typeSymbol.IsNullableAnnotated() ? NRT_STRING : STRING;
            case SpecialType.System_Object: return typeSymbol.IsNullableAnnotated() ? NRT_OBJECT : OBJECT;
            case SpecialType.System_Void: return VOID;

            case SpecialType.System_ValueType: return ClassName.VALUE_TYPE;
            case SpecialType.System_Nullable_T: return ClassName.NULLABLE;
            case SpecialType.System_IntPtr: return ClassName.INT_PTR;
            case SpecialType.System_UIntPtr: return ClassName.UINT_PTR;
        }
        if (typeSymbol2NameCache.TryGetValue(typeSymbol, out TypeName r)) {
            return r;
        }
        List<TypeName>? genericArgumentNames = null;
        if (typeSymbol.IsUnboundGenericType) {
            // typeof(IDictionary<,>)
            int typeArgumentsLength = typeSymbol.OriginalDefinition.TypeArguments.Length;
            genericArgumentNames = new List<TypeName>(typeArgumentsLength);
            for (int i = 0; i < typeArgumentsLength; i++) {
                genericArgumentNames.Add(TypeParameterName.Empty);
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
            r = outerClassName.NestedClass(typeSymbol.Name, genericArgumentNames, false,
                typeSymbol.NullableAnnotation.ToTypeNameAttributes());
        } else {
            r = ClassName.Get(typeSymbol.ContainingNamespace.ToDisplayString(),
                typeSymbol.Name, genericArgumentNames,
                typeSymbol.NullableAnnotation.ToTypeNameAttributes());
        }
        typeSymbol2NameCache[typeSymbol] = r;
        return r;
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
        return PointerTypeName.Get(ParseType(pointerTypeSymbol.PointedAtType), attributes);
    }

    /// <summary>
    /// 解析泛型变量
    /// </summary>
    public static TypeParameterName ParseTypeParameter(ITypeParameterSymbol typeParameterSymbol) {
        TypeNameAttributes attributes = typeParameterSymbol.NullableAnnotation.ToTypeNameAttributes();
        return TypeParameterName.Get(typeParameterSymbol.Name, attributes);
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

    #region overriding-method

    /// <summary>
    /// 重写给定方法
    /// （注意：如果是泛型类的方法，通常需要先构造目标泛型类以确定泛型参数）
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MethodSpec.Builder Overriding(IMethodSymbol methodInfo) {
        return CopyMethod(methodInfo, true);
    }

    private static MethodSpec.Builder CopyMethod(IMethodSymbol methodInfo, bool overriding = false) {
        Modifiers modifiers = ParseModifiers(methodInfo);
        if (overriding) {
            modifiers = Util.AddOverrideModifiers(modifiers, methodInfo.ContainingType.TypeKind == TypeKind.Class);
        }
        MethodSpec.Builder builder;
        if (methodInfo.MethodKind == MethodKind.Constructor) {
            builder = MethodSpec.NewConstructorBuilder();
        } else {
            builder = MethodSpec.NewMethodBuilder(methodInfo.Name);
        }
        builder.AddModifiers(modifiers);
        // 拷贝泛型参数
        CopyTypeParameters(builder, methodInfo);
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
    /// 拷贝方法泛型参数
    /// </summary>
    public static void CopyTypeParameters(MethodSpec.Builder builder, IMethodSymbol methodInfo) {
        ImmutableArray<ITypeParameterSymbol> typeParameterSymbols = methodInfo.TypeParameters;
        foreach (ITypeParameterSymbol typeParameterSymbol in typeParameterSymbols) {
            builder.AddTypeParameter(CopyTypeParameter(typeParameterSymbol));
        }
    }

    /// <summary>
    /// 拷贝方法参数
    ///
    /// ImmutableArray是struct，因此我们传入<see cref="IMethodSymbol"/>
    /// </summary>
    public static void CopyParameters(MethodSpec.Builder builder, IMethodSymbol methodInfo) {
        foreach (IParameterSymbol parameter in methodInfo.Parameters) {
            builder.AddParameter(CopyParameter(parameter));
        }
    }

    /// <summary>
    /// 拷贝方法参数
    /// </summary>
    /// <param name="parameterInfo"></param>
    /// <returns></returns>
    public static ParameterSpec CopyParameter(IParameterSymbol parameterInfo) {
        var builder = ParameterSpec.NewBuilder(ParseMethodParameterType(parameterInfo), parameterInfo.Name);
        // 处理默认值问题，值类型如果返回null需要使用default代替
        if (parameterInfo.HasExplicitDefaultValue) {
            object defValue = parameterInfo.ExplicitDefaultValue;
            if (defValue == null) {
                builder.DefaultValue("default"); // 统一使用default虽然不优美，但一定正确；适用泛型和值类型
            } else if (defValue is string) {
                builder.DefaultValue("$S", defValue);
            } else {
                builder.DefaultValue("$L", defValue);
            }
        }
        return builder.Build();
    }

    /// <summary>
    /// 解析泛型变量
    /// </summary>
    public static TypeParameterSpec CopyTypeParameter(ITypeParameterSymbol typeParameterSymbol) {
        TypeParameterConstraints attributes = TypeParameterConstraints.None;
        // In Out
        if (typeParameterSymbol.Variance == VarianceKind.In) {
            attributes |= TypeParameterConstraints.VarianceIn;
        } else if (typeParameterSymbol.Variance == VarianceKind.Out) {
            attributes |= TypeParameterConstraints.VarianceOut;
        }
        // Nullable
        if (typeParameterSymbol.HasNotNullConstraint) {
            attributes |= TypeParameterConstraints.NotNullableReferenceType;
        } else if (typeParameterSymbol.IsNullableAnnotated()) {
            attributes |= TypeParameterConstraints.NullableReferenceType;
        }
        // 类型约束
        if (typeParameterSymbol.HasReferenceTypeConstraint) {
            attributes |= TypeParameterConstraints.ReferenceTypeConstraint;
        }
        if (typeParameterSymbol.HasValueTypeConstraint) {
            attributes |= TypeParameterConstraints.ValueTypeConstraint;
        }
        if (typeParameterSymbol.HasConstructorConstraint) {
            attributes |= TypeParameterConstraints.DefaultConstructorConstraint;
        }
        // 上界
        ImmutableArray<ITypeSymbol> constraintTypes = typeParameterSymbol.ConstraintTypes;
        ImmutableArray<NullableAnnotation> nullableAnnotations = typeParameterSymbol.ConstraintNullableAnnotations;
        if (constraintTypes.Length == 0) {
            return TypeParameterSpec.Get(typeParameterSymbol.Name, attributes);
        }
        List<TypeName> bounds = new List<TypeName>(constraintTypes.Length);
        for (int index = 0; index < constraintTypes.Length; index++) {
            ITypeSymbol constraintType = constraintTypes[index];
            // 需要剔除object和ValueType
            if (constraintType.SpecialType == SpecialType.System_Object
                || constraintType.SpecialType == SpecialType.System_ValueType) {
                continue;
            }
            TypeName bound = ParseType(constraintType).AddAttributes(nullableAnnotations[index].ToTypeNameAttributes());
            bounds.Add(bound);
        }
        return TypeParameterSpec.Get(typeParameterSymbol.Name, attributes, bounds);
    }

    #endregion

    #region override-property

    /// <summary>
    /// 重写给定属性
    /// </summary>
    /// <param name="propertySymbol"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static PropertySpec.Builder Overriding(IPropertySymbol propertySymbol) {
        return CopyProperty(propertySymbol, true);
    }

    private static PropertySpec.Builder CopyProperty(IPropertySymbol propertySymbol, bool overriding) {
        PropertySpec.Builder builder;
        if (propertySymbol.IsIndexer) {
            IParameterSymbol parameterSymbol = propertySymbol.Parameters[0];
            TypeName indexType = ParseType(parameterSymbol.Type);
            string indexName = propertySymbol.Name;
            builder = PropertySpec.NewIndexerBuilder(ParseType(propertySymbol.Type), indexType, indexName);
        } else {
            builder = PropertySpec.NewBuilder(ParseType(propertySymbol.Type), propertySymbol.Name);
        }
        builder.hasGetter = propertySymbol.CanRead();
        builder.hasSetter = propertySymbol.CanWrite();

        ParseModifiers(propertySymbol, out var getterModifiers, out var setterModifiers);
        if (overriding) {
            bool fromClass = propertySymbol.ContainingType.TypeKind == TypeKind.Class;
            getterModifiers = Util.AddOverrideModifiers(getterModifiers, fromClass);
            setterModifiers = Util.AddOverrideModifiers(setterModifiers, fromClass);
        }
        // 隐藏setter中包含的getter修饰符
        if (propertySymbol.CanRead() && propertySymbol.CanWrite()) {
            setterModifiers &= ~getterModifiers;
        }
        builder.AddGetterModifiers(getterModifiers);
        builder.AddSetterModifiers(setterModifiers);
        return builder;
    }

    #endregion

    #region parse-modifiers

    /** 解析访问权限 */
    private static Modifiers ParseAccessibility(ISymbol symbol) {
        switch (symbol.DeclaredAccessibility) {
            case Accessibility.Public: return Modifiers.Public;
            case Accessibility.Private: return Modifiers.Private;
            case Accessibility.Protected: return Modifiers.Protected;
            case Accessibility.Internal: return Modifiers.Internal;
            case Accessibility.ProtectedAndInternal: return Modifiers.Protected | Modifiers.Internal;
            default: return Modifiers.None; // 默认解析为空
        }
    }

    /// <summary>
    /// 解析方法的修饰符
    /// </summary>
    public static Modifiers ParseModifiers(IMethodSymbol methodSymbol) {
        Modifiers modifiers = ParseAccessibility(methodSymbol);
        if (methodSymbol.IsStatic) modifiers |= Modifiers.Static;
        if (methodSymbol.IsReadOnly) modifiers |= Modifiers.ReadOnly; // 方法的Readonly是啥???
        if (methodSymbol.IsAsync) modifiers |= Modifiers.Async;
        if (methodSymbol.IsExtern) modifiers |= Modifiers.Extern;
        //
        // 重写相关
        if (methodSymbol.IsSealed) modifiers |= Modifiers.Sealed;
        if (methodSymbol.IsAbstract) modifiers |= Modifiers.Abstract;
        if (methodSymbol.IsVirtual) modifiers |= Modifiers.Virtual;
        if (methodSymbol.IsOverride) modifiers |= Modifiers.Override;
        // 处理unsafe
        bool hasPointerType = methodSymbol.ReturnType.Kind == SymbolKind.PointerType;
        if (!hasPointerType) {
            ImmutableArray<IParameterSymbol> parameterInfos = methodSymbol.Parameters;
            foreach (IParameterSymbol parameterInfo in parameterInfos) {
                hasPointerType |= parameterInfo.Type.Kind == SymbolKind.PointerType;
            }
        }
        if (hasPointerType) {
            modifiers |= Modifiers.Unsafe;
        }
        return modifiers;
    }

    /// <summary>
    /// 解析属性的修饰符
    ///
    /// 注意：属性可能只有setter没有getter
    /// </summary>
    public static void ParseModifiers(IPropertySymbol propertySymbol,
                                      out Modifiers getterModifiers,
                                      out Modifiers setterModifiers) {
        getterModifiers = 0;
        setterModifiers = 0;
        if (propertySymbol.CanRead()) {
            getterModifiers = ParseModifiers(propertySymbol.GetMethod!);
        }
        if (propertySymbol.CanWrite()) {
            setterModifiers = ParseModifiers(propertySymbol.SetMethod!);
        }
    }

    /// <summary>
    /// 解析字段的修饰符
    /// </summary>
    /// <param name="fieldSymbol"></param>
    /// <returns></returns>
    public static Modifiers ParseModifiers(IFieldSymbol fieldSymbol) {
        Modifiers modifiers = ParseAccessibility(fieldSymbol);
        if (fieldSymbol.IsStatic) modifiers |= Modifiers.Static;
        if (fieldSymbol.IsReadOnly) modifiers |= Modifiers.ReadOnly;
        if (fieldSymbol.IsVolatile) modifiers |= Modifiers.Volatile;
        return modifiers;
    }

    /// <summary>
    /// 解析类型的修饰符
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <returns></returns>
    public static Modifiers ParseModifiers(ITypeSymbol typeSymbol) {
        Modifiers modifiers = ParseAccessibility(typeSymbol);
        if (typeSymbol.IsStatic) modifiers |= Modifiers.Static;
        if (typeSymbol.IsReadOnly) modifiers |= Modifiers.ReadOnly;
        // 重写相关--sealed abstract是静态类...
        if (typeSymbol.IsSealed) modifiers |= Modifiers.Sealed;
        if (typeSymbol.IsAbstract) modifiers |= Modifiers.Abstract;
        return modifiers;
    }

    #endregion
}
}