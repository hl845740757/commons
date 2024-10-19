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
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 
/// </summary>
public class DefaultDsonObjectWriter : IDsonObjectWriter
{
    private readonly IDsonConverter converter;
    private readonly TypeWriteHelper typeWriteHelper;
    private readonly IDsonWriter<string> writer;

    public DefaultDsonObjectWriter(IDsonConverter converter, TypeWriteHelper typeWriteHelper, IDsonWriter<string> writer) {
        this.converter = converter;
        this.typeWriteHelper = typeWriteHelper;
        this.writer = writer;
    }

    #region 简单值

    public void WriteInt(string? name, int value, WireType wireType, INumberStyle style) {
        if (value != 0 || (!writer.IsAtName || converter.Options.appendDef)) {
            writer.WriteInt32(name, value, wireType, style);
        }
    }

    public void WriteLong(string? name, long value, WireType wireType, INumberStyle style) {
        if (value != 0 || (!writer.IsAtName || converter.Options.appendDef)) {
            writer.WriteInt64(name, value, wireType, style);
        }
    }

    public void WriteFloat(string? name, float value, INumberStyle style) {
        if (value != 0 || (!writer.IsAtName || converter.Options.appendDef)) {
            writer.WriteFloat(name, value, style);
        }
    }

    public void WriteDouble(string? name, double value, INumberStyle style) {
        if (value != 0 || (!writer.IsAtName || converter.Options.appendDef)) {
            writer.WriteDouble(name, value, style);
        }
    }

    public void WriteBool(string? name, bool value) {
        if (value || (!writer.IsAtName || converter.Options.appendDef)) {
            writer.WriteBool(name, value);
        }
    }

    public void WriteString(string? name, string? value, StringStyle style = StringStyle.Auto) {
        if (value == null) {
            WriteNull(name);
        } else {
            writer.WriteString(name, value, style);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNull(string? name) {
        // 用户已写入name或convert开启了null写入
        if (!writer.IsAtName || converter.Options.appendNull) {
            writer.WriteNull(name);
        }
    }

    public void WriteBytes(string? name, byte[]? bytes) {
        if (bytes == null) {
            WriteNull(name);
        } else {
            writer.WriteBinary(name, bytes, 0, bytes.Length);
        }
    }

    public void WriteBytes(string? name, byte[] bytes, int offset, int len) {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        writer.WriteBinary(name, bytes, offset, len);
    }

    public void WriteBinary(string? name, Binary binary) {
        if (binary.IsNull) {
            WriteNull(name);
        } else {
            writer.WriteBinary(name, binary);
        }
    }


    public void WritePtr(string? name, in ObjectPtr objectPtr) {
        writer.WritePtr(name, in objectPtr);
    }

    public void WriteLitePtr(string? name, in ObjectLitePtr objectLitePtr) {
        writer.WriteLitePtr(name, in objectLitePtr);
    }

    public void WriteDateTime(string? name, in DateTime dateTime) {
        writer.WriteDateTime(name, ExtDateTime.OfDateTime(in dateTime));
    }

    public void WriteExtDateTime(string? name, in ExtDateTime dateTime) {
        writer.WriteDateTime(name, in dateTime);
    }

    public void WriteTimestamp(string? name, in Timestamp timestamp) {
        writer.WriteTimestamp(name, in timestamp);
    }

    #endregion

    #region object

    public void WriteObject<T>(string? name, in T? value, Type declaredType, ObjectStyle? style = null) {
        if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
        if (value == null) {
            WriteNull(name);
            return;
        }
        // Nullable会直接返回被装箱的值的类型，而泛型参数T可能是Nullable<>，为避免装箱，我们需要转换为查找Nullable的Codec
        Type type = value.GetType();
        bool isNullable = type.IsValueType && declaredType.IsGenericType
                                           && declaredType.GetGenericTypeDefinition() == typeof(Nullable<>);
        DsonCodecImpl? codec;
        if (isNullable) {
            codec = converter.CodecRegistry.GetEncoder(declaredType);
        } else {
            codec = converter.CodecRegistry.GetEncoder(type);
        }
        if (codec != null) {
            if (writer.IsAtName) { // 写入name
                writer.WriteName(name);
            }
            ObjectStyle castStyle = style ?? FindObjectStyle(isNullable ? type : codec.GetEncoderType());
            if (codec is DsonCodecImpl<T> codecImpl) {
                codecImpl.WriteObject(this, value, declaredType, castStyle);
            } else {
                codec.WriteObject2(this, value, declaredType, castStyle); // 声明类型是object的情况下，value可能是装箱值类型
            }
            return;
        }
        // DsonValue
        if (value is DsonValue dsonValue) {
            Dsons.WriteDsonValue(writer, dsonValue, name);
            return;
        }
        throw DsonCodecException.UnsupportedType(type);
    }

    #endregion

    #region 流程

    public ConverterOptions Options => converter.Options;
    public string CurrentName => writer.CurrentName;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteName(string name) {
        writer.WriteName(name);
    }

    public void WriteTypeInfo(Type encoderType, Type declaredType) {
        TypeWritePolicy policy = converter.Options.typeWritePolicy;
        if ((policy == TypeWritePolicy.Optimized && !typeWriteHelper.IsOptimizable(encoderType, declaredType))
            || policy == TypeWritePolicy.Always) {
            TypeMeta typeMeta = converter.TypeMetaRegistry.OfType(encoderType);
            if (typeMeta == null) {
                throw new DsonCodecException($"typeMeta of encoderType: {encoderType} is absent");
            }
            writer.WriteSimpleHeader(typeMeta.MainClsName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStartObject(ObjectStyle style) {
        writer.WriteStartObject(style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEndObject() {
        writer.WriteEndObject();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStartArray(ObjectStyle style) {
        writer.WriteStartArray(style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEndArray() {
        writer.WriteEndArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteValueBytes(string name, DsonType dsonType, byte[] data) {
        writer.WriteValueBytes(name, dsonType, data);
    }

    public string EncodeKey<T>(T key) {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (key is string sv) return sv;
        if (key is int iv) return iv.ToString();
        if (key is long lv) return lv.ToString();
        if (key is uint uiv) return uiv.ToString();
        if (key is ulong ulv) return ulv.ToString();

        Type type = key.GetType();
        DsonCodecImpl<T> codecImpl = converter.CodecRegistry.GetEncoder(type) as DsonCodecImpl<T>;
        if (codecImpl == null || !codecImpl.IsEnumCodec) {
            throw DsonCodecException.UnsupportedType(type);
        }
        if (converter.Options.writeEnumAsString) {
            return codecImpl.GetName(key);
        } else {
            return codecImpl.GetNumber(key).ToString();
        }
    }

    public void Flush() {
        writer.Flush();
    }

    public void Dispose() {
        writer.Dispose();
    }

    /** 允许泛型参数不同时走不同的style */
    private ObjectStyle FindObjectStyle(Type type) {
        TypeMeta typeMeta = converter.TypeMetaRegistry.OfType(type);
        return typeMeta != null ? typeMeta.style : ObjectStyle.Indent;
    }

    #endregion

    #region 重复实现，提高效率

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStartObject(ObjectStyle style, Type encoderType, Type declaredType) {
        writer.WriteStartObject(style);
        WriteTypeInfo(encoderType, declaredType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStartObject(string name, ObjectStyle style) {
        writer.WriteName(name);
        writer.WriteStartObject(style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStartObject(string name, ObjectStyle style, Type encoderType, Type declaredType) {
        writer.WriteName(name);
        writer.WriteStartObject(style);
        WriteTypeInfo(encoderType, declaredType);
    }

    //
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStartArray(ObjectStyle style, Type encoderType, Type declaredType) {
        writer.WriteStartArray(style);
        WriteTypeInfo(encoderType, declaredType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStartArray(string name, ObjectStyle style) {
        writer.WriteName(name);
        writer.WriteStartArray(style);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStartArray(string name, ObjectStyle style, Type encoderType, Type declaredType) {
        writer.WriteName(name);
        writer.WriteStartArray(style);
        WriteTypeInfo(encoderType, declaredType);
    }

    #endregion
}
}