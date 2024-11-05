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

    private static final int INT_CODED_MASK1 = (-1) << 7; // 低7位0
    private static final int INT_CODED_MASK2 = (-1) << 14; // 低14位0
    private static final int INT_CODED_MASK3 = (-1) << 21;
    private static final int INT_CODED_MASK4 = (-1) << 28;

    private static final int INT_BIG_ENDIAN_MASK1 = (-1) >>> 7; // 高7位0
    private static final int INT_BIG_ENDIAN_MASK2 = (-1) >>> 14; // 高14位0
    private static final int INT_BIG_ENDIAN_MASK3 = (-1) >>> 21;
    private static final int INT_BIG_ENDIAN_MASK4 = (-1) >>> 28;

    private static final long LONG_CODED_MASK1 = (-1L) << 7;
    private static final long LONG_CODED_MASK2 = (-1L) << 14;
    private static final long LONG_CODED_MASK3 = (-1L) << 21;
    private static final long LONG_CODED_MASK4 = (-1L) << 28;
    private static final long LONG_CODED_MASK5 = (-1L) << 35;
    private static final long LONG_CODED_MASK6 = (-1L) << 42;
    private static final long LONG_CODED_MASK7 = (-1L) << 49;
    private static final long LONG_CODED_MASK8 = (-1L) << 56;
    private static final long LONG_CODED_MASK9 = (-1L) << 63;

    private static final long LONG_BIG_ENDIAN_MASK1 = (-1L) >>> 7;
    private static final long LONG_BIG_ENDIAN_MASK2 = (-1L) >>> 14;
    private static final long LONG_BIG_ENDIAN_MASK3 = (-1L) >>> 21;
    private static final long LONG_BIG_ENDIAN_MASK4 = (-1L) >>> 28;
    private static final long LONG_BIG_ENDIAN_MASK5 = (-1L) >>> 35;
    private static final long LONG_BIG_ENDIAN_MASK6 = (-1L) >>> 42;
    private static final long LONG_BIG_ENDIAN_MASK7 = (-1L) >>> 49;
    private static final long LONG_BIG_ENDIAN_MASK8 = (-1L) >>> 56;
    private static final long LONG_BIG_ENDIAN_MASK9 = (-1L) >>> 63;

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
        if ((value & LONG_CODED_MASK1) == 0) return 1; // 所有高位为0
        if ((value & LONG_CODED_MASK2) == 0) return 2;
        if ((value & LONG_CODED_MASK3) == 0) return 3;
        if ((value & LONG_CODED_MASK4) == 0) return 4;
        if ((value & LONG_CODED_MASK5) == 0) return 5;
        if ((value & LONG_CODED_MASK6) == 0) return 6;
        if ((value & LONG_CODED_MASK7) == 0) return 7;
        if ((value & LONG_CODED_MASK8) == 0) return 8;
        if ((value & LONG_CODED_MASK9) == 0) return 9;
        return 10;
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
        int rawBits = readRawBigEndianVarInt32(buffer, pos, newPos);
        return Float.intBitsToFloat(rawBits);
    }

    public static double readDouble(byte[] buffer, int pos, MutableInt newPos) {
        long rawBits = readRawFixed64(buffer, pos, newPos);
        return Double.longBitsToDouble(rawBits);
    }

    public static double readVarDouble(byte[] buffer, int pos, MutableInt newPos) {
        long rawBits = readRawBigEndianVarInt64(buffer, pos, newPos);
        return Double.longBitsToDouble(rawBits);
    }

    //-------------------
    private static int readRawFixed16(byte[] buffer, int pos, MutableInt newPos) {
        int r = (((buffer[pos] & 0xff))
                | ((buffer[pos + 1] & 0xff) << 8));
        newPos.setValue(pos + 2);
        return r;
    }

    private static int readRawFixed32(byte[] buffer, int pos, MutableInt newPos) {
        int r = (((buffer[pos] & 0xff))
                | ((buffer[pos + 1] & 0xff) << 8)
                | ((buffer[pos + 2] & 0xff) << 16)
                | ((buffer[pos + 3] & 0xff) << 24));
        newPos.setValue(pos + 4);
        return r;
    }

    private static long readRawFixed64(byte[] buffer, int pos, MutableInt newPos) {
        long r = (((buffer[pos] & 0xffL))
                | ((buffer[pos + 1] & 0xffL) << 8)
                | ((buffer[pos + 2] & 0xffL) << 16)
                | ((buffer[pos + 3] & 0xffL) << 24)
                | ((buffer[pos + 4] & 0xffL) << 32)
                | ((buffer[pos + 5] & 0xffL) << 40)
                | ((buffer[pos + 6] & 0xffL) << 48)
                | ((buffer[pos + 7] & 0xffL) << 56));
        newPos.setValue(pos + 8);
        return r;
    }

    private static int readRawVarInt32(byte[] buffer, int pos, MutableInt newPos) {
        // 循环展开
        byte b = buffer[pos++];
        int r = (b & 127);
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 7;
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 14;
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 21;
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 28; // 只有低4位有效
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        // 读取超过5个字节
        throw new DsonIOException("DsonInput encountered a malformed varint32.");
    }

    private static long readRawVarInt64(byte[] buffer, int pos, MutableInt newPos) {
        // int64循环展开的代码太长，还容易写错...
        long r = 0;
        int shift = 0;
        byte b;
        do {
            b = buffer[pos++];
            r |= (b & 127L) << shift; // 取后7位左移
            if ((b & 128L) == 0) { // 高位0
                newPos.setValue(pos);
                return r;
            }
            shift += 7;
        } while (shift < 64);
        // 读取超过10个字节
        throw new DsonIOException("DsonInput encountered a malformed varint64.");
    }

    /** 大端编码的VarInt32 */
    private static int readRawBigEndianVarInt32(byte[] buffer, int pos, MutableInt newPos) {
        // 循环展开
        byte b = buffer[pos++];
        int r = (b & 127) << 25;
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 18;
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 11;
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 4;
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127); // 只有低4位有效
        if ((b & 128) == 0) {
            newPos.setValue(pos);
            return r;
        }
        // 读取超过5个字节
        throw new DsonIOException("DsonInput encountered a malformed big endian varint32.");
    }

    /** 大端编码的VarInt64 */
    private static long readRawBigEndianVarInt64(byte[] buffer, int pos, MutableInt newPos) {
        // int64循环展开的代码太长，还容易写错...
        long r = 0;
        int shift = 57;
        byte b;
        do {
            b = buffer[pos++];
            r |= (b & 127L) << shift; // 取后7位左移
            if ((b & 128L) == 0) { // 高位0
                newPos.setValue(pos);
                return r;
            }
            shift -= 7;
        } while (shift > 0);
        // 最后一个字节不移位
        b = buffer[pos++];
        r |= (b & 127L);
        if ((b & 128L) == 0) {
            newPos.setValue(pos);
            return r;
        }
        // 读取超过10个字节
        throw new DsonIOException("DsonInput encountered a malformed big endian varint64.");
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
        return writeRawBigEndianVarInt32(buffer, pos, Float.floatToRawIntBits(value));
    }

    public static int writeDouble(byte[] buffer, int pos, double value) {
        return writeRawFixed64(buffer, pos, Double.doubleToRawLongBits(value));
    }

    public static int writeVarDouble(byte[] buffer, int pos, double value) {
        return writeRawBigEndianVarInt64(buffer, pos, Double.doubleToRawLongBits(value));
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

    private static int writeRawVarInt32(byte[] buffer, int pos, int value) {
        while (true) {
            int b = (value & 127); // 取低7位
            value >>>= 7;
            if (value != 0) {
                buffer[pos++] = (byte) (b | 128); // 高位补1
            } else {
                buffer[pos++] = (byte) b;
                return pos;
            }
        }
    }

    private static int writeRawVarInt64(byte[] buffer, int pos, long value) {
        while (true) {
            long b = (value & 127L); // 取低7位
            value >>>= 7;
            if (value != 0) {
                buffer[pos++] = (byte) (b | 128L); // 高位补1
            } else {
                buffer[pos++] = (byte) b;
                return pos;
            }
        }
    }

    private static int writeRawBigEndianVarInt32(byte[] buffer, int pos, int value) {
        while (true) {
            int b = (value & ~INT_BIG_ENDIAN_MASK1) >>> 25; // 取高7位
            value <<= 7;
            if (value != 0) {
                buffer[pos++] = (byte) (b | 128); // 高位补1
            } else {
                buffer[pos++] = (byte) b;
                return pos;
            }
        }
    }

    private static int writeRawBigEndianVarInt64(byte[] buffer, int pos, long value) {
        while (true) {
            long b = (value & ~LONG_BIG_ENDIAN_MASK1) >>> 57; // 取高7位
            value <<= 7;
            if (value != 0) {
                buffer[pos++] = (byte) (b | 128L); // 高位补1
            } else {
                buffer[pos++] = (byte) b;
                return pos;
            }
        }
    }
    //endregion
}