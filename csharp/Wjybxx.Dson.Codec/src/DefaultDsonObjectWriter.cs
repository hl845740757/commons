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
    private readonly IDsonWriter<string> writer;

    public DefaultDsonObjectWriter(IDsonConverter converter, IDsonWriter<string> writer) {
        this.converter = converter;
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

    public void WriteNull(string? name) {
        // 用户已写入name或convert开启了null写入
        if (!writer.IsAtName || converter.Options.appendNull) {
            writer.WriteNull(name);
        }
    }

    public void WriteBytes(string? name, byte[]? value) {
        if (value == null) {
            WriteNull(name);
        } else {
            writer.WriteBinary(name, Binary.UnsafeWrap(value));
        }
    }

    public void WriteBinary(string? name, Binary binary) {
        if (binary.IsNull) {
            WriteNull(name);
        } else {
            writer.WriteBinary(name, binary);
        }
    }

    public void WriteBinary(string? name, DsonChunk chunk) {
        writer.WriteBinary(name, chunk);
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
        // 常见基础类型也在CodecRegistry中
        Type type = value.GetType();
        DsonCodecImpl? codec = FindObjectEncoder(type);
        if (codec != null) {
            if (writer.IsAtName) { // 写入name
                writer.WriteName(name);
            }
            ObjectStyle castStyle = style ?? FindObjectStyle(type);
            // 注意：value的运行时类型不一定是T，因此类型转型时只能测试T
            if (codec.GetEncoderClass() == typeof(T)) {
                DsonCodecImpl<T> codecImpl = (DsonCodecImpl<T>)codec;
                codecImpl.WriteObject(this, value, declaredType, castStyle);
            } else {
                // 这里value通常不是结构体，不会被装箱
                codec.WriteObject2(this, value, declaredType, castStyle);
            }
            return;
        }
        // 类型补充
        if (value is byte[] bytes) {
            WriteBytes(name, bytes);
            return;
        }
        if (value is short shortVal) {
            WriteInt(name, shortVal, WireType.VarInt, NumberStyles.Simple);
            return;
        }
        if (value is byte byteVal) {
            WriteInt(name, byteVal, WireType.VarInt, NumberStyles.Simple);
            return;
        }
        if (value is sbyte sbyteVal) {
            WriteInt(name, sbyteVal, WireType.VarInt, NumberStyles.Simple);
            return;
        }
        if (value is char charVal) {
            WriteInt(name, charVal, WireType.VarInt, NumberStyles.Simple);
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

    public void WriteName(string name) {
        writer.WriteName(name);
    }

    public void WriteStartObject<T>(in T value, Type declaredType, ObjectStyle style = ObjectStyle.Indent) {
        writer.WriteStartObject(style);
        WriteClassId(in value, declaredType);
    }


    public void WriteEndObject() {
        writer.WriteEndObject();
    }

    public void WriteStartArray<T>(in T value, Type declaredType, ObjectStyle style = ObjectStyle.Indent) {
        writer.WriteStartArray(style);
        WriteClassId(in value, declaredType);
    }

    public void WriteEndArray() {
        writer.WriteEndArray();
    }

    public void WriteValueBytes(string name, DsonType dsonType, byte[] data) {
        if (data == null) throw new ArgumentNullException(nameof(data));
        writer.WriteValueBytes(name, dsonType, data);
    }

    public string EncodeKey<T>(T key) {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (key is string sv) {
            return sv;
        }
        if (key is int iv) {
            return iv.ToString();
        }
        if (key is long lv) {
            return lv.ToString();
        }
        Type type = key.GetType();
        if (!type.IsEnum) {
            throw DsonCodecException.UnsupportedType(type);
        }
        IDsonCodecRegistry rootRegistry = converter.CodecRegistry;
        DsonCodecImpl<T> codecImpl = (DsonCodecImpl<T>)rootRegistry.GetEncoder(type, rootRegistry);
        if (codecImpl == null) {
            throw DsonCodecException.UnsupportedType(type);
        }
        if (converter.Options.writeEnumAsString) {
            return codecImpl.GetName(key);
        } else {
            return codecImpl.GetNumber(key).ToString();
        }
    }

    public void Println() {
        if (writer is DsonTextWriter textWriter) {
            textWriter.Println();
        }
    }

    public void Flush() {
        writer.Flush();
    }

    public void Dispose() {
        writer.Dispose();
    }

    private void WriteClassId<T>(in T value, Type declaredType) {
        Type type = value!.GetType();
        if (!converter.Options.classIdPolicy.Test(declaredType, type)) {
            return;
        }
        TypeMeta typeMeta = converter.TypeMetaRegistry.OfType(type);
        if (typeMeta != null) {
            writer.WriteSimpleHeader(typeMeta.MainClsName);
        }
    }

    private ObjectStyle FindObjectStyle(Type type) {
        TypeMeta typeMeta = converter.TypeMetaRegistry.OfType(type);
        return typeMeta != null ? typeMeta.style : ObjectStyle.Indent;
    }

    private DsonCodecImpl? FindObjectEncoder(Type type) {
        IDsonCodecRegistry rootRegistry = converter.CodecRegistry;
        return rootRegistry.GetEncoder(type, rootRegistry);
    }

    #endregion
}
}