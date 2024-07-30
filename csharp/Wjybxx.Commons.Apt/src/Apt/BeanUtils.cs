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
using System.Text;

namespace Wjybxx.Commons.Apt
{
/// <summary>
/// 
/// </summary>
public class BeanUtils
{
    /// <summary>
    /// 是否包含无参构造方法
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static bool ContainsNoArgsConstructor(Type type) {
        return type.GetConstructor(BindingFlags.Instance
                                   | BindingFlags.Public
                                   | BindingFlags.NonPublic,
            binder: null, Array.Empty<Type>(), null) != null;
    }

    /// <summary>
    /// 是否包含给定参数类型的构造方法
    /// </summary>
    /// <param name="type"></param>
    /// <param name="argType"></param>
    /// <returns></returns>
    public static bool ContainsOneArgsConstructor(Type type, Type argType) {
        // TODO 参数如果是未构造泛型是否有问题
        return type.GetConstructor(BindingFlags.Instance
                                   | BindingFlags.Public
                                   | BindingFlags.NonPublic,
            binder: null, new Type[] { argType }, null) != null;
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
    /// 获取成员的类型(字段和属性)
    /// </summary>
    /// <param name="memberInfo"></param>
    /// <returns></returns>
    public static Type GetMemberType(MemberInfo memberInfo) {
        switch (memberInfo) {
            case FieldInfo fieldInfo: {
                return fieldInfo.FieldType;
            }
            case PropertyInfo propertyInfo: {
                return propertyInfo.PropertyType;
            }
            default: {
                throw new AssertionError();
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

    /// <summary>
    /// 获取类的所有字段和方法，包含继承得到的字段和方法和属性。
    /// </summary>
    /// <param name="type"></param>
    /// <param name="memberTypes"></param>
    /// <returns></returns>
    public static List<MemberInfo> GetAllMemberWithInherit(Type type, MemberTypes memberTypes = MemberTypes.Field
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
    /// 获取类定义的所有字段
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static List<FieldInfo> GetAllFieldsWithInherit(Type type) {
        return AptUtils.FlatInheritAndReverse(type)
            .SelectMany(e => e.GetMembers(BindingFlags.DeclaredOnly
                                          | BindingFlags.Public | BindingFlags.NonPublic
                                          | BindingFlags.Static | BindingFlags.Instance))
            .Where(e => e.MemberType == MemberTypes.Field)
            .Select(e => (FieldInfo)e)
            .ToList();
    }

    /// <summary>
    /// 获取类定义的所有方法
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static List<MethodInfo> GetAllMethodsWithInherit(Type type) {
        return AptUtils.FlatInheritAndReverse(type)
            .SelectMany(e => e.GetMembers(BindingFlags.DeclaredOnly
                                          | BindingFlags.Public | BindingFlags.NonPublic
                                          | BindingFlags.Static | BindingFlags.Instance))
            .Where(e => e.MemberType == MemberTypes.Method)
            .Select(e => (MethodInfo)e)
            .ToList();
    }

    #region getter/setter

    /// <summary>
    /// 是否包含public的Getter属性
    /// </summary>
    public static bool ContainsPublicGetter(FieldInfo fieldInfo,
                                            List<MemberInfo> allFieldsAndMethodWithInherit) {
        return FindPublicGetter(fieldInfo, allFieldsAndMethodWithInherit) != null;
    }

    /// <summary>
    /// 是否包含public的setter属性
    /// </summary>
    public static bool ContainsPublicSetter(FieldInfo fieldInfo,
                                            List<MemberInfo> allFieldsAndMethodWithInherit) {
        return FindPublicSetter(fieldInfo, allFieldsAndMethodWithInherit) != null;
    }

    /// <summary>
    /// 查询字段关联的Getter属性
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <param name="allFieldsAndMethodWithInherit"></param>
    /// <returns></returns>
    public static PropertyInfo? FindPublicGetter(FieldInfo fieldInfo,
                                                 List<MemberInfo> allFieldsAndMethodWithInherit) {
        string propertyName = PropertyNameOfField(fieldInfo.Name);
        return allFieldsAndMethodWithInherit.Where(e => e.MemberType == MemberTypes.Property)
            .Select(e => (PropertyInfo)e)
            .FirstOrDefault(e => {
                if (e.Name != propertyName) {
                    return false;
                }
                MethodInfo getMethod = e.GetMethod;
                if (getMethod == null || !getMethod.IsPublic) {
                    return false;
                }
                return true;
            });
    }

    /// <summary>
    /// 查询字段关联的Setter属性
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <param name="allFieldsAndMethodWithInherit"></param>
    /// <returns></returns>
    public static PropertyInfo? FindPublicSetter(FieldInfo fieldInfo,
                                                 List<MemberInfo> allFieldsAndMethodWithInherit) {
        string propertyName = PropertyNameOfField(fieldInfo.Name);
        return allFieldsAndMethodWithInherit.Where(e => e.MemberType == MemberTypes.Property)
            .Select(e => (PropertyInfo)e)
            .FirstOrDefault(e => {
                if (e.Name != propertyName) {
                    return false;
                }
                MethodInfo setMethod = e.SetMethod;
                if (setMethod == null || !setMethod.IsPublic) {
                    return false;
                }
                return true;
            });
    }

    /// <summary>
    /// 查询字段关联的属性(支持非public)
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <param name="allFieldsAndMethodWithInherit"></param>
    /// <returns></returns>
    public static PropertyInfo? FindProperty(FieldInfo fieldInfo,
                                             List<MemberInfo> allFieldsAndMethodWithInherit) {
        string propertyName = PropertyNameOfField(fieldInfo.Name);
        return allFieldsAndMethodWithInherit.Where(e => e.MemberType == MemberTypes.Property)
            .Select(e => (PropertyInfo)e)
            .FirstOrDefault(e => e.Name == propertyName);
    }

    /// <summary>
    /// 是否是自动属性生成的字段
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <returns></returns>
    public static bool IsAutoPropertyField(FieldInfo fieldInfo) {
        // <PropertyName>k__BackingField
        return fieldInfo.IsPrivate && fieldInfo.Name[0] == '<'
                                   && fieldInfo.Name.EndsWith("k__BackingField");
    }

    /// <summary>
    /// 获取字段的属性名
    /// (C#的规则是删除下划线，然后下划线后首个字符大写)
    /// </summary>
    public static string PropertyNameOfField(string fieldName) {
        if (fieldName[0] == '<') {
            // 自动属性字段
            int endIndex = fieldName.IndexOf('>');
            return fieldName.Substring(1, endIndex - 1);
        }
        if (fieldName.Contains('_')) {
            StringBuilder sb = new StringBuilder(fieldName.Length);
            bool nextUpper = true; // 首字符大写
            for (var i = 0; i < fieldName.Length; i++) {
                char c = fieldName[i];
                if (c == '_') {
                    nextUpper = true;
                } else {
                    if (nextUpper) {
                        nextUpper = false;
                        sb.Append(char.ToUpper(c));
                    } else {
                        sb.Append(c);
                    }
                }
            }
            return sb.ToString();
        }
        return FirstCharToUpperCase(fieldName);
    }

    /// <summary>
    /// 首字符大写
    /// </summary>
    public static string FirstCharToUpperCase(string str) {
        if (str.Length == 0) {
            return str;
        }
        char firstChar = str[0];
        if (char.IsLower(firstChar)) { // 可拦截非英文字符
            StringBuilder sb = new StringBuilder(str);
            sb[0] = char.ToUpper(firstChar);
            return sb.ToString();
        }
        return str;
    }

    /// <summary>
    /// 首字符小写
    /// </summary>
    public static string FirstCharToLowerCase(string str) {
        if (str.Length == 0) {
            return str;
        }
        char firstChar = str[0];
        if (char.IsUpper(firstChar)) { // 可拦截非英文字符
            StringBuilder sb = new StringBuilder(str);
            sb[0] = char.ToLower(firstChar);
            return sb.ToString();
        }
        return str;
    }

    #endregion
}
}