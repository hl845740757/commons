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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons
{
/// <summary>
/// Char工具类
/// </summary>
public static class CharUtil
{
    public const char LF = '\n';
    public const char CR = '\r';
    public const char NUL = '\0';

    private static readonly char[] HEX_DIGITS_UPPER = new[]
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F'
    };
    private static readonly char[] HEX_DIGITS_LOWER = new[]
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'a', 'b', 'c', 'd', 'e', 'f'
    };

    #region ASCII码

    /// <summary>
    /// 是否是ASCII码字符
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAscii(char ch) {
        return ch < 128;
    }

    /// <summary>
    /// 是否是ASCII码字母（大写+小写）
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiAlpha(char ch) {
        return IsAsciiAlphaUpper(ch) || IsAsciiAlphaLower(ch);
    }

    /// <summary>
    /// 是否是ASCII码小写字母
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiAlphaLower(char ch) {
        return ch >= 'a' && ch <= 'z';
    }

    /// <summary>
    /// 是否是ASCII码大写字母
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiAlphaUpper(char ch) {
        return ch >= 'A' && ch <= 'Z';
    }

    /// <summary>
    /// 是否是ASCII码数字
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiNumeric(char ch) {
        return ch >= '0' && ch <= '9';
    }

    /// <summary>
    /// 是否是ASCII码字母或数字
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiAlphaOrNumeric(char ch) {
        return IsAsciiAlphaLower(ch) || IsAsciiAlphaUpper(ch) || IsAsciiNumeric(ch);
    }

    /// <summary>
    /// 是否是ASCII码控制字符
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiControl(char ch) {
        return ch < 32 || ch == 127;
    }

    /// <summary>
    /// 是否是ASCII码可打印字符
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiPrintable(char ch) {
        return ch >= 32 && ch < 127;
    }

    #endregion

    /// <summary>
    /// 将字符转换为Unicode编码
    /// </summary>
    /// <param name="ch"></param>
    /// <returns></returns>
    public static string UnicodeEscaped(char ch) {
        return "\\u" +
               HEX_DIGITS_LOWER[ch >> 12 & 15] +
               HEX_DIGITS_LOWER[ch >> 8 & 15] +
               HEX_DIGITS_LOWER[ch >> 4 & 15] +
               HEX_DIGITS_LOWER[ch & 15];
    }

    /// <summary>
    /// 十进制char转number
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public static int ToNumber(char c) {
        // 实际上可通过减48来实现
        return c switch
        {
            '0' => 0,
            '1' => 1,
            '2' => 2,
            '3' => 3,
            '4' => 4,
            '5' => 5,
            '6' => 6,
            '7' => 7,
            '8' => 8,
            '9' => 9,
            _ => throw new ArgumentException("Illegal char " + c)
        };
    }

    /// <summary>
    /// 十进制数字转char
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public static char FromNumber(int number) {
        if (number < 0 || number > 9) {
            throw new ArgumentException("Illegal number " + number);
        }
        return HEX_DIGITS_UPPER[number];
    }

    /// <summary>
    /// 十六进制char转数字
    /// </summary>
    public static int ToHexNumber(char c) {
        return c switch
        {
            '0' => 0,
            '1' => 1,
            '2' => 2,
            '3' => 3,
            '4' => 4,
            '5' => 5,
            '6' => 6,
            '7' => 7,
            '8' => 8,
            '9' => 9,
            //
            'a' => 10,
            'A' => 10,
            //
            'b' => 11,
            'B' => 11,
            //
            'c' => 12,
            'C' => 12,
            //
            'd' => 13,
            'D' => 13,
            //
            'e' => 14,
            'E' => 14,
            //
            'f' => 15,
            'F' => 15,
            _ => throw new ArgumentException("Illegal hex char " + c)
        };
    }

    /// <summary>
    /// 16进制数字转char
    /// </summary>
    public static char FromHexNumber(int number, CaseMode caseMode = CaseMode.UpperCase) {
        if (number < 0 || number > 15) {
            throw new ArgumentException("Illegal hex number " + number);
        }
        return caseMode == CaseMode.UpperCase ? HEX_DIGITS_UPPER[number] : HEX_DIGITS_LOWER[number];
    }
}
}