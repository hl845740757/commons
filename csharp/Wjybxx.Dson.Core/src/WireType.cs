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

namespace Wjybxx.Dson
{
/// <summary>
/// 数字类型字段的编码方式
/// </summary>
public enum WireType
{
    /// <summary>
    /// 按照无符号数格式优化编码，将符号位看做数据位，再进行VarInt编码。
    /// 1.该编码对正数极为友好，对负数较为糟糕。
    /// 2.int32的负数，将固定占用5个字节；
    /// 3.int64的负数，将固定占用10个字节；
    /// </summary>
    Uint = 0,

    /// <summary>
    /// 按照有符号数格式优化编码，先进行ZigZag编码，再按照VarInt编码
    /// 1.该编码会增加正数的平均编码长度，但减少负数的平均编码长度，适合负数值较多的数据。
    /// 2.ZigZag对于正数，结果为<code>value * 2</code>；对于负数，结果为：<code>Math.Abs(value) * 2 -1</code>
    /// </summary>
    Sint = 1,

    /// <summary>
    /// 固定长度编码
    /// 1. int32 固定4字节
    /// 2. int64 固定8字节
    /// </summary>
    Fixed = 2,
}

/// <summary>
/// WireType的工具类
/// </summary>
public static class WireTypes
{
    /** 通过number查找关联枚举 */
    public static WireType ForNumber(int number) {
        return number switch
        {
            0 => WireType.Uint,
            1 => WireType.Sint,
            2 => WireType.Fixed,
            _ => throw new ArgumentException($"number: {number}")
        };
    }

    #region write/read

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt32(this WireType wireType, IDsonOutput output, int value) {
        switch (wireType) {
            case WireType.Uint: {
                output.WriteUInt32(value);
                break;
            }
            case WireType.Sint: {
                output.WriteSInt32(value);
                break;
            }
            case WireType.Fixed: {
                output.WriteFixed32(value);
                break;
            }
            default:
                throw new AssertionError();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ReadInt32(this WireType wireType, IDsonInput input) {
        return wireType switch
        {
            WireType.Uint => input.ReadUInt32(),
            WireType.Sint => input.ReadSInt32(),
            WireType.Fixed => input.ReadFixed32(),
            _ => throw new AssertionError()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteInt64(this WireType wireType, IDsonOutput output, long value) {
        switch (wireType) {
            case WireType.Uint: {
                output.WriteUInt64(value);
                break;
            }
            case WireType.Sint: {
                output.WriteSInt64(value);
                break;
            }
            case WireType.Fixed: {
                output.WriteFixed64(value);
                break;
            }
            default:
                throw new AssertionError();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ReadInt64(this WireType wireType, IDsonInput input) {
        return wireType switch
        {
            WireType.Uint => input.ReadUInt64(),
            WireType.Sint => input.ReadSInt64(),
            WireType.Fixed => input.ReadFixed64(),
            _ => throw new AssertionError()
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFloat(this WireType wireType, IDsonOutput output, float value) {
        if (wireType == WireType.Uint) {
            output.WriteVarFloat(value);
        } else {
            output.WriteFloat(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float ReadFloat(this WireType wireType, IDsonInput input) {
        return wireType == WireType.Uint ? input.ReadVarFloat() : input.ReadFloat();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDouble(this WireType wireType, IDsonOutput output, double value) {
        if (wireType == WireType.Uint) {
            output.WriteVarDouble(value);
        } else {
            output.WriteDouble(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ReadDouble(this WireType wireType, IDsonInput input) {
        return wireType == WireType.Uint ? input.ReadVarDouble() : input.ReadDouble();
    }
    
    #endregion

    #region 计算最佳WireType

    private const int INT_COMPRESS_MASK = (1 << 21) - 1; // 低21位
    private const long LONG_COMPRESS_MASK = (1L << 49) - 1; // 低49位
    private const int FLOAT_COMPRESS_MASK = ~(-1 << 11); // 高21位
    private const long DOUBLE_COMPRESS_MASK = ~(-1L << 15); // 高49位


    /** 计算int32的最佳序列化格式 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WireType BestOfInt32(int value) {
        if (value > INT_COMPRESS_MASK) return WireType.Fixed;
        if (value > 0) return WireType.Uint;
        if (value > -(INT_COMPRESS_MASK / 2)) return WireType.Sint;
        return WireType.Fixed;
    }

    /** 计算int64的最佳序列化格式 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WireType BestOfInt64(long value) {
        if (value > LONG_COMPRESS_MASK) return WireType.Fixed;
        if (value > 0) return WireType.Uint;
        if (value > -(LONG_COMPRESS_MASK / 2)) return WireType.Sint;
        return WireType.Fixed;
    }

    /** 计算float的最佳序列化格式 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WireType BestOfFloat(float value) {
        int rawBits = BitConverter.SingleToInt32Bits(value);
        // 当变长编码的开销更小时，使用变长编码 -- Float变长编码3字节可表达21个有效位，即后11位为0
        return (rawBits & FLOAT_COMPRESS_MASK) == 0 ? WireType.Uint : WireType.Fixed;
    }

    /** 计算double的最佳序列化格式 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WireType BestOfDouble(double value) {
        long rawBits = BitConverter.DoubleToInt64Bits(value);
        // 当变长编码的开销更小时，使用变长编码 -- Double变长编码7字节可表达49个有效位，即后15位为0
        return (rawBits & DOUBLE_COMPRESS_MASK) == 0 ? WireType.Uint : WireType.Fixed;
    }

    #endregion
}
}