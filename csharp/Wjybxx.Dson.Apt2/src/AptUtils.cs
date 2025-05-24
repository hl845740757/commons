#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using System.Text;
using Wjybxx.Commons.Poet;

namespace Wjybxx.Dson.Apt2
{
internal static class AptUtils
{
    #region apt-util

    private static readonly ClassName clsName_GeneratedAttribute = ClassName.Get("Wjybxx.Commons.Attributes", "GeneratedAttribute");
    private static readonly ClassName clsName_SourceFileRef = ClassName.Get("Wjybxx.Commons.Attributes", "SourceFileRefAttribute");

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

    #endregion

    #region bean-utils

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
        return FlatInheritAndReverse(type)
            .SelectMany(e => e.GetMembers(BindingFlags.DeclaredOnly
                                          | BindingFlags.Public | BindingFlags.NonPublic
                                          | BindingFlags.Static | BindingFlags.Instance))
            .Where(e => (e.MemberType & memberTypes) != 0)
            .ToList();
    }

    /// <summary>
    /// 查询字段关联的属性(支持非public)
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <param name="allMembers"></param>
    /// <returns></returns>
    public static PropertyInfo? FindProperty(FieldInfo fieldInfo,
                                             List<MemberInfo> allMembers) {
        string propertyName = PropertyNameOfField(fieldInfo.Name);
        return allMembers.Where(e => e.MemberType == MemberTypes.Property)
            .Cast<PropertyInfo>()
            .FirstOrDefault(e => e.Name == propertyName);
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

    public static MethodInfo? GetFirstMethod(this Type type, string name) {
        return type.GetMethod(name);
    }

    #endregion
}
}