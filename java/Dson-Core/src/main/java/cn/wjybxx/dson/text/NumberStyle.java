package cn.wjybxx.dson.text;

import java.text.DecimalFormat;
import java.text.DecimalFormatSymbols;
import java.util.ArrayList;
import java.util.Locale;

/**
 * 数字的打印格式
 * 考虑到扩展性，改为普通类。
 *
 * @author wjybxx
 * date - 2023/6/19
 */
public final class NumberStyle {

    public final int features;

    public NumberStyle(int features) {
        this.features = features;
    }

    // region factory

    public static final int MASK_SIMPLE = 0;
    public static final int MASK_HEX = 0x01;
    public static final int MASK_BINARY = 0x02;

    public static final int MASK_TYPED = 0x10;
    public static final int MASK_UNSIGNED = 0x20;
    public static final int MASK_FIXED = 0x40;

    public static final int MASK_NO_EXPONENT3 = 0x01 << 8;
    public static final int MASK_NO_EXPONENT7 = 0x02 << 8;
    public static final int MASK_RADIXES = 0x0F;

    /** 普通模式 */
    public static final NumberStyle SIMPLE = new NumberStyle(MASK_SIMPLE);
    /** 固定打印类型 */
    public static final NumberStyle TYPED = new NumberStyle(MASK_TYPED);
    /** 输出为无符号整数 -- 超出范围时自动追加类型 */
    public static final NumberStyle UNSIGNED = new NumberStyle(MASK_UNSIGNED);

    /** 16进制 */
    public static final NumberStyle HEX = new NumberStyle(MASK_HEX);
    /** 输出为无符号16进制 */
    public static final NumberStyle UNSIGNED_HEX = new NumberStyle(MASK_UNSIGNED | MASK_HEX);
    /** 固定长度的16进制 */
    public static final NumberStyle FIXED_HEX = new NumberStyle(MASK_FIXED | MASK_HEX);

    /** 2进制 */
    public static final NumberStyle BINARY = new NumberStyle(MASK_BINARY);
    /** 输出为无符号2进制 */
    public static final NumberStyle UNSIGNED_BINARY = new NumberStyle(MASK_UNSIGNED | MASK_BINARY);
    /** 固定长度的2进制 */
    public static final NumberStyle FIXED_BINARY = new NumberStyle(MASK_FIXED | MASK_BINARY);

    /** 浮点数禁用科学计数法，并最多保留小数点后3位(向最近的偶数舍入) -- 可能导致反序列化结果不相等 */
    public static final NumberStyle NO_EXPONENT3 = new NumberStyle(MASK_NO_EXPONENT3);
    /** 浮点数禁用科学计数法，并最多保留小数点后7位(向最近的偶数舍入) -- 可能导致反序列化结果不相等 */
    public static final NumberStyle NO_EXPONENT7 = new NumberStyle(MASK_NO_EXPONENT7);

    public static final NumberStyle TYPED_NO_EXPONENT3 = new NumberStyle(MASK_TYPED | MASK_NO_EXPONENT3);
    public static final NumberStyle TYPED_NO_EXPONENT7 = new NumberStyle(MASK_TYPED | MASK_NO_EXPONENT7);

    /** 打印为16进制，必定追加类型 */
    public NumberStyle withHex() {
        return new NumberStyle(features | MASK_HEX);
    }

    /** 打印为二进制，必定追加类型 */
    public NumberStyle withBinary() {
        return new NumberStyle(features | MASK_BINARY);
    }

    /** 固定打印类型 */
    public NumberStyle withTyped() {
        return new NumberStyle(features | MASK_TYPED);
    }

    /** 打印为无符号数，超出范围时追加类型 */
    public NumberStyle withUnsigned() {
        return new NumberStyle(features | MASK_UNSIGNED);
    }

    /** 固定长度编码（全Bit编码），适用十六进制和二进制 */
    public NumberStyle withFixed() {
        return new NumberStyle(features | MASK_FIXED);
    }

    /** 浮点数禁用科学计数法，并最多保留小数点后3位 -- 可能导致反序列化结果不相等 */
    public NumberStyle withNoExponent3() {
        return new NumberStyle(features | MASK_NO_EXPONENT3);
    }

    /** 浮点数禁用科学计数法，并最多保留小数点后7位 -- 可能导致反序列化结果不相等 */
    public NumberStyle withNoExponent7() {
        return new NumberStyle(features | MASK_NO_EXPONENT7);
    }

    // endrgion

    // region toString

    public StyleOut toString(int value, StyleOut styleOut) {
        int radix = features & NumberStyle.MASK_RADIXES;
        switch (radix) {
            case MASK_HEX: {
                // 16进制
                if ((features & NumberStyle.MASK_FIXED) != 0) {
                    return styleOut.setValue(String.format("0x%08X", value), true);
                }
                if ((features & NumberStyle.MASK_UNSIGNED) != 0) {
                    return styleOut.setValue("0x" + Integer.toHexString(value), true);
                }
                if (value < 0 && value != Integer.MIN_VALUE) {
                    return styleOut.setValue("-0x" + Integer.toHexString((-1 * value)), true);
                } else {
                    return styleOut.setValue("0x" + Integer.toHexString(value), true);
                }
            }
            case MASK_BINARY: {
                // 2进制
                if ((features & NumberStyle.MASK_FIXED) != 0) {
                    return styleOut.setValue("0b" + ToFixedBinaryString(value), true);
                }
                if ((features & NumberStyle.MASK_UNSIGNED) != 0) {
                    return styleOut.setValue("0b" + ToBinaryString(value), true);
                }
                if (value < 0 && value != Integer.MIN_VALUE) {
                    return styleOut.setValue("-0b" + ToBinaryString(-1 * value), true);
                } else {
                    return styleOut.setValue("0b" + ToBinaryString(value), true);
                }
            }
            default: {
                // 10进制
                if ((features & NumberStyle.MASK_UNSIGNED) != 0) {
                    return styleOut.setValue(Integer.toUnsignedString(value), true);
                }
                boolean isTyped = (features & NumberStyle.MASK_TYPED) != 0 || Math.abs(value) >= DOUBLE_MAX_LONG;
                return styleOut.setValue(Integer.toString(value), isTyped);
            }
        }
    }

    public StyleOut toString(long value, StyleOut styleOut) {
        int radix = features & NumberStyle.MASK_RADIXES;
        switch (radix) {
            case MASK_HEX: {
                // 16进制
                if ((features & NumberStyle.MASK_FIXED) != 0) {
                    return styleOut.setValue(String.format("0x%016X", value), true);
                }
                if ((features & NumberStyle.MASK_UNSIGNED) != 0) {
                    return styleOut.setValue("0x" + Long.toHexString(value), true);
                }
                if (value < 0 && value != Long.MIN_VALUE) {
                    return styleOut.setValue("-0x" + Long.toHexString((-1 * value)), true);
                } else {
                    return styleOut.setValue("0x" + Long.toHexString(value), true);
                }
            }
            case MASK_BINARY: {
                // 2进制
                if ((features & NumberStyle.MASK_FIXED) != 0) {
                    return styleOut.setValue("0b" + ToFixedBinaryString(value), true);
                }
                if ((features & NumberStyle.MASK_UNSIGNED) != 0) {
                    return styleOut.setValue("0b" + ToBinaryString(value), true);
                }
                if (value < 0 && value != Long.MIN_VALUE) {
                    return styleOut.setValue("-0b" + ToBinaryString(-1 * value), true);
                } else {
                    return styleOut.setValue("0b" + ToBinaryString(value), true);
                }
            }
            default: {
                // 10进制
                if ((features & NumberStyle.MASK_UNSIGNED) != 0) {
                    return styleOut.setValue(Long.toUnsignedString(value), true);
                }
                boolean isTyped = (features & NumberStyle.MASK_TYPED) != 0 || Math.abs(value) >= DOUBLE_MAX_LONG;
                return styleOut.setValue(Long.toString(value), isTyped);
            }
        }
    }

    public StyleOut toString(float value, StyleOut styleOut) {
        if (Float.isNaN(value) || Float.isInfinite(value)) {
            return styleOut.setValue(Float.toString(value), true);
        }
        boolean isTyped = (features & NumberStyle.MASK_TYPED) != 0;
        int lv = (int) value;
        if (lv == value) {
            return styleOut.setValue(Integer.toString(lv), isTyped);
        } else {
            String str;
            if ((features & NumberStyle.MASK_NO_EXPONENT3) != 0) {
                str = NO_EXPONENT_3.format(value);
            } else if ((features & NumberStyle.MASK_NO_EXPONENT7) != 0) {
                str = NO_EXPONENT_7.format(value);
            } else {
                str = Float.toString(value);
                isTyped |= isTyped || str.indexOf('E') >= 0;
            }
            // 数字截断问题
            if (str.equals("-0")) str = "0";
            return styleOut.setValue(str, isTyped);
        }
    }

    public StyleOut toString(double value, StyleOut styleOut) {
        if (Double.isNaN(value) || Double.isInfinite(value)) {
            return styleOut.setValue(Double.toString(value), true);
        }
        boolean isTyped = (features & NumberStyle.MASK_TYPED) != 0;
        long lv = (long) value;
        if (lv == value) {
            return styleOut.setValue(Long.toString(lv), isTyped);
        } else {
            String str;
            if ((features & NumberStyle.MASK_NO_EXPONENT3) != 0) {
                str = NO_EXPONENT_3.format(value);
            } else if ((features & NumberStyle.MASK_NO_EXPONENT7) != 0) {
                str = NO_EXPONENT_7.format(value);
            } else {
                str = Double.toString(value);
                isTyped |= isTyped || str.indexOf('E') >= 0;
            }
            // 数字截断问题
            if (str.equals("-0")) str = "0";
            return styleOut.setValue(str, isTyped);
        }
    }

    // endregion


    // region internal

    /** double能精确表示的最大整数 */
    private static final long DOUBLE_MAX_LONG = (1L << 53) - 1;

    /// <summary>
    /// 转2进制，长度补全为8的倍数
    /// </summary>
    private static String ToBinaryString(int value) {
        String binaryString = Integer.toBinaryString(value);
        int mod = binaryString.length() % 8;
        if (mod != 0) {
            binaryString = "0".repeat(8 - mod) + binaryString;
        }
        return binaryString;
    }

    private static String ToBinaryString(long value) {
        String binaryString = Long.toBinaryString(value);
        int mod = binaryString.length() % 8;
        if (mod != 0) {
            binaryString = "0".repeat(8 - mod) + binaryString;
        }
        return binaryString;
    }

    /// <summary>
    /// 转2进制，固定32位
    /// </summary>
    private static String ToFixedBinaryString(int value) {
        String binaryString = Integer.toBinaryString(value);
        int pad = 32 - binaryString.length();
        if (pad > 0) {
            binaryString = "0".repeat(pad) + binaryString;
        }
        return binaryString;
    }

    private static String ToFixedBinaryString(long value) {
        String binaryString = Long.toBinaryString(value);
        int pad = 64 - binaryString.length();
        if (pad > 0) {
            binaryString = "0".repeat(pad) + binaryString;
        }
        return binaryString;
    }

    @Override
    public String toString() {
        ArrayList<String> values = new ArrayList<>(4);
        if ((features & MASK_HEX) != 0) {
            values.add("hex");
        }
        if ((features & MASK_BINARY) != 0) {
            values.add("binary");
        }
        if ((features & MASK_TYPED) != 0) {
            values.add("typed");
        }
        if ((features & MASK_UNSIGNED) != 0) {
            values.add("unsigned");
        }
        if ((features & MASK_FIXED) != 0) {
            values.add("fixed");
        }
        if ((features & MASK_NO_EXPONENT3) != 0) {
            values.add("noExponent3");
        }
        if ((features & MASK_NO_EXPONENT7) != 0) {
            values.add("noExponent7");
        }
        return String.join("|", values);
    }

    private static final DecimalFormat NO_EXPONENT_3;
    private static final DecimalFormat NO_EXPONENT_7;
    private static final DecimalFormat NO_EXPONENT_17;

    static {
        DecimalFormatSymbols symbols =
                DecimalFormatSymbols.getInstance(Locale.ROOT);

        NO_EXPONENT_3 = new DecimalFormat("0.###", symbols);
        NO_EXPONENT_7 = new DecimalFormat("0.#######", symbols);
        NO_EXPONENT_17 = new DecimalFormat("0.#################", symbols);

        // 禁用科学计数法
        NO_EXPONENT_3.setMaximumFractionDigits(3);
        NO_EXPONENT_7.setMaximumFractionDigits(7);
        NO_EXPONENT_17.setMaximumFractionDigits(17);

        // 向最近的偶数舍入 - 与 C#/.NET 行为对齐
        NO_EXPONENT_3.setRoundingMode(java.math.RoundingMode.HALF_EVEN);
        NO_EXPONENT_7.setRoundingMode(java.math.RoundingMode.HALF_EVEN);
        NO_EXPONENT_17.setRoundingMode(java.math.RoundingMode.HALF_EVEN);
    }
    // endregion
}