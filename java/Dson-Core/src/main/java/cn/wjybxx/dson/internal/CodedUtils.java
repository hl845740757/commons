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

package cn.wjybxx.dson.internal;

import cn.wjybxx.base.mutable.MutableInt;
import cn.wjybxx.dson.io.DsonIOException;

/**
 * 以下参考自protobuf，以避免引入PB
 *
 * @author wjybxx
 * date 2023/3/31
 */
@SuppressWarnings("unused")
public final class CodedUtils {

    private CodedUtils() {
    }

    private static final int INT_CODED_MASK1 = -1 << 7; // 低7位0
    private static final int INT_CODED_MASK2 = -1 << 14; // 低14位0
    private static final int INT_CODED_MASK3 = -1 << 21;
    private static final int INT_CODED_MASK4 = -1 << 28;

    private static final long LONG_CODED_MASK1 = -1L << 8; // 低8位0
    private static final long LONG_CODED_MASK2 = -1L << 15; // 低15位0
    private static final long LONG_CODED_MASK3 = -1L << 22;
    private static final long LONG_CODED_MASK4 = -1L << 29;
    private static final long LONG_CODED_MASK5 = -1L << 36;
    private static final long LONG_CODED_MASK6 = -1L << 43;
    private static final long LONG_CODED_MASK7 = -1L << 50;
    private static final long LONG_CODED_MASK8 = -1L << 57;

    public static final int MAX_VAR_INT32_LENGTH = 5; // 7 * 5 = 35
    public static final int MAX_VAR_INT64_LENGTH = 10; // 7 * 9 = 63
    public static final int MAX_VAR_FLOAT32_LENGTH = 5; // 8 + 7 * 4
    public static final int MAX_VAR_FLOAT64_LENGTH = 9; // 8 + 7 * 8 =64

    /** 计算原始的32位变长整形的编码长度 -- 也可直接通过前导0个数计算 */
    public static int computeRawVarInt32Size(int value) {
        if ((value & INT_CODED_MASK1) == 0) return 1; // 所有高位为0
        if ((value & INT_CODED_MASK2) == 0) return 2;
        if ((value & INT_CODED_MASK3) == 0) return 3;
        if ((value & INT_CODED_MASK4) == 0) return 4;
        return 5;
    }

    /** 计算原始的64位变长整形的编码长度 -- 也可直接通过前导0个数计算 */
    public static int computeRawVarInt64Size(long value) {
        if ((value & LONG_CODED_MASK2) == 0) return 2;
        if ((value & LONG_CODED_MASK3) == 0) return 3;
        if ((value & LONG_CODED_MASK4) == 0) return 4;
        if ((value & LONG_CODED_MASK5) == 0) return 5;
        if ((value & LONG_CODED_MASK6) == 0) return 6;
        if ((value & LONG_CODED_MASK7) == 0) return 7;
        if ((value & LONG_CODED_MASK8) == 0) return 8;
        return 9;
    }

    public static int encodeZigZag32(int n) {
        return (n << 1) ^ (n >> 31);
    }

    public static int decodeZigZag32(final int n) {
        return (n >>> 1) ^ -(n & 1);
    }

    public static long encodeZigZag64(long n) {
        return (n << 1) ^ (n >> 63);
    }

    public static long decodeZigZag64(final long n) {
        return (n >>> 1) ^ -(n & 1);
    }

    //region protobuf decode

    public static int readUInt32(byte[] buffer, int pos, MutableInt newPos) {
        return readRawVarInt32(buffer, pos, newPos);
    }

    public static long readUInt64(byte[] buffer, int pos, MutableInt newPos) {
        return readRawVarInt64(buffer, pos, newPos);
    }

    public static int readSInt32(byte[] buffer, int pos, MutableInt newPos) {
        int rawBits = readRawVarInt32(buffer, pos, newPos);
        return decodeZigZag32(rawBits);
    }

    public static long readSInt64(byte[] buffer, int pos, MutableInt newPos) {
        long rawBits = readRawVarInt64(buffer, pos, newPos);
        return decodeZigZag64(rawBits);
    }

    public static int readFixed16(byte[] buffer, int pos, MutableInt newPos) {
        return readRawFixed16(buffer, pos, newPos);
    }

    public static int readFixed32(byte[] buffer, int pos, MutableInt newPos) {
        return readRawFixed32(buffer, pos, newPos);
    }

    public static long readFixed64(byte[] buffer, int pos, MutableInt newPos) {
        return readRawFixed64(buffer, pos, newPos);
    }

    //-------------------
    public static float readFloat(byte[] buffer, int pos, MutableInt newPos) {
        int rawBits = readRawFixed32(buffer, pos, newPos);
        return Float.intBitsToFloat(rawBits);
    }

    public static float readVarFloat(byte[] buffer, int pos, MutableInt newPos) {
        int rawBits = readRawVarFloat32(buffer, pos, newPos);
        return Float.intBitsToFloat(rawBits);
    }

    public static double readDouble(byte[] buffer, int pos, MutableInt newPos) {
        long rawBits = readRawFixed64(buffer, pos, newPos);
        return Double.longBitsToDouble(rawBits);
    }

    public static double readVarDouble(byte[] buffer, int pos, MutableInt newPos) {
        long rawBits = readRawVarFloat64(buffer, pos, newPos);
        return Double.longBitsToDouble(rawBits);
    }

    //-------------------
    private static int readRawFixed16(byte[] buffer, int pos, MutableInt newPos) {
        int r = (((buffer[pos] & 0xFF))
                | ((buffer[pos + 1] & 0xFF) << 8));
        newPos.setValue(pos + 2);
        return r;
    }

    private static int readRawFixed32(byte[] buffer, int pos, MutableInt newPos) {
        int r = (((buffer[pos] & 0xFF))
                | ((buffer[pos + 1] & 0xFF) << 8)
                | ((buffer[pos + 2] & 0xFF) << 16)
                | ((buffer[pos + 3] & 0xFF) << 24));
        newPos.setValue(pos + 4);
        return r;
    }

    private static long readRawFixed64(byte[] buffer, int pos, MutableInt newPos) {
        long r = (((buffer[pos] & 0xFFL))
                | ((buffer[pos + 1] & 0xFFL) << 8)
                | ((buffer[pos + 2] & 0xFFL) << 16)
                | ((buffer[pos + 3] & 0xFFL) << 24)
                | ((buffer[pos + 4] & 0xFFL) << 32)
                | ((buffer[pos + 5] & 0xFFL) << 40)
                | ((buffer[pos + 6] & 0xFFL) << 48)
                | ((buffer[pos + 7] & 0xFFL) << 56));
        newPos.setValue(pos + 8);
        return r;
    }

    private static int readRawVarInt32(byte[] buffer, int pos, MutableInt newPos) {
        // 循环展开
        int b = buffer[pos++];
        int r = (b & 127); // 0~6
        if (b > -1) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 7; // 7~13
        if (b > -1) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 14; // 14~20
        if (b > -1) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 21; // 21~27
        if (b > -1) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 15) << 28; // 28~31 取后4位
        if (b > -1) {
            newPos.setValue(pos);
            return r;
        }
        // 读取超过5个字节
        throw new DsonIOException("DsonInput encountered a malformed varint32.");
    }

    private static long readRawVarInt64(byte[] buffer, int pos, MutableInt newPos) {
        long r = buffer[pos++] & 0xFFL; // 低8位
        int shift = 8;
        for (int i = 0; i < 8; i++) {
            long b = buffer[pos++];
            r |= (b & 127L) << shift; // 取后7位左移
            if (b > -1L) { // 高位0
                newPos.setValue(pos);
                return r;
            }
            shift += 7;
        }
        // 读取超过9个字节
        throw new DsonIOException("DsonInput encountered a malformed varint64.");
    }

    private static int readRawVarFloat32(byte[] buffer, int pos, MutableInt newPos) {
        int r = buffer[pos++] << 24; // 31~24
        int b = buffer[pos++];
        r |= (b & 127) << 17; // 23~17
        if (b > -1) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 10; // 16~10
        if (b > -1) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 3; // 9~3
        if (b > -1) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 7); // 2~0 取后3位
        if (b > -1) {
            newPos.setValue(pos);
            return r;
        }
        // 读取超过5个字节
        throw new DsonIOException("DsonInput encountered a malformed varfloat32.");
    }

    private static long readRawVarFloat64(byte[] buffer, int pos, MutableInt newPos) {
        long r = (long) buffer[pos++] << 56; // 高8位
        int shift = 49;
        for (int i = 0; i < 8; i++) {
            long b = buffer[pos++];
            r |= (b & 127L) << shift; // 取后7位左移
            if (b > -1L) { // 高位0
                newPos.setValue(pos);
                return r;
            }
            shift -= 7;
        }
        // 读取超过9字节
        throw new DsonIOException("DsonInput encountered a malformed varfloat64.");
    }

    //endregion

    //region protobuf encode

    public static int writeUInt32(byte[] buffer, int pos, int value) {
        return writeRawVarInt32(buffer, pos, value);
    }

    public static int writeUInt64(byte[] buffer, int pos, long value) {
        return writeRawVarInt64(buffer, pos, value);
    }

    public static int writeSInt32(byte[] buffer, int pos, int value) {
        return writeRawVarInt32(buffer, pos, encodeZigZag32(value));
    }

    public static int writeSInt64(byte[] buffer, int pos, long value) {
        return writeRawVarInt64(buffer, pos, encodeZigZag64(value));
    }

    public static int writeFixed16(byte[] buffer, int pos, int value) {
        return writeRawFixed16(buffer, pos, value);
    }

    public static int writeFixed32(byte[] buffer, int pos, int value) {
        return writeRawFixed32(buffer, pos, value);
    }

    public static int writeFixed64(byte[] buffer, int pos, long value) {
        return writeRawFixed64(buffer, pos, value);
    }

    //-------------------
    public static int writeFloat(byte[] buffer, int pos, float value) {
        return writeRawFixed32(buffer, pos, Float.floatToRawIntBits(value));
    }

    public static int writeVarFloat(byte[] buffer, int pos, float value) {
        return writeRawVarFloat32(buffer, pos, Float.floatToRawIntBits(value));
    }

    public static int writeDouble(byte[] buffer, int pos, double value) {
        return writeRawFixed64(buffer, pos, Double.doubleToRawLongBits(value));
    }

    public static int writeVarDouble(byte[] buffer, int pos, double value) {
        return writeRawVarFloat64(buffer, pos, Double.doubleToRawLongBits(value));
    }

    //-------------------
    private static int writeRawFixed16(byte[] buffer, int pos, int value) {
        buffer[pos] = (byte) value;
        buffer[pos + 1] = (byte) (value >> 8);
        return pos + 2;
    }

    private static int writeRawFixed32(byte[] buffer, int pos, int value) {
        buffer[pos] = (byte) value;
        buffer[pos + 1] = (byte) (value >> 8);
        buffer[pos + 2] = (byte) (value >> 16);
        buffer[pos + 3] = (byte) (value >> 24);
        return pos + 4;
    }

    private static int writeRawFixed64(byte[] buffer, int pos, long value) {
        buffer[pos] = (byte) value;
        buffer[pos + 1] = (byte) (value >> 8);
        buffer[pos + 2] = (byte) (value >> 16);
        buffer[pos + 3] = (byte) (value >> 24);
        buffer[pos + 4] = (byte) (value >> 32);
        buffer[pos + 5] = (byte) (value >> 40);
        buffer[pos + 6] = (byte) (value >> 48);
        buffer[pos + 7] = (byte) (value >> 56);
        return pos + 8;
    }

    /** 小端编码：所有bit使用VarInt编码 */
    private static int writeRawVarInt32(byte[] buffer, int pos, int value) {
        // 循环展开
        int b = (value & 127); // 0~6
        value >>>= 7;
        if (value == 0) {
            buffer[pos++] = (byte) b;
            return pos;
        }
        buffer[pos++] = (byte) (b | 128);

        b = (value & 127); // 7~13
        value >>>= 7;
        if (value == 0) {
            buffer[pos++] = (byte) b;
            return pos;
        }
        buffer[pos++] = (byte) (b | 128);

        b = (value & 127); // 14~20
        value >>>= 7;
        if (value == 0) {
            buffer[pos++] = (byte) b;
            return pos;
        }
        buffer[pos++] = (byte) (b | 128);

        b = (value & 127); // 21~27
        value >>>= 7;
        if (value == 0) {
            buffer[pos++] = (byte) b;
            return pos;
        }
        buffer[pos++] = (byte) (b | 128);

        b = (value & 15); // 28~31 取后4位
        buffer[pos++] = (byte) b;
        return pos;
    }

    /** 小端编码：低8位固定写入，剩余bit使用VarInt编码 */
    private static int writeRawVarInt64(byte[] buffer, int pos, long value) {
        long b = (value & 255L); // 低8位
        value >>>= 8;
        buffer[pos++] = (byte) b;

        // fori循环有利于循环展开
        for (int i = 0; i < 8; i++) {
            b = (value & 127L); // 取低7位
            value >>>= 7;
            if (value != 0) {
                buffer[pos++] = (byte) (b | 128L); // 高位补1
            } else {
                buffer[pos++] = (byte) b;
                return pos;
            }
        }
        // 不可达
        throw new AssertionError();
    }

    /** 大端编码：高8位固定写入，剩余bit使用VarInt编码 */
    private static int writeRawVarFloat32(byte[] buffer, int pos, int value) {
        int b = (value >>> 24) & 0xFF; // 31~24
        value <<= 8;
        buffer[pos++] = (byte) b;

        b = (value >>> 25) & 127; // 23~17
        value <<= 7;
        if (value == 0) {
            buffer[pos++] = (byte) b;
            return pos;
        }
        buffer[pos++] = (byte) (b | 128);

        b = (value >>> 25) & 127; // 16~10
        value <<= 7;
        if (value == 0) {
            buffer[pos++] = (byte) b;
            return pos;
        }
        buffer[pos++] = (byte) (b | 128);

        b = (value >>> 25) & 127; // 9~3
        value <<= 7;
        if (value == 0) {
            buffer[pos++] = (byte) b;
            return pos;
        }
        buffer[pos++] = (byte) (b | 128);

        b = (value >>> 29) & 7; // 2~0 取后3位
        buffer[pos++] = (byte) b;
        return pos;
    }

    /** 大端编码：高8位固定写入，剩余bit使用VarInt编码 */
    private static int writeRawVarFloat64(byte[] buffer, int pos, long value) {
        long b = value >>> 56; // 高8位
        value <<= 8;
        buffer[pos++] = (byte) b;

        for (int i = 0; i < 8; i++) {
            b = (value >>> 57) & 127; // 取高7位
            value <<= 7;
            if (value != 0) {
                buffer[pos++] = (byte) (b | 128L); // 高位补1
            } else {
                buffer[pos++] = (byte) b;
                return pos;
            }
        }
        // 不可达
        throw new AssertionError();
    }
    //endregion
}