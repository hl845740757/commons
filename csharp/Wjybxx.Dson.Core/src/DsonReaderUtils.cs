#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson二进制编解码工具类
/// </summary>
public static class DsonReaderUtils
{
    // 其实可以使用Bit位，但该数据访问频率低先不处理
    /** 支持读取为bytes和直接写入bytes的数据类型 -- 这些类型不可以存储额外数据在WireType上 */
    private static readonly ImmutableList<DsonType> ValueBytesTypes = new[]
    {
        DsonType.String, DsonType.Binary, DsonType.Array, DsonType.Object, DsonType.Header
    }.ToImmutableList2();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ReadBool(IDsonInput input, int wireTypeBits) {
        if (wireTypeBits == 1) {
            return true;
        }
        if (wireTypeBits == 0) {
            return false;
        }
        throw new DsonIOException("invalid wireType for bool, bits: " + wireTypeBits);
    }

    #region binary

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBinary(IDsonOutput output, Binary binary) {
        output.WriteUInt32(binary.Length);
        output.WriteRawBytes(binary.Unwrap());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteBinary(IDsonOutput output, byte[] bytes, int offset, int len) {
        output.WriteUInt32(len);
        output.WriteRawBytes(bytes, offset, len);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Binary ReadBinary(IDsonInput input) {
        int size = input.ReadUInt32();
        int oldLimit = input.PushLimit(size);
        Binary binary;
        {
            binary = Binary.Wrap(input.ReadRawBytes(size));
        }
        input.PopLimit(oldLimit);
        return binary;
    }

    #endregion

    #region 内置结构体

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WireTypeOfPtr(ObjectPtr objectPtr) {
        int v = 0;
        if (objectPtr.HashLocalPath) {
            v |= ObjectPtr.MaskLocalPath;
        }
        if (objectPtr.HasCollection) {
            v |= ObjectPtr.MaskCollection;
        }
        if (objectPtr.Type != 0) {
            v |= ObjectPtr.MaskType;
        }
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WritePtr(IDsonOutput output, ObjectPtr objectPtr) {
        output.WriteUInt64(objectPtr.LocalId);
        if (objectPtr.HasCollection) {
            output.WriteString(objectPtr.Collection);
        }
        if (objectPtr.HashLocalPath) {
            output.WriteString(objectPtr.LocalPath);
        }
        if (objectPtr.Type != 0) {
            output.WriteUInt32(objectPtr.Type);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ObjectPtr ReadPtr(IDsonInput input, int wireTypeBits) {
        long localId = input.ReadUInt64();
        string collection = (wireTypeBits & ObjectPtr.MaskCollection) != 0 ? input.ReadString() : null;
        string localPath = (wireTypeBits & ObjectPtr.MaskLocalPath) != 0 ? input.ReadString() : null;
        int type = (wireTypeBits & ObjectPtr.MaskType) != 0 ? input.ReadUInt32() : 0;
        return new ObjectPtr(collection, localPath, localId, type);
    }

    private static void SkipPtr(IDsonInput input, int wireTypeBits) {
        input.ReadUInt64();
        if ((wireTypeBits & ObjectPtr.MaskCollection) != 0) {
            int len = input.ReadUInt32();
            input.SkipRawBytes(len);
        }
        if ((wireTypeBits & ObjectPtr.MaskLocalPath) != 0) {
            int len = input.ReadUInt32();
            input.SkipRawBytes(len);
        }
        if ((wireTypeBits & ObjectPtr.MaskType) != 0) {
            input.ReadUInt32();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDateTime(IDsonOutput output, ExtDateTime dateTime) {
        output.WriteUInt64(dateTime.Seconds);
        output.WriteUInt32(dateTime.Nanos);
        output.WriteSInt32(dateTime.Offset);
        // output.WriteRawByte(dateTime.Enables);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ExtDateTime ReadDateTime(IDsonInput input, int wireTypeBits) {
        return new ExtDateTime(
            input.ReadUInt64(),
            input.ReadUInt32(),
            input.ReadSInt32(),
            (byte)wireTypeBits);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteTimestamp(IDsonOutput output, Timestamp timestamp) {
        output.WriteUInt64(timestamp.Seconds);
        output.WriteUInt32(timestamp.Nanos);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Timestamp ReadTimestamp(IDsonInput input) {
        return new Timestamp(
            input.ReadUInt64(),
            input.ReadUInt32());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDouble4(IDsonOutput output, Double4 double4) {
        WireType w0 = WireTypes.BestOfDouble(double4.v0);
        WireType w1 = WireTypes.BestOfDouble(double4.v1);
        WireType w2 = WireTypes.BestOfDouble(double4.v2);
        WireType w3 = WireTypes.BestOfDouble(double4.v3);
        int mask = (int)w0 | ((int)w1 << 2) | ((int)w2 << 4) | ((int)w3 << 6);
        //
        output.WriteRawByte((byte)mask);
        w0.WriteDouble(output, double4.v0);
        w1.WriteDouble(output, double4.v1);
        w2.WriteDouble(output, double4.v2);
        w3.WriteDouble(output, double4.v3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 ReadDouble4(IDsonInput input, int wireTypeBits) {
        int mask = input.ReadRawByte();
        WireType w0 = (WireType)(mask & 3);
        WireType w1 = (WireType)((mask >> 2) & 3);
        WireType w2 = (WireType)((mask >> 4) & 3);
        WireType w3 = (WireType)((mask >> 6) & 3);
        return new Double4(
            w0.ReadDouble(input),
            w1.ReadDouble(input),
            w2.ReadDouble(input),
            w3.ReadDouble(input));
    }


    public static void WriteLong4(IDsonOutput output, Long4 value) {
        WireType w0 = WireTypes.BestOfInt64(value.v0);
        WireType w1 = WireTypes.BestOfInt64(value.v1);
        WireType w2 = WireTypes.BestOfInt64(value.v2);
        WireType w3 = WireTypes.BestOfInt64(value.v3);
        int mask = (int)w0 | ((int)w1 << 2) | ((int)w2 << 4) | ((int)w3 << 6);
        //
        output.WriteRawByte((byte)mask);
        w0.WriteInt64(output, value.v0);
        w1.WriteInt64(output, value.v1);
        w2.WriteInt64(output, value.v2);
        w3.WriteInt64(output, value.v3);
    }

    public static Long4 ReadLong4(IDsonInput input, int wireTypeBits) {
        int mask = input.ReadRawByte();
        WireType w0 = (WireType)(mask & 3);
        WireType w1 = (WireType)((mask >> 2) & 3);
        WireType w2 = (WireType)((mask >> 4) & 3);
        WireType w3 = (WireType)((mask >> 6) & 3);
        //
        return new Long4(
            w0.ReadInt64(input),
            w1.ReadInt64(input),
            w2.ReadInt64(input),
            w3.ReadInt64(input));
    }

    public static void WriteFxp4(IDsonOutput output, Fxp4 value) {
        WireType w0 = WireTypes.BestOfInt64(value.v0.rawValue);
        WireType w1 = WireTypes.BestOfInt64(value.v1.rawValue);
        WireType w2 = WireTypes.BestOfInt64(value.v2.rawValue);
        WireType w3 = WireTypes.BestOfInt64(value.v3.rawValue);
        int mask = (int)w0 | ((int)w1 << 2) | ((int)w2 << 4) | ((int)w3 << 6);
        //
        output.WriteRawByte((byte)mask);
        w0.WriteInt64(output, value.v0.rawValue);
        w1.WriteInt64(output, value.v1.rawValue);
        w2.WriteInt64(output, value.v2.rawValue);
        w3.WriteInt64(output, value.v3.rawValue);
    }

    public static Fxp4 ReadFxp4(IDsonInput input, int wireTypeBits) {
        int mask = input.ReadRawByte();
        WireType w0 = (WireType)(mask & 3);
        WireType w1 = (WireType)((mask >> 2) & 3);
        WireType w2 = (WireType)((mask >> 4) & 3);
        WireType w3 = (WireType)((mask >> 6) & 3);
        //
        Fxp64 v0 = new Fxp64(w0.ReadInt64(input));
        Fxp64 v1 = new Fxp64(w1.ReadInt64(input));
        Fxp64 v2 = new Fxp64(w2.ReadInt64(input));
        Fxp64 v3 = new Fxp64(w3.ReadInt64(input));
        return new Fxp4(v0, v1, v2, v3);
    }

    #endregion

    #region 特殊

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteValueBytes(IDsonOutput output, DsonType dsonType, byte[] data) {
        if (dsonType == DsonType.String || dsonType == DsonType.Binary) {
            output.WriteUInt32(data.Length);
        } else {
            output.WriteFixed32(data.Length);
        }
        output.WriteRawBytes(data);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] ReadValueAsBytes(IDsonInput input, DsonType dsonType) {
        int size;
        if (dsonType == DsonType.String || dsonType == DsonType.Binary) {
            size = input.ReadUInt32();
        } else {
            size = input.ReadFixed32();
        }
        return input.ReadRawBytes(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckReadValueAsBytes(DsonType dsonType) {
        if (!ValueBytesTypes.Contains(dsonType)) {
            throw DsonIOException.InvalidDsonType(ValueBytesTypes, dsonType);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CheckWriteValueAsBytes(DsonType dsonType) {
        if (!ValueBytesTypes.Contains(dsonType)) {
            throw DsonIOException.InvalidDsonType(ValueBytesTypes, dsonType);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SkipToEndOfObject(IDsonInput input) {
        int size = input.GetBytesUntilLimit();
        if (size > 0) {
            input.SkipRawBytes(size);
        }
    }

    #endregion

    public static void SkipValue(IDsonInput input, DsonContextType contextType,
                                 DsonType dsonType, WireType wireType, int wireTypeBits) {
        int skip; // 不构建引用的类型可以直接调用对应的Read方法
        switch (dsonType) {
            case DsonType.Int32: {
                wireType.ReadInt32(input);
                return;
            }
            case DsonType.Int64:
            case DsonType.Fxp64: {
                wireType.ReadInt64(input);
                return;
            }
            case DsonType.Float: {
                wireType.ReadFloat(input);
                return;
            }
            case DsonType.Double: {
                wireType.ReadDouble(input);
                return;
            }
            case DsonType.Bool:
            case DsonType.Null: {
                return;
            }
            case DsonType.String: {
                skip = input.ReadUInt32(); // string长度
                break;
            }
            case DsonType.Binary: {
                skip = input.ReadUInt32(); // length(data)
                break;
            }
            case DsonType.Pointer: {
                SkipPtr(input, wireTypeBits); // 避免构建字符串
                return;
            }
            case DsonType.DateTime: {
                ReadDateTime(input, wireTypeBits);
                return;
            }
            case DsonType.Timestamp: {
                ReadTimestamp(input);
                return;
            }
            case DsonType.Fxp4: {
                ReadFxp4(input, wireTypeBits);
                return;
            }
            case DsonType.Double4: {
                ReadDouble4(input, wireTypeBits);
                return;
            }
            case DsonType.Long4: {
                ReadLong4(input, wireTypeBits);
                return;
            }
            case DsonType.Header: {
                skip = input.ReadFixed16();
                break;
            }
            case DsonType.Array:
            case DsonType.Object: {
                skip = input.ReadFixed32();
                break;
            }
            default: {
                throw DsonIOException.InvalidDsonType(contextType, dsonType);
            }
        }
        if (skip > 0) {
            input.SkipRawBytes(skip);
        }
    }

    public static DsonReaderGuide WhatShouldIDo(DsonContextType contextType, DsonReaderState state) {
        if (contextType == DsonContextType.TopLevel) {
            if (state == DsonReaderState.EndOfFile) {
                return DsonReaderGuide.Close;
            }
            if (state == DsonReaderState.Value) {
                return DsonReaderGuide.ReadValue;
            }
            return DsonReaderGuide.ReadType;
        }
        switch (state) {
            case DsonReaderState.Type: return DsonReaderGuide.ReadType;
            case DsonReaderState.Value: return DsonReaderGuide.ReadValue;
            case DsonReaderState.Name: return DsonReaderGuide.ReadName;
            case DsonReaderState.WaitStartObject: {
                if (contextType == DsonContextType.Header) {
                    return DsonReaderGuide.StartHeader;
                }
                if (contextType == DsonContextType.Array) {
                    return DsonReaderGuide.StartArray;
                }
                return DsonReaderGuide.StartObject;
            }
            case DsonReaderState.WaitEndObject: {
                if (contextType == DsonContextType.Header) {
                    return DsonReaderGuide.EndHeader;
                }
                if (contextType == DsonContextType.Array) {
                    return DsonReaderGuide.EndArray;
                }
                return DsonReaderGuide.EndObject;
            }
            case DsonReaderState.Initial:
            case DsonReaderState.EndOfFile:
            default:
                throw new InvalidOperationException("invalid state " + state);
        }
    }
}
}