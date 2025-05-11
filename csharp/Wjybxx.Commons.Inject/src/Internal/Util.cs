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
    public static MethodInfo? FindOnActiveMethod(Type type) {
        // 当方法被重写时，应当调用子类的方法，因此从子类查起
        List<Type> types = FlatInherit(type, reverse: false);
        foreach (Type curType in types) {
            MethodInfo methodInfo = curType.GetMethods(BindingFlags.DeclaredOnly
                                                       | BindingFlags.Public | BindingFlags.NonPublic
                                                       | BindingFlags.Instance)
                .FirstOrDefault(e => e.IsDefined(typeof(InjectOnCreateAttribute)));
            if (methodInfo != null) {
                return methodInfo;
            }
        }
        return null;
    }

    /// <summary>
    /// 查找构造器注入点
    /// </summary>
    /// <param name="injectionPoints"></param>
    /// <returns></returns>
    public static InjectionPoint? FindConstructor(ImmutableList<InjectionPoint> injectionPoints) {
        if (injectionPoints.Count == 0) {
            return null;
        }
        InjectionPoint injectionPoint = injectionPoints[0];
        return injectionPoint.memberInfo.MemberType == MemberTypes.Constructor
            ? injectionPoint
            : null;
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
            ParameterInfo[] parameterInfos = constructorInfo.GetParameters();
            List<Dependency> dependencies = new List<Dependency>(parameterInfos.Length);
            for (int i = 0; i < parameterInfos.Length; i++) {
                Dependency dependency = ParseDependency(parameterInfos[i], i);
                dependencies.Add(dependency);
            }
            result.Add(new InjectionPoint(constructorInfo, dependencies));
        }
        // 从root开始，自上而下查找属性和字段 -- 不支持静态注入
        List<Type> types = FlatInherit(entryType, reverse: true);
        foreach (Type curType in types) {
            MemberInfo[] memberInfos = curType.GetMembers(BindingFlags.DeclaredOnly
                                                          | BindingFlags.Public | BindingFlags.NonPublic
                                                          | BindingFlags.Instance);
            foreach (MemberInfo memberInfo in memberInfos) {
                if (memberInfo.MemberType != MemberTypes.Field && memberInfo.MemberType != MemberTypes.Property) {
                    continue; // 可能有构造函数
                }
                if (!memberInfo.IsDefined(typeof(InjectAttribute))) {
                    continue; // 不包含注解
                }
                if (memberInfo is PropertyInfo propertyInfo && propertyInfo.SetMethod == null) {
                    continue; // 其实用户没定义set方法时，反射是可以拿到字段进行注入的，但没必要
                }
                Dependency dependency = ParseDependency(memberInfo);
                result.Add(new InjectionPoint(memberInfo, dependency));
            }
        }
        return result.ToImmutableList2();
    }

    /// <summary>
    /// 解析字段和属性的依赖
    /// </summary>
    /// <param name="memberInfo"></param>
    /// <returns></returns>
    public static Dependency ParseDependency(MemberInfo memberInfo) {
        ImmutableList<InjectAttribute> injectAttributes = memberInfo.GetCustomAttributes<InjectAttribute>().ToImmutableList2();
        return new Dependency(GetFieldType(memberInfo), injectAttributes, -1);
    }

    /// <summary>
    /// 解析方法参数的依赖
    /// </summary>
    /// <param name="parameterInfo"></param>
    /// <param name="index"></param>
    /// <returns></returns>
    public static Dependency ParseDependency(ParameterInfo parameterInfo, int index) {
        ImmutableList<InjectAttribute> injectAttributes = parameterInfo.GetCustomAttributes<InjectAttribute>().ToImmutableList2();
        return new Dependency(parameterInfo.ParameterType, injectAttributes, index);
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
        while (true) {
            result.Add(type);
            type = type.BaseType;
            if (type == null || type == typeOfObject) {
                break;
            }
        }
        if (reverse) {
            result.Reverse();
        }
        return result;
    }

    #endregion


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
    /// 是否是List类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsList(Type type) {
        return type.GetInterface(typeof(IList<>).FullName!) != null;
    }

    /// <summary>
    /// 是否是字典类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDictionary(Type type) {
        return type.GetInterface(typeof(IDictionary<,>).FullName!) != null;
    }

    /// <summary>
    /// 获取关联字段的类型
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Type GetFieldType(MemberInfo memberInfo) {
        switch (memberInfo.MemberType) {
            case MemberTypes.Field: {
                FieldInfo fieldInfo = (FieldInfo)memberInfo;
                return fieldInfo.FieldType;
            }
            case MemberTypes.Property: {
                PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
                return propertyInfo.PropertyType;
            }
            default: throw new AssertionError();
        }
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
}
}