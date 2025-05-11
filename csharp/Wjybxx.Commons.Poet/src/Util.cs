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
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 该文件主要包含从commons-core拷贝来的方法，方便代码生成器库使用
///
/// 这里的扩展方法不能是public的，否则会和commons库的扩展方法冲突。
/// </summary>
public static partial class Util
{
    #region 断言

    public static string CheckNotBlank(string value, string msg) {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(msg);
        return value;
    }

    public static T CheckNotNull<T>(T reference, string format, params object[] args) {
        if (reference == null) throw new NullReferenceException(string.Format(format, args));
        return reference;
    }

    public static void CheckArgument(bool condition, string format, params object[] args) {
        if (!condition) throw new ArgumentException(string.Format(format, args));
    }

    public static void CheckState(bool condition, string format, params object[] args) {
        if (!condition) throw new InvalidOperationException(string.Format(format, args));
    }

    #endregion

    #region string

    /// <summary>
    /// 通过索引区间获取子字符串。
    /// C#的字符串接口和Java差异较大，这里提供一个适配方法。
    /// </summary>
    /// <param name="value"></param>
    /// <param name="start">开始索引 inclusive</param>
    /// <param name="end">结束索引 exclusive</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string Substring2(this string value, int start, int end) {
        return value.Substring(start, end - start);
    }

    /// <summary>
    /// 该接口用于统一API -- 避免一会用原生API，一会儿用自定义API
    /// </summary>
    /// <param name="value"></param>
    /// <param name="start">开始索引 inclusive</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static string Substring2(this string value, int start) {
        return value.Substring(start);
    }

    /// <summary>
    /// 获取字符串的长度，如果字符为null，则返回0
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(string? value) {
        return value?.Length ?? 0;
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

    /// <summary>
    /// 字符串是否包含空白字符
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool ContainsWhitespace(string str) {
        int strLen = Length(str);
        if (strLen == 0) {
            return false;
        }
        for (int i = 0; i < strLen; i++) {
            if (char.IsWhiteSpace(str[i])) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 索引首个空白字符
    /// </summary>
    public static int IndexOfWhitespace(string cs, int startIndex = 0) {
        if (startIndex < 0) {
            throw new ArgumentException("startIndex " + startIndex);
        }
        int length = Length(cs);
        if (length == 0) {
            return -1;
        }
        for (int i = startIndex; i < length; i++) {
            if (char.IsWhiteSpace(cs[i])) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 反向索引首个空白字符
    /// </summary>
    public static int LastIndexOfWhitespace(string cs, int startIndex = -1) {
        if (startIndex < -1) {
            throw new ArgumentException("startIndex " + startIndex);
        }
        int length = Length(cs);
        if (length == 0) {
            return -1;
        }
        if (startIndex == -1 || startIndex >= length) {
            startIndex = length - 1;
        }
        for (int i = startIndex; i >= 0; i--) {
            if (char.IsWhiteSpace(cs[i])) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 删除字符串中的空白字符
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string DeleteWhitespace(string str) {
        int startIndex = IndexOfWhitespace(str);
        if (startIndex < 0) {
            return str;
        }
        int len = str.Length;
        StringBuilder sb = new StringBuilder(len);
        sb.Append(str, 0, startIndex);
        //
        for (int idx = startIndex + 1; idx < len; idx++) {
            char c = str[idx];
            if (char.IsWhiteSpace(c)) {
                continue;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>
    /// 获取字符串的所有行，仅支持 \n 和 \r\n
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static List<string> Lines(string str) {
        List<string> result = new List<string>();
        using (StringReader reader = new StringReader(str)) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                result.Add(line);
            }
        }
        return result;
    }

    #endregion

    #region array

    /// <summary>
    /// 拷贝数组
    /// </summary>
    /// <param name="src">原始数组</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] CopyOf<T>(T[] src) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        if (src.Length == 0) {
            return src;
        }
        T[] result = new T[src.Length];
        Array.Copy(src, result, src.Length);
        return result;
    }

    /// <summary>
    /// 拷贝数组
    /// </summary>
    /// <param name="src">原始数组</param>
    /// <param name="offset">拷贝的起始偏移量</param>
    /// <param name="len">要拷贝的长度；可大于或小于原始数组长度</param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] CopyOf<T>(T[] src, int offset, int len) {
        if (src == null) throw new ArgumentNullException(nameof(src));
        T[] result = new T[len];
        int copyLen = Math.Min(src.Length - offset, len);
        Array.Copy(src, offset, result, 0, copyLen);
        return result;
    }

    /// <summary>
    /// 数组转List
    /// </summary>
    public static List<T> ToList<T>(T[] array) {
        List<T> list = new(array.Length);
        foreach (T e in array) {
            list.Add(e);
        }
        return list;
    }

    #endregion

    #region colletion

    /// <summary>
    /// 获取集合的数量，如果集合为null，则返回0
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Count<T>(ICollection<T>? self) => self == null ? 0 : self.Count;

    internal static void AddAll<T>(this ICollection<T> self, IEnumerable<T> other) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (other == null) throw new ArgumentNullException(nameof(other));
        foreach (T e in other) {
            self.Add(e);
        }
    }

    internal static void TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue value) where TKey : notnull {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (!self.ContainsKey(key)) {
            self[key] = value;
        }
    }

    internal static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> self, IEnumerable<KeyValuePair<TKey, TValue>> pairs) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (pairs == null) throw new ArgumentNullException(nameof(pairs));
        foreach (KeyValuePair<TKey, TValue> pair in pairs) {
            self[pair.Key] = pair.Value;
        }
    }

    internal static bool TryPeek<T>(this Stack<T> stack, out T r) {
        if (stack.Count > 0) {
            r = stack.Peek();
            return true;
        }
        r = default;
        return false;
    }

    public static string ToString<T>(IEnumerable<T>? collection) {
        if (collection == null) return "null";
        StringBuilder sb = new StringBuilder(64);
        sb.Append('[');
        bool first = true;
        foreach (T value in collection) {
            if (first) {
                first = false;
            } else {
                sb.Append(',');
            }
            if (value == null) {
                sb.Append("null");
            } else {
                sb.Append(value.ToString());
            }
        }
        sb.Append(']');
        return sb.ToString();
    }

    #endregion

    #region list

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IList<T> EmptyList<T>() {
        return ImmutableList<T>.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IList<T> ToImmutableList<T>(IEnumerable<T>? collection) {
        if (collection == null) return ImmutableList<T>.Empty;
        if (collection is ImmutableList<T> immutableList) return immutableList;
        return ImmutableList<T>.CreateRange(collection);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfCustom<T>(IList<T> list, Predicate<T> filter) {
        return IndexOfCustom(list, filter, 0, list.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfCustom<T>(IList<T> list, Predicate<T> filter) {
        return LastIndexOfCustom(list, filter, 0, list.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int IndexOfCustom<T>(IList<T> list, Predicate<T> filter, int start, int end) {
        for (int idx = start; idx < end; idx++) {
            if (filter(list[idx])) {
                return idx;
            }
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfCustom<T>(IList<T> list, Predicate<T> filter, int start, int end) {
        for (int i = end - 1; i >= start; i--) {
            if (filter(list[i])) {
                return i;
            }
        }
        return -1;
    }

    public static bool SequenceEqual<T>(IList<T>? lhs, IList<T>? rhs) where T : class {
        if (ReferenceEquals(lhs, rhs)) return true;
        if (lhs == null || rhs == null) return false;
        int count = lhs.Count;
        if (count != rhs.Count) return false;
        for (int idx = 0; idx < count; idx++) {
            if (!Equals(lhs[idx], rhs[idx])) return false;
        }
        return true;
    }

    public static int HashCode<T>(IList<T?>? list) where T : class {
        if (list == null) {
            return 0;
        }
        int r = 1;
        for (int i = 0; i < list.Count; i++) {
            T e = list[i];
            r = r * 31 + (e == null ? 0 : e.GetHashCode());
        }
        return r;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<T> Concat<T>(IList<T>? lhs, IList<T>? rhs) {
        List<T> result = new List<T>(Count(lhs) + Count(rhs));
        if (lhs != null && lhs.Count > 0) {
            result.AddRange(lhs);
        }
        if (rhs != null && rhs.Count > 0) {
            result.AddRange(rhs);
        }
        return result;
    }

    #endregion

    #region reflect

    /// <summary>
    /// 是否是变长参数方法
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsVarArgsMethod(MethodInfo methodInfo) {
        ParameterInfo[] parameterInfos = methodInfo.GetParameters();
        if (parameterInfos.Length == 0) return false;
        return parameterInfos[parameterInfos.Length - 1].IsDefined(typeof(ParamArrayAttribute));
    }

    /// <summary>
    /// 是否是扩展方法
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsExtensionMethod(MethodInfo methodInfo) {
        return methodInfo.IsDefined(typeof(ExtensionAttribute));
    }

    /// <summary>
    /// 是否是异步方法
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsyncMethod(MethodInfo methodInfo) {
        return methodInfo.IsDefined(typeof(AsyncStateMachineAttribute));
    }

    /// <summary>
    /// 是否是外部方法--不准确
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsExternMethod(MethodInfo methodInfo) {
        return methodInfo.IsStatic
               && methodInfo.IsDefined(typeof(DllImportAttribute), inherit: false);
    }

    /// <summary>
    /// 是否是volatile字段
    /// 
    /// (c#把volatile也搞成属性，有点难崩...性能真的好吗，Flags不更高效?)
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsVolatileField(FieldInfo fieldInfo) {
        // RequiredCustomModifiers: volatile、readonly
        foreach (Type customModifier in fieldInfo.GetRequiredCustomModifiers()) {
            if (customModifier == typeof(IsVolatile)) return true;
        }
        return false;
    }

    /// <summary>
    /// 是否是普通类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNamedType(Type type) {
        return type.IsInterface || type.IsClass || type.IsValueType || type.IsEnum;
    }

    /// <summary>
    /// 获取类型的简单名 -- 不包含泛型参数个数信息
    /// </summary>
    /// <param name="namedType"></param>
    /// <returns></returns>
    public static string GetSimpleName(Type namedType) {
        string name = namedType.Name;
        int index = name.LastIndexOf('`');
        return index > 0 ? name.Substring2(0, index) : name;
    }

    /// <summary>
    /// 获取普通类型的元数据名
    /// <see cref="ClassName.ReflectionName()"/>
    /// </summary>
    /// <param name="namedType">普通类型</param>
    /// <returns></returns>
    public static string GetFullMetadataName(Type namedType) {
        string typeString = namedType.ToString();
        int index = typeString.LastIndexOf('[');
        if (index > 0 && typeString[index - 1] == '`') {
            return typeString.Substring2(0, index);
        }
        return typeString;
    }

    /// <summary>
    /// 是否是索引器属性
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <returns></returns>
    public static bool IsIndexerProperty(PropertyInfo propertyInfo) {
        if (!propertyInfo.Name.Equals("Item")) return false;
        if (propertyInfo.CanRead) {
            MethodInfo getMethod = propertyInfo.GetGetMethod(true)!;
            return getMethod.GetParameters().Length > 0;
        }
        if (propertyInfo.CanWrite) {
            MethodInfo setMethod = propertyInfo.GetSetMethod(true)!;
            return setMethod.GetParameters().Length > 1;
        }
        return false;
    }

    /// <summary>
    /// 是否是自动属性关联的字段
    /// </summary>
    /// <param name="fieldName"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAutoPropertyField(string fieldName) {
        // <PropertyName>k__BackingField
        return fieldName[0] == '<' && fieldName.EndsWith("k__BackingField");
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
        if (fieldName.IndexOf('_') >= 0) {
            StringBuilder sb = new StringBuilder(fieldName.Length);
            bool nextUpper = true; // 首字符大写
            foreach (char c in fieldName) {
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
    /// 解析方法的修饰符
    /// </summary>
    public static Modifiers ParseModifiers(MethodInfo methodInfo) {
        Modifiers modifiers = Modifiers.None;
        if (methodInfo.IsPublic) modifiers |= Modifiers.Public;
        if (methodInfo.IsAssembly) modifiers |= Modifiers.Internal;
        if (methodInfo.IsPrivate) modifiers |= Modifiers.Private;
        if (methodInfo.IsFamily) modifiers |= Modifiers.Protected;
        // 重写相关
        if (methodInfo.IsFinal) modifiers |= Modifiers.Sealed;
        if (methodInfo.IsAbstract) modifiers |= Modifiers.Abstract;
        if (methodInfo.IsVirtual) modifiers |= Modifiers.Virtual;
        if (methodInfo != methodInfo.GetBaseDefinition()) {
            modifiers |= Modifiers.Override;
        }
        //
        if (methodInfo.IsStatic) modifiers |= Modifiers.Static;
        if (IsAsyncMethod(methodInfo)) modifiers |= Modifiers.Async;
        // 解析unsafe
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
        return modifiers;
    }

    /// <summary>
    /// 解析属性的Modifiers
    ///
    /// 注意：属性可能没有Getter或Setter
    /// </summary>
    public static void ParseModifiers(PropertyInfo propertyInfo,
                                      out Modifiers getterModifiers,
                                      out Modifiers setterModifiers) {
        getterModifiers = Modifiers.None;
        setterModifiers = Modifiers.None;
        if (propertyInfo.CanRead) {
            MethodInfo getMethod = propertyInfo.GetGetMethod(true)!;
            getterModifiers = ParseModifiers(getMethod);
        }
        if (propertyInfo.CanWrite) {
            MethodInfo setMethod = propertyInfo.GetSetMethod(true)!;
            setterModifiers = ParseModifiers(setMethod);
        }
    }

    /// <summary>
    /// 解析字段的修饰符
    /// </summary>
    /// <param name="fieldInfo"></param>
    /// <returns></returns>
    public static Modifiers ParseModifiers(FieldInfo fieldInfo) {
        Modifiers modifiers = Modifiers.None;
        if (fieldInfo.IsStatic) modifiers |= Modifiers.Static;
        if (fieldInfo.IsPublic) modifiers |= Modifiers.Public;
        if (fieldInfo.IsPrivate) modifiers |= Modifiers.Private;
        if (fieldInfo.IsFamily) modifiers |= Modifiers.Protected;
        if (fieldInfo.IsAssembly) modifiers |= Modifiers.Internal;

        if (fieldInfo.IsInitOnly) modifiers |= Modifiers.ReadOnly;
        if (IsVolatileField(fieldInfo)) modifiers |= Modifiers.Volatile;
        return modifiers;
    }

    /// <summary>
    /// 解析类型的修饰符
    /// (注意：这里会返回static，但static不应该打印)
    /// </summary>
    /// <param name="typeInfo"></param>
    /// <returns></returns>
    public static Modifiers ParseModifiers(Type typeInfo) {
        Modifiers modifiers = Modifiers.None;
        if (!typeInfo.IsNested) {
            // 外部类未声明为public则为internal
            modifiers = typeInfo.IsPublic ? Modifiers.Public : Modifiers.Internal;
        } else {
            // 嵌套类修饰符
            if (typeInfo.IsNestedPublic) modifiers |= Modifiers.Public;
            if (typeInfo.IsNestedPrivate) modifiers |= Modifiers.Private;
            if (typeInfo.IsNestedFamily) modifiers |= Modifiers.Protected;
            if (typeInfo.IsNestedAssembly) modifiers |= Modifiers.Internal;
        }
        if (typeInfo.IsSealed) modifiers |= Modifiers.Sealed;
        if (typeInfo.IsAbstract) modifiers |= Modifiers.Abstract;
        if (typeInfo.IsSealed && typeInfo.IsAbstract) {
            modifiers |= Modifiers.Static; // 静态类是密封抽象类...
        }
        return modifiers;
    }

    /// <summary>
    /// 修正重写方法或属性时的修饰符
    /// </summary>
    /// <param name="modifiers">当前修饰符</param>
    /// <param name="fromClass">重新的元素是否来自于class</param>
    /// <returns></returns>
    public static Modifiers AddOverrideModifiers(Modifiers modifiers, bool fromClass) {
        if (fromClass) {
            modifiers |= Modifiers.Override; // 重写class的成员时追加Override
        }
        modifiers &= ~Modifiers.Abstract;
        modifiers &= ~Modifiers.Virtual;
        return modifiers;
    }

    #endregion
}
}