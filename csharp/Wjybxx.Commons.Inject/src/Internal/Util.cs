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
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Inject.Attributes;

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 工具类
/// </summary>
internal static class Util
{
    #region inject-point

    /// <summary>
    /// 查找对象创建钩子方法
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static MethodInfo? FindOnCreateMethod(Type type) {
        return FindAnnotatedMethod(type, typeof(InjectOnCreateAttribute));
    }

    /// <summary>
    /// 查找对象销毁钩子方法
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static MethodInfo? FindOnDisposeMethod(Type type) {
        return FindAnnotatedMethod(type, typeof(InjectOnDisposeAttribute));
    }

    /// <summary>
    /// 查找具有特定注解的钩子方法
    /// </summary>
    /// <param name="type"></param>
    /// <param name="attributeType"></param>
    /// <returns></returns>
    private static MethodInfo? FindAnnotatedMethod(Type type, Type attributeType) {
        const BindingFlags bindFlags = BindingFlags.DeclaredOnly
                                       | BindingFlags.Public | BindingFlags.NonPublic
                                       | BindingFlags.Instance;
        // 子类方法优先
        List<Type> types = FlatInherit(type, reverse: false);
        foreach (Type curType in types) {
            MethodInfo methodInfo = curType.GetMethods(bindFlags)
                .FirstOrDefault(e => e.IsDefined(attributeType));
            if (methodInfo != null) {
                return methodInfo;
            }
        }
        return null;
    }

    /// <summary>
    /// 如果有构造函数，则构造函数为第一个。
    /// 字段和属性按照声明顺序返回。
    /// </summary>
    /// <param name="entryType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static ImmutableList<InjectionPoint> GetInjectPoints(Type entryType) {
        if (entryType.IsGenericTypeDefinition) {
            throw new ArgumentException($"{entryType} is not constructedGenericType");
        }
        List<InjectionPoint> result = new List<InjectionPoint>();
        // 构造函数只查询当前类，且只查询第一个
        ConstructorInfo? constructorInfo = entryType
            .GetConstructors()
            .FirstOrDefault(e => e.IsDefined(typeof(InjectAttribute)));
        if (constructorInfo != null) {
            result.Add(CreateInjectPoint(constructorInfo));
        }
        // 字段、属性、方法，从基类开始注入
        List<Type> types = FlatInherit(entryType, reverse: true);
        foreach (Type curType in types) {
            MemberInfo[] memberInfos = curType.GetMembers(BindingFlags.DeclaredOnly
                                                          | BindingFlags.Public | BindingFlags.NonPublic
                                                          | BindingFlags.Instance);
            foreach (MemberInfo memberInfo in memberInfos) {
                if (!memberInfo.IsDefined(typeof(InjectAttribute))
                    || memberInfo.MemberType == MemberTypes.Constructor) {
                    continue; // 可能有构造函数
                }
                if (memberInfo is PropertyInfo propertyInfo && propertyInfo.SetMethod == null) {
                    continue; // 忽略没有Set方法的属性 -- 可以在编译时加警告...
                }
                result.Add(CreateInjectPoint(memberInfo));
            }
        }
        return result.ToImmutableList2();
    }

    private static InjectionPoint CreateInjectPoint(MemberInfo memberInfo) {
        switch (memberInfo.MemberType) {
            case MemberTypes.Field: {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                return new InjectionPoint(memberInfo, ParseDependency(memberInfo, fieldInfo.FieldType));
            }
            case MemberTypes.Property: {
                PropertyInfo fieldInfo = (PropertyInfo)memberInfo;
                return new InjectionPoint(memberInfo, ParseDependency(memberInfo, fieldInfo.PropertyType));
            }
            case MemberTypes.Constructor:
            case MemberTypes.Method: {
                MethodBase methodBase = (MethodBase)memberInfo;
                return new InjectionPoint(memberInfo, ParseDependencies(methodBase.GetParameters()));
            }
            default: throw new InvalidOperationException();
        }
    }

    /// <summary>
    /// 解析字段和属性的依赖
    /// </summary>
    private static Dependency ParseDependency(MemberInfo memberInfo, Type fieldType) {
        ImmutableList<InjectAttribute> injectAttributes = memberInfo.GetCustomAttributes<InjectAttribute>().ToImmutableList2();
        return new Dependency(fieldType, GetServiceType(fieldType), injectAttributes, -1);
    }

    /// <summary>
    /// 解析方法的依赖
    /// </summary>·
    private static List<Dependency> ParseDependencies(ParameterInfo[] parameterInfos) {
        List<Dependency> dependencies = new List<Dependency>(parameterInfos.Length);
        for (int index = 0; index < parameterInfos.Length; index++) {
            ParameterInfo parameterInfo = parameterInfos[index];
            Type fieldType = parameterInfo.ParameterType;

            ImmutableList<InjectAttribute> injectAttributes = parameterInfo.GetCustomAttributes<InjectAttribute>().ToImmutableList2();
            dependencies.Add(new Dependency(fieldType, GetServiceType(fieldType), injectAttributes, index));
        }
        return dependencies;
    }

    /// <summary>
    /// 将继承层次结构打平，并反转为超类在前 -- 结果不包含object，除非参数就是object
    /// </summary>
    /// <param name="type">要处理的类型</param>
    /// <param name="reverse">是否反转结果</param>
    /// <returns></returns>
    public static List<Type> FlatInherit(Type type, bool reverse) {
        if (type == null) throw new ArgumentNullException(nameof(type));
        Type typeOfObject = typeof(object);

        List<Type> result = new List<Type>();
        do {
            result.Add(type);
            type = type.BaseType;
        } while (type != null && type != typeOfObject);

        if (reverse) {
            result.Reverse();
        }
        return result;
    }

    #endregion

    /// <summary>
    /// 是否是List类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsList(Type type) {
        if (type.IsGenericType) {
            type = type.GetGenericTypeDefinition();
        }
        return type == typeof(IList<>) ||
               type.GetInterface(typeof(IList<>).FullName!) != null;
    }

    /// <summary>
    /// 是否是字典类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDictionary(Type type) {
        if (type.IsGenericType) {
            type = type.GetGenericTypeDefinition();
        }
        return type == typeof(IDictionary<,>)
               || type.GetInterface(typeof(IDictionary<,>).FullName!) != null;
    }

    /// <summary>
    /// 获取List和字典的Add方法
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static MethodInfo GetAddMethod(Type type) {
        if (type.IsGenericTypeDefinition) {
            throw new ArgumentException($"{type} is not constructedGenericType");
        }
        MethodInfo methodInfo = type.GetMethod("Add");
        if (methodInfo == null) {
            throw new ArgumentException($"{type} does not declare an Add method");
        }
        return methodInfo;
    }

    /// <summary>
    /// 根据字段的声明类型，获取字段依赖的服务类型
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type GetServiceType(Type fieldType) {
        if (fieldType.IsGenericTypeDefinition) {
            throw new ArgumentException(fieldType.ToString());
        }
        if (IsList(fieldType)) {
            return fieldType.GetGenericArguments()[0];
        }
        if (IsDictionary(fieldType)) {
            return fieldType.GetGenericArguments()[1];
        }
        return fieldType;
    }

    /// <summary>
    /// 转换为不可变List
    /// </summary>
    /// <param name="list"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static ImmutableList<T> ToImmutableList<T>(IList<T>? list) {
        return list == null ? ImmutableList<T>.Empty : ImmutableList<T>.CreateRange(list);
    }
}
}