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
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace Wjybxx.Commons.Apt;

internal class Util
{
    #region 断言

    public static T CheckNotNull<T>(T reference, string format, params object[] args) {
        if (reference == null) throw new NullReferenceException(string.Format(format, args));
        return reference;
    }

    public static void CheckArgument(bool condition, string format, params object[] args) {
        if (!condition) throw new ArgumentException(string.Format(format, args));
    }

    public static void CheckState(bool condition, string format, params object[] args) {
        if (!condition) throw new IllegalStateException(string.Format(format, args));
    }

    #endregion

    /** C#的<see cref="ToImmutableList{T}"/>是二叉平衡树... */
    public static IList<T> ToImmutableList<T>(ICollection<T>? collection) {
        if (collection == null) return ImmutableList<T>.Empty;
        return ImmutableList.CreateRange(collection);
    }
    
    /** 求两个Set的并集 */
    public static HashSet<T> Union<T>(ISet<T> a, ISet<T> b) {
        HashSet<T> result = new HashSet<T>(a);
        result.UnionWith(b);
        return result;
    }

    public static void RequireExactlyOneOf(Modifiers modifiers, Modifiers mutuallyExclusive) {
        int count = (int)(modifiers & mutuallyExclusive);
        if (MathCommon.BitCountFast(count) != 1) {
            throw new ArgumentException($"modifiers {modifiers} must contain one of {mutuallyExclusive}");
        }
    }

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
}