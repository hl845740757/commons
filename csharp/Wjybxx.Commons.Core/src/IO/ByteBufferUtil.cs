#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.IO;

/// <summary>
/// 字节数组IO操作工具类
/// 1.C#10不支持逻辑右移，但这里使用算术右移是等价的
/// </summary>
public static class ByteBufferUtil
{
    /// <summary>
    /// 检查buffer参数
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset">数据的起始索引</param>
    /// <param name="length">数据的长度</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckBuffer(byte[] buffer, int offset, int length) {
        CheckBuffer(buffer.Length, offset, length);
    }

    /// <summary>
    /// 检查buffer参数
    /// </summary>
    /// <param name="bufferLength">buffer数组的长度</param>
    /// <param name="offset">数据的起始索引</param>
    /// <param name="length">数据的长度</param>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckBuffer(int bufferLength, int offset, int length) {
        if ((offset | length | (bufferLength - (offset + length))) < 0) {
            throw new ArgumentException($"Array range is invalid. Buffer.length={bufferLength}, offset={offset}, length={length}");
        }
    }

    /// <summary>
    /// 检查buffer参数
    /// </summary>
    /// <param name="offset">数据的起始索引</param>
    /// <param name="bufferLength">数据的长度</param>
    /// <exception cref="ArgumentException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckBuffer(int bufferLength, int offset) {
        if (offset < 0 || offset > bufferLength) {
            throw new ArgumentException($"Array range is invalid. Buffer.length={bufferLength}, offset={offset}");
        }
    }

    #region byte

    /** C#的默认Byte类型是无符号的 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetByte(byte[] buffer, int index) {
        return buffer[index];
    }

    /** 向buffer中写入一个Byte */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetByte(byte[] buffer, int index, byte value) {
        buffer[index] = value;
    }

    /** 向buffer中写入一个Byte -- 快捷api */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetByte(byte[] buffer, int index, int value) {
        buffer[index] = (byte)value;
    }

    #endregion

    #region 大端编码

    /// <summary>
    /// 大端：向buffer中写入一个Int16
    /// </summary>
    public static void SetInt16(byte[] buffer, int index, short value) {
        buffer[index] = (byte)(value >> 8);
        buffer[index + 1] = (byte)value;
    }

    /// <summary>
    /// 大端：从buffer中读取一个Int16
    /// </summary>
    public static short GetInt16(byte[] buffer, int index) {
        return (short)((buffer[index] << 8)
                       | (buffer[index + 1] & 0xff));
    }

    /// <summary>
    /// 大端：向buffer中写入一个Int32
    /// </summary>
    public static void SetInt32(byte[] buffer, int index, int value) {
        buffer[index] = (byte)(value >> 24);
        buffer[index + 1] = (byte)(value >> 16);
        buffer[index + 2] = (byte)(value >> 8);
        buffer[index + 3] = (byte)value;
    }

    /// <summary>
    /// 大端：从buffer中读取一个Int32
    /// </summary>
    public static int GetInt32(byte[] buffer, int index) {
        return (((buffer[index] & 0xff) << 24)
                | ((buffer[index + 1] & 0xff) << 16)
                | ((buffer[index + 2] & 0xff) << 8)
                | ((buffer[index + 3] & 0xff)));
    }

    /// <summary>
    /// 大端：向buffer中写入一个UInt32
    /// </summary>
    public static void SetUInt32(byte[] buffer, int index, uint value) {
        buffer[index] = (byte)(value >> 24);
        buffer[index + 1] = (byte)(value >> 16);
        buffer[index + 2] = (byte)(value >> 8);
        buffer[index + 3] = (byte)value;
    }

    /// <summary>
    /// 大端：从buffer中读取一个UInt32
    /// </summary>
    public static uint GetUInt32(byte[] buffer, int index) {
        return (((buffer[index] & 0xffU) << 24)
                | ((buffer[index + 1] & 0xffU) << 16)
                | ((buffer[index + 2] & 0xffU) << 8)
                | ((buffer[index + 3] & 0xffU)));
    }

    /// <summary>
    /// 大端：向buffer中写入一个UInt48
    /// (写入long的低48位)
    /// </summary>
    public static void SetUInt48(byte[] buffer, int index, long value) {
        if (!MathCommon.IsUInt48(value)) {
            throw new ArgumentException($"{nameof(value)}: {value}");
        }
        buffer[index] = (byte)(value >> 40);
        buffer[index + 1] = (byte)(value >> 32);
        buffer[index + 2] = (byte)(value >> 24);
        buffer[index + 3] = (byte)(value >> 16);
        buffer[index + 4] = (byte)(value >> 8);
        buffer[index + 5] = (byte)value;
    }

    /// <summary>
    /// 大端：从buffer中读取一个UInt48
    /// (写入long的低48位)
    /// </summary>
    public static long GetUInt48(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL) << 40)
                | ((buffer[index + 1] & 0xffL) << 32)
                | ((buffer[index + 2] & 0xffL) << 24)
                | ((buffer[index + 3] & 0xffL) << 16)
                | ((buffer[index + 4] & 0xffL) << 8)
                | ((buffer[index + 5] & 0xffL)));
    }

    /// <summary>
    /// 大端：向buffer中写入一个Int64
    /// </summary>
    public static void SetInt64(byte[] buffer, int index, long value) {
        buffer[index] = (byte)(value >> 56);
        buffer[index + 1] = (byte)(value >> 48);
        buffer[index + 2] = (byte)(value >> 40);
        buffer[index + 3] = (byte)(value >> 32);
        buffer[index + 4] = (byte)(value >> 24);
        buffer[index + 5] = (byte)(value >> 16);
        buffer[index + 6] = (byte)(value >> 8);
        buffer[index + 7] = (byte)value;
    }

    /// <summary>
    /// 大端：从buffer中读取一个Int64
    /// </summary>
    public static long GetInt64(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL) << 56)
                | ((buffer[index + 1] & 0xffL) << 48)
                | ((buffer[index + 2] & 0xffL) << 40)
                | ((buffer[index + 3] & 0xffL) << 32)
                | ((buffer[index + 4] & 0xffL) << 24)
                | ((buffer[index + 5] & 0xffL) << 16)
                | ((buffer[index + 6] & 0xffL) << 8)
                | ((buffer[index + 7] & 0xffL)));
    }

    #endregion

    #region 小端编码

    /// <summary>
    /// 小端：向buffer中写入一个Int16
    /// </summary>
    public static void SetInt16LE(byte[] buffer, int index, int value) {
        buffer[index] = (byte)value;
        buffer[index + 1] = (byte)(value >> 8);
    }

    /// <summary>
    /// 小端：从buffer中读取一个Int16
    /// </summary>
    public static short GetInt16LE(byte[] buffer, int index) {
        return (short)((buffer[index] & 0xff)
                       | (buffer[index + 1] << 8));
    }

    /// <summary>
    /// 小端：向buffer中写入一个Int32
    /// </summary>
    public static void SetInt32LE(byte[] buffer, int index, int value) {
        buffer[index] = (byte)value;
        buffer[index + 1] = (byte)(value >> 8);
        buffer[index + 2] = (byte)(value >> 16);
        buffer[index + 3] = (byte)(value >> 24);
    }

    /// <summary>
    /// 小端：从buffer中读取一个Int32
    /// </summary>
    public static int GetInt32LE(byte[] buffer, int index) {
        return (((buffer[index] & 0xff))
                | ((buffer[index + 1] & 0xff) << 8)
                | ((buffer[index + 2] & 0xff) << 16)
                | ((buffer[index + 3] & 0xff) << 24));
    }

    /// <summary>
    /// 小端：向buffer中写入一个UInt32
    /// </summary>
    public static void SetUInt32LE(byte[] buffer, int index, uint value) {
        buffer[index] = (byte)value;
        buffer[index + 1] = (byte)(value >> 8);
        buffer[index + 2] = (byte)(value >> 16);
        buffer[index + 3] = (byte)(value >> 24);
    }

    /// <summary>
    /// 小端：从buffer中读取一个Int32
    /// </summary>
    public static uint GetUInt32LE(byte[] buffer, int index) {
        return (((buffer[index] & 0xffU))
                | ((buffer[index + 1] & 0xffU) << 8)
                | ((buffer[index + 2] & 0xffU) << 16)
                | ((buffer[index + 3] & 0xffU) << 24));
    }

    /// <summary>
    /// 小端：向buffer中写入一个UInt48
    /// (写入long的低48位)
    /// </summary>
    public static void SetUInt48LE(byte[] buffer, int index, long value) {
        if (!MathCommon.IsUInt48(value)) {
            throw new ArgumentException($"{nameof(value)}: {value}");
        }
        buffer[index] = (byte)value;
        buffer[index + 1] = (byte)(value >> 8);
        buffer[index + 2] = (byte)(value >> 16);
        buffer[index + 3] = (byte)(value >> 24);
        buffer[index + 4] = (byte)(value >> 32);
        buffer[index + 5] = (byte)(value >> 40);
    }

    /// <summary>
    /// 小端：从buffer中读取一个UInt48
    /// (写入long的低48位)
    /// </summary>
    public static long GetUInt48LE(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL))
                | ((buffer[index + 1] & 0xffL) << 8)
                | ((buffer[index + 2] & 0xffL) << 16)
                | ((buffer[index + 3] & 0xffL) << 24)
                | ((buffer[index + 4] & 0xffL) << 32)
                | ((buffer[index + 5] & 0xffL) << 40));
    }

    /// <summary>
    /// 小端：向buffer中写入一个Int64
    /// </summary>
    public static void SetInt64LE(byte[] buffer, int index, long value) {
        buffer[index] = (byte)value;
        buffer[index + 1] = (byte)(value >> 8);
        buffer[index + 2] = (byte)(value >> 16);
        buffer[index + 3] = (byte)(value >> 24);
        buffer[index + 4] = (byte)(value >> 32);
        buffer[index + 5] = (byte)(value >> 40);
        buffer[index + 6] = (byte)(value >> 48);
        buffer[index + 7] = (byte)(value >> 56);
    }

    /// <summary>
    /// 小端：从buffer中读取一个Int64
    /// </summary>
    public static long GetInt64LE(byte[] buffer, int index) {
        return (((buffer[index] & 0xffL))
                | ((buffer[index + 1] & 0xffL) << 8)
                | ((buffer[index + 2] & 0xffL) << 16)
                | ((buffer[index + 3] & 0xffL) << 24)
                | ((buffer[index + 4] & 0xffL) << 32)
                | ((buffer[index + 5] & 0xffL) << 40)
                | ((buffer[index + 6] & 0xffL) << 48)
                | ((buffer[index + 7] & 0xffL) << 56));
    }

    #endregion
}