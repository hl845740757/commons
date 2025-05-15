#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Time
{
/// <summary>
/// C#下该类作用好像不大
/// 
/// PS：可以通过扩展方法增加功能
/// </summary>
public sealed class TimeHelper
{
    private static readonly TimeHelper CST = new TimeHelper(TimeSpan.FromHours(8));
    private static readonly TimeHelper SYSTEM = new TimeHelper(TimeZoneInfo.Local.BaseUtcOffset);
    private static readonly TimeHelper UTC = new TimeHelper(TimeSpan.Zero);


    /// <summary>
    /// 时区偏移，单位秒
    ///
    /// 转本地时间时加offset，转时间戳时减offset
    /// </summary>
    private readonly int offset;

    private TimeHelper(int offset) {
        this.offset = offset;
    }

    private TimeHelper(TimeSpan timeSpan) {
        this.offset = (int)timeSpan.TotalSeconds;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="timeSpan">时区跨度，最大精确到秒</param>
    /// <returns></returns>
    public static TimeHelper Of(TimeSpan timeSpan) {
        int seconds = (int)timeSpan.TotalSeconds;
        if (seconds == CST.offset) {
            return CST;
        }
        if (seconds == SYSTEM.offset) {
            return SYSTEM;
        }
        if (seconds == UTC.offset) {
            return UTC;
        }
        return new TimeHelper(seconds);
    }

    /// <summary>
    /// 时区偏移的秒数
    /// </summary>
    public int Offset => offset;

    #region 时间戳和datetime转换

    /// <summary>
    /// 将本地时间转换为时区无关的毫秒时间戳
    /// </summary>
    /// <param name="localDateTime"></param>
    /// <returns></returns>
    public long ToEpochMillis(in DateTime localDateTime) {
        return (long)localDateTime.Subtract(DateTime.UnixEpoch).TotalMilliseconds - (offset * 1000L);
    }

    /// <summary>
    /// 计算在本地时区下的纪元天数
    /// 
    /// 注意：第一天的返回值默认是0，参数控制
    /// </summary>
    /// <param name="epochMilli">毫秒时间</param>
    /// <param name="ceil">是否向上取整</param>
    /// <returns>本地时区下的天数</returns>
    public int ToEpochDay(long epochMilli, bool ceil = false) {
        DateTime localDateTime = DateTime.UnixEpoch.AddMilliseconds(epochMilli + offset * 1000L);
        return DatetimeUtil.TotalDays(localDateTime, ceil);
    }

    /// <summary>
    /// 将毫秒时间转换为本地时间，并忽略毫秒部分
    /// </summary>
    /// <param name="epochMilli">毫秒时间</param>
    /// <returns></returns>
    public DateTime ToDateTime(long epochMilli) {
        return DateTime.UnixEpoch.AddMilliseconds(epochMilli + offset * 1000L);
    }

    /// <summary>
    /// 将毫秒时间转换为本地时间，并忽略毫秒部分
    /// </summary>
    /// <param name="epochMilli">毫秒时间</param>
    /// <returns></returns>
    public DateTime ToDateTimeIgnoreMs(long epochMilli) {
        long seconds = epochMilli / 1000;
        return DateTime.UnixEpoch.AddSeconds(seconds + offset);
    }

    #endregion

    #region util

    /// <summary>
    /// 获取指定日期的凌晨时间(00 : 00 : 00)
    /// </summary>
    /// <param name="epochMilli"></param>
    /// <returns></returns>
    private DateTime ToDateTimeBeginOfDay(long epochMilli) {
        DateTime dateTime = ToDateTimeIgnoreMs(epochMilli);
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
    }

    /// <summary>
    /// 获取指定日期的午夜时间(23 : 59 : 59, 999)
    /// (当前最后1毫秒)
    /// </summary>
    /// <param name="epochMilli"></param>
    /// <returns></returns>
    private DateTime ToDateTimeEndOfDay(long epochMilli) {
        DateTime dateTime = ToDateTimeIgnoreMs(epochMilli);
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day)
            .AddMilliseconds(DatetimeUtil.MillisPerDay - 1);
    }

    /// <summary>
    /// 获取指定时间戳所在日期的凌晨(00 : 00 : 00)的毫秒时间戳
    /// </summary>
    /// <param name="epochMilli">指定时间戳，用于确定日期</param>
    /// <returns></returns>
    public long GetBeginOfToday(long epochMilli) {
        DateTime dateTime = ToDateTimeBeginOfDay(epochMilli);
        return ToEpochMillis(in dateTime);
    }

    /// <summary>
    /// 获取指定时间戳所在日期的午夜(23 : 59 : 59, 999)的毫秒时间戳
    /// </summary>
    /// <param name="epochMilli">指定时间戳，用于确定日期</param>
    /// <returns></returns>
    public long GetEndOfToday(long epochMilli) {
        DateTime dateTime = ToDateTimeEndOfDay(epochMilli);
        return ToEpochMillis(in dateTime);
    }

    /// <summary>
    /// 获取指定时间戳所在周的周一凌晨(00 : 00 : 00)的毫秒时间戳
    /// </summary>
    /// <param name="epochMilli">指定时间戳，用于确定所在的周</param>
    /// <returns></returns>
    public long GetBeginOfWeek(long epochMilli) {
        DateTime startOfDay = ToDateTimeBeginOfDay(epochMilli);
        int deltaDay = startOfDay.DayOfWeek.GetNumber() - DayOfWeek.Monday.GetNumber();
        return ToEpochMillis(startOfDay) - (deltaDay * DatetimeUtil.MillisPerDay);
    }

    /// <summary>
    /// 获取指定时间戳所在周的周日(23 : 59 : 59, 999)的毫秒时间戳
    /// 
    /// </summary>
    /// <param name="epochMilli">指定时间戳，用于确定所在的周</param>
    /// <returns></returns>
    public long GetEndOfWeek(long epochMilli) {
        return GetBeginOfWeek(epochMilli) + DatetimeUtil.MillisPerWeek - 1;
    }

    /// <summary>
    /// 获取本月的开始时间戳
    /// 
    /// 本月第一天的(00 : 00 : 00)的毫秒时间戳
    /// </summary>
    /// <param name="epochMilli"></param>
    /// <returns></returns>
    public long GetBeginOfMonth(long epochMilli) {
        DateTime dateTime = ToDateTime(epochMilli);
        dateTime = new DateTime(dateTime.Year, dateTime.Month, 1);
        return ToEpochMillis(dateTime);
    }

    /// <summary>
    /// 获取本月的结束时间戳
    ///
    /// 本月最后一天的(23 : 59 : 59, 999)
    /// </summary>
    /// <param name="epochMilli"></param>
    /// <returns></returns>
    public long GetEndOfMonth(long epochMilli) {
        DateTime dateTime = ToDateTime(epochMilli);
        dateTime = new DateTime(dateTime.Year, dateTime.Month, DateTime.DaysInMonth(dateTime.Year, dateTime.Month))
            .AddMilliseconds(DatetimeUtil.MillisPerDay - 1);
        return ToEpochMillis(dateTime);
    }

    /// <summary>
    /// 判断两个时间是否是同一天
    ///
    /// （同样的两个时间戳，在不同的时区，结果可能不同）
    /// </summary>
    /// <param name="time1">第一个时间戳</param>
    /// <param name="time2">第二个时间戳</param>
    /// <returns>如果是同一天则返回true，否则返回false</returns>
    public bool IsSameDay(long time1, long time2) {
        return ToEpochDay(time1) == ToEpochDay(time2);
    }

    /// <summary>
    /// 计算两个时间戳相差的天数
    /// </summary>
    /// <param name="time1">第一个时间戳</param>
    /// <param name="time2">第二个时间戳</param>
    /// <returns>同一天返回0，否则返回值大于0</returns>
    public int DifferDays(long time1, long time2) {
        return Math.Abs(ToEpochDay(time1) - ToEpochDay(time2));
    }

    /// <summary>
    /// 判断两个时间是否是同一周
    /// </summary>
    /// <param name="time1">第一个时间戳</param>
    /// <param name="time2">第二个时间戳</param>
    /// <returns>如果是同一周则返回true，否则返回false</returns>
    public bool IsSameWeek(long time1, long time2) {
        return GetBeginOfWeek(time1) == GetBeginOfWeek(time2);
    }

    /// <summary>
    /// 计算两个时间戳相差的周数
    /// </summary>
    /// <param name="time1">第一个时间戳</param>
    /// <param name="time2">第二个时间戳</param>
    /// <returns>同一周返回0，否则返回值大于0</returns>
    public int DifferWeeks(long time1, long time2) {
        long deltaTime = GetBeginOfWeek(time1) - GetBeginOfWeek(time2);
        return (int)Math.Abs(deltaTime / DatetimeUtil.MillisPerWeek);
    }

    #endregion
}
}