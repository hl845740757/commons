#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Internal
{
/// <summary>
/// 以下参考自protobuf，以避免引入PB
/// </summary>
internal static class CodedUtil
{
    private const int INT_CODED_MASK1 = -1 << 7; // 低7位0
    private const int INT_CODED_MASK2 = -1 << 14; // 低14位0
    private const int INT_CODED_MASK3 = -1 << 21;
    private const int INT_CODED_MASK4 = -1 << 28;

    private const long LONG_CODED_MASK1 = -1L << 8; // 低8位0
    private const long LONG_CODED_MASK2 = -1L << 15; // 低15位0
    private const long LONG_CODED_MASK3 = -1L << 22;
    private const long LONG_CODED_MASK4 = -1L << 29;
    private const long LONG_CODED_MASK5 = -1L << 36;
    private const long LONG_CODED_MASK6 = -1L << 43;
    private const long LONG_CODED_MASK7 = -1L << 50;
    private const long LONG_CODED_MASK8 = -1L << 57;
    
    public const int MAX_VAR_INT32_LENGTH = 5; // 7 * 5 = 35
    public const int MAX_VAR_INT64_LENGTH = 10; // 7 * 9 = 63
    public const int MAX_VAR_FLOAT32_LENGTH = 5; // 8 + 7 * 4
    public const int MAX_VAR_FLOAT64_LENGTH = 9; // 8 + 7 * 8 =64

    /// <summary>
    /// 计算原始的32位变长整形的编码长度
    /// </summary>
    /// <param name="value"></param>
    /// <returns>编码长度</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputeRawVarInt32Size(int value) {
        if ((value & INT_CODED_MASK1) == 0) return 1; // 所有高位为0
        if ((value & INT_CODED_MASK2) == 0) return 2;
        if ((value & INT_CODED_MASK3) == 0) return 3;
        if ((value & INT_CODED_MASK4) == 0) return 4;
        return 5;
    }

    /// <summary>
    /// 计算原始的64位变长整形的编码长度
    /// </summary>
    /// <param name="value"></param>
    /// <returns>编码长度</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputeRawVarInt64Size(long value) {
        if ((value & LONG_CODED_MASK2) == 0) return 2;
        if ((value & LONG_CODED_MASK3) == 0) return 3;
        if ((value & LONG_CODED_MASK4) == 0) return 4;
        if ((value & LONG_CODED_MASK5) == 0) return 5;
        if ((value & LONG_CODED_MASK6) == 0) return 6;
        if ((value & LONG_CODED_MASK7) == 0) return 7;
        if ((value & LONG_CODED_MASK8) == 0) return 8;
        return 9;
    }

    /** https://protobuf.dev/programming-guides/encoding  */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeZigZag32(int n) => (n << 1) ^ (n >> 31);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DecodeZigZag32(int n) => ((n >> 1) & int.MaxValue) ^ -(n & 1); // & max 实现逻辑右移1位

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long EncodeZigZag64(long n) => (n << 1 ^ n >> 63);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long DecodeZigZag64(long n) => ((n >> 1) & long.MaxValue) ^ -(n & 1L); // & max 实现逻辑右移1位

    #region protobuf decode

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadUInt32(byte[] buffer, int pos, out int newPos) {
        return ReadRawVarInt32(buffer, pos, out newPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadUInt64(byte[] buffer, int pos, out int newPos) {
        return ReadRawVarInt64(buffer, pos, out newPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadSInt32(byte[] buffer, int pos, out int newPos) {
        int rawBits = ReadRawVarInt32(buffer, pos, out newPos);
        return DecodeZigZag32(rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadSInt64(byte[] buffer, int pos, out int newPos) {
        long rawBits = ReadRawVarInt64(buffer, pos, out newPos);
        return DecodeZigZag64(rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadFixed16(byte[] buffer, int pos, out int newPos) {
        return ReadRawFixed16(buffer, pos, out newPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadFixed32(byte[] buffer, int pos, out int newPos) {
        return ReadRawFixed32(buffer, pos, out newPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadFixed64(byte[] buffer, int pos, out int newPos) {
        return ReadRawFixed64(buffer, pos, out newPos);
    }

    //-------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float ReadFloat(byte[] buffer, int pos, out int newPos) {
        int rawBits = ReadRawFixed32(buffer, pos, out newPos);
        return BitConverter.Int32BitsToSingle(rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float ReadVarFloat(byte[] buffer, int pos, out int newPos) {
        int rawBits = ReadRawVarFloat32(buffer, pos, out newPos);
        return BitConverter.Int32BitsToSingle(rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ReadDouble(byte[] buffer, int pos, out int newPos) {
        long rawBits = ReadRawFixed64(buffer, pos, out newPos);
        return BitConverter.Int64BitsToDouble(rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ReadVarDouble(byte[] buffer, int pos, out int newPos) {
        long rawBits = ReadRawVarFloat64(buffer, pos, out newPos);
        return BitConverter.Int64BitsToDouble(rawBits);
    }

    //-------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ReadRawFixed16(byte[] buffer, int pos, out int newPos) {
        int r = (((buffer[pos] & 0xFF))
                 | ((buffer[pos + 1] & 0xFF) << 8));
        newPos = pos + 2;
        return r;
    }

    private static int ReadRawFixed32(byte[] buffer, int pos, out int newPos) {
        int r = (((buffer[pos] & 0xFF))
                 | ((buffer[pos + 1] & 0xFF) << 8)
                 | ((buffer[pos + 2] & 0xFF) << 16)
                 | ((buffer[pos + 3] & 0xFF) << 24));
        newPos = pos + 4;
        return r;
    }

    private static long ReadRawFixed64(byte[] buffer, int pos, out int newPos) {
        long r = (((buffer[pos] & 0xFFL))
                  | ((buffer[pos + 1] & 0xFFL) << 8)
                  | ((buffer[pos + 2] & 0xFFL) << 16)
                  | ((buffer[pos + 3] & 0xFFL) << 24)
                  | ((buffer[pos + 4] & 0xFFL) << 32)
                  | ((buffer[pos + 5] & 0xFFL) << 40)
                  | ((buffer[pos + 6] & 0xFFL) << 48)
                  | ((buffer[pos + 7] & 0xFFL) << 56));
        newPos = pos + 8;
        return r;
    }

    private static int ReadRawVarInt32(byte[] buffer, int pos, out int newPos) {
        // 循环展开 -- C# byte是无符号数，转int高位补0
        int b = buffer[pos++];
        int r = (b & 127); // 0~6
        if (b < 128) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 7; // 7~13
        if (b < 128) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 14; // 14~20
        if (b < 128) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 21; // 21~27
        if (b < 128) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 15) << 28; // 28~31 取后4位
        if (b < 128) {
            newPos = pos;
            return r;
        }
        // 读取超过5个字节
        throw new DsonIOException("DsonInput encountered a malformed varint32.");
    }

    private static long ReadRawVarInt64(byte[] buffer, int pos, out int newPos) {
        long r = buffer[pos++] & 0xFFL; // 低8位
        int shift = 8;
        for (int i = 0; i < 8; i++) {
            long b = buffer[pos++];
            r |= (b & 127L) << shift; // 取后7位左移
            if (b < 128L) { // 高位0
                newPos = pos;
                return r;
            }
            shift += 7;
        }
        // 读取超过9个字节
        throw new DsonIOException("DsonInput encountered a malformed varint64.");
    }

    private static int ReadRawVarFloat32(byte[] buffer, int pos, out int newPos) {
        int r = buffer[pos++] << 24; // 31~24
        int b = buffer[pos++];
        r |= (b & 127) << 17; // 23~17
        if (b < 128) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 10; // 16~10
        if (b < 128) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127) << 3; // 9~3
        if (b < 128) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 7); // 2~0 取后3位
        if (b < 128) {
            newPos = pos;
            return r;
        }
        // 读取超过5个字节
        throw new DsonIOException("DsonInput encountered a malformed varfloat32.");
    }

    private static long ReadRawVarFloat64(byte[] buffer, int pos, out int newPos) {
        long r = (long)buffer[pos++] << 56; // 高8位
        int shift = 49;
        for (int i = 0; i < 8; i++) {
            long b = buffer[pos++];
            r |= (b & 127L) << shift; // 取后7位左移
            if (b < 128L) { // 高位0
                newPos = pos;
                return r;
            }
            shift -= 7;
        }
        // 读取超过9个字节
        throw new DsonIOException("DsonInput encountered a malformed varfloat64.");
    }

    #endregion

    #region protobuf encode

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteUInt32(byte[] buffer, int pos, int value) {
        return WriteRawVarInt32(buffer, pos, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteUInt64(byte[] buffer, int pos, long value) {
        return WriteRawVarInt64(buffer, pos, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteSInt32(byte[] buffer, int pos, int value) {
        return WriteRawVarInt32(buffer, pos, EncodeZigZag32(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteSInt64(byte[] buffer, int pos, long value) {
        return WriteRawVarInt64(buffer, pos, EncodeZigZag64(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed16(byte[] buffer, int pos, int value) {
        return WriteRawFixed16(buffer, pos, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed32(byte[] buffer, int pos, int value) {
        return WriteRawFixed32(buffer, pos, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed64(byte[] buffer, int pos, long value) {
        return WriteRawFixed64(buffer, pos, value);
    }

    //-------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFloat(byte[] buffer, int pos, float value) {
        return WriteRawFixed32(buffer, pos, BitConverter.SingleToInt32Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarFloat(byte[] buffer, int pos, float value) {
        return WriteRawVarFloat32(buffer, pos, BitConverter.SingleToInt32Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteDouble(byte[] buffer, int pos, double value) {
        return WriteRawFixed64(buffer, pos, BitConverter.DoubleToInt64Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarDouble(byte[] buffer, int pos, double value) {
        return WriteRawVarFloat64(buffer, pos, BitConverter.DoubleToInt64Bits(value));
    }

    //-------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteRawFixed16(byte[] buffer, int pos, int value) {
        buffer[pos] = (byte)value;
        buffer[pos + 1] = (byte)(value >> 8);
        return pos + 2;
    }

    private static int WriteRawFixed32(byte[] buffer, int pos, int value) {
        buffer[pos] = (byte)value;
        buffer[pos + 1] = (byte)(value >> 8);
        buffer[pos + 2] = (byte)(value >> 16);
        buffer[pos + 3] = (byte)(value >> 24);
        return pos + 4;
    }

    private static int WriteRawFixed64(byte[] buffer, int pos, long value) {
        buffer[pos] = (byte)value;
        buffer[pos + 1] = (byte)(value >> 8);
        buffer[pos + 2] = (byte)(value >> 16);
        buffer[pos + 3] = (byte)(value >> 24);
        buffer[pos + 4] = (byte)(value >> 32);
        buffer[pos + 5] = (byte)(value >> 40);
        buffer[pos + 6] = (byte)(value >> 48);
        buffer[pos + 7] = (byte)(value >> 56);
        return pos + 8;
    }

    /** 小端编码：所有bit使用VarInt编码 */
    private static int WriteRawVarInt32(byte[] buffer, int pos, int value) {
        // 循环展开
        int b = (value & 127); // 0~6
        value = (value >> 7) & (int.MaxValue >> 6); // 逻辑右移7位，此后可算术右移
        if (value == 0) {
            buffer[pos++] = (byte)b;
            return pos;
        }
        buffer[pos++] = (byte)(b | 128);

        b = (value & 127); // 7~13
        value >>= 7;
        if (value == 0) {
            buffer[pos++] = (byte)b;
            return pos;
        }
        buffer[pos++] = (byte)(b | 128);

        b = (value & 127); // 14~20
        value >>= 7;
        if (value == 0) {
            buffer[pos++] = (byte)b;
            return pos;
        }
        buffer[pos++] = (byte)(b | 128);

        b = (value & 127); // 21~27
        value >>= 7;
        if (value == 0) {
            buffer[pos++] = (byte)b;
            return pos;
        }
        buffer[pos++] = (byte)(b | 128);

        b = (value & 15); // 28~31 只可取后4位
        buffer[pos++] = (byte)b;
        return pos;
    }

    /** 小端编码：低8位固定写入，剩余bit使用VarInt编码 */
    private static int WriteRawVarInt64(byte[] buffer, int pos, long value) {
        long b = (value & 255L); // 低8位
        value = (value >> 8) & (long.MaxValue >> 7); // 逻辑右移8位，此后可算术右移
        buffer[pos++] = (byte)b;

        // 使用fori循环有利于循环展开
        for (int i = 0; i < 8; i++) {
            b = (value & 127L); // 取低7位
            value >>= 7;
            if (value != 0) {
                buffer[pos++] = (byte)(b | 128L); // 高位补1
            } else {
                buffer[pos++] = (byte)b;
                return pos;
            }
        }
        // 不可达
        throw new AssertionError();
    }

    /** 大端编码：高8位固定写入，剩余bit使用VarInt编码； */
    private static int WriteRawVarFloat32(byte[] buffer, int pos, int value) {
        int b = (value >> 24) & 0xFF; // 31~24
        value <<= 8;
        buffer[pos++] = (byte)b;

        b = (value >> 25) & 127; // 23~17
        value <<= 7;
        if (value == 0) {
            buffer[pos++] = (byte)b;
            return pos;
        }
        buffer[pos++] = (byte)(b | 128);

        b = (value >> 25) & 127; // 16~10
        value <<= 7;
        if (value == 0) {
            buffer[pos++] = (byte)b;
            return pos;
        }
        buffer[pos++] = (byte)(b | 128);

        b = (value >> 25) & 127; // 9~3
        value <<= 7;
        if (value == 0) {
            buffer[pos++] = (byte)b;
            return pos;
        }
        buffer[pos++] = (byte)(b | 128);

        b = (value >> 29) & 7; // 2~0 只可取后3位
        buffer[pos++] = (byte)b;
        return pos;
    }

    /** 大端编码：高8位固定写入，剩余bit使用VarInt编码 */
    private static int WriteRawVarFloat64(byte[] buffer, int pos, long value) {
        long b = (value >> 56) & 0xFFL; // 高8位
        value <<= 8;
        buffer[pos++] = (byte)b;

        // 使用fori循环有利于循环展开
        for (int i = 0; i < 8; i++) {
            b = (value >> 57) & 127L; // 取高7位
            value <<= 7;
            if (value != 0) {
                buffer[pos++] = (byte)(b | 128L); // 高位补1
            } else {
                buffer[pos++] = (byte)b;
                return pos;
            }
        }
        // 不可达
        throw new AssertionError();
    }

    #endregion
}
}