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

package cn.wjybxx.dson;

import cn.wjybxx.dson.io.DsonInput;
import cn.wjybxx.dson.io.DsonOutput;

/**
 * 数字类型字段的编码方式
 *
 * @author wjybxx
 * date - 2023/4/19
 */
public enum WireType {

    /**
     * 按照无符号数格式优化编码，将符号位看做数据位，再进行VarInt编码。
     * 1.该编码对正数极为友好，对负数较为糟糕。
     * 2.int32的负数，将固定占用5个字节；
     * 3.int64的负数，将固定占用10个字节；
     */
    UINT(0),

    /**
     * 按照有符号数格式优化编码，先进行ZigZag编码，再按照VarInt编码
     * 1.该编码会增加正数的平均编码长度，但减少负数的平均编码长度，适合负数值较多的数据。
     * 2.ZigZag对于正数，结果为{@code value * 2}；对于负数，结果为：{@code Math.abs(value) * 2 -1}
     */
    SINT(1),

    /**
     * 固定长度编码
     * 1.int32 固定4字节
     * 2.int64 固定8字节
     */
    FIXED(2);

    private final int number;

    WireType(int number) {
        this.number = number;
    }

    public int getNumber() {
        return number;
    }

    public static WireType forNumber(int number) {
        return switch (number) {
            case 0 -> UINT;
            case 1 -> SINT;
            case 2 -> FIXED;
            default -> throw new IllegalArgumentException("invalid wireType " + number);
        };
    }

    // region read/write

    public final void writeInt32(DsonOutput output, int value) {
        switch (this) {
            case UINT -> output.writeUInt32(value);
            case SINT -> output.writeSInt32(value);
            case FIXED -> output.writeFixed32(value);
        }
    }

    public final int readInt32(DsonInput input) {
        return switch (this) {
            case UINT -> input.readUInt32();
            case SINT -> input.readSInt32();
            case FIXED -> input.readFixed32();
        };
    }

    public final void writeInt64(DsonOutput output, long value) {
        switch (this) {
            case UINT -> output.writeUInt64(value);
            case SINT -> output.writeSInt64(value);
            case FIXED -> output.writeFixed64(value);
        }
    }

    public final long readInt64(DsonInput input) {
        return switch (this) {
            case UINT -> input.readUInt64();
            case SINT -> input.readSInt64();
            case FIXED -> input.readFixed64();
        };
    }

    public final void writeFloat(DsonOutput output, float value) {
        if (this == UINT) {
            output.writeVarFloat(value);
        } else {
            output.writeFloat(value);
        }
    }

    public final float readFloat(DsonInput input) {
        return this == UINT ? input.readVarFloat() : input.readFloat();
    }

    public final void writeDouble(DsonOutput output, double value) {
        if (this == UINT) {
            output.writeVarDouble(value);
        } else {
            output.writeDouble(value);
        }
    }

    public final double readDouble(DsonInput input) {
        return this == UINT ? input.readVarDouble() : input.readDouble();
    }

    // endregion

    // region 计算最佳WireType

    private static final int INT_COMPRESS_MASK = (1 << 21) - 1; // 低21位
    private static final long LONG_COMPRESS_MASK = (1L << 49) - 1; // 低49位
    private static final int FLOAT_COMPRESS_MASK = ~(-1 << 11); // 高21位
    private static final long DOUBLE_COMPRESS_MASK = ~(-1L << 15); // 高49位

    /** 计算int32的最佳序列化格式 */
    public static WireType bestOfInt32(int value) {
        if (value > INT_COMPRESS_MASK) return WireType.FIXED;
        if (value > 0) return WireType.UINT;
        if (value > -(INT_COMPRESS_MASK / 2)) return WireType.SINT;
        return WireType.FIXED;
    }

    /** 计算int64的最佳序列化格式 */
    public static WireType bestOfInt64(long value) {
        if (value > LONG_COMPRESS_MASK) return WireType.FIXED;
        if (value > 0) return WireType.UINT;
        if (value > -(LONG_COMPRESS_MASK / 2)) return WireType.SINT;
        return WireType.FIXED;
    }

    /** 计算float的最佳序列化方式 */
    public static WireType bestOfFloat(float value) {
        int rawBits = Float.floatToRawIntBits(value);
        // 当变长编码的开销更小时，使用变长编码 -- Float变长编码3字节可表达21个有效位，即后11位为0
        return (rawBits & FLOAT_COMPRESS_MASK) == 0 ? WireType.UINT : WireType.FIXED;
    }

    /** 计算double的最佳序列化方式 */
    public static WireType bestOfDouble(double value) {
        long rawBits = Double.doubleToRawLongBits(value);
        // 当变长编码的开销更小时，使用变长编码 -- Double变长编码7字节可表达49个有效位，即后15位为0
        return (rawBits & DOUBLE_COMPRESS_MASK) == 0 ? WireType.UINT : WireType.FIXED;
    }
    // endregion

}