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
using System.Collections.Generic;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson二进制编解码工具类
/// </summary>
public static class DsonReaderUtils
{
    /** 支持读取为bytes和直接写入bytes的数据类型 -- 这些类型不可以存储额外数据在WireType上 */
    public static readonly IList<DsonType> ValueBytesTypes = new[]
    {
        DsonType.String, DsonType.Binary, DsonType.Array, DsonType.Object, DsonType.Header
    }.ToImmutableList2();

    #region number

    public static void WriteInt32(IDsonOutput output, int value, WireType wireType) {
        switch (wireType) {
            case WireType.VarInt: {
                output.WriteInt32(value);
                break;
            }
            case WireType.Uint: {
                output.WriteUint32(value);
                break;
            }
            case WireType.Sint: {
                output.WriteSint32(value);
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

    public static int ReadInt32(IDsonInput input, WireType wireType) {
        switch (wireType) {
            case WireType.VarInt: {
                return input.ReadInt32();
            }
            case WireType.Uint: {
                return input.ReadUint32();
            }
            case WireType.Sint: {
                return input.ReadSint32();
            }
            case WireType.Fixed: {
                return input.ReadFixed32();
            }
            default:
                throw new AssertionError();
        }
    }

    public static void WriteInt64(IDsonOutput output, long value, WireType wireType) {
        switch (wireType) {
            case WireType.VarInt: {
                output.WriteInt64(value);
                break;
            }
            case WireType.Uint: {
                output.WriteUint64(value);
                break;
            }
            case WireType.Sint: {
                output.WriteSint64(value);
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

    public static long ReadInt64(IDsonInput input, WireType wireType) {
        switch (wireType) {
            case WireType.VarInt: {
                return input.ReadInt64();
            }
            case WireType.Uint: {
                return input.ReadUint64();
            }
            case WireType.Sint: {
                return input.ReadSint64();
            }
            case WireType.Fixed: {
                return input.ReadFixed64();
            }
            default:
                throw new AssertionError();
        }
    }

    /**
     * 1.浮点数的前16位固定写入，因此只统计后16位
     * 2.wireType表示后导0对应的字节数
     * 3.由于编码依赖了上层的wireType比特位，因此不能写在Output接口中
     */
    public static int WireTypeOfFloat(float value) {
        int rawBits = BitConverter.SingleToInt32Bits(value);
        if ((rawBits & 0xFF) != 0) return 0;
        if ((rawBits & 0xFF00) != 0) return 1;
        return 2;
    }

    /** 小端编码，从末尾非0开始写入 */
    public static void WriteFloat(IDsonOutput output, float value, int wireType) {
        if (wireType == 0) {
            output.WriteFloat(value);
            return;
        }

        int rawBits = BitConverter.SingleToInt32Bits(value);
        for (int i = 0; i < wireType; i++) {
            rawBits = rawBits >> 8;
        }
        for (int i = wireType; i < 4; i++) {
            output.WriteRawByte((byte)rawBits);
            rawBits = rawBits >> 8;
        }
    }

    public static float ReadFloat(IDsonInput input, int wireType) {
        if (wireType == 0) {
            return input.ReadFloat();
        }

        int rawBits = 0;
        for (int i = wireType; i < 4; i++) {
            rawBits |= (input.ReadRawByte() & 0XFF) << (8 * i);
        }
        return BitConverter.Int32BitsToSingle(rawBits);
    }

    /**
     * 1.浮点数的前16位固定写入，因此只统计后48位
     * 2.wireType表示后导0对应的字节数
     * 3.由于编码依赖了上层的wireType比特位，因此不能写在Output接口中
     */
    public static int WireTypeOfDouble(double value) {
        long rawBits = BitConverter.DoubleToInt64Bits(value);
        if ((rawBits & 0xFFL) != 0) return 0;
        if ((rawBits & 0xFF00L) != 0) return 1;
        if ((rawBits & 0xFF_0000L) != 0) return 2;
        if ((rawBits & 0xFF00_0000L) != 0) return 3;
        if ((rawBits & 0xFF_0000_0000L) != 0) return 4;
        if ((rawBits & 0xFF00_0000_0000L) != 0) return 5;
        return 6;
    }

    public static void WriteDouble(IDsonOutput output, double value, int wireType) {
        if (wireType == 0) {
            output.WriteDouble(value);
            return;
        }

        long rawBits = BitConverter.DoubleToInt64Bits(value);
        for (int i = 0; i < wireType; i++) {
            rawBits = rawBits >> 8;
        }
        for (int i = wireType; i < 8; i++) {
            output.WriteRawByte((byte)rawBits);
            rawBits = rawBits >> 8;
        }
    }

    public static double ReadDouble(IDsonInput input, int wireType) {
        if (wireType == 0) {
            return input.ReadDouble();
        }

        long rawBits = 0;
        for (int i = wireType; i < 8; i++) {
            rawBits |= (input.ReadRawByte() & 0XFFL) << (8 * i);
        }
        return BitConverter.Int64BitsToDouble(rawBits);
    }

    public static bool ReadBool(IDsonInput input, int wireTypeBits) {
        if (wireTypeBits == 1) {
            return true;
        }
        if (wireTypeBits == 0) {
            return false;
        }
        throw new DsonIOException("invalid wireType for bool, bits: " + wireTypeBits);
    }

    #endregion

    #region binary

    public static void WriteBinary(IDsonOutput output, Binary binary) {
        output.WriteUint32(binary.Length);
        output.WriteRawBytes(binary.UnsafeBuffer);
    }

    public static void WriteBinary(IDsonOutput output, DsonChunk chunk) {
        output.WriteUint32(chunk.Length);
        output.WriteRawBytes(chunk.Buffer, chunk.Offset, chunk.Length);
    }

    public static Binary ReadBinary(IDsonInput input) {
        int size = input.ReadUint32();
        int oldLimit = input.PushLimit(size);
        Binary binary;
        {
            binary = Binary.UnsafeWrap(input.ReadRawBytes(size));
        }
        input.PopLimit(oldLimit);
        return binary;
    }

    #endregion

    #region 内置结构体

    public static int WireTypeOfPtr(in ObjectPtr objectPtr) {
        int v = 0;
        if (objectPtr.HasNamespace) {
            v |= ObjectPtr.MaskNamespace;
        }
        if (objectPtr.Type != 0) {
            v |= ObjectPtr.MaskType;
        }
        if (objectPtr.Policy != 0) {
            v |= ObjectPtr.MaskPolicy;
        }
        return v;
    }

    public static void WritePtr(IDsonOutput output, in ObjectPtr objectPtr) {
        output.WriteString(objectPtr.LocalId);
        if (objectPtr.HasNamespace) {
            output.WriteString(objectPtr.Namespace);
        }
        if (objectPtr.Type != 0) {
            output.WriteRawByte(objectPtr.Type);
        }
        if (objectPtr.Policy != 0) {
            output.WriteRawByte(objectPtr.Policy);
        }
    }

    public static ObjectPtr ReadPtr(IDsonInput input, int wireTypeBits) {
        string localId = input.ReadString();
        string ns = DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskNamespace) ? input.ReadString() : null;
        byte type = DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskType) ? input.ReadRawByte() : (byte)0;
        byte policy = DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskPolicy) ? input.ReadRawByte() : (byte)0;
        return new ObjectPtr(localId, ns, type, policy);
    }

    public static int WireTypeOfLitePtr(in ObjectLitePtr objectLitePtr) {
        int v = 0;
        if (objectLitePtr.HasNamespace) {
            v |= ObjectPtr.MaskNamespace;
        }
        if (objectLitePtr.Type != 0) {
            v |= ObjectPtr.MaskType;
        }
        if (objectLitePtr.Policy != 0) {
            v |= ObjectPtr.MaskPolicy;
        }
        return v;
    }

    public static void WriteLitePtr(IDsonOutput output, in ObjectLitePtr objectLiteRef) {
        output.WriteUint64(objectLiteRef.LocalId);
        if (objectLiteRef.HasNamespace) {
            output.WriteString(objectLiteRef.Namespace);
        }
        if (objectLiteRef.Type != 0) {
            output.WriteRawByte(objectLiteRef.Type);
        }
        if (objectLiteRef.Policy != 0) {
            output.WriteRawByte(objectLiteRef.Policy);
        }
    }

    public static ObjectLitePtr ReadLitePtr(IDsonInput input, int wireTypeBits) {
        long localId = input.ReadUint64();
        string ns = DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskNamespace) ? input.ReadString() : null;
        byte type = DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskType) ? input.ReadRawByte() : (byte)0;
        byte policy = DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskPolicy) ? input.ReadRawByte() : (byte)0;
        return new ObjectLitePtr(localId, ns, type, policy);
    }

    public static void WriteDateTime(IDsonOutput output, in ExtDateTime dateTime) {
        output.WriteUint64(dateTime.Seconds);
        output.WriteUint32(dateTime.Nanos);
        output.WriteSint32(dateTime.Offset);
        // output.WriteRawByte(dateTime.Enables);
    }

    public static ExtDateTime ReadDateTime(IDsonInput input, int wireTypeBits) {
        return new ExtDateTime(
            input.ReadUint64(),
            input.ReadUint32(),
            input.ReadSint32(),
            (byte)wireTypeBits);
    }

    public static void WriteTimestamp(IDsonOutput output, in Timestamp timestamp) {
        output.WriteUint64(timestamp.Seconds);
        output.WriteUint32(timestamp.Nanos);
    }

    public static Timestamp ReadTimestamp(IDsonInput input) {
        return new Timestamp(
            input.ReadUint64(),
            input.ReadUint32());
    }

    #endregion

    #region 特殊

    public static void WriteValueBytes(IDsonOutput output, DsonType dsonType, byte[] data) {
        if (dsonType == DsonType.String || dsonType == DsonType.Binary) {
            output.WriteUint32(data.Length);
        } else {
            output.WriteFixed32(data.Length);
        }
        output.WriteRawBytes(data);
    }

    public static byte[] ReadValueAsBytes(IDsonInput input, DsonType dsonType) {
        int size;
        if (dsonType == DsonType.String || dsonType == DsonType.Binary) {
            size = input.ReadUint32();
        } else {
            size = input.ReadFixed32();
        }
        return input.ReadRawBytes(size);
    }

    public static void CheckReadValueAsBytes(DsonType dsonType) {
        if (!ValueBytesTypes.Contains(dsonType)) {
            throw DsonIOException.InvalidDsonType(ValueBytesTypes, dsonType);
        }
    }

    public static void CheckWriteValueAsBytes(DsonType dsonType) {
        if (!ValueBytesTypes.Contains(dsonType)) {
            throw DsonIOException.InvalidDsonType(ValueBytesTypes, dsonType);
        }
    }

    public static void SkipToEndOfObject(IDsonInput input) {
        int size = input.GetBytesUntilLimit();
        if (size > 0) {
            input.SkipRawBytes(size);
        }
    }

    #endregion

    public static void SkipValue(IDsonInput input, DsonContextType contextType,
                                 DsonType dsonType, WireType wireType, int wireTypeBits) {
        int skip;
        switch (dsonType) {
            case DsonType.Float: {
                skip = 4 - wireTypeBits;
                break;
            }
            case DsonType.Double: {
                skip = 8 - wireTypeBits;
                break;
            }
            case DsonType.Bool:
            case DsonType.Null: {
                return;
            }
            case DsonType.Int32: {
                ReadInt32(input, wireType);
                return;
            }
            case DsonType.Int64: {
                ReadInt64(input, wireType);
                return;
            }
            case DsonType.String: {
                skip = input.ReadUint32(); // string长度
                break;
            }
            case DsonType.Binary: {
                skip = input.ReadUint32(); // length(data)
                break;
            }
            case DsonType.Pointer: {
                skip = input.ReadUint32(); // localId长度
                input.SkipRawBytes(skip);

                if (DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskNamespace)) {
                    skip = input.ReadUint32(); // namespace长度
                    input.SkipRawBytes(skip);
                }
                if (DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskType)) {
                    input.ReadRawByte();
                }
                if (DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskPolicy)) {
                    input.ReadRawByte();
                }
                return;
            }
            case DsonType.LitePointer: {
                input.ReadUint64(); // localId
                if (DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskNamespace)) {
                    skip = input.ReadUint32(); // namespace长度
                    input.SkipRawBytes(skip);
                }
                if (DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskType)) {
                    input.ReadRawByte();
                }
                if (DsonInternals.IsSet(wireTypeBits, ObjectPtr.MaskPolicy)) {
                    input.ReadRawByte();
                }
                return;
            }
            case DsonType.DateTime: {
                input.ReadUint64();
                input.ReadUint32();
                input.ReadSint32();
                // input.ReadRawByte();
                return;
            }
            case DsonType.Timestamp: {
                input.ReadUint64();
                input.ReadUint32();
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