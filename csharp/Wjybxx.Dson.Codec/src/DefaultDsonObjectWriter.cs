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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
internal class DefaultDsonObjectWriter : IDsonObjectWriter
{
#nullable disable
    private DefaultDsonConverter converter;
    private IDsonWriter<string> writer;

    private long _nextLocalId;
    private readonly LinkedDictionary<object, ObjectPtr> referenceTable = new(ReferenceComparer.Inst);
    private ObjectPtr _stack;
#nullable restore
    private DefaultDsonObjectWriter() {
    }

    private static readonly ConcurrentObjectPool<DefaultDsonObjectWriter> pool = new(
        () => new DefaultDsonObjectWriter(), e => e.Dispose());

    public static DefaultDsonObjectWriter GetPooled() {
        return pool.Acquire();
    }

    public static void Release(DefaultDsonObjectWriter reader) {
        pool.Release(reader);
    }

    public void Init(DefaultDsonConverter converter, IDsonWriter<string> writer) {
        this.converter = converter;
        this.writer = writer;
    }

    public void AddReference(object reference, ObjectPtr ptr) {
        if (reference == null) {
            throw new ArgumentNullException(nameof(reference));
        }
        if (!ptr.HasCollection) {
            _nextLocalId = Math.Max(_nextLocalId, ptr.LocalId);
        }
        referenceTable.Add(reference, ptr);
    }

    public ObjectPtr AddReference(object reference) {
        if (reference == null) {
            throw new ArgumentNullException(nameof(reference));
        }
        // 如果是值类型，占用localId不影响正确性
        if (referenceTable.TryGetValue(reference, out ObjectPtr ptr)) {
            return ptr;
        }
        ptr = new ObjectPtr(++_nextLocalId);
        referenceTable.Add(reference, ptr);
        return ptr;
    }

    public void AddReferences(IEnumerable collection) {
        foreach (object obj in collection) {
            AddReference(obj ?? throw new NullReferenceException("collection has null elements"));
        }
    }

    public void WriteAll(Type declaredType, SerializeFeatures features) {
        if (referenceTable.Count == 0) {
            return;
        }
        var pair = referenceTable.PeekFirst();
        object reference = pair.Key;
        _stack = pair.Value;
        do {
            WriteObject<object>(null!, reference, declaredType, features);

        } while (referenceTable.NextKey(reference, out reference, out _stack));
    }
#nullable disable

    #region 简单值

    public void WriteInt(string name, int value, SerializeFeatures features) {
        if (value != 0 || !writer.IsAtName || IsWriteZeroValue(features)) {
            writer.WriteInt32(name, value, features.ToNumberStyle());
        }
    }

    public void WriteLong(string name, long value, SerializeFeatures features) {
        if (value != 0 || !writer.IsAtName || IsWriteZeroValue(features)) {
            writer.WriteInt64(name, value, features.ToNumberStyle());
        }
    }

    public void WriteFloat(string name, float value, SerializeFeatures features) {
        if (value != 0 || !writer.IsAtName || IsWriteZeroValue(features)) {
            writer.WriteFloat(name, value, features.ToNumberStyle());
        }
    }

    public void WriteDouble(string name, double value, SerializeFeatures features) {
        if (value != 0 || !writer.IsAtName || IsWriteZeroValue(features)) {
            writer.WriteDouble(name, value, features.ToNumberStyle());
        }
    }

    public void WriteBool(string name, bool value, SerializeFeatures features) {
        if (value || !writer.IsAtName || IsWriteZeroValue(features)) {
            writer.WriteBool(name, value);
        }
    }

    public void WriteString(string name, string? value, SerializeFeatures features) {
        if (value == null) {
            if (IsWriteNullStringAsEmpty(features)) {
                writer.WriteString(name, "", StringStyle.Quote);
            } else {
                WriteNull(name, features);
            }
        } else {
            writer.WriteString(name, value, features.ToStringStyle());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNull(string name, SerializeFeatures features) {
        if (!writer.IsAtName || IsWriteNullValue(features)) {
            writer.WriteNull(name);
        }
    }

    public void WriteBytes(string name, byte[] bytes, int offset, int len) {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        writer.WriteBinary(name, bytes, offset, len);
    }

    public void WriteBytes(string name, byte[]? bytes, SerializeFeatures features) {
        if (bytes == null) {
            WriteNull(name, features);
        } else {
            writer.WriteBinary(name, bytes, 0, bytes.Length);
        }
    }

    public void WriteBinary(string name, Binary? binary, SerializeFeatures features) {
        if (binary == null) {
            WriteNull(name, features);
        } else {
            writer.WriteBinary(name, binary);
        }
    }

    public void WritePtr(string name, in ObjectPtr objectPtr) {
        writer.WritePtr(name, in objectPtr);
    }

    public void WriteDateTime(string name, in DateTime dateTime) {
        writer.WriteDateTime(name, ExtDateTime.OfDateTime(in dateTime));
    }

    public void WriteExtDateTime(string name, in ExtDateTime dateTime) {
        writer.WriteDateTime(name, in dateTime);
    }

    public void WriteTimestamp(string name, in Timestamp timestamp) {
        writer.WriteTimestamp(name, in timestamp);
    }

    #endregion

    #region 简单值-无name版

    public void WriteInt(int value, SerializeFeatures features) {
        writer.WriteInt32(value, features.ToNumberStyle());
    }

    public void WriteLong(long value, SerializeFeatures features) {
        writer.WriteInt64(value, features.ToNumberStyle());
    }

    public void WriteFloat(float value, SerializeFeatures features) {
        writer.WriteFloat(value, features.ToNumberStyle());
    }

    public void WriteDouble(double value, SerializeFeatures features) {
        writer.WriteDouble(value, features.ToNumberStyle());
    }

    public void WriteBool(bool value, SerializeFeatures features) {
        writer.WriteBool(value);
    }

    public void WriteString(string value, SerializeFeatures features) {
        if (value == null) {
            if (IsWriteNullStringAsEmpty(features)) {
                writer.WriteString("", StringStyle.Quote);
            } else {
                WriteNull();
            }
        } else {
            writer.WriteString(value, features.ToStringStyle());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNull() {
        writer.WriteNull();
    }

    public void WriteBytes(byte[] bytes, int offset, int len) {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        writer.WriteBinary(bytes, offset, len);
    }

    public void WriteBytes(byte[]? bytes, SerializeFeatures features) {
        if (bytes == null) {
            WriteNull();
        } else {
            writer.WriteBinary(bytes, 0, bytes.Length);
        }
    }

    public void WriteBinary(Binary? binary, SerializeFeatures features) {
        if (binary == null) {
            WriteNull();
        } else {
            writer.WriteBinary(binary);
        }
    }

    public void WritePtr(in ObjectPtr objectPtr) {
        writer.WritePtr(in objectPtr);
    }

    public void WriteDateTime(in DateTime dateTime) {
        writer.WriteDateTime(ExtDateTime.OfDateTime(in dateTime));
    }

    public void WriteExtDateTime(in ExtDateTime dateTime) {
        writer.WriteDateTime(in dateTime);
    }

    public void WriteTimestamp(in Timestamp timestamp) {
        writer.WriteTimestamp(in timestamp);
    }

    #endregion

#nullable restore

    #region object

    public void WriteObject(string name, object? value, Type declaredType, SerializeFeatures features) {
        WriteObject<object>(name, value, declaredType, features);
    }

    public void WriteObject<T>(string name, in T? value, SerializeFeatures features) {
        WriteObject<T>(name, in value, typeof(T), features);
    }

    public void WriteObject(object? value, Type declaredType, SerializeFeatures features) {
        WriteObject<object>(null, value, declaredType, features);
    }

    public void WriteObject<T>(in T? value, SerializeFeatures features) {
        WriteObject<T>(null, in value, typeof(T), features);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">仅用于避免装箱，不能用于其它语义</typeparam>
    private void WriteObject<T>(string? name, in T? value, Type declaredType, SerializeFeatures features) {
        if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
        // 处理Nullable -- 手动传入的Type参数可能是值类型，但泛型参数可能是Object
        if (declaredType.IsValueType && typeof(T).IsValueType) {
            DsonCodecImpl<T> encoder = converter.CodecRegistry.GetEncoder(declaredType) as DsonCodecImpl<T>;
            if (encoder == null) {
                throw DsonCodecException.UnsupportedType(declaredType);
            }
            if (encoder.IsNullableCodec && !encoder.HasValue(in value)) {
                WriteNull(name!, features);
                return;
            }
            if (writer.IsAtName) {
                writer.WriteName(name);
            }
            encoder.WriteObject(this, in value, declaredType, features);
            return;
        }
        // 声明类型不是值类型就是引用类型或装箱类型 - 集合在外层处理了string类型
        if (value == null) {
            WriteNull(name!, features);
            return;
        }
        Type runtimeType = value.GetType();
        {
            DsonCodecImpl? encoder = converter.CodecRegistry.GetEncoder(runtimeType);
            if (encoder != null) {
                if (writer.IsAtName) {
                    writer.WriteName(name);
                }
                if (writer.ContextType != DsonContextType.TopLevel
                    && IsSerializeReference(features, encoder)) {
                    // 非顶层对象转为引用写入
                    WritePtr(AddReference(value));
                } else if (encoder is DsonCodecImpl<T> castEncoder) {
                    // 避免值类型装箱
                    castEncoder.WriteObject(this, value, declaredType, features);
                } else {
                    encoder.WriteObject2(this, value, declaredType, features);
                }
                return;
            }
        }
        // DsonValue
        if (value is DsonValue dsonValue) {
            Dsons.WriteDsonValue(writer, dsonValue, name);
            return;
        }
        throw DsonCodecException.UnsupportedType(runtimeType);
    }

    #endregion

    #region 流程

    public IDsonConverter Converter {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => converter;
    }
    public ConverterOptions Options {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => converter.Options;
    }
    public ITypeMetaRegistry TypeMetaRegistry {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => converter.TypeMetaRegistry;
    }
    public IDsonCodecRegistry CodecRegistry {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => converter.CodecRegistry;
    }
    public string CurrentName {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => writer.CurrentName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteName(string name) {
        writer.WriteName(name);
    }

    public void WriteStartObject(Type encoderType, SerializeFeatures features) {
        TypeMeta? typeMeta = converter.TypeMetaRegistry.OfType(encoderType);
        WriteStartObject(typeMeta, features);
    }

    public void WriteStartObject(TypeMeta? typeMeta, SerializeFeatures features) {
        ObjectStyle style = GetObjectStyle(features, typeMeta);
        writer.WriteStartObject(style);
        writer.Attach(typeMeta);
    }

    public void WriteEndObject() {
        writer.WriteEndObject();
    }

    public void WriteStartArray(Type encoderType, SerializeFeatures features) {
        TypeMeta? typeMeta = converter.TypeMetaRegistry.OfType(encoderType);
        WriteStartArray(typeMeta, features);
    }

    public void WriteStartArray(TypeMeta? typeMeta, SerializeFeatures features) {
        ObjectStyle style = GetObjectStyle(features, typeMeta);
        writer.WriteStartArray(style);
        writer.Attach(typeMeta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEndArray() {
        writer.WriteEndArray();
    }

    public void WriteHeader(Type encoderType, Type declaredType,
                            SerializeFeatures features, SerializeHeader header) {
        // 顶层对象需要写入LocalId，不论是否是值类型
        if (writer.ContextDepth == 1) {
            header.collection = _stack.Collection;
            header.localId = _stack.LocalId;
        }
        TypeWritePolicy typePolicy = converter.Options.typeWritePolicy;
        bool typed = (features & SerializeFeatures.WriteTypeName) != 0
                     || converter.TypeWriteHelper.RequireTypeName(typePolicy, encoderType, declaredType);
        bool headerIsEmpty = header.isEmpty;
        if (!typed && headerIsEmpty) {
            return;
        }
        TypeMeta typeMeta = writer.Attachment() as TypeMeta;
        if (typeMeta == null) {
            throw new DsonCodecException("ContextError"); // 必须先调用WriteStart
        }
        if (headerIsEmpty) {
            if (writer is DsonTextWriter textWriter) {
                textWriter.WriteSimpleHeader(typeMeta.MainClsName);
            } else {
                writer.WriteStartHeader();
                writer.WriteString(DsonHeader.Names_ClassName, typeMeta.MainClsName);
                writer.WriteEndHeader();
            }
            return;
        }
        // 逐项写入
        writer.WriteStartHeader();
        if (typed) {
            writer.WriteString(DsonHeader.Names_ClassName, typeMeta.MainClsName);
        }
        if (!string.IsNullOrEmpty(header.collection)) {
            writer.WriteString(DsonHeader.Names_Collection, header.collection);
        }
        if (header.localId != 0) {
            writer.WriteInt64(DsonHeader.Names_LocalId, header.localId, NumberStyle.Simple);
        }
        if (header.count > 0) {
            writer.WriteInt32(DsonHeader.Names_Count, header.count, NumberStyle.Simple);
        }
        if (header.version != 0) {
            writer.WriteInt32(DsonHeader.Names_Version, header.version, NumberStyle.Simple);
        }
        writer.WriteEndHeader();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteValueBytes(string name, DsonType dsonType, byte[] data) {
        writer.WriteValueBytes(name, dsonType, data);
    }

    public TypeMeta? ContainerTypeMeta => writer.Attachment() as TypeMeta;

    public DsonCodecImpl<T>? GetInlinableCodec<T>() {
        DsonCodecImpl decoder = converter.CodecRegistry.GetEncoder(typeof(T));
        if (decoder is DsonCodecImpl<T> castDecoder && castDecoder.IsInlinableCodec) {
            return castDecoder;
        }
        return null;
    }

    public void Flush() {
        writer.Flush();
    }

    public void Dispose() {
        writer.Dispose();
        referenceTable?.Clear();
    }

    #endregion

    #region util

    private bool IsWriteZeroValue(SerializeFeatures features) {
        if ((features & SerializeFeatures.WriteZeroValue) != 0) return true;
        if ((features & SerializeFeatures.SkipZeroValue) != 0) return false;
        TypeMeta typeMeta = ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.encodeFeatures;
            if ((features & SerializeFeatures.WriteZeroValue) != 0) return true;
            if ((features & SerializeFeatures.SkipZeroValue) != 0) return false;
        }
        features = converter.Options.encodeFeatures;
        return (features & SerializeFeatures.WriteZeroValue) != 0;
    }

    private bool IsWriteNullValue(SerializeFeatures features) {
        if ((features & SerializeFeatures.WriteNullValue) != 0) return true;
        if ((features & SerializeFeatures.SkipNullValue) != 0) return false;
        TypeMeta typeMeta = ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.encodeFeatures;
            if ((features & SerializeFeatures.WriteNullValue) != 0) return true;
            if ((features & SerializeFeatures.SkipNullValue) != 0) return false;
        }
        features = converter.Options.encodeFeatures;
        return (features & SerializeFeatures.WriteNullValue) != 0;
    }

    private bool IsWriteNullStringAsEmpty(SerializeFeatures features) {
        if (((features & SerializeFeatures.NullStringAsNull) != 0)) return false;
        if ((features & SerializeFeatures.NullStringAsEmpty) != 0) return true;
        TypeMeta typeMeta = ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.encodeFeatures;
            if (((features & SerializeFeatures.NullStringAsNull) != 0)) return false;
            if ((features & SerializeFeatures.NullStringAsEmpty) != 0) return true;
        }
        features = converter.Options.encodeFeatures;
        if (((features & SerializeFeatures.NullStringAsNull) != 0)) return false;
        return (features & SerializeFeatures.NullStringAsEmpty) != 0;
    }

    private bool IsSerializeReference(SerializeFeatures features, DsonCodecImpl codecImpl) {
        if (codecImpl.DisableSerializeReference) {
            return false;
        }
        if ((features & SerializeFeatures.SerializeInline) != 0) return false;
        if ((features & SerializeFeatures.SerializeReference) != 0) return true;
        TypeMeta typeMeta = converter.TypeMetaRegistry.OfType(codecImpl.GetEncoderType());
        if (typeMeta == null) {
            throw DsonCodecException.UnsupportedType(codecImpl.GetEncoderType());
        }
        features = typeMeta.encodeFeatures;
        if ((features & SerializeFeatures.SerializeInline) != 0) return false;
        return (features & SerializeFeatures.SerializeReference) != 0;
    }

    private static ObjectStyle GetObjectStyle(SerializeFeatures features, TypeMeta? typeMeta) {
        if ((features & SerializeFeatures.ObjectFlow) != 0) return ObjectStyle.Flow;
        if ((features & SerializeFeatures.ObjectIndent) != 0) return ObjectStyle.Indent;
        if (typeMeta != null) {
            return (typeMeta.encodeFeatures & SerializeFeatures.ObjectFlow) != 0
                ? ObjectStyle.Flow
                : ObjectStyle.Indent;
        }
        return ObjectStyle.Indent;
    }

    private class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Inst = new ReferenceComparer();

        public bool Equals(object? x, object? y) {
            return x == y;
        }

        public int GetHashCode(object obj) {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    #endregion
}
}