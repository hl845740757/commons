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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Wjybxx.Commons.Poet;

namespace Wjybxx.Commons.Apt
{
/// <summary>
///
/// </summary>
public static class BeanUtils
{
    #region constructors

    /// <summary>
    /// 是否包含无参构造方法
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool ContainsNoArgsConstructor(Type type) {
        return GetNoArgsConstructor(type) != null;
    }

    /// <summary>
    /// 是否包含给定参数类型的构造方法
    /// </summary>
    /// <param name="type"></param>
    /// <param name="argType"></param>
    /// <returns></returns>
    public static bool ContainsOneArgsConstructor(Type type, Type argType) {
        return GetOneArgsConstructor(type, argType) != null;
    }

    public static ConstructorInfo? GetNoArgsConstructor(Type type) {
        return type.GetConstructor(BindingFlags.Instance
                                   | BindingFlags.Public
                                   | BindingFlags.NonPublic,
            binder: null, Array.Empty<Type>(), null);
    }

    public static ConstructorInfo? GetOneArgsConstructor(Type type, Type argType) {
        return type.GetConstructor(BindingFlags.Instance
                                   | BindingFlags.Public
                                   | BindingFlags.NonPublic,
            binder: null, new Type[] { argType }, null);
    }

    /// <summary>
    /// 是否包含无参构造方法
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool ContainsNoArgsConstructor(INamedTypeSymbol type) {
        return GetNoArgsConstructor(type) != null;
    }

    /// <summary>
    /// 是否包含给定参数类型的构造方法
    /// </summary>
    /// <param name="type"></param>
    /// <param name="argType"></param>
    /// <returns></returns>
    public static bool ContainsOneArgsConstructor(INamedTypeSymbol type, ITypeSymbol argType) {
        return GetOneArgsConstructor(type, argType) != null;
    }

    public static IMethodSymbol? GetNoArgsConstructor(INamedTypeSymbol type, bool _ = false) {
        foreach (var methodSymbol in type.InstanceConstructors) {
            if (methodSymbol.Parameters.Length == 0) return methodSymbol;
        }
        return null;
    }

    public static IMethodSymbol? GetOneArgsConstructor(INamedTypeSymbol type, ITypeSymbol argType) {
        // TODO 参数如果是未构造泛型是否有问题
        foreach (var methodSymbol in type.InstanceConstructors) {
            if (methodSymbol.Parameters.Length != 1) continue;
            ITypeSymbol parameterType = methodSymbol.Parameters[0].Type;
            if (argType.IsSameType(parameterType)) return methodSymbol;
        }
        return null;
    }

    #endregion

    #region get-members

    /// <summary>
    /// 获取类的所有字段和方法，包含继承得到的字段和方法和属性。
    /// (查询的开销较大，用户应当缓存结果)
    /// </summary>
    /// <param name="type"></param>
    /// <param name="memberTypes"></param>
    /// <returns></returns>
    public static List<MemberInfo> GetAllMembersWithInherit(Type type, MemberTypes memberTypes = MemberTypes.Field
                                                                                                 | MemberTypes.Property
                                                                                                 | MemberTypes.Method) {
        // FlattenHierarchy 不能拉取到超类的private字段
        return AptUtils.FlatInheritAndReverse(type)
            .SelectMany(e => e.GetMembers(BindingFlags.DeclaredOnly
                                          | BindingFlags.Public | BindingFlags.NonPublic
                                          | BindingFlags.Static | BindingFlags.Instance))
            .Where(e => (e.MemberType & memberTypes) != 0)
            .ToList();
    }

    /// <summary>
    /// 获取类的所有public和protected成员
    /// (查询的开销较大，用户应当缓存结果)
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static List<ISymbol> GetAllMembersWithInherit(INamedTypeSymbol type) {
        return GetAllMembersWithInherit(type, new List<SymbolKind>()
        {
            SymbolKind.Field, SymbolKind.Method, SymbolKind.Property
        });
    }

    /// <summary>
    /// 获取类的所有public和protected成员
    /// (查询的开销较大，用户应当缓存结果)
    /// </summary>
    /// <param name="type"></param>
    /// <param name="kinds"></param>
    /// <returns></returns>
    public static List<ISymbol> GetAllMembersWithInherit(INamedTypeSymbol type, List<SymbolKind> kinds) {
        return AptUtils.FlatInheritAndReverse(type)
            .SelectMany(typeSymbol => typeSymbol.GetMembers().Where(e => kinds.Contains(e.Kind)))
            .ToList();
    }

    /// <summary>
    /// 获取第一个指定名称的成员
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static ISymbol? GetFirstMember(this INamedTypeSymbol typeSymbol, string name) {
        foreach (ISymbol member in typeSymbol.GetMembers()) {
            if (member.Name == name) return member;
        }
        return null;
    }

    /// <summary>
    /// 获取第一个指定名称的方法
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IMethodSymbol? GetFirstMethod(this INamedTypeSymbol typeSymbol, string name) {
        foreach (ISymbol member in typeSymbol.GetMembers()) {
            if (member.Kind == SymbolKind.Method && member.Name == name) return (IMethodSymbol?)member;
        }
        return null;
    }

    #endregion

    #region fields-props

    /// <summary>
    /// 查找关联属性时默认忽略大小写，因为有部分场景使用的是小驼峰属性名
    /// 究其原因，还是因为属性的定位存在模糊，有人将其视作字段的代替（应用角度），有人将其视作方法（底层角度）
    /// </summary>
    /// <param name="value1"></param>
    /// <param name="value2"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool Equals(string value1, string value2, bool ignoreCase) {
        return string.Equals(value1, value2, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    /// <summary>
    /// 查询字段关联的属性(支持非public)
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <param name="allMembers"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static PropertyInfo? FindProperty(FieldInfo fieldInfo,
                                             List<MemberInfo> allMembers,
                                             bool ignoreCase = true) {
        string propertyName = PropertyNameOfField(fieldInfo.Name);
        return allMembers.Where(e => e.MemberType == MemberTypes.Property)
            .Cast<PropertyInfo>()
            .FirstOrDefault(e => Equals(e.Name, propertyName, ignoreCase)
                                 && e.PropertyType == fieldInfo.FieldType);
    }

    /// <summary>
    /// 查询字段关联的属性(支持非public)
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <param name="allMembers"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static IPropertySymbol? FindProperty(IFieldSymbol fieldInfo,
                                                List<ISymbol> allMembers,
                                                bool ignoreCase = true) {
        string propertyName = PropertyNameOfField(fieldInfo.Name);
        return allMembers.Where(e => e.Kind == SymbolKind.Property)
            .Cast<IPropertySymbol>()
            .FirstOrDefault(e => Equals(e.Name, propertyName, ignoreCase)
                                 && e.Type.IsSameType(fieldInfo.Type));
    }

    /// <summary>
    /// 通过名字查询属性 -- 应当校验属性的类型
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="allMembers"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static PropertyInfo? FindProperty(string fieldName,
                                             List<MemberInfo> allMembers,
                                             bool ignoreCase = true) {
        string propertyName = PropertyNameOfField(fieldName);
        return allMembers.Where(e => e.MemberType == MemberTypes.Property)
            .Cast<PropertyInfo>()
            .FirstOrDefault(e => Equals(e.Name, propertyName, ignoreCase));
    }

    /// <summary>
    /// 通过名字查询属性 -- 应当校验属性的类型
    /// </summary>
    /// <param name="fieldName"></param>
    /// <param name="allMembers"></param>
    /// <param name="ignoreCase"></param>
    /// <returns></returns>
    public static IPropertySymbol? FindProperty(string fieldName,
                                                List<ISymbol> allMembers,
                                                bool ignoreCase = true) {
        string propertyName = PropertyNameOfField(fieldName);
        return allMembers.Where(e => e.Kind == SymbolKind.Property)
            .Cast<IPropertySymbol>()
            .FirstOrDefault(e => Equals(e.Name, propertyName, ignoreCase));
    }

    /// <summary>
    /// 是否是自动属性生成的字段
    /// </summary>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAutoPropertyField(string fieldName) {
        return Util.IsAutoPropertyField(fieldName);
    }

    /// <summary>
    /// 获取字段的属性名
    /// (C#的规则是删除下划线，然后下划线后首个字符大写)
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string PropertyNameOfField(string fieldName) {
        return Util.PropertyNameOfField(fieldName);
    }

    /// <summary>
    /// 获取关联的字段类型
    /// </summary>
    /// <param name="memberInfo"></param>
    /// <returns></returns>
    public static Type GetFieldType(MemberInfo memberInfo) {
        switch (memberInfo) {
            case FieldInfo fieldInfo: {
                return fieldInfo.FieldType;
            }
            case PropertyInfo propertyInfo: {
                return propertyInfo.PropertyType;
            }
            default: {
                throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// 获取关联的字段类型
    /// </summary>
    /// <param name="memberInfo"></param>
    /// <returns></returns>
    public static ITypeSymbol GetFieldType(ISymbol memberInfo) {
        switch (memberInfo) {
            case IFieldSymbol fieldInfo: {
                return fieldInfo.Type;
            }
            case IPropertySymbol propertyInfo: {
                return propertyInfo.Type;
            }
            default: {
                throw new InvalidOperationException();
            }
        }
    }

    /// <summary>
    /// 判断是否是静态属性
    /// </summary>
    public static bool IsStaticMember(MemberInfo memberInfo) {
        switch (memberInfo) {
            case FieldInfo fieldInfo: {
                return fieldInfo.IsStatic;
            }
            case PropertyInfo propertyInfo: {
                return IsStaticProperty(propertyInfo);
            }
            case MethodInfo methodInfo: {
                return methodInfo.IsStatic;
            }
            case ConstructorInfo constructorInfo: {
                return constructorInfo.IsStatic;
            }
            case EventInfo eventInfo: {
                MethodInfo raiseMethod = eventInfo.RaiseMethod!;
                return raiseMethod.IsStatic;
            }
            default: {
                return true;
            }
        }
    }

    /// <summary>
    /// 判断是否是静态属性
    /// </summary>
    public static bool IsStaticProperty(PropertyInfo propertyInfo) {
        MethodInfo getMethod = propertyInfo.GetMethod;
        if (getMethod != null) {
            return getMethod.IsStatic;
        }
        MethodInfo setMethod = propertyInfo.SetMethod!;
        return setMethod.IsStatic;
    }

    #endregion
}
}