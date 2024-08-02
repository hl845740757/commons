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
using Wjybxx.Commons;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 时间戳
/// </summary>
public readonly struct Timestamp : IEquatable<Timestamp>
{
    /** 纪元时间-秒 */
    public long Seconds { get; }
    /** 纪元时间的纳秒部分 */
    public int Nanos { get; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="seconds">纪元时间秒时间戳</param>
    /// <param name="nanos">时间戳的纳秒部分</param>
    public Timestamp(long seconds, int nanos = 0) {
        ValidateNanos(nanos);
        this.Seconds = seconds;
        this.Nanos = nanos;
    }

    #region convert

    /// <summary>
    /// 解析时间戳字符串。
    /// 如果字符串以ms结尾，表示毫秒时间戳，否则表示秒时间戳。
    /// </summary>
    public static Timestamp Parse(string rawStr) {
        string str = DsonTexts.DeleteUnderline(rawStr);
        if (str.Length == 0) {
            throw new ArgumentException("NumberFormatException:" + rawStr);
        }
        int length = str.Length;
        if (length > 2 && str[length - 1] == 's' && str[length - 2] == 'm') {
            long epochMillis = long.Parse(str.AsSpan(0, length - 2));
            return OfEpochMillis(epochMillis);
        }
        long seconds = long.Parse(str);
        return new Timestamp(seconds, 0);
    }

    /** 通过纪元毫秒时间戳构建Timestamp */
    public static Timestamp OfEpochMillis(long epochMillis) {
        long seconds = epochMillis / 1000;
        int nanos = (int)(epochMillis % 1000 * DatetimeUtil.NanosPerMilli);
        return new Timestamp(seconds, nanos);
    }

    /** 转换为纪元毫秒时间戳 */
    public long ToEpochMillis() {
        return (Seconds * 1000) + (Nanos / 1000_000);
    }

    /** 纳秒部分是否可转为毫秒 -- 纳秒恰为整毫秒时返回true */
    public bool CanConvertNanosToMillis() {
        return (Nanos % 1000_000) == 0;
    }

    /** 将纳秒部分换行为毫秒 */
    public int ConvertNanosToMillis() {
        return Nanos / 1000_000;
    }

    #endregion

    #region util

    /** 检查纳秒范围 */
    public static void ValidateNanos(int nanos) {
        if (nanos > 999_999_999 || nanos < 0) {
            throw new ArgumentException("nanos > 999999999 or < 0");
        }
    }

    /** 检查毫秒范围 */
    public static void ValidateMillis(int millis) {
        if (millis > 999 || millis < 0) {
            throw new ArgumentException("millis > 999 or < 0");
        }
    }

    #endregion

    #region equals

    public bool Equals(Timestamp other) {
        return Seconds == other.Seconds && Nanos == other.Nanos;
    }

    public override bool Equals(object? obj) {
        return obj is Timestamp other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Seconds, Nanos);
    }

    public static bool operator ==(Timestamp left, Timestamp right) {
        return left.Equals(right);
    }

    public static bool operator !=(Timestamp left, Timestamp right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(Seconds)}: {Seconds}, {nameof(Nanos)}: {Nanos}";
    }

    #region 常量

    public const string NamesSeconds = "seconds";
    public const string NamesNanos = "nanos";
    public const string NamesMillis = "millis";

    public static readonly FieldNumber NumbersSeconds = FieldNumber.OfLnumber(0);
    public static readonly FieldNumber NumbersNanos = FieldNumber.OfLnumber(1);

    #endregion
}
}