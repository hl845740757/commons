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
#pragma warning restore CS1591

    private const uint INT_CODED_MASK1 = (~0U) << 7; // 低7位0
    private const uint INT_CODED_MASK2 = (~0U) << 14; // 低14位0
    private const uint INT_CODED_MASK3 = (~0U) << 21;
    private const uint INT_CODED_MASK4 = (~0U) << 28;

    private const ulong LONG_CODED_MASK1 = (~0UL) << 7;
    private const ulong LONG_CODED_MASK2 = (~0UL) << 14;
    private const ulong LONG_CODED_MASK3 = (~0UL) << 21;
    private const ulong LONG_CODED_MASK4 = (~0UL) << 28;
    private const ulong LONG_CODED_MASK5 = (~0UL) << 35;
    private const ulong LONG_CODED_MASK6 = (~0UL) << 42;
    private const ulong LONG_CODED_MASK7 = (~0UL) << 49;
    private const ulong LONG_CODED_MASK8 = (~0UL) << 56;
    private const ulong LONG_CODED_MASK9 = (~0UL) << 63;

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

#pragma warning disable CS1591
    /** https://protobuf.dev/programming-guides/encoding  */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint EncodeZigZag32(int n) => (uint)(n << 1 ^ n >> 31);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int DecodeZigZag32(uint n) => (int)(n >> 1) ^ -((int)n & 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong EncodeZigZag64(long n) => (ulong)(n << 1 ^ n >> 63);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long DecodeZigZag64(ulong n) => (long)(n >> 1) ^ -((long)n & 1L);

#pragma warning restore CS1591

    #region protobuf decode

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadInt32(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawVarint64(buffer, pos, out newPos);
        return (int)rawBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadInt64(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawVarint64(buffer, pos, out newPos);
        return (long)rawBits;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadUint32(byte[] buffer, int pos, out int newPos) {
        return (int)ReadRawVarint64(buffer, pos, out newPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadUint64(byte[] buffer, int pos, out int newPos) {
        return (long)ReadRawVarint64(buffer, pos, out newPos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int ReadSint32(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawVarint64(buffer, pos, out newPos);
        return DecodeZigZag32((uint)rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static long ReadSint64(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawVarint64(buffer, pos, out newPos);
        return DecodeZigZag64(rawBits);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static float ReadFloat(byte[] buffer, int pos, out int newPos) {
        uint rawBits = ReadRawFixed32(buffer, pos, out newPos);
        return BitConverter.Int32BitsToSingle((int)rawBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static double ReadDouble(byte[] buffer, int pos, out int newPos) {
        ulong rawBits = ReadRawFixed64(buffer, pos, out newPos);
        return BitConverter.Int64BitsToDouble((long)rawBits);
    }

    /** varint编码不区分int和long，而是固定读取到高位字节为0，因此无需两个方法 */
    private static ulong ReadRawVarint64(byte[] buffer, int pos, out int newPos) {
        // 单字节优化
        byte b = buffer[pos++];
        ulong r = b & 127UL;
        if (b < 128U) {
            newPos = pos;
            return r;
        }
        int shift = 7;
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
        throw new DsonIOException("DsonInput encountered a malformed varint.");
    }

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

    #endregion

    #region protobuf encode

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteInt32(byte[] buffer, int pos, int value) {
        if (value >= 0) {
            return WriteRawVarint32(buffer, pos, (uint)value);
        } else {
            return WriteRawVarint64(buffer, pos, (ulong)value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteInt64(byte[] buffer, int pos, long value) {
        return WriteRawVarint64(buffer, pos, (ulong)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteUint32(byte[] buffer, int pos, int value) {
        return WriteRawVarint32(buffer, pos, (uint)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteUint64(byte[] buffer, int pos, long value) {
        return WriteRawVarint64(buffer, pos, (ulong)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteSint32(byte[] buffer, int pos, int value) {
        return WriteRawVarint32(buffer, pos, EncodeZigZag32(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteSint64(byte[] buffer, int pos, long value) {
        return WriteRawVarint64(buffer, pos, EncodeZigZag64(value));
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteFloat(byte[] buffer, int pos, float value) {
        return WriteRawFixed32(buffer, pos, (uint)BitConverter.SingleToInt32Bits(value));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int WriteDouble(byte[] buffer, int pos, double value) {
        return WriteRawFixed64(buffer, pos, (ulong)BitConverter.DoubleToInt64Bits(value));
    }

    /// <summary>
    /// 写入一个变长的64位整数，所有的负数都将固定10字节
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="pos">开始写入的位置</param>
    /// <param name="value">要写入的值</param>
    /// <returns>写入后的新坐标</returns>
    private static int WriteRawVarint64(byte[] buffer, int pos, ulong value) {
        if (value < 128UL) { // 小数值较多的情况下有意义
            buffer[pos] = (byte)value;
            return pos + 1;
        }
        while (true) {
            if (value > 127UL) {
                buffer[pos++] = (byte)((value & 127UL) | 128UL); // 截取后7位，高位补1
                value >>= 7;
            } else {
                buffer[pos++] = (byte)value;
                return pos;
            }
        }
    }

    /// <summary>
    /// 写入一个变长的32位整数
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="pos">开始写入的位置</param>
    /// <param name="value">要写入的值</param>
    /// <returns>写入后的新坐标</returns>
    private static int WriteRawVarint32(byte[] buffer, int pos, uint value) {
        if (value < 128U) { // 小数值较多的情况下有意义
            buffer[pos] = (byte)value;
            return pos + 1;
        }
        while (true) {
            if (value > 127U) {
                buffer[pos++] = (byte)((value & 127U) | 128U); // 截取后7位，高位补1
                value >>= 7;
            } else {
                buffer[pos++] = (byte)value;
                return pos;
            }
        }
    }

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

    #endregion
}
}