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
using System.Globalization;
using System.Text;

namespace Wjybxx.Commons
{
/// <summary>
/// 日期时间工具类
/// </summary>
public static class DatetimeUtil
{
    /** 1微秒对应的Tick数 */
    public const long TicksPerMicrosecond = 10;
    /** 1毫秒对应的Tick数 */
    public const long TicksPerMillisecond = TicksPerMicrosecond * 1000; // 10,000
    /** 1秒对应的Tick数 */
    public const long TicksPerSecond = TicksPerMillisecond * 1000; // 10,000,000
    /** 1分钟对应的Tick数 */
    public const long TicksPerMinute = TicksPerSecond * 60; // 600,000,000
    /** 1小时对应的Tick数 */
    public const long TicksPerHour = TicksPerMinute * 60; // 36,000,000,000
    /** 1天对应的Tick数 */
    public const long TicksPerDay = TicksPerHour * 24; // 864,000,000,000

    /** 1个Tick对应的Nanos */
    public const int NanosPerTick = 100;
    /** 1毫秒的纳秒数 */
    public const long NanosPerMilli = 1000_000;
    /** 1秒的纳秒数 */
    public const long NanosPerSecond = 1000_000_000;
    /** 1小时的纳秒数 */
    public const long NanosPerMinutes = NanosPerSecond * 60L;
    /** 一小时的纳秒数 */
    public const long NanosPerHours = NanosPerMinutes * 60L;
    /** 一天的纳秒数 */
    public const long NanosPerDay = NanosPerHours * 24L;

    /** 1秒的毫秒数 */
    public const long MillisPerSecond = 1000;
    /** 1分钟的毫秒数 */
    public const long MillisPerMinute = MillisPerSecond * 60;
    /** 1小时的毫秒数 */
    public const long MillisPerHour = MillisPerMinute * 60;
    /** 1天的毫秒数 */
    public const long MillisPerDay = MillisPerHour * 24;
    /** 1周的毫秒数 */
    public const long MillisPerWeek = MillisPerDay * 7;

    /** 1分钟的秒数 */
    public const int SecondsPerMinute = 60;
    /** 1小时的秒数 */
    public const int SecondsPerHour = SecondsPerMinute * 60;
    /** 1天的秒数 - 86400 */
    public const int SecondsPerDay = SecondsPerHour * 24;
    /** 1周的秒数 */
    public const int SecondsPerWeek = SecondsPerDay * 7;

    /** UTC基准时间偏移 */
    public static readonly TimeSpan ZoneOffsetUtc = TimeSpan.Zero;
    /** 中国时区偏移 */
    public static readonly TimeSpan ZoneOffsetCst = TimeSpan.FromHours(8);
    /** 系统的时区偏移 */
    public static readonly TimeSpan ZoneOffsetSystem = TimeZoneInfo.Local.BaseUtcOffset;

    /** Unix纪元时间 */
    public static readonly DateTime UnixEpoch = DateTime.UnixEpoch;
    /** Unix纪元日期 */
    public static readonly DateOnly DateUnixEpoch = new DateOnly(1970, 1, 1);
    /** 一天的开始时间 */
    public static readonly TimeOnly TimeStartOfDay = new TimeOnly(0, 0, 0);
    /** 一天的结束时间 -- 精确到毫秒 */
    public static readonly TimeOnly TimeEndOfDay = new TimeOnly(23, 59, 59, 999);

    /// <summary>
    /// 获取当前的Unix时间戳(毫秒)
    /// </summary>
    /// <returns></returns>
    public static long CurrentEpochMillis() {
        return (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
    }

    /// <summary>
    /// 获取当前的Unix时间戳(秒)
    /// </summary>
    /// <returns></returns>
    public static long CurrentEpochSeconds() {
        return (long)DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// 获取给定日期所属月份的天数
    /// </summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns></returns>
    public static int LengthOfMonth(in DateTime dateTime) {
        return DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
    }

    #region 时间戳转换

    /// <summary>
    /// 转unix秒时间戳
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static long ToEpochSeconds(in DateTime dateTime) {
        return (long)dateTime.Subtract(DateTime.UnixEpoch).TotalSeconds;
    }

    /// <summary>
    /// 转Unix毫秒时间戳
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static long ToEpochMillis(in DateTime dateTime) {
        return (long)dateTime.Subtract(DateTime.UnixEpoch).TotalMilliseconds;
    }

    /// <summary>
    /// 将unix时间戳转为UTC时间
    /// </summary>
    /// <param name="epochMillis">unix时间戳</param>
    public static DateTime ToDateTime(long epochMillis) {
        return DateTime.UnixEpoch.AddMilliseconds(epochMillis);
    }

    /// <summary>
    /// 将unix时间戳转为本地时间
    /// </summary>
    /// <param name="epochMillis">unix时间戳</param>
    /// <param name="offset">时区偏移</param>
    /// <returns></returns>
    public static DateTime ToLocalDateTime(long epochMillis, TimeSpan offset) {
        return DateTime.UnixEpoch.AddMilliseconds(epochMillis + offset.TotalMilliseconds);
    }

    /// <summary>
    /// 将时间转换为当天的总秒数
    /// </summary>
    /// <param name="timeOnly"></param>
    /// <returns></returns>
    public static int ToSecondOfDay(in TimeOnly timeOnly) {
        return (int)(timeOnly.Ticks / TicksPerSecond);
    }

    /// <summary>
    /// 秒数转时间
    /// </summary>
    /// <param name="seconds">一天内的秒数</param>
    /// <returns></returns>
    public static TimeOnly TimeOfDaySeconds(int seconds) {
        return new TimeOnly(seconds * TicksPerSecond);
    }

    /// <summary>
    /// 将时间转换为当天的总毫秒数
    /// </summary>
    /// <param name="timeOnly"></param>
    /// <returns></returns>
    public static long ToMillisOfDay(in TimeOnly timeOnly) {
        return timeOnly.Ticks / TicksPerMillisecond;
    }

    /// <summary>
    /// 毫秒数转时间
    /// </summary>
    /// <param name="millis">一天内的毫秒数</param>
    /// <returns></returns>
    public static TimeOnly TimeOfDayMillis(int millis) {
        return new TimeOnly(millis * TicksPerMillisecond);
    }

    /// <summary>
    /// 获取日期时间的日期部分
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static DateOnly ToDateOnly(this in DateTime dateTime) {
        return new DateOnly(dateTime.Year, dateTime.Month, dateTime.Day);
    }

    /// <summary>
    /// 获取日期时间的时间部分
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static TimeOnly ToTimeOnly(this in DateTime dateTime) {
        return new TimeOnly(dateTime.Hour, dateTime.Minute, dateTime.Second);
    }

    #endregion

    #region parse/format

    /// <summary>
    /// 解析日期时间
    /// </summary>
    public static DateTime ParseDateTime(string datetimeString) {
        return DateTime.ParseExact(datetimeString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 解析日期
    /// </summary>
    public static DateOnly ParseDate(string dateString) {
        return DateOnly.ParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 解析时间
    /// </summary>
    public static TimeOnly ParseTime(string timeString) {
        return TimeOnly.ParseExact(timeString, "HH:mm:ss", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 格式化日期时间为ISO-8601格式
    /// 固定为:<code>yyyy-MM-ddTHH:mm:ss</code>
    /// </summary>
    /// <param name="dateTime"></param>
    /// <returns></returns>
    public static string FormatDateTime(DateTime dateTime) {
        return dateTime.ToString("s");
    }

    /// <summary>
    /// 格式化日期为ISO-8601格式
    /// 固定为:<code>"yyyy-MM-dd"</code>格式
    /// </summary>
    public static string FormatDate(DateOnly dateTime) {
        return dateTime.ToString("O");
    }

    /// <summary>
    /// 格式化时间为ISO-8601格式
    /// 固定为:<code>HH:mm:ss</code>格式
    /// </summary>
    public static string FormatTime(TimeOnly dateTime) {
        return dateTime.ToString("HH:mm:ss");
    }

    /// <summary>
    /// 解析时区偏移
    /// 支持的格式包括：<code>Z, ±H, ±HH, ±H:mm, ±HH:mm, ±HH:mm:ss</code>
    /// </summary>
    /// <param name="offsetString"></param>
    /// <returns>时区偏移秒数</returns>
    /// <exception cref="ArgumentException"></exception>
    public static int ParseOffset(string offsetString) {
        if (offsetString == "Z" || offsetString == "z") {
            return 0;
        }
        if (offsetString[0] != '+' && offsetString[0] != '-') {
            throw new ArgumentException("Invalid offsetString, plus/minus not found when expected: " + offsetString);
        }
        // 不想写得太复杂，补全后解析
        StringBuilder sb = new StringBuilder(offsetString, 9);
        switch (offsetString.Length) {
            case 2: { // ±H
                sb.Insert(1, '0').Append(":00:00");
                break;
            }
            case 3: { // ±HH
                sb.Append(":00:00");
                break;
            }
            case 5: { // ±H:mm
                sb.Insert(1, '0').Append(":00");
                break;
            }
            case 6: { // ±HH:mm
                sb.Append(":00");
                break;
            }
            case 9: { // ±HH:mm:ss
                break;
            }
            default: {
                throw new ArgumentException("Invalid offsetString: " + offsetString);
            }
        }
        int seconds = ToSecondOfDay(ParseTime(sb.ToString(1, 8)));
        if (offsetString[0] == '+') {
            return seconds;
        }
        return -1 * seconds;
    }

    /// <summary>
    /// 格式化时间偏移
    /// 可能的结果：
    /// <code> Z, ±HH:mm, ±HH:mm:ss</code>
    /// </summary>
    /// <param name="offsetSeconds">时区偏移秒数</param>
    /// <returns></returns>
    public static string FormatOffset(int offsetSeconds) {
        if (offsetSeconds == 0) {
            return "Z";
        }
        string sign = offsetSeconds < 0 ? "-" : "+";
        string offsetString = FormatTime(TimeOfDaySeconds(Math.Abs(offsetSeconds)));
        if (offsetSeconds % 60 == 0) { // 没有秒部分
            return sign + offsetString.Substring(0, 5);
        } else {
            return sign + offsetString;
        }
    }

    #endregion
}
}