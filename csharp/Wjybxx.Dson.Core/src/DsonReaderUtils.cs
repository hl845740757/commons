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
        output.WriteRawBytes(binary.UnsafeBuffer);
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
            binary = Binary.UnsafeWrap(input.ReadRawBytes(size));
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
    public static int WireTypeOfDouble4(Double4 double4) {
        int v = 0;
        if (WireTypes.BestOfDouble(double4.v0) == WireType.Uint) v |= 0x01;
        if (WireTypes.BestOfDouble(double4.v1) == WireType.Uint) v |= 0x02;
        if (WireTypes.BestOfDouble(double4.v2) == WireType.Uint) v |= 0x04;
        return v;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDouble4(IDsonOutput output, Double4 double4, int wireTypeBits) {
        if ((wireTypeBits & 0x01) != 0) {
            output.WriteVarDouble(double4.v0);
        } else {
            output.WriteDouble(double4.v0);
        }
        if ((wireTypeBits & 0x02) != 0) {
            output.WriteVarDouble(double4.v1);
        } else {
            output.WriteDouble(double4.v1);
        }
        if ((wireTypeBits & 0x04) != 0) {
            output.WriteVarDouble(double4.v2);
        } else {
            output.WriteDouble(double4.v2);
        }
        output.WriteVarDouble(double4.v3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Double4 ReadDouble4(IDsonInput input, int wireTypeBits) {
        double v0 = (wireTypeBits & 0x01) != 0 ? input.ReadVarDouble() : input.ReadDouble();
        double v1 = (wireTypeBits & 0x02) != 0 ? input.ReadVarDouble() : input.ReadDouble();
        double v2 = (wireTypeBits & 0x04) != 0 ? input.ReadVarDouble() : input.ReadDouble();
        double v3 = input.ReadVarDouble();
        return new Double4(v0, v1, v2, v3);
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
        int skip;
        switch (dsonType) {
            case DsonType.Int32: {
                wireType.ReadInt32(input);
                return;
            }
            case DsonType.Int64: {
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
                input.ReadUInt64(); // localId;
                if ((wireTypeBits & ObjectPtr.MaskCollection) != 0) {
                    skip = input.ReadUInt32(); // collection长度
                    input.SkipRawBytes(skip);
                }
                if ((wireTypeBits & ObjectPtr.MaskLocalPath) != 0) {
                    skip = input.ReadUInt32(); // localPath长度
                    input.SkipRawBytes(skip);
                }
                if ((wireTypeBits & ObjectPtr.MaskType) != 0) {
                    input.ReadUInt32();
                }
                return;
            }
            case DsonType.DateTime: {
                input.ReadUInt64();
                input.ReadUInt32();
                input.ReadSInt32();
                // input.ReadRawByte(); // 已转移到 wireTypeBits
                return;
            }
            case DsonType.Timestamp: {
                input.ReadUInt64();
                input.ReadUInt32();
                return;
            }
            case DsonType.Double4: {
                if ((wireTypeBits & 0x01) != 0) {
                    input.ReadVarDouble();
                } else {
                    input.ReadDouble();
                }
                if ((wireTypeBits & 0x02) != 0) {
                    input.ReadVarDouble();
                } else {
                    input.ReadDouble();
                }
                if ((wireTypeBits & 0x04) != 0) {
                    input.ReadVarDouble();
                } else {
                    input.ReadDouble();
                }
                input.ReadVarDouble();
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