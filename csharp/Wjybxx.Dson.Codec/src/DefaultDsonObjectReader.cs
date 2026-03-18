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
internal class DefaultDsonObjectReader : IDsonObjectReader
{
#nullable disable
    private DefaultDsonConverter converter;
    private readonly LinkedDictionary<ObjectPtr, ItemContext> referenceTable = new(LocalIdComparer.Inst);
    // private readonly LinkedDictionary<ObjectPtr, ObjectPtr> pointerLink = new(LocalPathComparer.Inst);
    private DsonCollectionReader<string> reader;
    private ObjectPtr _stack;
    private readonly List<ObjectPtr> _listCache = new List<ObjectPtr>();
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
            if (!referenceTable.TryAdd(itemContext.pointer, itemContext)) {
                throw new Exception("Duplicate pointer: " + itemContext.pointer);
            }
        }
    }

    public object ReadFirst(Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        ObjectPtr ptr = referenceTable.PeekFirstKey();
        return GetReference(ptr, declaredType, features, factory);
    }

    public object ReadFirst(Type declaredType, long localId, DeserializeFeatures features, Func<object>? factory = null) {
        ObjectPtr ptr = localId != 0 ? new ObjectPtr(localId) : referenceTable.PeekFirstKey();
        return GetReference(ptr, declaredType, features, factory);
    }

    public List<T> ReadAll<T>(DeserializeFeatures features, Func<object>? factory = null) {
        _listCache.AddRange(referenceTable.Keys); // 用于保持原始顺序
        //
        List<T> result = new List<T>(referenceTable.Count);
        foreach (ObjectPtr ptr in _listCache) {
            result.Add((T)GetReference(ptr, typeof(T), features, factory));
        }
        return result;
    }

    private object GetReference(ObjectPtr ptr, Type declaredType, DeserializeFeatures features, Func<object>? factory) {
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
        _stack = itemContext.pointer;
        reader = itemContext.reader;
        // 用户的Codec可能没有立即发布引用，这里进行修正；值类型统一在这里发布引用
        object inst = ReadObject(declaredType, features, factory);
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
            _stack = prevContext.pointer;
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
        if (read < dsonHeader.Count && dsonHeader.TryGetValue(DsonHeader.Names_Collection, out dsonValue)) {
            header.collection = dsonValue.AsString();
            read++;
        }
        if (read < dsonHeader.Count && dsonHeader.TryGetValue(DsonHeader.Names_Version, out dsonValue)) {
            header.version = dsonValue.AsNumber().IntValue;
            read++;
        }
        return header;
    }

    #region 简单值

    public int ReadInt(string name, DeserializeFeatures features) {
        return ReadName(name) ? DsonCodecHelper.ReadInt(reader, name) : 0;
    }

    public long ReadLong(string name, DeserializeFeatures features) {
        return ReadName(name) ? DsonCodecHelper.ReadLong(reader, name) : 0;
    }

    public float ReadFloat(string name, DeserializeFeatures features) {
        return ReadName(name) ? DsonCodecHelper.ReadFloat(reader, name) : 0;
    }

    public double ReadDouble(string name, DeserializeFeatures features) {
        return ReadName(name) ? DsonCodecHelper.ReadDouble(reader, name) : 0;
    }

    public bool ReadBool(string name, DeserializeFeatures features) {
        return ReadName(name) && DsonCodecHelper.ReadBool(reader, name);
    }

    public string? ReadString(string name, DeserializeFeatures features) {
        return ReadName(name) ? DsonCodecHelper.ReadString(reader, name) : null;
    }

    public void ReadNull(string name) {
        if (ReadName(name)) {
            DsonCodecHelper.ReadNull(reader, name);
        }
    }

    public byte[]? ReadBytes(string name, DeserializeFeatures features) {
        Binary binary = ReadBinary(name, features);
        return binary == null ? null : binary.Unwrap();
    }

    public Binary? ReadBinary(string name, DeserializeFeatures features) {
        return ReadName(name) ? DsonCodecHelper.ReadBinary(reader, name) : null;
    }

    public ObjectPtr ReadPtr(string name) {
        return ReadName(name) ? DsonCodecHelper.ReadPtr(reader, name) : default;
    }

    public DateTime ReadDateTime(string name) {
        return ReadName(name) ? DsonCodecHelper.ReadDateTime(reader, name).ToDateTime() : default;
    }

    public ExtDateTime ReadExtDateTime(string name) {
        return ReadName(name) ? DsonCodecHelper.ReadDateTime(reader, name) : default;
    }

    public Timestamp ReadTimestamp(string name) {
        return ReadName(name) ? DsonCodecHelper.ReadTimestamp(reader, name) : default;
    }

    public Double4 ReadDouble4(string name) {
        return ReadName(name) ? DsonCodecHelper.ReadDouble4(reader, name) : default;
    }

    public T ReadEnum<T>(string name, DeserializeFeatures features = default) {
        if (!ReadName(name)) {
            return default;
        }
        if (CodecRegistry.GetDecoder(typeof(T)) is DsonCodecImpl<T> codecImpl) {
            return codecImpl.ReadObject(this, typeof(T), features);
        }
        throw new DsonCodecException($"Invalid EnumType: {typeof(T)}");
    }

    #endregion

    #region 简单值-无name版

    public int ReadInt(DeserializeFeatures features) {
        return DsonCodecHelper.ReadInt(reader, null);
    }

    public long ReadLong(DeserializeFeatures features) {
        return DsonCodecHelper.ReadLong(reader, null);
    }

    public float ReadFloat(DeserializeFeatures features) {
        return DsonCodecHelper.ReadFloat(reader, null);
    }

    public double ReadDouble(DeserializeFeatures features) {
        return DsonCodecHelper.ReadDouble(reader, null);
    }

    public bool ReadBool(DeserializeFeatures features) {
        return DsonCodecHelper.ReadBool(reader, null);
    }

    public string? ReadString(DeserializeFeatures features) {
        return DsonCodecHelper.ReadString(reader, null);
    }

    public void ReadNull() {
        DsonCodecHelper.ReadNull(reader, null);
    }

    public byte[]? ReadBytes(DeserializeFeatures features) {
        Binary binary = ReadBinary(features);
        return binary == null ? null : binary.Unwrap();
    }

    public Binary? ReadBinary(DeserializeFeatures features) {
        return DsonCodecHelper.ReadBinary(reader, null);
    }

    public ObjectPtr ReadPtr() {
        return DsonCodecHelper.ReadPtr(reader, null);
    }

    public DateTime ReadDateTime() {
        return DsonCodecHelper.ReadDateTime(reader, null).ToDateTime();
    }

    public ExtDateTime ReadExtDateTime() {
        return DsonCodecHelper.ReadDateTime(reader, null);
    }

    public Timestamp ReadTimestamp() {
        return DsonCodecHelper.ReadTimestamp(reader, null);
    }

    public Double4 ReadDouble4() {
        return DsonCodecHelper.ReadDouble4(reader, null);
    }

    public T ReadEnum<T>(DeserializeFeatures features = default) {
        if (CodecRegistry.GetDecoder(typeof(T)) is DsonCodecImpl<T> codecImpl) {
            return codecImpl.ReadObject(this, typeof(T), features);
        }
        throw new DsonCodecException($"Invalid EnumType: {typeof(T)}");
    }

    #endregion

    #region object处理

    public object ReadObject(string name, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        return ReadObject<object>(name, declaredType, features, factory);
    }

    public T ReadObject<T>(string name, DeserializeFeatures features, Func<object>? factory = null) {
        return ReadObject<T>(name, typeof(T), features, factory);
    }

    public object ReadObject(Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        return ReadObject<object>(null, declaredType, features, factory);
    }

    public T ReadObject<T>(DeserializeFeatures features, Func<object>? factory = null) {
        return ReadObject<T>(null, typeof(T), features, factory);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">仅用于避免装箱，不能用于其它语义</typeparam>
    private T ReadObject<T>(string? name, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
        if (!ReadName(name)) { //  字段不存在，返回默认值
            return default;
        }
        DsonType dsonType = reader.CurrentDsonType;
        if (dsonType == DsonType.Null) { // null直接返回
            reader.ReadNull(name);
            return default;
        }
        // DsonValue接收原始数据
        if ((features & DeserializeFeatures.ReadAsDsonValue) != 0) {
            return (T)(object)Dsons.ReadDsonValue(reader);
        }
        if (!declaredType.IsValueType && typeof(DsonValue).IsAssignableFrom(declaredType)) {
            return (T)(object)Dsons.ReadDsonValue(reader);
        }
        // 引用解析，值类型也可能是顶层对象
        if (dsonType == DsonType.Pointer
            && declaredType != typeof(ObjectPath)
            && declaredType != typeof(ObjectPtr)) {
            ObjectPtr ptr = reader.ReadPtr();
            return (T)ReadReference(ptr, declaredType, features, factory);
        }
        // 容器类型只能通过codec解码
        if (dsonType.IsContainer()) {
            string? clsName = GetClassName(reader.CurrentValue);
            DsonCodecImpl decoder = FindObjectDecoder(declaredType, factory, clsName);
            if (decoder == null) {
                throw DsonCodecException.Incompatible(declaredType, clsName);
            }
            // 避免结构体装箱
            if (decoder is DsonCodecImpl<T> codecImpl) {
                return codecImpl.ReadObject(this, declaredType, features, factory);
            } else {
                return (T)decoder.ReadObject2(this, declaredType, features, factory);
            }
        } else {
            // 非容器类型 -- Dson内建结构，Enum，Const等
            if (converter.CodecRegistry.GetDecoder(declaredType) is DsonCodecImpl<T> decoder) {
                return decoder.ReadObject(this, declaredType, features, factory);
            }
            // 默认类型转换-声明类型可能是个抽象类型，eg：Number
            return (T)DsonCodecHelper.ReadDsonValueValue(reader, name);
        }
    }

    private object? ReadReference(ObjectPtr rawPtr, Type declaredType, DeserializeFeatures features, Func<object>? factory) {
        if (rawPtr.LocalId == 0) {
            return null;
        }
        // 引用中可能包含额外数据，需要清理
        ObjectPtr ptr = new ObjectPtr(rawPtr.Collection, null, rawPtr.LocalId);
        return referenceTable.ContainsKey(ptr) ? GetReference(ptr, declaredType, features, factory) : null;
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

    private DsonCodecImpl? FindObjectDecoder<T>(Type declaredType, Func<T>? factory, string? clsName) {
        // factory不为null时，直接按照声明类型查找 -- factory创建的实例可能和写入的真实类型不兼容
        if (factory != null) {
            return converter.CodecRegistry.GetDecoder(declaredType);
        }
        // 如果factory为null，最终的codec关联的type一定是声明类型的子类型
        // 尝试按真实类型读 -- IsAssignableFrom 支持 Nullable
        if (!string.IsNullOrWhiteSpace(clsName)) {
            TypeMeta typeMeta = converter.TypeMetaRegistry.OfName(clsName);
            if (typeMeta != null && declaredType.IsAssignableFrom(typeMeta.type)) {
                return converter.CodecRegistry.GetDecoder(typeMeta.type);
            }
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的(投影) LinkedDictionary => Dictionary
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

    public string ReadName() {
        if (reader.IsAtType) {
            reader.ReadDsonType();
        }
        return reader.ReadName();
    }

    public bool ReadName(string? name) {
        DsonCollectionReader<string> reader = this.reader;
        // array
        if (reader.ContextType.IsArrayLike()) {
            if (reader.IsAtValue) {
                return true;
            }
            if (reader.IsAtType) {
                return reader.ReadDsonType() != DsonType.EndOfObject;
            }
            return reader.CurrentDsonType != DsonType.EndOfObject;
        }
        // object
        if (reader.IsAtValue) {
            if (name == null || reader.CurrentName == name) {
                return true;
            }
            reader.SkipValue();
        }
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (reader.IsAtType) {
            if (reader.Attachment() is Context context) {
                if (context.Contains(name)) {
                    context.SetNext(name);
                    reader.ReadDsonType();
                    reader.ReadName();
                    return true;
                }
                return false; // 主动读模式下不破坏输入，因此不抛出异常
            }
            reader.ReadDsonType();
            reader.ReadName(name); // 被动读不匹配的情况下抛出异常
            return true;
        } else {
            if (reader.CurrentDsonType == DsonType.EndOfObject) {
                return false;
            }
            return name == reader.ReadName();
        }
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
        //
        if ((features & DeserializeFeatures.PassiveReading) == 0) {
            Context context = contextPool.Acquire();
            context.typeMeta = typeMeta;
            context.SetKeySet(reader.Keys());
            reader.SetKeyItr(context, DsonNull.NULL);
            reader.Attach(context);
        } else {
            reader.Attach(typeMeta);
        }
        //
        if (reader.ContextDepth == 1) {
            ItemContext itemContext = referenceTable[_stack];
            return itemContext.header;
        }
        DsonValue dsonValue = reader.GetContainer();
        SerializeHeader header = ReadHeader(dsonValue);
        if (header.count == 0) {
            header.count = features.ToInitCapacity();
        }
        return header;
    }

    public void ReadEndObject() {
        if (reader.Attach(null) is Context context) {
            contextPool.Release(context);
        }
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
        SerializeHeader header = ReadHeader(dsonValue);
        if (header.count == 0) {
            header.count = features.ToInitCapacity();
        }
        return header;
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

    public TypeMeta? ContainerTypeMeta {
        get {
            return reader.Attachment() switch
            {
                TypeMeta typeMeta => typeMeta,
                Context context => context.typeMeta,
                _ => null
            };
        }
    }

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

    #region util

    private bool IsReadZeroValue(DeserializeFeatures features) {
        if ((features & DeserializeFeatures.ReadZeroValue) != 0) return true;
        if ((features & DeserializeFeatures.SkipZeroValue) != 0) return false;
        TypeMeta typeMeta = ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.decodeFeatures;
            if ((features & DeserializeFeatures.ReadZeroValue) != 0) return true;
            if ((features & DeserializeFeatures.SkipZeroValue) != 0) return false;
        }
        features = converter.Options.decodeFeatures;
        return (features & DeserializeFeatures.ReadZeroValue) != 0;
    }

    private bool IsReadNullValue(DeserializeFeatures features) {
        if ((features & DeserializeFeatures.ReadNullValue) != 0) return true;
        if ((features & DeserializeFeatures.SkipNullValue) != 0) return false;
        TypeMeta typeMeta = ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.decodeFeatures;
            if ((features & DeserializeFeatures.ReadNullValue) != 0) return true;
            if ((features & DeserializeFeatures.SkipNullValue) != 0) return false;
        }
        features = converter.Options.decodeFeatures;
        return (features & DeserializeFeatures.ReadNullValue) != 0;
    }

    private bool IsReadEmptyStringAsNull(DeserializeFeatures features) {
        if (((features & DeserializeFeatures.EmptyStringAsEmpty) != 0)) return false;
        if ((features & DeserializeFeatures.EmptyStringAsNull) != 0) return true;
        TypeMeta typeMeta = ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.decodeFeatures;
            if (((features & DeserializeFeatures.EmptyStringAsEmpty) != 0)) return false;
            if ((features & DeserializeFeatures.EmptyStringAsNull) != 0) return true;
        }
        features = converter.Options.decodeFeatures;
        if (((features & DeserializeFeatures.EmptyStringAsEmpty) != 0)) return false;
        return (features & DeserializeFeatures.EmptyStringAsNull) != 0;
    }

    #endregion

    #region context

    private static readonly ConcurrentObjectPool<Context> contextPool = new(
        () => new Context(), context => context.Dispose(), 256);

    private const int STATUS_NEW = 0;
    private const int STATUS_PROCESSING = 1;

    private struct ItemContext
    {
        public DsonCollectionReader<string> reader; // 解码时创建
        public SerializeHeader header;
        public DsonValue dsonValue; // 讲道理都是DsonObject
        public object objectValue; // 用户在NewInstance后可能没有立即发布引用
        public int status;

        public ObjectPtr pointer => new ObjectPtr(header.collection, null, header.localId);
    }

    private class Context : ISequentialEnumerator<string>
    {
        private readonly LinkedHashSet<string> keyQueue = new LinkedHashSet<string>();
        public TypeMeta typeMeta;
        private ICollection<string> _keySet;
        private string? _current;

        public void SetKeySet(ICollection<string> keySet) {
            this._keySet = keySet;
            foreach (string name in this._keySet) {
                keyQueue.Add(name);
            }
        }

        public void SetNext(string nextName) {
            if (nextName == null) throw new ArgumentNullException(nameof(nextName));
            if (keyQueue.TryPeekFirst(out string name) && name == nextName) {
                return;
            }
            keyQueue.AddFirst(nextName);
        }

        public bool Contains(string name) => _keySet.Contains(name);

        public bool HasNext() {
            return !keyQueue.IsEmpty;
        }

        public bool MoveNext() {
            return keyQueue.TryRemoveFirst(out _current);
        }

        public void Reset() {
        }

        public void Dispose() {
            keyQueue.Clear();
            typeMeta = null;
            _keySet = null!;
            _current = null!;
        }

        public string? Current => _current;
        object? IEnumerator.Current => Current;
    }

    private class LocalIdComparer : IEqualityComparer<ObjectPtr>
    {
        public static readonly LocalIdComparer Inst = new LocalIdComparer();

        public bool Equals(ObjectPtr x, ObjectPtr y) {
            return x.LocalId == y.LocalId
                   && x.Collection == y.Collection;
        }

        public int GetHashCode(ObjectPtr obj) {
            int hashCode = obj.LocalId.GetHashCode();
            hashCode = (hashCode * 397) ^ (obj.Collection != null ? obj.Collection.GetHashCode() : 0);
            return hashCode;
        }
    }

    private class LocalPathComparer : IEqualityComparer<ObjectPtr>
    {
        public static readonly LocalPathComparer Inst = new LocalPathComparer();

        public bool Equals(ObjectPtr x, ObjectPtr y) {
            return x.LocalPath == y.LocalPath
                   && x.Collection == y.Collection;
        }

        public int GetHashCode(ObjectPtr obj) {
            int hashCode = obj.LocalPath.GetHashCode();
            hashCode = (hashCode * 397) ^ (obj.Collection != null ? obj.Collection.GetHashCode() : 0);
            return hashCode;
        }
    }

    #endregion
}
}