/*
 * Copyright 2023-2026 wjybxx(845740757@qq.com)
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

package cn.wjybxx.dson.types;

import java.util.Objects;

/**
 * 4位定点数（万分比）。仅用于Dson层，业务层应定义自己的定点数类型。
 *
 * @author wjybxx
 */
public final class FixedPoint4 implements Comparable<FixedPoint4> {

    public static final long SCALE = 10_000L;
    public static final FixedPoint4 ZERO = new FixedPoint4(0);
    public static final FixedPoint4 ONE = new FixedPoint4(SCALE);

    public final long rawValue;

    public FixedPoint4(long rawValue) {
        this.rawValue = rawValue;
    }

    public static FixedPoint4 fromRaw(long rawValue) {
        return new FixedPoint4(rawValue);
    }

    /** 通过整数构造，超出表示范围时抛出ArithmeticException。 */
    public static FixedPoint4 fromInteger(long value) {
        return new FixedPoint4(Math.multiplyExact(value, SCALE));
    }

    public long toInteger() {
        return rawValue / SCALE;
    }

    public double toDouble() {
        return rawValue / (double) SCALE;
    }

    /** 整数部分（固定正数，以避免负0问题） */
    public long getIntegerPart() {
        return Math.abs(rawValue / SCALE);
    }

    /** 小数部分（固定正数，以避免负0问题） */
    public int getFractionPart() {
        return (int) Math.abs(rawValue % SCALE);
    }

    /**
     * 按double精度缩放后向零截断，不进行舍入或十进制精度修正。
     * 非有限值或缩放后超出long范围时抛出ArithmeticException。
     */
    public static FixedPoint4 fromDouble(double value) {
        double scaled = value * SCALE;
        // Long.MAX_VALUE转double会变成2^63，因此上界必须是开区间。
        if (!Double.isFinite(scaled) || scaled < -0x1p63 || scaled >= 0x1p63) {
            throw new ArithmeticException("FixedPoint4 overflow: " + value);
        }
        return new FixedPoint4((long) scaled);
    }

    /**
     * 解析普通ASCII十进制文本，允许整体符号以及1至4位小数。
     * 不接受空白字符；格式错误或整数片段溢出抛出NumberFormatException，
     * 缩放后的数值溢出抛出ArithmeticException。
     */
    public static FixedPoint4 parse(String value) {
        Objects.requireNonNull(value);
        int start = 0;
        int end = value.length();
        //
        boolean negative = start < end && value.charAt(start) == '-';
        if (start < end && (value.charAt(start) == '-' || value.charAt(start) == '+')) start++;
        int point = value.indexOf('.', start);
        int integerEnd = point == -1 ? end : point;
        int fractionDigits = point == -1 ? 0 : end - point - 1;
        if (integerEnd == start || (point != -1 && (fractionDigits < 1 || fractionDigits > 4))) {
            throw new NumberFormatException(value);
        }

        long integer = Long.parseLong(value, start, integerEnd, 10);
        long fraction = point == -1 ? 0 : Long.parseLong(value, point + 1, end, 10);
        for (int i = fractionDigits; i < 4; i++) {
            fraction *= 10;
        }
        // 负数直接相减，避免Long.MIN_VALUE的正数绝对值溢出。
        long rawValue = negative
                ? Math.subtractExact(Math.multiplyExact(-integer, SCALE), fraction)
                : Math.addExact(Math.multiplyExact(integer, SCALE), fraction);
        return new FixedPoint4(rawValue);
    }

    @Override
    public String toString() {
        long p1 = Math.abs(rawValue % SCALE);
        if (p1 == 0) {
            long p0 = rawValue / SCALE;
            return Long.toString(p0);
        }
        StringBuilder sb = new StringBuilder();
        toString0(this, sb);
        return sb.toString();
    }

    public static void toString0(FixedPoint4 fx4, StringBuilder builder) {
        if (fx4.rawValue < 0) {
            builder.append('-');
        }
        builder.append(fx4.getIntegerPart());
        //
        int fractionPart = fx4.getFractionPart();
        if (fractionPart != 0) {
            builder.append('.');
            if (fractionPart < 10) {
                builder.append("000");
            } else if (fractionPart < 100) {
                builder.append("00");
            } else if (fractionPart < 1000) {
                builder.append('0');
            }
            while (fractionPart % 10 == 0) {
                fractionPart /= 10;
            }
            builder.append(fractionPart);
        }
    }

    @Override
    public boolean equals(Object o) {
        return o instanceof FixedPoint4 that && rawValue == that.rawValue;
    }

    @Override
    public int hashCode() {
        return Long.hashCode(rawValue);
    }

    @Override
    public int compareTo(FixedPoint4 that) {
        return Long.compare(rawValue, that.rawValue);
    }
}
