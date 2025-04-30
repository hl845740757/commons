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
using System.Collections;
using System.Text;

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 该文件定义内部API
/// </summary>
public static partial class Util
{
    #region 字面量

    /** 将给定char转换为字符串字面量 -- c#其实包含 @ 字面量字符串 */
    internal static string CharacterLiteralWithoutSingleQuotes(char c) {
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
    internal static string StringLiteralWithDoubleQuotes(string value, string indent) {
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
    internal static string CharToUnicodeString(char c) {
        int v = 0x10000 + (int)c;
        return "\\u" + v.ToString("X").Substring2(1, 5);
    }

    /// <summary>
    /// 将Ascii码字符串转为BitArray
    /// </summary>
    /// <param name="charArray"></param>
    /// <returns></returns>
    internal static BitArray CharToBitArray(string charArray) {
        BitArray r = new BitArray(128);
        for (var i = 0; i < charArray.Length; i++) {
            r.Set(charArray[i], true);
        }
        return r;
    }

    #endregion

    #region 常量缓存

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
    internal static string ArrayRankSymbol(int rank) {
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
    internal static string PointerRankSymbol(int rank) {
        if (rank < 1 || rank > pointerRankSymbols.Length) {
            throw new ArgumentException("rank: " + rank);
        }
        return pointerRankSymbols[rank - 1];
    }

    #endregion
}
}