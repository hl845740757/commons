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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
internal class DefaultDsonObjectReader : IDsonObjectReader
{
#nullable disable
    private DefaultDsonConverter converter;
    private DsonCollectionReader<string> reader;

    private readonly LinkedDictionary<int, ItemContext> referenceTable = new();
    private int _stack;
    private readonly List<int> _listCache = new List<int>();
#nullable restore

    private DefaultDsonObjectReader() {
    }

    private static readonly ConcurrentObjectPool<DefaultDsonObjectReader> pool = new(
        () => new DefaultDsonObjectReader(), e => e.Dispose());

    public static DefaultDsonObjectReader GetPooled() {
        return pool.Acquire();
    }

    public static void Release(DefaultDsonObjectReader reader) {
        pool.Release(reader);
    }

    public void Init(DefaultDsonConverter converter) {
        this.converter = converter;
    }

    public void AddReferences(DsonArray<string> collection) {
        if (collection.Count == 0) {
            throw new Exception("Empty collection");
        }
        foreach (DsonValue dsonValue in collection) {
            if (dsonValue.DsonType == DsonType.Header) {
                continue; // 文件头
            }
            SerializeHeader header = ReadHeader(dsonValue);
            ItemContext itemContext = new ItemContext()
            {
                header = header,
                dsonValue = dsonValue,
            };
            // 默认覆盖的话容易隐藏错误，还是抛出异常更安全
            if (!referenceTable.TryAdd(itemContext.Pointer, itemContext)) {
                throw new Exception("Duplicate pointer: " + itemContext.Pointer);
            }
        }
    }

    public object ReadFirst(Type declaredType, DeserializeFeatures features) {
        int ptr = referenceTable.PeekFirstKey();
        return GetReference(ptr, declaredType, features);
    }

    public object ReadFirst(Type declaredType, int localId, DeserializeFeatures features) {
        int ptr = localId != 0 ? localId : referenceTable.PeekFirstKey();
        return GetReference(ptr, declaredType, features);
    }

    public List<T> ReadAll<T>(DeserializeFeatures features) {
        _listCache.AddRange(referenceTable.Keys); // 用于保持原始顺序
        //
        List<T> result = new List<T>(referenceTable.Count);
        foreach (int ptr in _listCache) {
            result.Add((T)GetReference(ptr, typeof(T), features));
        }
        return result;
    }

    private object GetReference(int ptr, Type declaredType, DeserializeFeatures features) {
        ItemContext itemContext = referenceTable[ptr];
        if (itemContext.objectValue != null) {
            return itemContext.objectValue;
        }
        if (itemContext.status == STATUS_PROCESSING) {
            throw new DsonCodecException("constructor cyclic dependency: " + ptr);
        }
        itemContext.reader = converter.Options.readerPool.Acquire();
        itemContext.reader.UnsafeInit(converter.Options.binReaderSettings, itemContext.dsonValue, true);
        itemContext.status = STATUS_PROCESSING;
        // 将新上下文移动至当前上下文后
        if (reader == null) {
            referenceTable.PutFirst(ptr, itemContext);
        } else {
            referenceTable.PutAfter(ptr, itemContext, _stack);
        }
        _stack = itemContext.Pointer;
        reader = itemContext.reader;
        // 用户的Codec可能没有立即发布引用，这里进行修正；值类型统一在这里发布引用
        object inst = ReadObject<object>(name: null, declaredType, features);
        itemContext = referenceTable[ptr];
        if (itemContext.objectValue == null) {
            itemContext.objectValue = inst;
            referenceTable[ptr] = itemContext;
        }
        return inst;
    }

    private void BackToPrevContext() {
        if (reader.ContextDepth == 0
            && referenceTable.PrevKey(_stack, out _, out ItemContext prevContext)) {
            _stack = prevContext.Pointer;
            reader = prevContext.reader;
        }
    }

    public void PublishReference<T>(T reference) {
        if (reader.ContextDepth == 1) { // 可多次发布覆盖
            ItemContext context = referenceTable[_stack];
            context.objectValue = reference;
            referenceTable[_stack] = context;
        }
    }

    private static SerializeHeader ReadHeader(DsonValue container) {
        SerializeHeader header = default;
        DsonHeader<string> dsonHeader;
        if (container is DsonObject<string> dsonObject) {
            dsonHeader = dsonObject.Header;
            header.count = dsonObject.Count; // 忽略header中的count，更精确
        } else {
            DsonArray<string> dsonArray = container.AsArray();
            dsonHeader = dsonArray.Header;
            header.count = dsonArray.Count;
        }
        if (dsonHeader.IsEmpty) {
            return header;
        }
        // DsonHeader使用的是ArrayDictionary，查询效率其实不太好，但我们绝大多数header只有clsName，因此通过read计数优化
        DsonValue dsonValue;
        int read = 0;
        if (read < dsonHeader.Count && dsonHeader.TryGetValue(DsonHeader.Names_ClassName, out dsonValue)) {
            header.clsName = dsonValue.AsString();
            read++;
        }
        if (read < dsonHeader.Count && dsonHeader.TryGetValue(DsonHeader.Names_LocalId, out dsonValue)) {
            header.localId = dsonValue.AsNumber().IntValue;
            read++;
        }
        return header;
    }

    #region 简单值

    public int ReadInt(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadInt(reader);
    }

    public long ReadLong(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadLong(reader);
    }

    public float ReadFloat(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadFloat(reader);
    }

    public double ReadDouble(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadDouble(reader);
    }

    public Fxp64 ReadFxp64(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadFxp64(reader);
    }

    public bool ReadBool(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadBool(reader);
    }

    public string? ReadString(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadString(reader);
    }

    public void ReadNull(string name) {
        ReadName(name);
        DsonCodecHelper.ReadNull(reader);
    }

    public byte[]? ReadBytes(string name, DeserializeFeatures features) {
        Binary binary = ReadBinary(name, features);
        return binary == null ? null : binary.Unwrap();
    }

    public Binary? ReadBinary(string name, DeserializeFeatures features) {
        ReadName(name);
        return DsonCodecHelper.ReadBinary(reader);
    }

    public ObjectPtr ReadPtr(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadPtr(reader);
    }

    public DateTime ReadDateTime(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadDateTime(reader).ToDateTime();
    }

    public ExtDateTime ReadExtDateTime(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadDateTime(reader);
    }

    public Timestamp ReadTimestamp(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadTimestamp(reader);
    }

    public Double4 ReadDouble4(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadDouble4(reader);
    }

    public Long4 ReadLong4(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadLong4(reader);
    }

    public Fxp4 ReadFxp4(string name) {
        ReadName(name);
        return DsonCodecHelper.ReadFxp4(reader);
    }

    public T ReadEnum<T>(string name, DeserializeFeatures features) {
        ReadName(name);
        return ReadEnum<T>(features);
    }

    public List<T>? ReadList<T>(string name, DeserializeFeatures features) {
        ReadName(name);
        return ReadList<T>(features);
    }

    public Dictionary<K, V>? ReadDictionary<K, V>(string name, DeserializeFeatures features) {
        ReadName(name);
        return ReadDictionary<K, V>(features);
    }

    #endregion

    #region 简单值-无name版

    public int ReadInt(DeserializeFeatures features) {
        return DsonCodecHelper.ReadInt(reader);
    }

    public long ReadLong(DeserializeFeatures features) {
        return DsonCodecHelper.ReadLong(reader);
    }

    public float ReadFloat(DeserializeFeatures features) {
        return DsonCodecHelper.ReadFloat(reader);
    }

    public double ReadDouble(DeserializeFeatures features) {
        return DsonCodecHelper.ReadDouble(reader);
    }

    public Fxp64 ReadFxp64(DeserializeFeatures features) {
        return DsonCodecHelper.ReadFxp64(reader);
    }

    public bool ReadBool(DeserializeFeatures features) {
        return DsonCodecHelper.ReadBool(reader);
    }

    public string? ReadString(DeserializeFeatures features) {
        return DsonCodecHelper.ReadString(reader);
    }

    public void ReadNull() {
        DsonCodecHelper.ReadNull(reader);
    }

    public byte[]? ReadBytes(DeserializeFeatures features) {
        Binary binary = ReadBinary(features);
        return binary == null ? null : binary.Unwrap();
    }

    public Binary? ReadBinary(DeserializeFeatures features) {
        return DsonCodecHelper.ReadBinary(reader);
    }

    public ObjectPtr ReadPtr() {
        return DsonCodecHelper.ReadPtr(reader);
    }

    public DateTime ReadDateTime() {
        return DsonCodecHelper.ReadDateTime(reader).ToDateTime();
    }

    public ExtDateTime ReadExtDateTime() {
        return DsonCodecHelper.ReadDateTime(reader);
    }

    public Timestamp ReadTimestamp() {
        return DsonCodecHelper.ReadTimestamp(reader);
    }

    public Double4 ReadDouble4() {
        return DsonCodecHelper.ReadDouble4(reader);
    }

    public Long4 ReadLong4() {
        return DsonCodecHelper.ReadLong4(reader);
    }

    public Fxp4 ReadFxp4() {
        return DsonCodecHelper.ReadFxp4(reader);
    }

    public T ReadEnum<T>(DeserializeFeatures features) {
        if (reader.CurrentDsonType == DsonType.Null) {
            reader.ReadNull();
            return default;
        }
        if (CodecRegistry.GetDecoder(typeof(T)) is DsonCodecImpl<T> codecImpl) {
            return codecImpl.ReadObject(this, typeof(T), features);
        }
        throw new DsonCodecException($"Invalid EnumType: {typeof(T)}");
    }

    public List<T>? ReadList<T>(DeserializeFeatures features) {
        if (reader.CurrentDsonType == DsonType.Null) {
            reader.ReadNull();
            return null;
        }
        Type targetType = typeof(List<T>);
        if (CodecRegistry.GetDecoder(targetType) is DsonCodecImpl<List<T>> codecImpl) {
            return codecImpl.ReadObject(this, targetType, features);
        }
        throw new AssertionError();
    }

    public Dictionary<K, V>? ReadDictionary<K, V>(DeserializeFeatures features) {
        if (reader.CurrentDsonType == DsonType.Null) {
            reader.ReadNull();
            return null;
        }
        Type targetType = typeof(Dictionary<K, V>);
        if (CodecRegistry.GetDecoder(targetType) is DsonCodecImpl<Dictionary<K, V>> codecImpl) {
            return codecImpl.ReadObject(this, targetType, features);
        }
        throw new AssertionError();
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
        if (reader.IsAtType) reader.ReadDsonType();
        if (reader.IsAtName) reader.ReadName(name); // name不匹配抛出异常
        //
        DsonType dsonType = reader.CurrentDsonType;
        if (dsonType == DsonType.Null) { // null直接返回
            reader.ReadNull(name);
            return default;
        }
        // DsonValue接收原始数据 - 通过Feature指定时，字段通常应该声明为object
        if ((features & DeserializeFeatures.ReadAsDsonValue) != 0) {
            return (T)(object)Dsons.ReadDsonValue(reader);
        }
        if (!declaredType.IsValueType && typeof(DsonValue).IsAssignableFrom(declaredType)) {
            return (T)(object)Dsons.ReadDsonValue(reader);
        }
        // 引用解析，值类型也可能是顶层对象 - 编辑器生成的数据，不过还是应该避免如此
        if (dsonType == DsonType.Pointer
            && declaredType != typeof(ObjectPath)
            && declaredType != typeof(ObjectPtr)) {
            ObjectPtr ptr = reader.ReadPtr();
            return (T)ReadReference(ptr, declaredType, features);
        }
        // 容器类型只能通过codec解码
        if (dsonType == DsonType.Object || dsonType == DsonType.Array) {
            string? clsName = GetClassName(reader.CurrentValue);
            DsonCodecImpl decoder = FindObjectDecoder(declaredType, clsName);
            if (decoder == null) {
                throw DsonCodecException.Incompatible(declaredType, clsName);
            }
            // 避免结构体装箱
            if (decoder is DsonCodecImpl<T> codecImpl) {
                return codecImpl.ReadObject(this, declaredType, features);
            } else {
                return (T)decoder.ReadObject2(this, declaredType, features);
            }
        } else {
            // 非容器类型 -- Dson内建结构，Enum，Const等
            if (converter.CodecRegistry.GetDecoder(declaredType) is DsonCodecImpl<T> decoder) {
                return decoder.ReadObject(this, declaredType, features);
            }
            // 默认类型转换-声明类型可能是个抽象类型，eg：Number
            return (T)DsonCodecHelper.ReadDsonValueValue(reader);
        }
    }

    private object? ReadReference(ObjectPtr rawPtr, Type declaredType, DeserializeFeatures features) {
        if (rawPtr.LocalId == 0) {
            return null;
        }
        // 默认的序列化只支持引用当前文件（集合）内的对象
        int ptr = (int)rawPtr.LocalId;
        if (!string.IsNullOrEmpty(rawPtr.Collection) || !referenceTable.ContainsKey(ptr)) {
            throw new DsonCodecException($"Invalid Ptr: {rawPtr}");
        }
        return GetReference(ptr, declaredType, features);
    }

    private static string? GetClassName(DsonValue dsonValue) {
        DsonHeader<string> header;
        if (dsonValue is DsonObject<string> dsonObject) {
            header = dsonObject.Header;
        } else {
            header = dsonValue.AsArray().Header;
        }
        if (header.TryGetValue(DsonHeader.Names_ClassName, out DsonValue value)) {
            return value.AsString();
        }
        return null;
    }

    private DsonCodecImpl? FindObjectDecoder(Type declaredType, string? clsName) {
        // 尝试按真实类型读 -- IsAssignableFrom 支持 Nullable
        if (!string.IsNullOrWhiteSpace(clsName)) {
            TypeMeta typeMeta = converter.TypeMetaRegistry.OfName(clsName);
            if (typeMeta != null && declaredType.IsAssignableFrom(typeMeta.type)) {
                return converter.CodecRegistry.GetDecoder(typeMeta.type);
            }
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的(投影)
        return converter.CodecRegistry.GetDecoder(declaredType);
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

    public DsonType ReadDsonType() {
        return reader.IsAtType ? reader.ReadDsonType() : reader.CurrentDsonType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadName() {
        if (reader.IsAtType) {
            reader.ReadDsonType();
        }
        return reader.ReadName();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadName(string name) {
        if (reader.IsAtType) {
            reader.ReadDsonType();
        }
        reader.ReadName(name);
    }

    public DsonType CurrentDsonType => reader.CurrentDsonType;
    public string CurrentName => reader.CurrentName;

    public SerializeHeader ReadStartObject(Type encoderType, DeserializeFeatures features) {
        TypeMeta? typeMeta = converter.TypeMetaRegistry.OfType(encoderType);
        return ReadStartObject(typeMeta, features);
    }

    public SerializeHeader ReadStartObject(TypeMeta? typeMeta, DeserializeFeatures features) {
        DsonCollectionReader<string> reader = this.reader;
        reader.ReadStartObject();
        if (reader.PeekDsonType() == DsonType.Header) {
            reader.ReadDsonType();
            reader.SkipValue();
        }
        reader.Attach(typeMeta);
        //
        if (reader.ContextDepth == 1) {
            ItemContext itemContext = referenceTable[_stack];
            return itemContext.header;
        }
        DsonValue dsonValue = reader.GetContainer();
        return ReadHeader(dsonValue);
    }

    public void ReadEndObject() {
        reader.SkipToEndOfObject();
        reader.ReadEndObject();
        BackToPrevContext();
    }

    public SerializeHeader ReadStartArray(Type encoderType, DeserializeFeatures features) {
        TypeMeta? typeMeta = converter.TypeMetaRegistry.OfType(encoderType);
        return ReadStartArray(typeMeta, features);
    }

    public SerializeHeader ReadStartArray(TypeMeta? typeMeta, DeserializeFeatures features) {
        DsonCollectionReader<string> reader = this.reader;
        reader.ReadStartArray();
        if (reader.PeekDsonType() == DsonType.Header) {
            reader.ReadDsonType();
            reader.SkipValue();
        }
        reader.Attach(typeMeta);
        //
        if (reader.ContextDepth == 1) {
            ItemContext itemContext = referenceTable[_stack];
            return itemContext.header;
        }
        DsonValue dsonValue = reader.GetContainer();
        return ReadHeader(dsonValue);
    }

    public void ReadEndArray() {
        reader.SkipToEndOfObject();
        reader.ReadEndArray();
        BackToPrevContext();
    }

    public void SkipName() {
        reader.SkipName();
    }

    public void SkipValue() {
        reader.SkipValue();
    }

    public void SkipToEndOfObject() {
        reader.SkipToEndOfObject();
    }

    public byte[] ReadValueAsBytes(string name) {
        return reader.ReadValueAsBytes(name);
    }

    public TypeMeta? ContainerTypeMeta => reader.Attachment() as TypeMeta;

    public DsonCodecImpl<T>? GetInlinableCodec<T>() {
        DsonCodecImpl decoder = converter.CodecRegistry.GetDecoder(typeof(T));
        if (decoder is DsonCodecImpl<T> castDecoder && castDecoder.IsInlinableCodec) {
            return castDecoder;
        }
        return null;
    }

    public void SetEnableNameIntern(bool? value) {
        reader.SetEnableNameIntern(value);
    }

    public void SetComponentType(DsonType dsonType) {
        //
    }

    public void Dispose() {
        this.converter = null;
        this.referenceTable.Clear();
        this.reader = null;
        _stack = default;
        _listCache.Clear();
    }

    #endregion

#nullable disable

    #region context

    private const int STATUS_NEW = 0;
    private const int STATUS_PROCESSING = 1;

    private struct ItemContext
    {
        public DsonCollectionReader<string> reader; // 解码时创建
        public SerializeHeader header;
        public DsonValue dsonValue; // 讲道理都是DsonObject
        public object objectValue; // 用户在NewInstance后可能没有立即发布引用
        public int status;
        public int Pointer => header.localId;
    }

    #endregion
}
}