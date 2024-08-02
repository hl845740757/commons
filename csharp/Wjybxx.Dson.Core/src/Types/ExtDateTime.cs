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
using Wjybxx.Commons;
using Wjybxx.Dson.Internal;

namespace Wjybxx.Dson.Types
{
/// <summary>
/// 日期时间
/// 为提高辨识度，我们命名为'ExtDateTime'
/// </summary>
public readonly struct ExtDateTime : IEquatable<ExtDateTime>
{
    public const byte MaskNone = 0;
    public const byte MaskDate = 1;
    public const byte MaskTime = 1 << 1;
    public const byte MaskOffset = 1 << 2;

    public const byte MaskDatetime = MaskDate | MaskTime;
    public const byte MaskDatetimeOffset = MaskDate | MaskTime | MaskOffset;
    public const byte MaskAll = MaskDatetimeOffset;

    /** 纪元时间-秒 */
    public long Seconds { get; }
    /** 纪元时间的纳秒部分 */
    public int Nanos { get; }
    /** 时区偏移-秒 */
    public int Offset { get; }
    /** 哪些字段有效 */
    public byte Enables { get; }

    /// <summary>
    /// 该接口慎用，通常我们需要精确到毫秒
    /// </summary>
    /// <param name="seconds">纪元时间秒时间戳</param>
    public ExtDateTime(long seconds)
        : this(seconds, 0, 0, MaskDatetime) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="seconds">纪元时间秒时间戳</param>
    /// <param name="nanos">时间戳的纳秒部分</param>
    /// <param name="offset">时区偏移量--秒</param>
    /// <param name="enables">启用的字段信息；只有有效的字段才会被保存(序列化)</param>
    public ExtDateTime(long seconds, int nanos, int offset, byte enables) {
        if ((enables & MaskAll) != enables) {
            throw new ArgumentException("invalid enables: " + enables);
        }
        if (seconds != 0 && !DsonInternals.IsAnySet(enables, MaskDatetime)) {
            throw new ArgumentException("date and time are disabled, but seconds is not 0");
        }
        if (nanos != 0 && !DsonInternals.IsSet(enables, MaskTime)) {
            throw new ArgumentException("time is disabled, but nanos is not 0");
        }
        if (offset != 0 && !DsonInternals.IsSet(enables, MaskOffset)) {
            throw new ArgumentException("offset is disabled, but the value is not 0");
        }
        Timestamp.ValidateNanos(nanos);
        this.Seconds = seconds;
        this.Nanos = nanos;
        this.Offset = offset;
        this.Enables = enables;
    }

    private const int NanosPerTick = 100;
    private const int TicksPerSecond = 10_000_000;

    public static ExtDateTime OfDateTime(in DateTime dateTime) {
        long totalTicks = dateTime.Subtract(DateTime.UnixEpoch).Ticks;
        long seconds = totalTicks / TicksPerSecond; // totalSeconds
        int nanos = (int)(totalTicks % TicksPerSecond * NanosPerTick); // remainNanos -- 取余避免越界
        return new ExtDateTime(seconds, nanos, 0, MaskDatetime);
    }

    public DateTime ToDateTime() {
        long totalTicks = Seconds * TicksPerSecond + Nanos / NanosPerTick;
        return DateTime.UnixEpoch.Add(new TimeSpan(totalTicks));
    }

    public ExtDateTime WithOffset(int offset) {
        return new ExtDateTime(Seconds, Nanos, offset, (byte)(Enables | MaskOffset));
    }

    public ExtDateTime WithoutOffset() {
        return new ExtDateTime(Seconds, Nanos, 0, (byte)(Enables & MaskDatetime));
    }

    #region props

    public bool HasDate => DsonInternals.IsSet(Enables, MaskDate);

    public bool HasTime => DsonInternals.IsSet(Enables, MaskTime);

    public bool HasOffset => DsonInternals.IsSet(Enables, MaskOffset);

    public bool HasFields(byte mask) {
        return DsonInternals.IsSet(Enables, mask);
    }

    /** 是否可以缩写 */
    public bool CanBeAbbreviated() {
        return Nanos == 0 && (Enables == MaskDatetime);
    }

    /** 纳秒部分是否可转为毫秒 -- 纳秒恰为整毫秒时返回true */
    public bool CanConvertNanosToMillis() {
        return (Nanos % 1000_000) == 0;
    }

    /** 将纳秒部分换行为毫秒 */
    public int ConvertNanosToMillis() {
        return Nanos / 1000_000;
    }

    # endregion

    #region equals

    public bool Equals(ExtDateTime other) {
        return Seconds == other.Seconds && Nanos == other.Nanos && Offset == other.Offset && Enables == other.Enables;
    }

    public override bool Equals(object? obj) {
        return obj is ExtDateTime other && Equals(other);
    }

    public override int GetHashCode() {
        return HashCode.Combine(Seconds, Nanos, Offset, Enables);
    }

    public static bool operator ==(ExtDateTime left, ExtDateTime right) {
        return left.Equals(right);
    }

    public static bool operator !=(ExtDateTime left, ExtDateTime right) {
        return !left.Equals(right);
    }

    #endregion

    public override string ToString() {
        return $"{nameof(Seconds)}: {Seconds}, {nameof(Nanos)}: {Nanos}, {nameof(Offset)}: {Offset}, {nameof(Enables)}: {Enables}";
    }

    #region 解析

    public static DateTime ParseDateTime(string datetimeString) {
        return DateTime.ParseExact(datetimeString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    /** 为避免dotnet5的兼容性问题，我们返回DateTime */
    public static DateTime ParseDate(string dateString) {
        return DateTime.ParseExact(dateString + "T00:00:00", "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    /** 为避免dotnet的兼容性问题，我们返回总秒数 */
    public static int ParseTime(string timeString) {
        return DatetimeUtil.ParseTime2(timeString);
    }

    /// <summary>
    /// 格式化日期时间为ISO-8601格式
    /// 固定为:<code>yyyy-MM-ddTHH:mm:ss</code>
    /// </summary>
    public static string FormatDateTime(long seconds) {
        return DateTime.UnixEpoch.AddSeconds(seconds).ToString("s");
    }

    /// <summary>
    /// 格式化日期为ISO-8601格式
    /// 固定为:<code>"yyyy-MM-dd"</code>格式
    /// </summary>
    public static string FormatDate(long epochSeconds) {
        DateTime dateTime = DateTime.UnixEpoch.AddSeconds(epochSeconds);
        string fullString = dateTime.ToString("s");
        return fullString.Substring(0, fullString.IndexOf('T'));
    }

    /// <summary>
    /// 格式化时间为ISO-8601格式
    /// 固定为:<code>HH:mm:ss</code>格式
    /// </summary>
    public static string FormatTime(long epochSeconds) {
        DateTime dateTime = DateTime.UnixEpoch.AddSeconds(epochSeconds);
        int secondOfDay = DatetimeUtil.ToSecondOfDay(dateTime.Hour, dateTime.Minute, dateTime.Second);
        return DatetimeUtil.FormatTime2(secondOfDay);
    }

    /// <summary>
    /// 解析时区偏移
    /// Z, ±H, ±HH, ±H:mm, ±HH:mm, ±HH:mm:ss
    /// </summary>
    /// <param name="offsetString"></param>
    /// <returns></returns>
    public static int ParseOffset(string offsetString) {
        return DatetimeUtil.ParseOffset(offsetString);
    }

    public static string FormatOffset(int offsetSeconds) {
        return DatetimeUtil.FormatOffset(offsetSeconds);
    }

    #endregion

    #region 常量

    public const string NamesDate = "date";
    public const string NamesTime = "time";
    public const string NamesMillis = "millis";

    public const string NamesSeconds = "seconds";
    public const string NamesNanos = "nanos";
    public const string NamesOffset = "offset";
    public const string NamesEnables = "enables";

    public static readonly FieldNumber NumbersSeconds = FieldNumber.OfLnumber(0);
    public static readonly FieldNumber NumbersNanos = FieldNumber.OfLnumber(1);
    public static readonly FieldNumber NumbersOffset = FieldNumber.OfLnumber(2);
    public static readonly FieldNumber NumbersEnables = FieldNumber.OfLnumber(3);

    #endregion
}
}