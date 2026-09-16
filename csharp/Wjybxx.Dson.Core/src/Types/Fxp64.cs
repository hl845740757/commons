using System;
using System.Globalization;
using System.Text;

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 64位定点数（万分比）
/// 注：该结构仅用于Dson层，业务层还是需定义自己的定点数结构。
/// </summary>
public readonly struct Fxp64 : IEquatable<Fxp64>, IComparable<Fxp64>
{
    public const long Scale = 10_000;
    public static Fxp64 Zero => new Fxp64(0);
    public static Fxp64 One => new Fxp64(Scale);

    public readonly long rawValue;

    public Fxp64(long rawValue) {
        this.rawValue = rawValue;
    }

    public static Fxp64 FromRaw(long rawValue) => new Fxp64(rawValue);

    /// <summary>
    /// 通过整数值构造，超出表示范围时抛出溢出异常。
    /// </summary>
    public static Fxp64 FromInteger(long value) => new Fxp64(checked(value * Scale));

    /// <summary>
    /// 通过浮点数构造，将超过四位的小数向零截断。
    /// </summary>
    public static Fxp64 FromDouble(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new OverflowException();
        }

        long rawValue = checked((long)(value * Scale));
        return new Fxp64(rawValue);
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
    public static Fxp64 Parse(string value) {
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
        return new Fxp64(negative ? -rawValue : rawValue);
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

    internal static void ToString0(Fxp64 value, StringBuilder builder) {
        if (value.rawValue < 0) {
            builder.Append('-');
        }
        builder.Append(value.IntegerPart);
        //
        int fractionPart = value.FractionPart;
        if (fractionPart != 0) {
            builder.Append('.');
            // 输出高位0
            long temp = fractionPart;
            while (temp * 10 < Scale) {
                temp *= 10;
                builder.Append('0');
            }
            while (fractionPart % 10 == 0) {
                fractionPart /= 10;
            }
            builder.Append(fractionPart);
        }
    }

    #region equals

    public bool Equals(Fxp64 other) {
        return rawValue == other.rawValue;
    }

    public override bool Equals(object? obj) {
        return obj is Fxp64 other && Equals(other);
    }

    public override int GetHashCode() {
        return rawValue.GetHashCode();
    }

    public static bool operator ==(Fxp64 left, Fxp64 right) {
        return left.Equals(right);
    }

    public static bool operator !=(Fxp64 left, Fxp64 right) {
        return !left.Equals(right);
    }

    #endregion

    #region 比较

    public int CompareTo(Fxp64 other) {
        return rawValue.CompareTo(other.rawValue);
    }

    public static bool operator <(Fxp64 left, Fxp64 right) {
        return left.rawValue < right.rawValue;
    }

    public static bool operator >(Fxp64 left, Fxp64 right) {
        return left.rawValue > right.rawValue;
    }

    public static bool operator <=(Fxp64 left, Fxp64 right) {
        return left.rawValue <= right.rawValue;
    }

    public static bool operator >=(Fxp64 left, Fxp64 right) {
        return left.rawValue >= right.rawValue;
    }

    #endregion
}
}