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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
internal class DsonObjectReader : IDsonObjectReader
{
#nullable disable
    private DsonConverter _converter;
    private IDsonReader<string> _reader;
    private bool _isTextReader;

    private readonly Dictionary<int, object> _referenceTable = new();
    private readonly List<LazyRef> _lazyRefQueue = new();
    private string? _clsName;
    private bool _lastReadHasDeferredWork;
#nullable restore

    private DsonObjectReader() {
    }

    private static readonly ConcurrentObjectPool<DsonObjectReader> POOL = new(
        () => new DsonObjectReader(), e => e.Dispose());

    public static DsonObjectReader GetPooled() {
        return POOL.Acquire();
    }

    public static void Release(DsonObjectReader reader) {
        POOL.Release(reader);
    }

    public void Init(DsonConverter converter, IDsonReader<string> reader) {
        this._converter = converter;
        this._reader = reader;
        this._isTextReader = reader is DsonTextReader;
    }

    public object ReadFirst(Type declaredType, DeserializeFeatures features) {
        object? r = null;
        while (_reader.ReadDsonType() != DsonType.EndOfObject) {
            r ??= ReadObject<object>(name: null, declaredType, features);
        }
        ResolveReferences();
        return r ?? throw new DsonCodecException("input is empty");
    }

    public List<T> ReadAll<T>(DeserializeFeatures features) {
        List<T> result = new List<T>();
        while (_reader.ReadDsonType() != DsonType.EndOfObject) {
            object inst = ReadObject<object>(name: null, typeof(T), features);
            result.Add((T)inst);
        }
        ResolveReferences();
        return result;
    }

    private SerializeHeader ReadHeader() {
        if (_reader.ReadDsonType() != DsonType.Header) {
            return default;
        }
        // clsName已提前读取 - 避免重复构造clsName字符串
        SerializeHeader header = new SerializeHeader() { clsName = _clsName };
        _reader.ReadStartHeader();
        while (_reader.ReadDsonType() != DsonType.EndOfObject) {
            string name = _reader.ReadName();
            switch (name) {
                case DsonHeader.Names_LocalId: {
                    header.localId = DsonCodecHelper.ReadInt(_reader); // 可能不是精确类型
                    break;
                }
                case DsonHeader.Names_Count: {
                    header.count = DsonCodecHelper.ReadInt(_reader); // 可能不是精确类型
                    break;
                }
                default: {
                    _reader.SkipValue();
                    break;
                }
            }
        }
        _reader.ReadEndHeader();
        return header;
    }

    #region 简单值

    public int ReadInt(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadInt(_reader);
    }

    public long ReadLong(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadLong(_reader);
    }

    public float ReadFloat(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadFloat(_reader);
    }

    public double ReadDouble(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadDouble(_reader);
    }

    public Fxp64 ReadFxp64(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadFxp64(_reader);
    }

    public bool ReadBool(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadBool(_reader);
    }

    public string? ReadString(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadString(_reader);
    }

    public void ReadNull(string name) {
        ReadName(name);
        DsonCodecHelper.ReadNull(_reader);
    }

    public byte[]? ReadBytes(string name, DeserializeFeatures features) {
        Binary binary = ReadBinary(name, features);
        return binary == null ? null : binary.Unwrap();
    }

    public Binary? ReadBinary(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadBinary(_reader);
    }

    public RefId ReadRefId(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadRefId(_reader);
    }

    public DateTime ReadDateTime(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadDateTime(_reader).ToDateTime();
    }

    public ExtDateTime ReadExtDateTime(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadDateTime(_reader);
    }

    public Timestamp ReadTimestamp(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadTimestamp(_reader);
    }

    public Double4 ReadDouble4(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadDouble4(_reader);
    }

    public Long4 ReadLong4(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadLong4(_reader);
    }

    public Fxp4 ReadFxp4(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadFxp4(_reader);
    }

    public T ReadEnum<T>(string name, DeserializeFeatures features) where T : struct {
        ReadName(name);
        return ReadEnum<T>(features);
    }

    #endregion

    #region 简单值-无name版

    public int ReadInt(DeserializeFeatures features) {
        return DsonCodecHelper.ReadInt(_reader);
    }

    public long ReadLong(DeserializeFeatures features) {
        return DsonCodecHelper.ReadLong(_reader);
    }

    public float ReadFloat(DeserializeFeatures features) {
        return DsonCodecHelper.ReadFloat(_reader);
    }

    public double ReadDouble(DeserializeFeatures features) {
        return DsonCodecHelper.ReadDouble(_reader);
    }

    public Fxp64 ReadFxp64(DeserializeFeatures features) {
        return DsonCodecHelper.ReadFxp64(_reader);
    }

    public bool ReadBool(DeserializeFeatures features) {
        return DsonCodecHelper.ReadBool(_reader);
    }

    public string? ReadString(DeserializeFeatures features) {
        return DsonCodecHelper.ReadString(_reader);
    }

    public void ReadNull() {
        DsonCodecHelper.ReadNull(_reader);
    }

    public byte[]? ReadBytes(DeserializeFeatures features) {
        Binary binary = ReadBinary(features);
        return binary == null ? null : binary.Unwrap();
    }

    public Binary? ReadBinary(DeserializeFeatures features) {
        return DsonCodecHelper.ReadBinary(_reader);
    }

    public RefId ReadRefId() {
        return DsonCodecHelper.ReadRefId(_reader);
    }

    public DateTime ReadDateTime() {
        return DsonCodecHelper.ReadDateTime(_reader).ToDateTime();
    }

    public ExtDateTime ReadExtDateTime() {
        return DsonCodecHelper.ReadDateTime(_reader);
    }

    public Timestamp ReadTimestamp() {
        return DsonCodecHelper.ReadTimestamp(_reader);
    }

    public Double4 ReadDouble4() {
        return DsonCodecHelper.ReadDouble4(_reader);
    }

    public Long4 ReadLong4() {
        return DsonCodecHelper.ReadLong4(_reader);
    }

    public Fxp4 ReadFxp4() {
        return DsonCodecHelper.ReadFxp4(_reader);
    }

    public T ReadEnum<T>(DeserializeFeatures features) where T : struct {
        if (_reader.CurrentDsonType == DsonType.Null) {
            _reader.ReadNull();
            return default;
        }
        if (CodecRegistry.GetDecoder(typeof(T)) is DsonCodecImpl<T> codecImpl) {
            return codecImpl.ReadObject(this, typeof(T), features);
        }
        throw new DsonCodecException($"Invalid EnumType: {typeof(T)}");
    }

    #endregion

    #region object处理

    public object ReadObject(string name, Type declaredType, DeserializeFeatures features) {
        return ReadObject<object>(name, declaredType, features);
    }

    public object ReadObject(Type declaredType, DeserializeFeatures features) {
        return ReadObject<object>(name: null, declaredType, features);
    }

    public T ReadObject<T>(string name, DeserializeFeatures features) {
        return ReadObject<T>(name, typeof(T), features);
    }

    public T ReadObject<T>(DeserializeFeatures features) {
        return ReadObject<T>(name: null, typeof(T), features);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">仅用于避免装箱，不能用于其它语义</typeparam>
    private T ReadObject<T>(string? name, Type declaredType, DeserializeFeatures features) {
        if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
        _lastReadHasDeferredWork = false;
        if (!_reader.IsAtValue) {
            if (_reader.IsAtType) _reader.ReadDsonType();
            if (_reader.IsAtName) _reader.ReadName(name);
        }
        // DsonValue接收原始数据 - 通过Feature指定时，字段通常应该声明为object
        if ((features & DeserializeFeatures.ReadAsDsonValue) != 0) {
            return (T)(object)Dsons.ReadDsonValue(_reader);
        }
        if (!declaredType.IsValueType && typeof(DsonValue).IsAssignableFrom(declaredType)) {
            return (T)(object)Dsons.ReadDsonValue(_reader);
        }
        //
        DsonType dsonType = _reader.CurrentDsonType;
        switch (dsonType) {
            case DsonType.Object:
            case DsonType.Array: {
                // 容器类型只能通过codec解码
                _reader.PeekClassName(DsonHeader.Names_ClassName, out _clsName);
                DsonCodecImpl decoder = FindObjectDecoder(declaredType, _clsName);
                if (decoder == null) {
                    throw DsonCodecException.Incompatible(declaredType, _clsName);
                }
                // 避免结构体装箱
                if (decoder is DsonCodecImpl<T> codecImpl) {
                    return codecImpl.ReadObject(this, declaredType, features);
                } else {
                    return (T)decoder.ReadObject2(this, declaredType, features);
                }
            }
            case DsonType.Null: {
                // Null返回默认值
                _reader.ReadNull(name);
                return default;
            }
            case DsonType.RefId: {
                // 可能序列化为引用的字段需要外部显示测试
                if (declaredType != typeof(ObjectPath) && declaredType != typeof(RefId)) {
                    throw DsonCodecException.Incompatible(declaredType, DsonType.RefId);
                }
                goto default;
            }
            default: {
                // 非容器类型 -- Dson内建结构，Enum，Const等
                if (_converter.CodecRegistry.GetDecoder(declaredType) is DsonCodecImpl<T> decoder) {
                    return decoder.ReadObject(this, declaredType, features);
                }
                // 默认类型转换-声明类型可能是个抽象类型，eg：Number
                return (T)DsonCodecHelper.ReadDsonValueValue(_reader);
            }
        }
    }

    private DsonCodecImpl? FindObjectDecoder(Type declaredType, string? clsName) {
        // 尝试按真实类型读 -- IsAssignableFrom 支持 Nullable
        if (!string.IsNullOrWhiteSpace(clsName)) {
            TypeMeta typeMeta = _converter.TypeMetaRegistry.OfName(clsName);
            if (typeMeta != null && declaredType.IsAssignableFrom(typeMeta.type)) {
                return _converter.CodecRegistry.GetDecoder(typeMeta.type);
            }
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的(投影)
        return _converter.CodecRegistry.GetDecoder(declaredType);
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
    public bool IsTextReader {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _isTextReader;
    }

    public DsonType ReadDsonType() {
        return _reader.IsAtType ? _reader.ReadDsonType() : _reader.CurrentDsonType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadName() {
        if (_reader.IsAtType) {
            _reader.ReadDsonType();
        }
        return _reader.ReadName();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadName(string name) {
        if (_reader.IsAtType) {
            _reader.ReadDsonType();
        }
        _reader.ReadName(name);
    }

    public DsonType CurrentDsonType => _reader.CurrentDsonType;
    public string CurrentName => _reader.CurrentName;

    public SerializeHeader ReadStartObject(Type encoderType, DeserializeFeatures features) {
        TypeMeta? typeMeta = _converter.TypeMetaRegistry.OfType(encoderType);
        return ReadStartObject(typeMeta, features);
    }

    public SerializeHeader ReadStartObject(TypeMeta? typeMeta, DeserializeFeatures features) {
        _reader.ReadStartObject();
        _reader.UserContextData = typeMeta;
        return ReadHeader();
    }

    public void ReadEndObject() {
        int flags = _reader.UserContextFlags;
        _reader.SkipToEndOfObject();
        _reader.ReadEndObject();
        if ((flags & MaskHasLazyReference) != 0) {
            _reader.UserContextFlags |= MaskHasLazyReference;
            _lastReadHasDeferredWork = true;
        }
    }

    public SerializeHeader ReadStartArray(Type encoderType, DeserializeFeatures features) {
        TypeMeta? typeMeta = _converter.TypeMetaRegistry.OfType(encoderType);
        return ReadStartArray(typeMeta, features);
    }

    public SerializeHeader ReadStartArray(TypeMeta? typeMeta, DeserializeFeatures features) {
        _reader.ReadStartArray();
        _reader.UserContextData = typeMeta;
        return ReadHeader();
    }

    public void ReadEndArray() {
        int flags = _reader.UserContextFlags;
        _reader.SkipToEndOfObject();
        _reader.ReadEndArray();
        if ((flags & MaskHasLazyReference) != 0) {
            _reader.UserContextFlags |= MaskHasLazyReference;
            _lastReadHasDeferredWork = true;
        }
    }

    public void SkipValue() {
        _reader.SkipValue();
    }

    public byte[] ReadValueAsBytes(string name) {
        return _reader.ReadValueAsBytes(name);
    }

    public TypeMeta? ContainerTypeMeta => _reader.UserContextData as TypeMeta;

    public DsonCodecImpl<T>? GetInlinableCodec<T>() {
        DsonCodecImpl decoder = _converter.CodecRegistry.GetDecoder(typeof(T));
        if (decoder is DsonCodecImpl<T> castDecoder && castDecoder.IsInlinableCodec) {
            return castDecoder;
        }
        return null;
    }

    public void SetEnableNameIntern(bool? value) {
        _reader.SetEnableNameIntern(value);
    }

    public void Dispose() {
        this._converter = null;
        this._reader = null;
        _referenceTable.Clear();
        _lazyRefQueue.Clear();
        _clsName = null;
        _lastReadHasDeferredWork = false;
    }

    #endregion

    #region 引用解析

    /// <summary>
    /// 解析引用
    /// </summary>
    private void ResolveReferences() {
#if NET6_0_OR_GREATER
        Span<LazyRef> span = CollectionsMarshal.AsSpan(_lazyRefQueue);
        for (int index = 0; index < span.Length; index++) {
            ref LazyRef lazyRef = ref span[index];
#else
        for (int index = 0; index < _lazyRefQueue.Count; index++) {
            LazyRef lazyRef = _lazyRefQueue[index];
#endif
            if (lazyRef.target == null && lazyRef.refId != 0) {
                if (!_referenceTable.TryGetValue(lazyRef.refId, out object target)) {
                    throw new DsonCodecException($"refId: {lazyRef.refId} not found");
                }
                lazyRef.target = target;
            }
            if (lazyRef.func != null) {
                lazyRef.target = lazyRef.func(lazyRef.target);
            }
            if (lazyRef.keyArray != null) {
                lazyRef.codec.SetField(lazyRef.inst, lazyRef.keyArray, lazyRef.index, lazyRef.target);
            } else if (lazyRef.fieldName != null) {
                lazyRef.codec.SetField(lazyRef.inst, lazyRef.fieldName, lazyRef.target);
            } else {
                lazyRef.codec.SetField(lazyRef.inst, lazyRef.index, lazyRef.target);
            }
        }
    }

    public void PublishReference(int refId, object target) {
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (refId != 0 && _reader.ContextDepth == 1) {
            _referenceTable[refId] = target;
        }
        // 此时其实可以注入部分引用以避免队列过大，但频繁扫描队列性能也不好（且可能导致频繁的数据拷贝）
    }

    public bool TryReadRefId(out int refId) {
        if (_reader.IsAtType) {
            _reader.ReadDsonType();
        }
        if (_reader.CurrentDsonType == DsonType.RefId) {
            refId = _reader.ReadRefId().LocalId;
            return true;
        }
        refId = 0;
        return false;
    }

    public void DeferReference(int refId, IDsonCodec codec, object inst, string fieldName) {
        if (fieldName == null) throw new ArgumentNullException(nameof(fieldName));
        // 前向引用立即解析，后向引用末尾统一解析
        if (_referenceTable.TryGetValue(refId, out object target)) {
            codec.SetField(inst, fieldName, target);
            return;
        }
        _lazyRefQueue.Add(new LazyRef()
        {
            codec = codec,
            inst = inst,
            fieldName = fieldName,
            refId = refId,
        });
        _reader.UserContextFlags |= MaskHasLazyReference;
    }

    public void DeferReference<T>(int refId, IDsonCodec codec, List<T> inst, int index) {
        if (_referenceTable.TryGetValue(refId, out object target)) {
            codec.SetField(inst, index, target);
            return;
        }
        _lazyRefQueue.Add(new LazyRef()
        {
            codec = codec,
            inst = inst,
            index = index,
            refId = refId
        });
        _reader.UserContextFlags |= MaskHasLazyReference;
    }

    public void DeferReference<K, V>(int refId, IDsonCodec codec, Dictionary<K, V> inst, List<K> keyArray, int index) {
        if (_referenceTable.TryGetValue(refId, out object target)) {
            codec.SetField(inst, keyArray, index, target);
            return;
        }
        _lazyRefQueue.Add(new LazyRef()
        {
            codec = codec,
            inst = inst,
            keyArray = keyArray,
            index = index,
            refId = refId
        });
        _reader.UserContextFlags |= MaskHasLazyReference;
    }

    public void DeferToTargetType(IDsonCodec codec, object inst, string fieldName, object? fieldValue, Func<object, object>? func) {
        if (fieldName == null) throw new ArgumentNullException(nameof(fieldName));
        if (fieldValue == null) {
            codec.SetField(inst, fieldName, null);
            return;
        }
        if (_lazyRefQueue.Count == 0 || !_lastReadHasDeferredWork) {
            if (func != null) {
                fieldValue = func(fieldValue);
            }
            codec.SetField(inst, fieldName, fieldValue);
            return;
        }
        // 目前只有SerializeReference的非List/Dictionary类型需要延迟转换
        _lazyRefQueue.Add(new LazyRef()
        {
            codec = codec,
            inst = inst,
            fieldName = fieldName,
            target = fieldValue,
            func = func,
        });
    }

    #endregion

#nullable disable

    private const int MaskHasLazyReference = 0x01; // 对象的直接字段（或元素）有延迟引用请求

    [StructLayout(LayoutKind.Auto)]
    private struct LazyRef
    {
        public IDsonCodec codec;
        public object inst;
        public string? fieldName; // 非null表示普通对象
        public object? keyArray; // 非null表示字典
        public int index; // List或keyArray索引

        public int refId; // 引用id
        public object? target; // 非null表示只执行转换
        public Func<object, object>? func; // 类型转换
    }
}
}