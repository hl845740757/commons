/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.base.time;

import java.time.LocalDateTime;
import java.time.LocalTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.util.concurrent.TimeUnit;

/**
 * 时间工具类 -- 以毫秒为基本单位。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public class TimeUtils {

    private TimeUtils() {

    }

    /** 中国时区 */
    public static final ZoneOffset ZONE_OFFSET_CST = ZoneOffset.ofHours(8);
    /** UTC时区 */
    public static final ZoneOffset ZONE_OFFSET_UTC = ZoneOffset.UTC;
    /** 系统时区 */
    public static final ZoneOffset ZONE_OFFSET_SYSTEM = ZoneOffset.systemDefault().getRules().getOffset(LocalDateTime.now());

    /** 1毫秒的纳秒数 */
    public static final long NANOS_PER_MILLI = 1000_000L;
    /** 1秒的纳秒数 */
    public static final long NANOS_PER_SECOND = 1000_000_000L;
    /** 1分钟的纳秒数 */
    public static final long NANOS_PER_MINUTES = NANOS_PER_SECOND * 60L;
    /** 1小时的纳秒数 */
    public static final long NANOS_PER_HOURS = NANOS_PER_MINUTES * 60L;
    /** 1天的纳秒数 */
    public static final long NANOS_PER_DAY = NANOS_PER_HOURS * 24L;

    /** 1秒的毫秒数 */
    public static final int MILLIS_PER_SECOND = 1000;
    /** 1分钟的毫秒数 */
    public static final int MILLIS_PER_MINUTE = 60 * MILLIS_PER_SECOND;
    /** 1小时的毫秒数 */
    public static final int MILLIS_PER_HOUR = 60 * MILLIS_PER_MINUTE;
    /** 1天的毫秒数 */
    public static final int MILLIS_PER_DAY = 24 * MILLIS_PER_HOUR;
    /** 1周的毫秒数 */
    public static final int MILLIS_PER_WEEK = 7 * MILLIS_PER_DAY;

    /** 1分钟的秒数 */
    public static final int SECONDS_PER_MINUTE = 60;
    /** 1小时的秒数 */
    public static final int SECONDS_PER_HOUR = 3600;
    /** 1天的秒数 */
    public static final int SECONDS_PER_DAY = 3600 * 24;
    /** 1周的秒数 */
    public static final int SECONDS_PER_WEEK = SECONDS_PER_DAY * 7;

    /** 1天的小时数 */
    public static final int HOURS_PER_DAY = 24;
    /** 1周的小时数 */
    public static final int HOURS_PER_WEEK = 24 * 7;

    /**
     * 一天的开始：午夜 00:00:00
     * The time of midnight at the start of the day, '00:00'.
     */
    public static final LocalTime START_OF_DAY = LocalTime.MIN;
    /**
     * 一天的结束：午夜 23:59:59
     */
    public static final LocalTime END_OF_DAY = LocalTime.MAX;

    /** 默认的时间格式 */
    public static final String DEFAULT_PATTERN = "yyyy-MM-dd HH:mm:ss";
    /** 默认时间格式器 */
    public static final DateTimeFormatter DEFAULT_FORMATTER = DateTimeFormatter.ofPattern(DEFAULT_PATTERN);
    /** 年月日的格式化器 */
    public static final DateTimeFormatter YYYY_MM_DD = DateTimeFormatter.ofPattern("yyyy-MM-dd");
    /** 时分秒的格式化器 */
    public static final DateTimeFormatter HH_MM_SS = DateTimeFormatter.ofPattern("HH:mm:ss");
    /** 时分的格式化器 */
    public static final DateTimeFormatter HH_MM = DateTimeFormatter.ofPattern("HH:mm");

    public static long toEpochMillis(LocalDateTime dateTime) {
        final long millis = dateTime.getNano() / TimeUtils.NANOS_PER_MILLI;
        return dateTime.toEpochSecond(ZoneOffset.UTC) * 1000L + millis;
    }

    public LocalDateTime toDateTime(long epochMilli) {
        final long extraMilli = epochMilli % 1000;
        final int nanoOfSecond = (int) (extraMilli * TimeUtils.NANOS_PER_MILLI);
        return LocalDateTime.ofEpochSecond(epochMilli / 1000, nanoOfSecond, ZoneOffset.UTC);
    }

    public static long toMillisOfDay(LocalTime time) {
        return time.toSecondOfDay() * 1000L + time.getNano() / NANOS_PER_MILLI;
    }

    public static int toSecondOfDay(LocalTime time) {
        return time.toSecondOfDay();
    }

    /**
     * 将秒时间和毫秒时间合并为毫秒时间
     *
     * @param seconds 时间的秒部分
     * @param millis  时间的毫秒部分
     */
    public static long toMillis(long seconds, long millis) {
        return seconds * 1000 + millis;
    }

    /** 获取月份的天数，总是忘记api... */
    public static int lengthOfMonth(LocalDateTime localDateTime) {
        return localDateTime.toLocalDate().lengthOfMonth();
    }

    /** 获取时间单位的字符串缩写 */
    public static String abbreviate(TimeUnit unit) {
        return switch (unit) {
            case NANOSECONDS -> "ns";
            case MICROSECONDS -> "μs";
            case MILLISECONDS -> "ms";
            case SECONDS -> "s";
            case MINUTES -> "min";
            case HOURS -> "h";
            case DAYS -> "d";
            default -> throw new AssertionError();
        };
    }
}