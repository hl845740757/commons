using System;
using System.Globalization;
using System.Text;

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 4位定点数（万分比）
/// 注：该结构仅用于Dson层，业务层还是需定义自己的Fixed64结构。
/// </summary>
public readonly struct FixedPoint4 : IEquatable<FixedPoint4>, IComparable<FixedPoint4>
{
    public const long Scale = 10_000;
    public static FixedPoint4 Zero => new FixedPoint4(0);
    public static FixedPoint4 One => new FixedPoint4(Scale);

    public readonly long rawValue;

    public FixedPoint4(long rawValue) {
        this.rawValue = rawValue;
    }

    public static FixedPoint4 FromRaw(long rawValue) => new FixedPoint4(rawValue);

    /// <summary>
    /// 通过整数值构造，超出表示范围时抛出溢出异常。
    /// </summary>
    public static FixedPoint4 FromInteger(long value) => new FixedPoint4(checked(value * Scale));

    /// <summary>
    /// 通过浮点数构造，将超过四位的小数向零截断。
    /// </summary>
    public static FixedPoint4 FromDouble(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new OverflowException();
        }

        long rawValue = checked((long)(value * Scale));
        return new FixedPoint4(rawValue);
    }

    /// <summary>
    /// 转换为整数值（小数部分被丢弃）
    /// </summary>
    /// <returns></returns>
    public long ToInteger() => rawValue / Scale;

    /// <summary>
    /// 转换为Double值
    /// </summary>
    public double ToDouble() => rawValue / (double)Scale;

    /// <summary>
    /// 整数部分（固定正数，以避免负0问题）
    /// </summary>
    public long IntegerPart => Math.Abs(rawValue / Scale);
    /// <summary>
    /// 小数部分（固定正数，以避免负0问题）
    /// </summary>
    public int FractionPart => (int)Math.Abs(rawValue % Scale);

    /// <summary>
    /// 解析普通十进制文本，小数部分最多四位。
    /// </summary>
    public static FixedPoint4 Parse(string value) {
        ReadOnlySpan<char> text = value.AsSpan();
        bool negative = text[0] == '-';
        if (text[0] == '-' || text[0] == '+') {
            text = text.Slice(1);
        }

        int index = text.IndexOf('.');
        ReadOnlySpan<char> integerPart = index == -1 ? text : text.Slice(0, index);
        long p0 = long.Parse(integerPart, NumberStyles.None, CultureInfo.InvariantCulture);
        long p1 = 0;
        if (index != -1) {
            ReadOnlySpan<char> fractionPart = text.Slice(index + 1);
            if (fractionPart.Length == 0 || fractionPart.Length > 4) {
                throw new FormatException(value);
            }

            p1 = long.Parse(fractionPart, NumberStyles.None, CultureInfo.InvariantCulture);
            for (int i = fractionPart.Length; i < 4; i++) {
                p1 *= 10;
            }
        }

        long rawValue = checked(p0 * Scale + p1);
        return new FixedPoint4(negative ? -rawValue : rawValue);
    }

    public override string ToString() {
        long p1 = rawValue % Scale;
        if (p1 == 0) {
            long p0 = rawValue / Scale;
            return p0.ToString();
        }
        StringBuilder sb = new StringBuilder();
        ToString0(this, sb);
        return sb.ToString();
    }

    internal static void ToString0(FixedPoint4 fx4, StringBuilder builder) {
        if (fx4.rawValue < 0) {
            builder.Append('-');
        }
        builder.Append(fx4.IntegerPart);
        //
        int fractionPart = fx4.FractionPart;
        if (fractionPart != 0) {
            builder.Append('.');
            if (fractionPart < 10) {
                builder.Append('0', 3);
            } else if (fractionPart < 100) {
                builder.Append('0', 2);
            } else if (fractionPart < 1000) {
                builder.Append('0');
            }
            while (fractionPart % 10 == 0) {
                fractionPart /= 10;
            }
            builder.Append(fractionPart);
        }
    }

    #region equals

    public bool Equals(FixedPoint4 other) {
        return rawValue == other.rawValue;
    }

    public override bool Equals(object? obj) {
        return obj is FixedPoint4 other && Equals(other);
    }

    public override int GetHashCode() {
        return rawValue.GetHashCode();
    }

    public static bool operator ==(FixedPoint4 left, FixedPoint4 right) {
        return left.Equals(right);
    }

    public static bool operator !=(FixedPoint4 left, FixedPoint4 right) {
        return !left.Equals(right);
    }

    #endregion

    #region 比较

    public int CompareTo(FixedPoint4 other) {
        return rawValue.CompareTo(other.rawValue);
    }

    public static bool operator <(FixedPoint4 left, FixedPoint4 right) {
        return left.rawValue < right.rawValue;
    }

    public static bool operator >(FixedPoint4 left, FixedPoint4 right) {
        return left.rawValue > right.rawValue;
    }

    public static bool operator <=(FixedPoint4 left, FixedPoint4 right) {
        return left.rawValue <= right.rawValue;
    }

    public static bool operator >=(FixedPoint4 left, FixedPoint4 right) {
        return left.rawValue >= right.rawValue;
    }

    #endregion
}
}