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
using System.Globalization;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 数字格式化实现
/// </summary>
public static class NumberStyles
{
    /// <summary>
    /// 注：支持16进制和2进制
    /// </summary>
    public static StyleOut ToString(this NumberStyle style, int value) {
        NumberStyle radix = style & NumberStyle.MaskRadixes;
        switch (radix) {
            case NumberStyle.Hex: {
                // 16进制
                if ((style & NumberStyle.Fixed) != 0) {
                    return new StyleOut("0x" + value.ToString("X8"), true);
                }
                if (value < 0 && value != int.MinValue && (style & NumberStyle.Signed) != 0) {
                    return new StyleOut("-0x" + (-1 * value).ToString("X"), true);
                } else {
                    return new StyleOut("0x" + value.ToString("X"), true);
                }
            }
            case NumberStyle.Binary: {
                // 2进制
                if ((style & NumberStyle.Fixed) != 0) {
                    return new StyleOut("0b" + ToFixedBinaryString(value), true);
                }
                if (value < 0 && value != int.MinValue && (style & NumberStyle.Signed) != 0) {
                    return new StyleOut("-0b" + ToBinaryString(-1 * value), true);
                } else {
                    return new StyleOut("0b" + ToBinaryString(value), true);
                }
            }
            default: {
                // 10进制
                bool isTyped = (style & NumberStyle.Typed) != 0;
                return new StyleOut(value.ToString(), isTyped);
            }
        }
    }

    /// <summary>
    /// 注：支持16进制和2进制
    /// </summary>
    public static StyleOut ToString(this NumberStyle style, long value) {
        NumberStyle radix = style & NumberStyle.MaskRadixes;
        switch (radix) {
            case NumberStyle.Hex: {
                // 16进制
                if ((style & NumberStyle.Fixed) != 0) {
                    return new StyleOut("0x" + value.ToString("X16"), true);
                }
                if (value < 0 && value != int.MinValue && (style & NumberStyle.Signed) != 0) {
                    return new StyleOut("-0x" + (-1 * value).ToString("X"), true);
                } else {
                    return new StyleOut("0x" + value.ToString("X"), true);
                }
            }
            case NumberStyle.Binary: {
                // 2进制
                if ((style & NumberStyle.Fixed) != 0) {
                    return new StyleOut("0b" + ToFixedBinaryString(value), true);
                }
                if (value < 0 && value != int.MinValue && (style & NumberStyle.Signed) != 0) {
                    return new StyleOut("-0b" + ToBinaryString(-1 * value), true);
                } else {
                    return new StyleOut("0b" + ToBinaryString(value), true);
                }
            }
            default: {
                // 10进制
                bool isTyped = (style & NumberStyle.Typed) != 0 || Math.Abs(value) >= DoubleMaxLong;
                return new StyleOut(value.ToString(), isTyped);
            }
        }
    }

    /// <summary>
    /// C#并不内置支持IEEE-754语义的十六进制浮点字面量（例如 0x1.921fb54442d18p+1），因此只支持简单模式
    /// </summary>
    public static StyleOut ToString(this NumberStyle style, float value) {
        if (float.IsInfinity(value) || float.IsNaN(value)) {
            return new StyleOut(value.ToString(CultureInfo.InvariantCulture), true);
        }
        bool isTyped = (style & NumberStyle.Typed) != 0;
        int iv = (int)value;
        if (iv == value) {
            return new StyleOut(iv.ToString(), isTyped);
        } else {
            string str;
            if ((style & NumberStyle.NoExponent3) != 0) {
                str = value.ToString("0.###");
            } else if ((style & NumberStyle.NoExponent7) != 0) {
                str = value.ToString("0.#######");
            } else {
                str = value.ToString(CultureInfo.InvariantCulture);
                isTyped |= str.Contains('E');
            }
            // 数字截断问题
            if (str == "-0") str = "0";
            return new StyleOut(str, isTyped);
        }
    }

    /// <summary>
    /// C#并不内置支持IEEE-754语义的十六进制浮点字面量（例如 0x1.921fb54442d18p+1），因此只支持简单模式
    /// </summary>
    public static StyleOut ToString(this NumberStyle style, double value) {
        if (double.IsInfinity(value) || double.IsNaN(value)) {
            return new StyleOut(value.ToString(CultureInfo.InvariantCulture), true);
        }
        bool isTyped = (style & NumberStyle.Typed) != 0;
        long lv = (long)value;
        if (lv == value) {
            return new StyleOut(lv.ToString(), isTyped);
        } else {
            string str;
            if ((style & NumberStyle.NoExponent3) != 0) {
                str = value.ToString("0.###");
            } else if ((style & NumberStyle.NoExponent7) != 0) {
                str = value.ToString("0.#######");
            } else {
                str = value.ToString(CultureInfo.InvariantCulture);
                isTyped |= str.Contains('E');
            }
            // 数字截断问题
            if (str == "-0") str = "0";
            return new StyleOut(str, isTyped);
        }
    }

    /// <summary>
    /// double能精确表示的最大整数
    /// </summary>
    private const long DoubleMaxLong = (1L << 53) - 1;

    /// <summary>
    /// 转2进制，长度补全为8的倍数
    /// </summary>
    private static string ToBinaryString(int value) {
        string binaryString = Convert.ToString(value, 2);
        int mod = binaryString.Length % 8;
        if (mod != 0) {
            binaryString = binaryString.PadLeft(8 - mod, '0');
        }
        return binaryString;
    }

    private static string ToBinaryString(long value) {
        string binaryString = Convert.ToString(value, 2);
        int mod = binaryString.Length % 8;
        if (mod != 0) {
            binaryString = binaryString.PadLeft(8 - mod, '0');
        }
        return binaryString;
    }

    /// <summary>
    /// 转2进制，固定32位
    /// </summary>
    private static string ToFixedBinaryString(int value) {
        string binaryString = Convert.ToString(value, 2);
        int pad = 32 - binaryString.Length;
        if (pad > 0) {
            binaryString = binaryString.PadLeft(pad, '0');
        }
        return binaryString;
    }

    private static string ToFixedBinaryString(long value) {
        string binaryString = Convert.ToString(value, 2);
        int pad = 64 - binaryString.Length;
        if (pad > 0) {
            binaryString = binaryString.PadLeft(pad, '0');
        }
        return binaryString;
    }
}
}