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
internal class DsonObjectWriter : IDsonObjectWriter
{
#nullable disable
    private DsonConverter _converter;
    private IDsonWriter<string> _writer;
    private bool _isTextWriter;

    private readonly LinkedDictionary<object, int> _referenceTable = new(ReferenceComparer.Inst);
    private int _stack;
    private int _nextLocalId;
#nullable restore
    private DsonObjectWriter() {
    }

    private static readonly ConcurrentObjectPool<DsonObjectWriter> POOL = new(
        () => new DsonObjectWriter(), e => e.Dispose());

    public static DsonObjectWriter GetPooled() {
        return POOL.Acquire();
    }

    public static void Release(DsonObjectWriter reader) {
        POOL.Release(reader);
    }

    public void Init(DsonConverter converter, IDsonWriter<string> writer) {
        this._converter = converter;
        this._writer = writer;
        this._isTextWriter = writer is DsonTextWriter;
    }

    public RefId AddReference(object reference) {
        if (reference == null) {
            throw new ArgumentNullException(nameof(reference));
        }
        if (_referenceTable.TryGetValue(reference, out int localId)) {
            return new RefId(localId);
        }
        localId = ++_nextLocalId;
        _referenceTable.Add(reference, localId);
        return new RefId(localId);
    }

    public void AddReferences(IEnumerable collection) {
        foreach (object obj in collection) {
            AddReference(obj ?? throw new NullReferenceException("collection has null elements"));
        }
    }

    public void WriteAll(Type declaredType, SerializeFeatures features) {
        if (_referenceTable.Count == 0) {
            return;
        }
        var pair = _referenceTable.PeekFirst();
        object current;
        object next = pair.Key;
        _stack = pair.Value;
        do {
            current = next;
            WriteObject<object>(null!, current, declaredType, features);

        } while (_referenceTable.NextKey(current, out next, out _stack));
    }
#nullable disable

    #region 简单值

    public void WriteInt(string name, int value, SerializeFeatures features) {
        if (value != 0 || !_writer.IsAtName || IsWriteZeroValue(features)) {
            _writer.WriteInt32(name, value, _isTextWriter ? features.ToNumberStyle() : default);
        }
    }

    public void WriteLong(string name, long value, SerializeFeatures features) {
        if (value != 0 || !_writer.IsAtName || IsWriteZeroValue(features)) {
            _writer.WriteInt64(name, value, _isTextWriter ? features.ToNumberStyle() : default);
        }
    }

    public void WriteFloat(string name, float value, SerializeFeatures features) {
        if (value != 0 || !_writer.IsAtName || IsWriteZeroValue(features)) {
            _writer.WriteFloat(name, value, _isTextWriter ? features.ToNumberStyle() : default);
        }
    }

    public void WriteDouble(string name, double value, SerializeFeatures features) {
        if (value != 0 || !_writer.IsAtName || IsWriteZeroValue(features)) {
            _writer.WriteDouble(name, value, _isTextWriter ? features.ToNumberStyle() : default);
        }
    }

    public void WriteFxp64(string name, Fxp64 value, SerializeFeatures features) {
        if (value.rawValue != 0 || !_writer.IsAtName || IsWriteZeroValue(features)) {
            _writer.WriteFxp64(name, value);
        }
    }

    public void WriteBool(string name, bool value, SerializeFeatures features) {
        if (value || !_writer.IsAtName || IsWriteZeroValue(features)) {
            _writer.WriteBool(name, value);
        }
    }

    public void WriteString(string name, string? value, SerializeFeatures features) {
        if (value == null) {
            if (IsWriteNullStringAsEmpty(features)) {
                _writer.WriteString(name, "", StringStyle.Quote);
            } else {
                WriteNull(name, features);
            }
        } else {
            _writer.WriteString(name, value, _isTextWriter ? features.ToStringStyle() : default);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNull(string name, SerializeFeatures features) {
        if (!_writer.IsAtName || IsWriteNullValue(features)) {
            _writer.WriteNull(name);
        }
    }

    public void WriteBytes(string name, byte[] bytes, int offset, int len) {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        _writer.WriteBinary(name, bytes, offset, len);
    }

    public void WriteBytes(string name, byte[]? bytes, SerializeFeatures features) {
        if (bytes == null) {
            WriteNull(name, features);
        } else {
            _writer.WriteBinary(name, bytes, 0, bytes.Length);
        }
    }

    public void WriteBinary(string name, Binary? binary, SerializeFeatures features) {
        if (binary == null) {
            WriteNull(name, features);
        } else {
            _writer.WriteBinary(name, binary);
        }
    }

    public void WriteRefId(string name, RefId refId) {
        _writer.WriteRefId(name, refId);
    }

    public void WriteDateTime(string name, DateTime dateTime) {
        _writer.WriteDateTime(name, ExtDateTime.OfDateTime(in dateTime));
    }

    public void WriteExtDateTime(string name, ExtDateTime dateTime) {
        _writer.WriteDateTime(name, dateTime);
    }

    public void WriteTimestamp(string name, Timestamp timestamp) {
        _writer.WriteTimestamp(name, timestamp);
    }

    public void WriteDouble4(string name, Double4 double4, string? elementNames = null) {
        _writer.WriteDouble4(name, double4, elementNames);
    }

    public void WriteLong4(string name, Long4 long4, string? elementNames = null) {
        _writer.WriteLong4(name, long4, elementNames);
    }

    public void WriteFxp4(string name, Fxp4 fv4, string? elementNames = null) {
        _writer.WriteFxp4(name, fv4, elementNames);
    }

    public void WriteEnum<T>(string name, T value, SerializeFeatures features = 0) where T : struct {
        if (_writer.IsAtName) {
            _writer.WriteName(name);
        }
        if (CodecRegistry.GetEncoder(typeof(T)) is DsonCodecImpl<T> codecImpl) {
            codecImpl.WriteObject(this, value, typeof(T), features);
        } else {
            throw new DsonCodecException($"Invalid EnumType: {typeof(T)}");
        }
    }

    #endregion

    #region 简单值-无name版

    public void WriteInt(int value, SerializeFeatures features) {
        _writer.WriteInt32(value, _isTextWriter ? features.ToNumberStyle() : default);
    }

    public void WriteLong(long value, SerializeFeatures features) {
        _writer.WriteInt64(value, _isTextWriter ? features.ToNumberStyle() : default);
    }

    public void WriteFloat(float value, SerializeFeatures features) {
        _writer.WriteFloat(value, _isTextWriter ? features.ToNumberStyle() : default);
    }

    public void WriteDouble(double value, SerializeFeatures features) {
        _writer.WriteDouble(value, _isTextWriter ? features.ToNumberStyle() : default);
    }

    public void WriteFxp64(Fxp64 value, SerializeFeatures features) {
        _writer.WriteFxp64(value);
    }

    public void WriteBool(bool value, SerializeFeatures features) {
        _writer.WriteBool(value);
    }

    public void WriteString(string value, SerializeFeatures features) {
        if (value == null) {
            if (IsWriteNullStringAsEmpty(features)) {
                _writer.WriteString("", StringStyle.Quote);
            } else {
                WriteNull();
            }
        } else {
            _writer.WriteString(value, _isTextWriter ? features.ToStringStyle() : default);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteNull() {
        _writer.WriteNull();
    }

    public void WriteBytes(byte[] bytes, int offset, int len) {
        if (bytes == null) throw new ArgumentNullException(nameof(bytes));
        _writer.WriteBinary(bytes, offset, len);
    }

    public void WriteBytes(byte[]? bytes, SerializeFeatures features) {
        if (bytes == null) {
            WriteNull();
        } else {
            _writer.WriteBinary(bytes, 0, bytes.Length);
        }
    }

    public void WriteBinary(Binary? binary, SerializeFeatures features) {
        if (binary == null) {
            WriteNull();
        } else {
            _writer.WriteBinary(binary);
        }
    }

    public void WriteRefId(RefId refId) {
        _writer.WriteRefId(refId);
    }

    public void WriteDateTime(DateTime dateTime) {
        _writer.WriteDateTime(ExtDateTime.OfDateTime(in dateTime));
    }

    public void WriteExtDateTime(ExtDateTime dateTime) {
        _writer.WriteDateTime(dateTime);
    }

    public void WriteTimestamp(Timestamp timestamp) {
        _writer.WriteTimestamp(timestamp);
    }

    public void WriteDouble4(Double4 double4, string? elementNames = null) {
        _writer.WriteDouble4(double4, elementNames);
    }

    public void WriteLong4(Long4 long4, string? elementNames = null) {
        _writer.WriteLong4(long4, elementNames);
    }

    public void WriteFxp4(Fxp4 fv4, string? elementNames = null) {
        _writer.WriteFxp4(fv4, elementNames);
    }

    public void WriteEnum<T>(T value, SerializeFeatures features = 0) where T : struct {
        if (CodecRegistry.GetEncoder(typeof(T)) is DsonCodecImpl<T> codecImpl) {
            codecImpl.WriteObject(this, value, typeof(T), features);
        } else {
            throw new DsonCodecException($"Invalid EnumType: {typeof(T)}");
        }
    }

    #endregion

#nullable restore

    #region object

    public void WriteObject<T>(string name, in T? value, SerializeFeatures features) {
        WriteObject<T>(name, in value, typeof(T), features);
    }

    public void WriteObject<T>(in T? value, SerializeFeatures features) {
        WriteObject<T>(null, in value, typeof(T), features);
    }

    public void WriteObject(string name, object? value, Type declaredType, SerializeFeatures features) {
        WriteObject<object>(name, value, declaredType, features);
    }

    public void WriteObject(object? value, Type declaredType, SerializeFeatures features) {
        WriteObject<object>(null, value, declaredType, features);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">仅用于避免装箱，不能用于其它语义</typeparam>
    private void WriteObject<T>(string? name, in T? value, Type declaredType, SerializeFeatures features) {
        if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
        // 处理Nullable -- 手动传入的Type参数可能是值类型，但泛型参数可能是Object
        if (typeof(T).IsValueType) {
            DsonCodecImpl<T> castEncoder = _converter.CodecRegistry.GetEncoder(declaredType) as DsonCodecImpl<T>;
            if (castEncoder == null) {
                throw DsonCodecException.UnsupportedType(declaredType);
            }
            if (castEncoder.IsNullableCodec && !castEncoder.HasValue(in value)) {
                WriteNull(name!, features);
                return;
            }
            if (_writer.IsAtName) {
                _writer.WriteName(name);
            }
            castEncoder.WriteObject(this, in value, declaredType, features);
            return;
        }
        // 声明类型不是值类型就是引用类型或装箱类型 - 集合在外层处理了string类型
        if (value == null) {
            WriteNull(name!, features);
            return;
        }
        // DsonValue - DsonValue不能查询Encoder否则可能查到List/Map的编码器，除非我们显式注册相关编解码器
        if (value is DsonValue dsonValue) {
            Dsons.WriteDsonValue(_writer, dsonValue, name);
            return;
        }
        Type runtimeType = value.GetType(); // 值类型调用GetType会装箱
        DsonCodecImpl? encoder = _converter.CodecRegistry.GetEncoder(runtimeType);
        if (encoder != null) {
            if (_writer.IsAtName) {
                _writer.WriteName(name);
            }
            if (_writer.ContextType != DsonContextType.TopLevel
                && IsSerializeReference(features, encoder)) {
                // 非顶层对象转为引用写入
                WriteRefId(AddReference(value));
            } else if (encoder is DsonCodecImpl<T> castEncoder) {
                // 避免值类型装箱
                castEncoder.WriteObject(this, value, declaredType, features);
            } else {
                encoder.WriteObject2(this, value, declaredType, features);
            }
            return;
        }
        throw DsonCodecException.UnsupportedType(runtimeType);
    }

    #endregion

    #region 流程

    public IDsonConverter Converter {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _converter;
    }
    public ConverterOptions Options {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _converter.Options;
    }
    public ITypeMetaRegistry TypeMetaRegistry {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _converter.TypeMetaRegistry;
    }
    public IDsonCodecRegistry CodecRegistry {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _converter.CodecRegistry;
    }
    public bool IsTextWriter {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _isTextWriter;
    }
    public string CurrentName {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _writer.CurrentName;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteName(string name) {
        _writer.WriteName(name);
    }

    public void WriteStartObject(Type encoderType, SerializeFeatures features) {
        TypeMeta? typeMeta = _converter.TypeMetaRegistry.OfType(encoderType);
        WriteStartObject(typeMeta, features);
    }

    public void WriteStartObject(TypeMeta? typeMeta, SerializeFeatures features) {
        ObjectStyle style = _isTextWriter ? GetObjectStyle(features, typeMeta) : default;
        _writer.WriteStartObject(style);
        _writer.UserContextData = typeMeta;
    }

    public void WriteEndObject() {
        _writer.WriteEndObject();
    }

    public void WriteStartArray(Type encoderType, SerializeFeatures features) {
        TypeMeta? typeMeta = _converter.TypeMetaRegistry.OfType(encoderType);
        WriteStartArray(typeMeta, features);
    }

    public void WriteStartArray(TypeMeta? typeMeta, SerializeFeatures features) {
        ObjectStyle style = _isTextWriter ? GetObjectStyle(features, typeMeta) : default;
        _writer.WriteStartArray(style);
        _writer.UserContextData = typeMeta;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteEndArray() {
        _writer.WriteEndArray();
    }

    public void WriteHeader(Type encoderType, Type declaredType, int count) {
        SerializeHeader header = new SerializeHeader();
        // 顶层元素才需要写入localId
        if (_writer.ContextDepth == 1) {
            header.localId = _stack;
        }
        // count大于默认初始化空间(4)才有写入价值
        if (count > 4) {
            header.count = count;
        }
        // 只有运行时类型和声明类型不一致时才需要写入类型名
        bool typed = _converter.TypeWriteHelper.RequireTypeName(encoderType, declaredType);
        if (typed) {
            TypeMeta typeMeta = (TypeMeta)_writer.UserContextData
                                ?? throw new InvalidOperationException(encoderType.FullName);
            header.clsName = typeMeta.MainName;
        }
        if (header.IsEmpty) {
            return;
        }
        // 只有clsName时写为简化Header样式：@{Vector3}
        if (header.IsClassNameOnly && _writer is DsonTextWriter textWriter) {
            textWriter.WriteSimpleHeader(header.clsName);
            return;
        }
        // 逐项写入
        _writer.WriteStartHeader();
        if (typed) {
            _writer.WriteString(DsonHeader.Names_ClassName, header.clsName);
        }
        if (header.localId != 0) {
            _writer.WriteInt64(DsonHeader.Names_LocalId, header.localId, NumberStyle.Simple);
        }
        if (header.count > 0) {
            _writer.WriteInt32(DsonHeader.Names_Count, header.count, NumberStyle.Simple);
        }
        _writer.WriteEndHeader();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteValueBytes(string name, DsonType dsonType, byte[] data) {
        _writer.WriteValueBytes(name, dsonType, data);
    }

    public TypeMeta? ContainerTypeMeta => _writer.UserContextData as TypeMeta;

    public DsonCodecImpl<T>? GetInlinableCodec<T>() {
        DsonCodecImpl encoder = _converter.CodecRegistry.GetEncoder(typeof(T));
        if (encoder is DsonCodecImpl<T> castEncoder && castEncoder.IsInlinableCodec) {
            return castEncoder;
        }
        return null;
    }

    public void Flush() {
        _writer.Flush();
    }

    public void Dispose() {
        _writer.Dispose();
        _referenceTable?.Clear();
        _stack = 0;
        _nextLocalId = 0;
        _isTextWriter = false;
    }

    #endregion

    #region util

    private bool IsWriteZeroValue(SerializeFeatures features) {
        // 默认情况下写入0值，只有开启忽略0值的情况下才跳过0值
        if ((features & SerializeFeatures.SkipZeroValue) != 0) return false;
        if ((features & SerializeFeatures.WriteZeroValue) != 0) return true;
        TypeMeta typeMeta = ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.encodeFeatures;
            if ((features & SerializeFeatures.SkipZeroValue) != 0) return false;
            if ((features & SerializeFeatures.WriteZeroValue) != 0) return true;
        }
        features = _converter.Options.encodeFeatures;
        return (features & SerializeFeatures.SkipZeroValue) == 0;
    }

    private bool IsWriteNullValue(SerializeFeatures features) {
        // 默认情况下写入null值，只有开启忽略null值的情况下才跳过null值
        if ((features & SerializeFeatures.SkipNullValue) != 0) return false;
        if ((features & SerializeFeatures.WriteNullValue) != 0) return true;
        TypeMeta typeMeta = ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.encodeFeatures;
            if ((features & SerializeFeatures.SkipNullValue) != 0) return false;
            if ((features & SerializeFeatures.WriteNullValue) != 0) return true;
        }
        features = _converter.Options.encodeFeatures;
        return (features & SerializeFeatures.SkipNullValue) == 0;
    }

    private bool IsWriteNullStringAsEmpty(SerializeFeatures features) {
        // null字符串字段默认不特殊对待，只有开启特性的情况下才写为空字符串
        if ((features & SerializeFeatures.NullStringAsEmpty) != 0) return true;
        if (((features & SerializeFeatures.NullStringAsNull) != 0)) return false;
        TypeMeta typeMeta = ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.encodeFeatures;
            if ((features & SerializeFeatures.NullStringAsEmpty) != 0) return true;
            if (((features & SerializeFeatures.NullStringAsNull) != 0)) return false;
        }
        features = _converter.Options.encodeFeatures;
        return (features & SerializeFeatures.NullStringAsEmpty) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsSerializeReference(SerializeFeatures features, DsonCodecImpl codecImpl) {
        if (codecImpl.DisableSerializeReference) {
            return false;
        }
        return (features & SerializeFeatures.SerializeReference) != 0;
    }

    private static ObjectStyle GetObjectStyle(SerializeFeatures features, TypeMeta? typeMeta) {
        if ((features & SerializeFeatures.ObjectFlow) != 0) return ObjectStyle.Flow;
        if ((features & SerializeFeatures.ObjectIndent) != 0) return ObjectStyle.Indent;
        if (typeMeta != null) {
            features = typeMeta.encodeFeatures;
            if ((features & SerializeFeatures.ObjectFlow) != 0) return ObjectStyle.Flow;
            if ((features & SerializeFeatures.ObjectIndent) != 0) return ObjectStyle.Indent;
        }
        return ObjectStyle.Indent;
    }

    private class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Inst = new ReferenceComparer();

        public bool Equals(object? x, object? y) {
            return x == y; // ReferenceEquals
        }

        public int GetHashCode(object obj) {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    #endregion
}
}