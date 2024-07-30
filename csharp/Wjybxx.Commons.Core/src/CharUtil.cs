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

namespace Wjybxx.Commons
{

/// <summary>
/// Char工具类
/// </summary>
public static class CharUtil
{
    private static readonly char[] HEX_DIGITS_UPPER = new[]
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F'
    };
    private static readonly char[] HEX_DIGITS_LOWER = new[]
    {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F'
    };

    /// <summary>
    /// 十进制char转number
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public static int DecimalCharToNumber(char c, int? index = null) {
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
            _ => throw new ArgumentException("Illegal hexadecimal character " + c + (index == null ? "" : " at index " + index))
        };
    }

    /// <summary>
    /// 十六进制char转数字
    /// </summary>
    public static int HexCharToNumber(char c, int? index = null) {
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
            _ => throw new ArgumentException("Illegal hexadecimal character " + c + (index == null ? "" : " at index " + index))
        };
    }

    /// <summary>
    /// 十进制数字转char
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public static char DecimalNumberToChar(int number, int? index = null) {
        if (number < 0 || number > 9) {
            throw new ArgumentException("Illegal hexadecimal number " + number + (index == null ? "" : " at index " + index));
        }
        return HEX_DIGITS_UPPER[number];
    }
    
    /// <summary>
    /// 16进制数字转char
    /// </summary>
    public static char HexNumberToChar(int number, CaseMode caseMode = CaseMode.UpperCase, int? index = null) {
        if (number < 0 || number > 15) {
            throw new ArgumentException("Illegal hexadecimal number " + number + (index == null ? "" : " at index " + index));
        }
        return caseMode == CaseMode.UpperCase ? HEX_DIGITS_UPPER[number] : HEX_DIGITS_LOWER[number];
    }
}
}