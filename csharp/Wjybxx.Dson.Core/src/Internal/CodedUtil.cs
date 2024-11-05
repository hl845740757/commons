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
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Internal
{
/// <summary>
/// 以下参考自protobuf，以避免引入PB
/// </summary>
internal static class CodedUtil
{
    private const uint INT_CODED_MASK1 = (~0U) << 7; // 低7位0
    private const uint INT_CODED_MASK2 = (~0U) << 14; // 低14位0
    private const uint INT_CODED_MASK3 = (~0U) << 21;
    private const uint INT_CODED_MASK4 = (~0U) << 28;

    private const uint INT_BIG_ENDIAN_MASK1 = (~0U) >> 7;
    private const uint INT_BIG_ENDIAN_MASK2 = (~0U) >> 14;
    private const uint INT_BIG_ENDIAN_MASK3 = (~0U) >> 21;
    private const uint INT_BIG_ENDIAN_MASK4 = (~0U) >> 28;

    private const ulong LONG_CODED_MASK1 = (~0UL) << 7;
    private const ulong LONG_CODED_MASK2 = (~0UL) << 14;
    private const ulong LONG_CODED_MASK3 = (~0UL) << 21;
    private const ulong LONG_CODED_MASK4 = (~0UL) << 28;
    private const ulong LONG_CODED_MASK5 = (~0UL) << 35;
    private const ulong LONG_CODED_MASK6 = (~0UL) << 42;
    private const ulong LONG_CODED_MASK7 = (~0UL) << 49;
    private const ulong LONG_CODED_MASK8 = (~0UL) << 56;
    private const ulong LONG_CODED_MASK9 = (~0UL) << 63;

    private const ulong LONG_BIG_ENDIAN_MASK1 = (~0UL) >> 7;
    private const ulong LONG_BIG_ENDIAN_MASK2 = (~0UL) >> 14;
    private const ulong LONG_BIG_ENDIAN_MASK3 = (~0UL) >> 21;
    private const ulong LONG_BIG_ENDIAN_MASK4 = (~0UL) >> 28;
    private const ulong LONG_BIG_ENDIAN_MASK5 = (~0UL) >> 35;
    private const ulong LONG_BIG_ENDIAN_MASK6 = (~0UL) >> 42;
    private const ulong LONG_BIG_ENDIAN_MASK7 = (~0UL) >> 49;
    private const ulong LONG_BIG_ENDIAN_MASK8 = (~0UL) >> 56;
    private const ulong LONG_BIG_ENDIAN_MASK9 = (~0UL) >> 63;

    /// <summary>
    /// 计算原始的32位变长整形的编码长度
    /// </summary>
    /// <param name="value"></param>
    /// <returns>编码长度</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputeRawVarInt32Size(uint value) {
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
    public static int ComputeRawVarInt64Size(ulong value) {
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

    /** https://protobuf.dev/programming-guides/encoding  */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int EncodeZigZag32(int n) => (n << 1 ^ n >> 31);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DecodeZigZag32(int n) => n >> 1 ^ -(n & 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long EncodeZigZag64(long n) => (n << 1 ^ n >> 63);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long DecodeZigZag64(long n) => n >> 1 ^ -(n & 1L);

    #region protobuf decode

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadUInt32(byte[] buffer, int pos, out int newPos) {
        return (int)ReadRawVarInt32(buffer, pos, out newPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadUInt64(byte[] buffer, int pos, out int newPos) {
        return (long)ReadRawVarInt64(buffer, pos, out newPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadSInt32(byte[] buffer, int pos, out int newPos) {
        uint rawBits = ReadRawVarInt32(buffer, pos, out newPos);
        return DecodeZigZag32((int)rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadSInt64(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawVarInt64(buffer, pos, out newPos);
        return DecodeZigZag64((long)rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadFixed16(byte[] buffer, int pos, out int newPos) {
        uint rawBits = ReadRawFixed16(buffer, pos, out newPos);
        return (int)rawBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadFixed32(byte[] buffer, int pos, out int newPos) {
        uint rawBits = ReadRawFixed32(buffer, pos, out newPos);
        return (int)rawBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadFixed64(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawFixed64(buffer, pos, out newPos);
        return (long)rawBits;
    }

    //-------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float ReadFloat(byte[] buffer, int pos, out int newPos) {
        uint rawBits = ReadRawFixed32(buffer, pos, out newPos);
        return BitConverter.Int32BitsToSingle((int)rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float ReadVarFloat(byte[] buffer, int pos, out int newPos) {
        uint rawBits = ReadRawBigEndianVarInt32(buffer, pos, out newPos);
        return BitConverter.Int32BitsToSingle((int)rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ReadDouble(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawFixed64(buffer, pos, out newPos);
        return BitConverter.Int64BitsToDouble((long)rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ReadVarDouble(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawBigEndianVarInt64(buffer, pos, out newPos);
        return BitConverter.Int64BitsToDouble((long)rawBits);
    }

    //-------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint ReadRawFixed16(byte[] buffer, int pos, out int newPos) {
        uint r = (((buffer[pos] & 0xffU))
                  | ((buffer[pos + 1] & 0xffU) << 8));
        newPos = pos + 2;
        return r;
    }

    private static uint ReadRawFixed32(byte[] buffer, int pos, out int newPos) {
        uint r = (((buffer[pos] & 0xffU))
                  | ((buffer[pos + 1] & 0xffU) << 8)
                  | ((buffer[pos + 2] & 0xffU) << 16)
                  | ((buffer[pos + 3] & 0xffU) << 24));
        newPos = pos + 4;
        return r;
    }

    private static ulong ReadRawFixed64(byte[] buffer, int pos, out int newPos) {
        ulong r = (((buffer[pos] & 0xffUL))
                   | ((buffer[pos + 1] & 0xffUL) << 8)
                   | ((buffer[pos + 2] & 0xffUL) << 16)
                   | ((buffer[pos + 3] & 0xffUL) << 24)
                   | ((buffer[pos + 4] & 0xffUL) << 32)
                   | ((buffer[pos + 5] & 0xffUL) << 40)
                   | ((buffer[pos + 6] & 0xffUL) << 48)
                   | ((buffer[pos + 7] & 0xffUL) << 56));
        newPos = pos + 8;
        return r;
    }

    private static uint ReadRawVarInt32(byte[] buffer, int pos, out int newPos) {
        // 循环展开
        byte b = buffer[pos++];
        uint r = (b & 127U);
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127U) << 7;
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127U) << 14;
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127U) << 21;
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127U) << 28; // 只有低4位有效
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        throw new DsonIOException("DsonInput encountered a malformed varint32.");
    }

    private static ulong ReadRawVarInt64(byte[] buffer, int pos, out int newPos) {
        // int64循环展开的代码太长，还容易写错...
        ulong r = 0;
        int shift = 0;
        byte b;
        do {
            b = buffer[pos++];
            r |= (b & 127UL) << shift; // 取后7位左移
            if (b < 128U) { // 高位0
                newPos = pos;
                return r;
            }
            shift += 7;
        } while (shift < 64);
        // 读取超过10个字节
        throw new DsonIOException("DsonInput encountered a malformed varint64.");
    }

    private static uint ReadRawBigEndianVarInt32(byte[] buffer, int pos, out int newPos) {
        // 循环展开
        byte b = buffer[pos++];
        uint r = (b & 127U) << 25;
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127U) << 18;
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127U) << 11;
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127U) << 4;
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        b = buffer[pos++];
        r |= (b & 127U); // 只有低4位有效
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        throw new DsonIOException("DsonInput encountered a malformed big endian varint32.");
    }

    private static ulong ReadRawBigEndianVarInt64(byte[] buffer, int pos, out int newPos) {
        // int64循环展开的代码太长，还容易写错...
        ulong r = 0;
        int shift = 57;
        byte b;
        do {
            b = buffer[pos++];
            r |= (b & 127UL) << shift; // 取后7位左移
            if (b < 128UL) { // 高位0
                newPos = pos;
                return r;
            }
            shift -= 7;
        } while (shift > 0);
        // 最后一个字节不移位
        b = buffer[pos++];
        r |= (b & 127UL);
        if (b < 128UL) {
            newPos = pos;
            return r;
        }
        // 读取超过10个字节
        throw new DsonIOException("DsonInput encountered a malformed big endian varint64.");
    }

    #endregion

    #region protobuf encode

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteUInt32(byte[] buffer, int pos, int value) {
        return WriteRawVarInt32(buffer, pos, (uint)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteUInt64(byte[] buffer, int pos, long value) {
        return WriteRawVarInt64(buffer, pos, (ulong)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteSInt32(byte[] buffer, int pos, int value) {
        return WriteRawVarInt32(buffer, pos, (uint)EncodeZigZag32(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteSInt64(byte[] buffer, int pos, long value) {
        return WriteRawVarInt64(buffer, pos, (ulong)EncodeZigZag64(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed16(byte[] buffer, int pos, int value) {
        return WriteRawFixed16(buffer, pos, (uint)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed32(byte[] buffer, int pos, int value) {
        return WriteRawFixed32(buffer, pos, (uint)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFixed64(byte[] buffer, int pos, long value) {
        return WriteRawFixed64(buffer, pos, (ulong)value);
    }

    //-------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFloat(byte[] buffer, int pos, float value) {
        return WriteRawFixed32(buffer, pos, (uint)BitConverter.SingleToInt32Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarFloat(byte[] buffer, int pos, float value) {
        return WriteRawBigEndianVarInt32(buffer, pos, (uint)BitConverter.SingleToInt32Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteDouble(byte[] buffer, int pos, double value) {
        return WriteRawFixed64(buffer, pos, (ulong)BitConverter.DoubleToInt64Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteVarDouble(byte[] buffer, int pos, double value) {
        return WriteRawBigEndianVarInt64(buffer, pos, (ulong)BitConverter.DoubleToInt64Bits(value));
    }

    //-------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int WriteRawFixed16(byte[] buffer, int pos, uint value) {
        buffer[pos] = (byte)value;
        buffer[pos + 1] = (byte)(value >> 8);
        return pos + 2;
    }

    private static int WriteRawFixed32(byte[] buffer, int pos, uint value) {
        buffer[pos] = (byte)value;
        buffer[pos + 1] = (byte)(value >> 8);
        buffer[pos + 2] = (byte)(value >> 16);
        buffer[pos + 3] = (byte)(value >> 24);
        return pos + 4;
    }

    private static int WriteRawFixed64(byte[] buffer, int pos, ulong value) {
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

    private static int WriteRawVarInt32(byte[] buffer, int pos, uint value) {
        while (true) {
            uint b = (value & 127U); // 取低7位
            value >>= 7;
            if (value != 0) {
                buffer[pos++] = (byte)(b | 128U); // 高位补1
            } else {
                buffer[pos++] = (byte)b;
                return pos;
            }
        }
    }

    private static int WriteRawVarInt64(byte[] buffer, int pos, ulong value) {
        while (true) {
            ulong b = (value & 127UL); // 取低7位
            value >>= 7;
            if (value != 0) {
                buffer[pos++] = (byte)(b | 128Ul); // 高位补1
            } else {
                buffer[pos++] = (byte)b;
                return pos;
            }
        }
    }

    private static int WriteRawBigEndianVarInt32(byte[] buffer, int pos, uint value) {
        while (true) {
            uint b = (value & ~INT_BIG_ENDIAN_MASK1) >> 25; // 取高7位
            value <<= 7;
            if (value != 0) {
                buffer[pos++] = (byte)(b | 128U); // 高位补1
            } else {
                buffer[pos++] = (byte)b;
                return pos;
            }
        }
    }

    private static int WriteRawBigEndianVarInt64(byte[] buffer, int pos, ulong value) {
        while (true) {
            ulong b = (value & ~LONG_BIG_ENDIAN_MASK1) >> 57; // 取高7位
            value <<= 7;
            if (value != 0) {
                buffer[pos++] = (byte)(b | 128UL); // 高位补1
            } else {
                buffer[pos++] = (byte)b;
                return pos;
            }
        }
    }

    #endregion
}
}