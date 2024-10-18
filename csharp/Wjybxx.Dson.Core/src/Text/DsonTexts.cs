#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Internal;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// Dson文本解析工具类
/// </summary>
public static class DsonTexts
{
    // 类型标签
    public const string LabelInt32 = "i";
    public const string LabelInt64 = "L";
    public const string LabelUInt32 = "ui";
    public const string LabelUInt64 = "uL";
    public const string LabelFloat = "f";
    public const string LabelDouble = "d";
    public const string LabelBool = "b";
    public const string LabelString = "s";
    public const string LabelNull = "N";

    /** 单行纯文本，字符串不需要加引号，不对内容进行转义 */
    public const string LabelStringLine = "sL";

    public const string LabelBinary = "bin";
    public const string LabelPtr = "ptr";
    public const string LabelLitePtr = "lptr";
    public const string LabelDateTime = "dt";
    public const string LabelTimestamp = "ts";

    public const string LabelBeginObject = "{";
    public const string LabelEndObject = "}";
    public const string LabelBeginArray = "[";
    public const string LabelEndArray = "]";
    public const string LabelBeginHeader = "@{";

    // 行首标签
    public const char HeadComment = '#';
    public const char HeadAppend = '-';
    public const char HeadAppendLine = '|';
    public const char HeadSwitchMode = '^';

    /** 所有内建值类型标签 */
    private static readonly IGenericSet<string> builtinStructLabels = new[]
    {
        LabelPtr, LabelLitePtr, LabelDateTime, LabelTimestamp
    }.ToImmutableLinkedHashSet();

    /** 有特殊含义的字符串 */
    private static readonly IGenericSet<string> parseableStrings = new[]
    {
        "true", "false",
        "null", "undefine",
        "NaN", "Infinity", "-Infinity"
    }.ToImmutableLinkedHashSet();

    /**
     * 规定哪些不安全较为容易，规定哪些安全反而不容易
     * 这些字符都是128内，使用bitset很快，还可以避免第三方依赖
     */
    private static readonly BitArray unsafeCharSet = new BitArray(128);

    static DsonTexts() {
        char[] tokenCharArray = "{}[],:/@\"\\".ToCharArray();
        foreach (char c in tokenCharArray) {
            unsafeCharSet.Set(c, true);
        }
    }

    /** 添加全局不安全字符 */
    public static void AddUnsafeChars(char[] unsafeChars) {
        foreach (char c in unsafeChars) {
            unsafeCharSet.Set(c, true);
        }
    }

    /** 是否是缩进字符 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsIndentChar(int c) {
        return c == ' ' || c == '\t';
    }

    /**
     * 是否是不安全的字符，不能省略引号的字符
     * 注意：safeChar也可能组合出不安全的无引号字符串，比如：123, 0.5, null,true,false，因此不能因为每个字符安全，就认为整个字符串安全
     */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUnsafeStringChar(int c) {
        if (c < 128) { // BitArray不能访问索引外的字符
            return unsafeCharSet.Get(c) || char.IsWhiteSpace((char)c);
        }
        return char.IsWhiteSpace((char)c);
    }

    /**
     * 是否可省略字符串的引号
     * 其实并不建议底层默认判断是否可以不加引号，用户可以根据自己的数据决定是否加引号，比如；guid可能就是可以不加引号的
     * 这里的计算是保守的，保守一些不容易出错，因为情况太多，否则既难以保证正确性，性能也差。
     */
    public static bool CanUnquoteString(string value, int maxLengthOfUnquoteString) {
        if (value.Length == 0 || value.Length > maxLengthOfUnquoteString) {
            return false; // 长字符串都加引号，避免不必要的计算
        }
        if (parseableStrings.Contains(value)) {
            return false; // 特殊字符串值
        }
        // 这遍历的不是unicode码点，但不影响
        for (int i = 0; i < value.Length; i++) {
            char c = value[i];
            if (IsUnsafeStringChar(c)) {
                return false;
            }
        }
        // 是否是可解析的数字类型，这个开销大放最后检测
        if (IsParsable(value)) {
            return false;
        }
        return true;
    }

    /** 是否是ASCII码中的可打印字符构成的文本 */
    public static bool IsAsciiText(string text) {
        for (int i = 0, len = text.Length; i < len; i++) {
            char c = text[i];
            if (c < 32 || c > 126) {
                return false;
            }
        }
        return true;
    }

    #region bool/null

    public static bool ParseBool(string str) {
        if (str == "true" || str == "1") return true;
        if (str == "false" || str == "0") return false;
        throw new ArgumentException("invalid bool str: " + str);
    }

    public static void CheckNullString(string str) {
        if ("null" == str) {
            return;
        }
        throw new ArgumentException("invalid null str: " + str);
    }

    #endregion

    #region 数字

    /** 是否是可解析的数字类型 */
    public static bool IsParsable(string str) {
        int length = str.Length;
        if (length == 0 || length > 67 + 16) {
            return false; // 最长也不应该比二进制格式长，16是下划线预留
        }
        return CommonsLang3.IsParsable(str);
    }

    public static int ParseInt32(string rawStr) {
        string str = DeleteUnderline(rawStr);
        if (str.Length == 0) {
            throw new ArgumentException("NumberFormatException:" + rawStr);
        }
        int lookOffset;
        int sign;
        char firstChar = str[0];
        if (firstChar == '+') {
            sign = 1;
            lookOffset = 1;
        } else if (firstChar == '-') {
            sign = -1;
            lookOffset = 1;
        } else {
            sign = 1;
            lookOffset = 0;
        }
        if (lookOffset + 2 < str.Length && str[lookOffset] == '0') {
            char baseChar = str[lookOffset + 1];
            if (baseChar == 'x' || baseChar == 'X') {
                str = str.Substring(lookOffset); // 解析16进制时可带有0x
                return sign * (int)Convert.ToUInt32(str, 16);
            }
            if (baseChar == 'b' || baseChar == 'B') {
                str = str.Substring(lookOffset + 2); // c#解析二进制时不能带有0b...
                return sign * (int)Convert.ToUInt32(str, 2);
            }
        }
        if (lookOffset > 0) {
            return sign * (int)uint.Parse(str.AsSpan(lookOffset)); // 避免切割字符串
        } else {
            return sign * (int)uint.Parse(str);
        }
    }

    public static long ParseInt64(string rawStr) {
        string str = DeleteUnderline(rawStr);
        if (str.Length == 0) {
            throw new ArgumentException("NumberFormatException:" + rawStr);
        }
        int lookOffset;
        int sign;
        char firstChar = str[0];
        if (firstChar == '+') {
            sign = 1;
            lookOffset = 1;
        } else if (firstChar == '-') {
            sign = -1;
            lookOffset = 1;
        } else {
            sign = 1;
            lookOffset = 0;
        }
        if (lookOffset + 2 < str.Length && str[lookOffset] == '0') {
            char baseChar = str[lookOffset + 1];
            if (baseChar == 'x' || baseChar == 'X') {
                str = str.Substring(lookOffset); // 解析16进制时可带有0x
                return sign * (long)Convert.ToUInt64(str, 16);
            }
            if (baseChar == 'b' || baseChar == 'B') {
                str = str.Substring(lookOffset + 2); // c#解析二进制时不能带有0b...
                return sign * (long)Convert.ToUInt64(str, 2);
            }
        }
        if (lookOffset > 0) {
            return sign * (long)ulong.Parse(str.AsSpan(lookOffset)); // 避免切割字符串
        } else {
            return sign * (long)ulong.Parse(str);
        }
    }

    public static float ParseFloat(string rawStr) {
        string str = DeleteUnderline(rawStr);
        if (str.Length == 0) {
            throw new ArgumentException("NumberFormatException:" + rawStr);
        }
        if (rawStr.Equals("Infinity")) {
            return float.PositiveInfinity;
        }
        if (rawStr.Equals("-Infinity")) {
            return float.NegativeInfinity;
        }
        return float.Parse(str);
    }

    public static double ParseDouble(string rawStr) {
        string str = DeleteUnderline(rawStr);
        if (str.Length == 0) {
            throw new ArgumentException("NumberFormatException:" + rawStr);
        }
        if (rawStr.Equals("Infinity")) {
            return double.PositiveInfinity;
        }
        if (rawStr.Equals("-Infinity")) {
            return double.NegativeInfinity;
        }
        return double.Parse(str);
    }

    private static readonly ThreadLocal<StringBuilder> localBuilder = new ThreadLocal<StringBuilder>(() => new StringBuilder(64));

    private static StringBuilder GetCachedBuilder() => localBuilder.Value!.Clear();

    /** 删除数字字符串中的下划线 */
    public static string DeleteUnderline(string str) {
        if (str.IndexOf('_') < 0) { // 避免额外字符串
            return str;
        }
        int length = str.Length;
        if (str[0] == '_' || str[length - 1] == '_') {
            throw new ArgumentException(str); // 首尾不能下划线
        }
        StringBuilder sb = GetCachedBuilder();
        bool hasUnderline = false;
        for (int i = 0; i < length; i++) {
            char c = str[i];
            if (c == '_') {
                if (hasUnderline) {
                    throw new ArgumentException(str); // 不能多个连续下划线
                }
                hasUnderline = true;
            } else {
                sb.Append(c);
                hasUnderline = false;
            }
        }
        return sb.ToString();
    }

    #endregion

    /** 获取类型名对应的Token类型 */
    public static DsonTokenType TokenTypeOfClsName(string label) {
        if (label == null) throw new ArgumentNullException(nameof(label));
        return label switch
        {
            LabelInt32 => DsonTokenType.Int32,
            LabelInt64 => DsonTokenType.Int64,
            LabelFloat => DsonTokenType.Float,
            LabelDouble => DsonTokenType.Double,
            LabelBool => DsonTokenType.Bool,
            LabelString => DsonTokenType.String,
            LabelStringLine => DsonTokenType.String,
            LabelNull => DsonTokenType.Null,
            LabelBinary => DsonTokenType.Binary,
            _ => builtinStructLabels.Contains(label) ? DsonTokenType.BuiltinStruct : DsonTokenType.SimpleHeader
        };
    }

    /** 获取dsonType关联的无位置Token */
    public static DsonToken ClsNameTokenOfType(DsonType dsonType) {
        return dsonType switch
        {
            DsonType.Int32 => new DsonToken(DsonTokenType.Int32, LabelInt32, -1),
            DsonType.Int64 => new DsonToken(DsonTokenType.Int64, LabelInt64, -1),
            DsonType.Float => new DsonToken(DsonTokenType.Float, LabelFloat, -1),
            DsonType.Double => new DsonToken(DsonTokenType.Double, LabelDouble, -1),
            DsonType.Bool => new DsonToken(DsonTokenType.Bool, LabelBool, -1),
            DsonType.String => new DsonToken(DsonTokenType.String, LabelString, -1),
            DsonType.Null => new DsonToken(DsonTokenType.Null, LabelNull, -1),
            DsonType.Binary => new DsonToken(DsonTokenType.Binary, LabelBinary, -1),
            DsonType.Pointer => new DsonToken(DsonTokenType.BuiltinStruct, LabelPtr, -1),
            DsonType.LitePointer => new DsonToken(DsonTokenType.BuiltinStruct, LabelLitePtr, -1),
            DsonType.DateTime => new DsonToken(DsonTokenType.BuiltinStruct, LabelDateTime, -1),
            DsonType.Timestamp => new DsonToken(DsonTokenType.BuiltinStruct, LabelTimestamp, -1),
            _ => throw new ArgumentException(nameof(dsonType))
        };
    }
}
}