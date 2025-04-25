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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Wjybxx.Commons.Poet
{
internal static class Util
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

    #region 集合

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

    /** 合并List */
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

    #region 字面量

    /** 将给定char转换为字符串字面量 -- c#其实包含 @ 字面量字符串 */
    public static string CharacterLiteralWithoutSingleQuotes(char c) {
        switch (c) {
            case '\b': return "\\b"; /* \u0008: backspace (BS) */
            case '\t': return "\\t"; /* \u0009: horizontal tab (HT) */
            case '\n': return "\\n"; /* \u000a: linefeed (LF) */
            case '\f': return "\\f"; /* \u000c: form feed (FF) */
            case '\r': return "\\r"; /* \u000d: carriage return (CR) */
            case '\"': return "\""; /* \u0022: double quote (") */
            case '\'': return "\\'"; /* \u0027: single quote (') */
            case '\\': return "\\\\"; /* \u005c: backslash (\) */
            default:
                return char.IsControl(c) ? CharToUnicodeString(c) : char.ToString(c);
        }
    }

    /** Returns the string literal representing {@code value}, including wrapping double quotes. */
    public static string StringLiteralWithDoubleQuotes(string value, string indent) {
        StringBuilder result = new StringBuilder(value.Length + 2);
        result.Append('"');
        for (int i = 0; i < value.Length; i++) {
            char c = value[i];
            // trivial case: single quote must not be escaped
            if (c == '\'') {
                result.Append("'");
                continue;
            }
            // trivial case: double quotes must be escaped
            if (c == '\"') {
                result.Append("\\\"");
                continue;
            }
            // default case: just let character literal do its work
            result.Append(CharacterLiteralWithoutSingleQuotes(c));
            // need to append indent after linefeed?
            if (c == '\n' && i + 1 < value.Length) {
                result.Append("\"\n").Append(indent).Append(indent).Append("+ \"");
            }
        }
        result.Append('"');
        return result.ToString();
    }

    /// <summary>
    /// 将char转为unicode转义字符
    /// </summary>
    public static string CharToUnicodeString(char c) {
        int v = 0x10000 + (int)c;
        return "\\u" + v.ToString("X").Substring2(1, 5);
    }

    #endregion

    /// <summary>
    /// 将Ascii码字符串转为BitArray
    /// </summary>
    /// <param name="charArray"></param>
    /// <returns></returns>
    public static BitArray CharToBitArray(string charArray) {
        BitArray r = new BitArray(128);
        for (var i = 0; i < charArray.Length; i++) {
            r.Set(charArray[i], true);
        }
        return r;
    }

    /** 数组符号 -- 最大支持9阶，我都没见过3阶以上的数组... */
    private static readonly string[] arrayRankSymbols =
    {
        "[]",
        "[][]",
        "[][][]",
        "[][][][]",
        "[][][][][]",
        "[][][][][][]",
        "[][][][][][][]",
        "[][][][][][][][]",
        "[][][][][][][][][]"
    };

    /// <summary>
    /// 获取数组阶数对应的符号
    /// </summary>
    /// <param name="rank"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string ArrayRankSymbol(int rank) {
        if (rank < 1 || rank > arrayRankSymbols.Length) {
            throw new ArgumentException("rank: " + rank);
        }
        return arrayRankSymbols[rank - 1];
    }

    /** 指针符号 -- 最大支持6阶 */
    private static readonly string[] pointerRankSymbols =
    {
        "*",
        "**",
        "***",
        "****",
        "*****",
        "******",
    };

    /// <summary>
    /// 获取数组阶数对应的符号
    /// </summary>
    /// <param name="rank"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string PointerRankSymbol(int rank) {
        if (rank < 1 || rank > pointerRankSymbols.Length) {
            throw new ArgumentException("rank: " + rank);
        }
        return pointerRankSymbols[rank - 1];
    }

    #region copy-comons-core

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
    public static string Substring2(this string value, int start, int end) {
        return value.Substring(start, end - start);
    }

    /// <summary>
    /// 该接口用于统一API -- 避免一会用原生API，一会儿用自定义API
    /// </summary>
    /// <param name="value"></param>
    /// <param name="start">开始索引 inclusive</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Substring2(this string value, int start) {
        return value.Substring(start);
    }

    /// <summary>
    /// 获取字符串的所有行，仅支持 \n 和 \r\n
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static List<string> Lines(this string str) {
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

    #endregion

    #region colletion

    /// <summary>
    /// 获取集合的数量，如果集合为null，则返回0
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Count<T>(ICollection<T>? self) => self == null ? 0 : self.Count;

    public static void AddAll<T>(this ICollection<T> self, IEnumerable<T> other) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (other == null) throw new ArgumentNullException(nameof(other));
        foreach (T e in other) {
            self.Add(e);
        }
    }

    public static void TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key, TValue value) where TKey : notnull {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (!self.ContainsKey(key)) {
            self[key] = value;
        }
    }

    public static void PutAll<TKey, TValue>(this IDictionary<TKey, TValue> self, IEnumerable<KeyValuePair<TKey, TValue>> pairs) {
        if (self == null) throw new ArgumentNullException(nameof(self));
        if (pairs == null) throw new ArgumentNullException(nameof(pairs));
        foreach (KeyValuePair<TKey, TValue> pair in pairs) {
            self[pair.Key] = pair.Value;
        }
    }

    public static bool TryPeek<T>(this Stack<T> stack, out T r) {
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
    public static int IndexOfCustom<T>(IList<T> list, Predicate<T> filter) {
        return IndexOfCustom(list, filter, 0, list.Count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int LastIndexOfCustom<T>(IList<T> list, Predicate<T> filter) {
        return LastIndexOfCustom(list, filter, 0, list.Count);
    }

    public static int IndexOfCustom<T>(IList<T> list, Predicate<T> filter, int start, int end) {
        for (int idx = start; idx < end; idx++) {
            if (filter(list[idx])) {
                return idx;
            }
        }
        return -1;
    }

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

    #endregion

    #endregion
}
}