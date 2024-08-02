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
using System.Text;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 这里提供默认数字格式化方式
/// </summary>
public static class NumberStyles
{
    /** 普通打印 -- 超过表示范围时会添加类型标签 */
    public static INumberStyle Simple { get; } = new SimpleStyle();
    /** 总是打印类型 */
    public static INumberStyle Typed { get; } = new TypedStyle();

    /** 打印为无符号数 -- 超过表示范围时会添加类型标签；通常用于打印Flags类型 */
    public static INumberStyle Unsigned { get; } = new UnsignedStyle();
    /** 打印为带类型无符号数；通常用于打印Flags类型 */
    public static INumberStyle TypedUnsigned { get; } = new TypedUnsignedStyle();

    /** 16进制，打印正负号 -- 不支持浮点数 */
    public static INumberStyle SignedHex { get; } = new SignedHexStyle();
    /** 无符号16进制，按位打印 -- 不支持浮点数 */
    public static INumberStyle UnsignedHex { get; } = new UnsignedHexStyle();

    /** 2进制，打印正负号 -- 不支持浮点数 */
    public static INumberStyle SignedBinary { get; } = new SignedBinaryStyle();
    /** 无符号2进制，按位打印 -- 不支持浮点数 */
    public static INumberStyle UnsignedBinary { get; } = new UnsignedBinaryStyle();
    /** 固定位数2进制，按位打印 -- 不支持浮点数 */
    public static INumberStyle FixedBinary { get; } = new FixedBinaryStyle();

    /** double能精确表示的最大整数 */
    private const long DoubleMaxLong = (1L << 53) - 1;

    #region simple

    private class SimpleStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            return new StyleOut(value.ToString(), false);
        }

        public StyleOut ToString(long value) {
            return new StyleOut(value.ToString(), Math.Abs(value) >= DoubleMaxLong);
        }

        public StyleOut ToString(float value) {
            if (float.IsInfinity(value) || float.IsNaN(value)) {
                return new StyleOut(value.ToString(CultureInfo.InvariantCulture), true);
            }
            int iv = (int)value;
            if (iv == value) {
                return new StyleOut(iv.ToString(), false);
            } else {
                string str = value.ToString(CultureInfo.InvariantCulture);
                return new StyleOut(str, str.IndexOf('E') >= 0);
            }
        }

        public StyleOut ToString(double value) {
            if (double.IsInfinity(value) || double.IsNaN(value)) {
                return new StyleOut(value.ToString(CultureInfo.InvariantCulture), true);
            }
            long lv = (long)value;
            if (lv == value) {
                return new StyleOut(lv.ToString(), false);
            } else {
                string str = value.ToString(CultureInfo.InvariantCulture);
                return new StyleOut(str, str.IndexOf('E') >= 0);
            }
        }
    }

    private class TypedStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            return new StyleOut(value.ToString(), true);
        }

        public StyleOut ToString(long value) {
            return new StyleOut(value.ToString(), true);
        }

        public StyleOut ToString(float value) {
            return new StyleOut(Simple.ToString(value).Value, true);
        }

        public StyleOut ToString(double value) {
            return new StyleOut(Simple.ToString(value).Value, true);
        }
    }

    private class UnsignedStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            uint castV = (uint)value;
            return new StyleOut(castV.ToString(), false);
        }

        public StyleOut ToString(long value) {
            ulong castV = (ulong)value;
            return new StyleOut(castV.ToString(), Math.Abs(value) >= DoubleMaxLong);
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    private class TypedUnsignedStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            uint castV = (uint)value;
            return new StyleOut(castV.ToString(), true);
        }

        public StyleOut ToString(long value) {
            ulong castV = (ulong)value;
            return new StyleOut(castV.ToString(), true);
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region 16进制

    private class SignedHexStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            if (value < 0 && value != int.MinValue) {
                return new StyleOut("-0x" + (-1 * value).ToString("X"), true);
            } else {
                return new StyleOut("0x" + value.ToString("X"), true);
            }
        }

        public StyleOut ToString(long value) {
            if (value < 0 && value != long.MinValue) {
                return new StyleOut("-0x" + (-1 * value).ToString("X"), true);
            } else {
                return new StyleOut("0x" + value.ToString("X"), true);
            }
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    private class UnsignedHexStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            return new StyleOut("0x" + value.ToString("X"), true);
        }

        public StyleOut ToString(long value) {
            return new StyleOut("0x" + value.ToString("X"), true);
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    #endregion

    #region 二进制

    private static string ToBinaryString(int value) {
        return Convert.ToString(value, 2);
    }

    private static string ToBinaryString(long value) {
        return Convert.ToString(value, 2);
    }

    private class SignedBinaryStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            if (value < 0 && value != int.MinValue) {
                return new StyleOut("-0b" + ToBinaryString(-1 * value), true);
            } else {
                return new StyleOut("0b" + ToBinaryString(value), true);
            }
        }

        public StyleOut ToString(long value) {
            if (value < 0 && value != long.MinValue) {
                return new StyleOut("-0b" + ToBinaryString(-1 * value), true);
            } else {
                return new StyleOut("0b" + ToBinaryString(value), true);
            }
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    private class UnsignedBinaryStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            return new StyleOut("0b" + ToBinaryString(value), true);
        }

        public StyleOut ToString(long value) {
            return new StyleOut("0b" + ToBinaryString(value), true);
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    private class FixedBinaryStyle : INumberStyle
    {
        public StyleOut ToString(int value) {
            string binaryString = ToBinaryString(value);
            StringBuilder sb = new StringBuilder(34)
                .Append("0b");
            if (binaryString.Length < 32) {
                sb.Insert(2, "0", 32 - binaryString.Length);
            }
            sb.Append(binaryString);
            return new StyleOut(sb.ToString(), true);
        }

        public StyleOut ToString(long value) {
            string binaryString = ToBinaryString(value);
            StringBuilder sb = new StringBuilder(34)
                .Append("0b");
            if (binaryString.Length < 64) {
                sb.Insert(2, "0", 64 - binaryString.Length);
            }
            sb.Append(binaryString);
            return new StyleOut(sb.ToString(), true);
        }

        public StyleOut ToString(float value) {
            throw new NotImplementedException();
        }

        public StyleOut ToString(double value) {
            throw new NotImplementedException();
        }
    }

    #endregion

    /// <summary>
    /// 将枚举转换为对应Style实例
    /// </summary>
    /// <param name="style"></param>
    /// <returns></returns>
    public static INumberStyle ToNumberStyle(this NumberStyle style) {
        return style switch
        {
            NumberStyle.Simple => Simple,
            NumberStyle.Typed => Typed,
            NumberStyle.Unsigned => Unsigned,
            NumberStyle.TypedUnsigned => TypedUnsigned,
            NumberStyle.SignedHex => SignedHex,
            NumberStyle.UnsignedHex => UnsignedHex,
            NumberStyle.SignedBinary => SignedBinary,
            NumberStyle.UnsignedBinary => UnsignedBinary,
            NumberStyle.FixedBinary => FixedBinary,
            _ => throw new ArgumentException(style.ToString())
        };
    }
}
}