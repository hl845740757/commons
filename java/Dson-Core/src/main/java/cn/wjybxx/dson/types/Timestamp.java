package cn.wjybxx.dson.types;

import cn.wjybxx.base.time.TimeUtils;
import cn.wjybxx.dson.DsonLites;
import cn.wjybxx.dson.text.DsonTexts;

import javax.annotation.concurrent.Immutable;
import java.time.Duration;
import java.time.Instant;

/**
 * 日期时间
 *
 * @author wjybxx
 * date - 2023/6/17
 */
@Immutable
public final class Timestamp {

    /** 纪元时间-秒 */
    private final long seconds;
    /** 纪元时间的纳秒部分 */
    private final int nanos;

    /**
     * @param seconds 纪元时间-秒
     */
    public Timestamp(long seconds) {
        this(seconds, 0);
    }

    public Timestamp(long seconds, int nanos) {
        validateNanos(nanos);
        this.seconds = seconds;
        this.nanos = nanos;
    }

    // region

    public long getSeconds() {
        return seconds;
    }

    public int getNanos() {
        return nanos;
    }


    // endregion

    // region

    /**
     * 解析时间戳字符串。
     * 如果字符串以ms结尾，表示毫秒时间戳，否则表示秒时间戳。
     */
    public static Timestamp parse(String rawStr) {
        String str = DsonTexts.deleteUnderline(rawStr);
        if (str.isEmpty()) {
            throw new IllegalArgumentException(rawStr);
        }
        int length = str.length();
        if (length > 2 && str.charAt(length - 1) == 's' && str.charAt(length - 2) == 'm') {
            long epochMillis = Long.parseLong(str, 0, length - 2, 10);
            return ofEpochMillis(epochMillis);
        }
        long seconds = Long.parseLong(str);
        return new Timestamp(seconds, 0);
    }

    public static Timestamp ofInstant(Instant instant) {
        return new Timestamp(instant.getEpochSecond(), instant.getNano());
    }

    public Instant toInstant() {
        return Instant.ofEpochSecond(seconds, nanos);
    }

    public static Timestamp ofDuration(Duration duration) {
        return new Timestamp(duration.getSeconds(), duration.getNano());
    }

    public Duration toDuration() {
        return Duration.ofSeconds(seconds, nanos);
    }

    /** 通过纪元毫秒时间戳构建Timestamp */
    public static Timestamp ofEpochMillis(long epochMillis) {
        long seconds = epochMillis / 1000;
        int nanos = (int) (epochMillis % 1000 * TimeUtils.NANOS_PER_MILLI);
        return new Timestamp(seconds, nanos);
    }

    /** 转换为纪元毫秒时间戳 */
    public long toEpochMillis() {
        return (seconds * 1000) + (nanos / 1000_000);
    }

    public boolean canConvertNanosToMillis() {
        return (nanos % 1000_000) == 0;
    }

    public int convertNanosToMillis() {
        return nanos / 1000_000;
    }

    // endregion

    // region util

    /** 检查nanos的范围 */
    public static void validateNanos(int nanos) {
        if (nanos > 999_999_999 || nanos < 0) {
            throw new IllegalArgumentException("nanos > 999999999 or < 0");
        }
    }

    /** 检测毫秒的范围 */
    public static void validateMillis(int millis) {
        if (millis > 999 || millis < 0) {
            throw new IllegalArgumentException("millis > 999 or < 0");
        }
    }
    // endregion

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        Timestamp timestamp = (Timestamp) o;
        if (seconds != timestamp.seconds) return false;
        return nanos == timestamp.nanos;
    }

    @Override
    public int hashCode() {
        int result = (int) (seconds ^ (seconds >>> 32));
        result = 31 * result + nanos;
        return result;
    }

    // endregion

    @Override
    public String toString() {
        return "Timestamp{" +
                "seconds=" + seconds +
                ", nanos=" + nanos +
                '}';
    }

    // region 常量
    public static final String NAMES_SECONDS = "seconds";
    public static final String NAMES_NANOS = "nanos";
    public static final String NAMES_MILLIS = "millis";

    public static final int NUMBERS_SECONDS = DsonLites.makeFullNumberZeroIdep(0);
    public static final int NUMBERS_NANOS = DsonLites.makeFullNumberZeroIdep(1);

    // endregion
}