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

namespace Wjybxx.Commons;

/// <summary>
/// 日期时间工具类
/// </summary>
public static class DatetimeUtil
{
    /** 1毫秒对应的tick数 */
    public const long TicksPerMillisecond = 10000;
    /** 1秒对应的tick数 */
    public const long TicksPerSecond = TicksPerMillisecond * 1000;

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
    /** 一天的开始时间 -- 毫秒 */
    public static readonly TimeOnly TimeStartOfDay = new TimeOnly(0, 0, 0);
    /** 一天的结束时间 -- 毫秒 */
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
    /// 获取给定日期所属月份的天数
    /// </summary>
    /// <param name="dateTime">日期时间</param>
    /// <returns></returns>
    public static int LengthOfMonth(in DateTime dateTime) {
        return DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
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
    /// 将时间转换为当天的总毫秒数
    /// </summary>
    /// <param name="timeOnly"></param>
    /// <returns></returns>
    public static long ToMillisOfDay(in TimeOnly timeOnly) {
        return timeOnly.Ticks / TicksPerMillisecond;
    }
}