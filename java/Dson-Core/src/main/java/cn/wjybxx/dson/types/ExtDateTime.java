package cn.wjybxx.dson.types;

import cn.wjybxx.dson.DsonLites;
import cn.wjybxx.dson.internal.DsonInternals;

import javax.annotation.concurrent.Immutable;
import java.time.*;
import java.time.format.DateTimeFormatter;

/**
 * 日期时间
 *
 * @author wjybxx
 * date - 2023/6/17
 */
@Immutable
public final class ExtDateTime {

    public static final byte MASK_NONE = 0;
    public static final byte MASK_DATE = 1;
    public static final byte MASK_TIME = 1 << 1;
    public static final byte MASK_OFFSET = 1 << 2;

    public static final byte MASK_DATETIME = MASK_DATE | MASK_TIME;
    public static final byte MASK_DATETIME_OFFSET = MASK_DATE | MASK_TIME | MASK_OFFSET;
    public static final byte MASK_ALL = MASK_DATETIME_OFFSET;

    /** 纪元时间-秒 */
    private final long seconds;
    /** 纪元时间的纳秒部分 */
    private final int nanos;
    /** 时区偏移-秒 */
    private final int offset;
    /** 哪些字段有效 */
    private final byte enables;

    /**
     * 该接口慎用，通常我们需要精确到毫秒
     *
     * @param seconds 纪元时间-秒
     */
    public ExtDateTime(long seconds) {
        this(seconds, 0, 0, MASK_DATETIME);
    }

    public ExtDateTime(long seconds, int nanos, int offset, byte enables) {
        if ((enables & MASK_ALL) != enables) {
            throw new IllegalArgumentException("invalid enables: " + enables);
        }
        if (seconds != 0 && !DsonInternals.isAnySet(enables, MASK_DATETIME)) {
            throw new IllegalArgumentException("date and time are disabled, but seconds is not 0");
        }
        if (nanos != 0 && !DsonInternals.isSet(enables, MASK_TIME)) {
            throw new IllegalArgumentException("time is disabled, but nanos is not 0");
        }
        if (offset != 0 && !DsonInternals.isSet(enables, MASK_OFFSET)) {
            throw new IllegalArgumentException("offset is disabled, but the value is not 0");
        }
        Timestamp.validateNanos(nanos);
        this.seconds = seconds;
        this.nanos = nanos;
        this.offset = offset;
        this.enables = enables;
    }

    public static ExtDateTime ofDateTime(LocalDateTime localDateTime) {
        long epochSecond = localDateTime.toEpochSecond(ZoneOffset.UTC);
        int nanos = localDateTime.getNano();
        return new ExtDateTime(epochSecond, nanos, 0, MASK_DATETIME);
    }

    public LocalDateTime toDateTime() {
        return LocalDateTime.ofEpochSecond(seconds, nanos, ZoneOffset.UTC);
    }

    public static ExtDateTime ofInstant(Instant instant) {
        return new ExtDateTime(instant.getEpochSecond(), instant.getNano(), 0, MASK_DATETIME);
    }

    public ExtDateTime withOffset(int offset) {
        return new ExtDateTime(seconds, nanos, offset, (byte) (enables | MASK_OFFSET));
    }

    public ExtDateTime withoutOffset() {
        return new ExtDateTime(seconds, nanos, 0, (byte) (enables & MASK_DATETIME));
    }

    // region

    public long getSeconds() {
        return seconds;
    }

    public int getNanos() {
        return nanos;
    }

    public int getOffset() {
        return offset;
    }

    public byte getEnables() {
        return enables;
    }

    public boolean hasDate() {
        return DsonInternals.isSet(enables, MASK_DATE);
    }

    public boolean hasTime() {
        return DsonInternals.isSet(enables, MASK_TIME);
    }

    public boolean hasOffset() {
        return DsonInternals.isSet(enables, MASK_OFFSET);
    }

    public boolean hasFields(byte mask) {
        return DsonInternals.isSet(enables, mask);
    }

    public boolean canBeAbbreviated() {
        return nanos == 0 && (enables == MASK_DATETIME);
    }

    public boolean canConvertNanosToMillis() {
        return (nanos % 1000_000) == 0;
    }

    public int convertNanosToMillis() {
        return nanos / 1000_000;
    }
    // endregion

    //region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        ExtDateTime offsetDateTime = (ExtDateTime) o;

        if (seconds != offsetDateTime.seconds) return false;
        if (nanos != offsetDateTime.nanos) return false;
        if (offset != offsetDateTime.offset) return false;
        return enables == offsetDateTime.enables;
    }

    @Override
    public int hashCode() {
        int result = (int) (seconds ^ (seconds >>> 32));
        result = 31 * result + nanos;
        result = 31 * result + offset;
        result = 31 * result + enables;
        return result;
    }

    // endregion

    @Override
    public String toString() {
        StringBuilder sb = new StringBuilder();
        sb.append("ExtDateTime{");
        if (hasDate()) {
            sb.append("date: '").append(formatDate(seconds));
        }
        if (hasTime()) {
            if (hasDate()) {
                sb.append(", ");
            }
            sb.append("time: '").append(formatTime(seconds));
        }
        if (nanos != 0) {
            sb.append(", ");
            if (canConvertNanosToMillis()) {
                sb.append("millis: ").append(convertNanosToMillis());
            } else {
                sb.append("nanos: ").append(nanos);
            }
        }
        if (hasOffset()) {
            sb.append(", ");
            sb.append("offset: '").append(formatOffset(offset))
                    .append("'");
        }
        return sb.append('}')
                .toString();
    }

    // region parse/format

    /** @return 固定格式 yyyy-MM-dd */
    public static String formatDate(long epochSecond) {
        return LocalDateTime.ofEpochSecond(epochSecond, 0, ZoneOffset.UTC)
                .toLocalDate()
                .toString();
    }

    /** @return 固定格式 HH:mm:ss */
    public static String formatTime(long epochSecond) {
        return LocalDateTime.ofEpochSecond(epochSecond, 1, ZoneOffset.UTC)
                .toLocalTime()
                .toString()
                .substring(0, 8);
    }

    /** @return 固定格式 yyyy-MM-dd'T'HH:mm:ss */
    public static String formatDateTime(long epochSecond) {
        return formatDate(epochSecond) + "T" + formatTime(epochSecond);
    }

    /** @param dateString 限定格式 yyyy-MM-dd */
    public static LocalDate parseDate(String dateString) {
//        if (dateString.length() != 10) throw new IllegalArgumentException("invalid dateString " + dateString);
        return LocalDate.parse(dateString, DateTimeFormatter.ISO_DATE);
    }

    /** @param timeString 限定格式 HH:mm:ss */
    public static LocalTime parseTime(String timeString) {
        if (timeString.length() != 8) throw new IllegalArgumentException("invalid timeString " + timeString);
        return LocalTime.parse(timeString, DateTimeFormatter.ISO_TIME);
    }

    /** @param timeString 限定格式 yyyy-MM-dd'T'HH:mm:ss */
    public static LocalDateTime parseDateTime(String timeString) {
        return LocalDateTime.parse(timeString, DateTimeFormatter.ISO_DATE_TIME);
    }

    /**
     * Z, ±HH:mm, ±HH:mm:ss
     */
    public static String formatOffset(int offsetSeconds) {
        if (offsetSeconds == 0) {
            return "Z";
        }
        String pre = offsetSeconds < 0 ? "-" : "+";
        return pre + LocalTime.ofSecondOfDay(Math.abs(offsetSeconds)).toString();
    }

    /**
     * Z, ±H, ±HH, ±HH:mm, ±HH:mm:ss
     */
    public static int parseOffset(String offsetString) {
        return switch (offsetString.length()) {
            case 1, 2, 3, 6, 9 -> ZoneOffset.of(offsetString).getTotalSeconds();
            default -> throw new IllegalArgumentException("invalid offset string " + offsetString);
        };
    }

    // endregion

    // region 常量
    public static final String NAMES_DATE = "date";
    public static final String NAMES_TIME = "time";
    public static final String NAMES_MILLIS = "millis";

    public static final String NAMES_SECONDS = "seconds";
    public static final String NAMES_NANOS = "nanos";
    public static final String NAMES_OFFSET = "offset";
    public static final String NAMES_ENABLES = "enables";

    public static final int NUMBERS_SECONDS = DsonLites.makeFullNumberZeroIdep(0);
    public static final int NUMBERS_NANOS = DsonLites.makeFullNumberZeroIdep(1);
    public static final int NUMBERS_OFFSET = DsonLites.makeFullNumberZeroIdep(2);
    public static final int NUMBERS_ENABLES = DsonLites.makeFullNumberZeroIdep(3);

    // endregion
}