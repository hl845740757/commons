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
    private readonly LinkedDictionary<ObjectPtr, ItemContext> referenceTable = new(PointerComparer.Inst);
    private DsonCollectionReader<string> reader;
    private ObjectPtr _stack;

    private Type _rootDeclaredType;
    private DeserializeFeatures _rootFeatures;
    private Func<object>? _rootFactory;
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

    public void AddReference(DsonArray<string> collection) {
        if (collection.Count == 0) {
            throw new Exception("Empty collection");
        }
        foreach (DsonValue dsonValue in collection) {
            if (dsonValue.DsonType == DsonType.Header) {
                continue; // 文件头
            }
            ItemContext itemContext = new ItemContext()
            {
                header = ReadHeader(dsonValue),
                dsonValue = dsonValue,
            };
            referenceTable[itemContext.pointer] = itemContext; // localId重复时覆盖
        }
    }

    public object ReadFirst(Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        _rootDeclaredType = declaredType;
        _rootFeatures = features;
        _rootFactory = factory;
        ObjectPtr ptr = referenceTable.PeekFirstKey();
        return GetReference(ptr);
    }

    public List<T> ReadAll<T>(Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        _rootDeclaredType = declaredType;
        _rootFeatures = features;
        _rootFactory = factory;
        _listCache.AddRange(referenceTable.Keys); // 用于保持原始顺序
        //
        List<T> result = new List<T>(referenceTable.Count);
        foreach (ObjectPtr ptr in _listCache) {
            result.Add((T)GetReference(ptr));
        }
        return result;
    }

    private object GetReference(ObjectPtr ptr) {
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
        // 注意：read的过程中，Current可能变更，由ReadEnd发布到目标上下文
        return ReadObject(_rootDeclaredType, _rootFeatures, _rootFactory);
    }

    private void BackToPrevContext() {
        if (reader.ContextDepth == 0
            && referenceTable.PrevKey(_stack, out _, out ItemContext prevContext)) {
            _stack = prevContext.pointer;
            reader = prevContext.reader;
        }
    }

    public void PublishReference<T>(in T reference) {
        if (reader.ContextDepth == 1) { // 可多次发布覆盖
            ItemContext context = referenceTable[_stack];
            context.objectValue = reference;
            referenceTable[_stack] = context;
        }
    }

    private SerializeHeader ReadHeader(DsonValue container) {
        DsonHeader<string> dsonHeader;
        if (container is DsonObject<string> dsonObject) {
            dsonHeader = dsonObject.Header;
        } else {
            dsonHeader = container.AsArray().Header;
        }
        // DsonHeader使用的是ArrayDictionary，查询效率不好
        if (dsonHeader.IsEmpty) {
            return default;
        }
        SerializeHeader header = default;
        if (dsonHeader.TryGetValue(DsonHeader.Names_ClassName, out DsonValue tempValue)) {
            header.clsName = tempValue.AsString();
        }
        if (dsonHeader.TryGetValue(DsonHeader.Names_Collection, out tempValue)) {
            header.collection = tempValue.AsString();
        }
        if (dsonHeader.TryGetValue(DsonHeader.Names_LocalId, out tempValue)) {
            header.localId = tempValue.AsNumber().LongValue; // 手写文本可能是double
        }
        if (dsonHeader.TryGetValue(DsonHeader.Names_Count, out tempValue)) {
            header.count = tempValue.AsNumber().IntValue;
        }
        if (dsonHeader.TryGetValue(DsonHeader.Names_Version, out tempValue)) {
            header.version = tempValue.AsNumber().IntValue;
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
        string value = ReadName(name) ? DsonCodecHelper.ReadString(reader, name) : null;
        if (value != null && value.Length == 0 && IsReadEmptyStringAsNull(features)) {
            return null;
        }
        return value;
    }

    public void ReadNull(string name) {
        if (ReadName(name)) {
            DsonCodecHelper.ReadNull(reader, name);
        }
    }

    public byte[]? ReadBytes(string name, DeserializeFeatures features) {
        Binary binary = ReadBinary(name, features);
        return binary == null ? null : binary.UnsafeBuffer;
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
        string value = DsonCodecHelper.ReadString(reader, null);
        if (value != null && value.Length == 0 && IsReadEmptyStringAsNull(features)) {
            return null;
        }
        return value;
    }

    public void ReadNull() {
        DsonCodecHelper.ReadNull(reader, null);
    }

    public byte[]? ReadBytes(DeserializeFeatures features) {
        Binary binary = ReadBinary(features);
        return binary == null ? null : binary.UnsafeBuffer;
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
        if (dsonType == DsonType.Pointer && !declaredType.IsValueType) { // 引用解析
            ObjectPtr ptr = reader.CurrentValue.AsPointer(); // 注意：未触发Read
            if (ptr.LocalId != 0 && referenceTable.ContainsKey(ptr)) {
                reader.ReadPtr();
                return (T)GetReference(ptr);
            }
        }
        // DsonValue接收原始数据
        if (!declaredType.IsValueType && typeof(DsonValue).IsAssignableFrom(declaredType)) {
            return (T)(object)Dsons.ReadDsonValue(reader);
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
    public DsonContextType ContextType => reader.ContextType;

    public DsonType ReadDsonType() {
        return reader.IsAtType ? reader.ReadDsonType() : reader.CurrentDsonType;
    }

    public string ReadName() {
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
            // 用户尚未调用readDsonType，可指定下一个key的值
            Context context = (Context)reader.Attachment();
            if (context.Contains(name)) {
                context.SetNext(name);
                reader.ReadDsonType();
                reader.ReadName();
                return true;
            }
            return false;
        } else {
            if (reader.CurrentDsonType == DsonType.EndOfObject) {
                return false;
            }
            return name == reader.ReadName(); // 不抛出异常
        }
    }

    public DsonType CurrentDsonType => reader.CurrentDsonType;
    public string CurrentName => reader.CurrentName;

    public SerializeHeader ReadStartObject(Type encoderType, DeserializeFeatures features) {
        TypeMeta? typeMeta = converter.TypeMetaRegistry.OfType(encoderType);
        if (typeMeta == null) {
            throw DsonCodecException.UnsupportedType(encoderType);
        }
        return ReadStartObject(typeMeta, features);
    }

    public SerializeHeader ReadStartObject(TypeMeta typeMeta, DeserializeFeatures features) {
        reader.ReadStartObject();
        if (reader.PeekDsonType() == DsonType.Header) {
            reader.ReadDsonType();
            reader.SkipValue();
        }
        //
        Context context = contextPool.Acquire();
        context.SetKeySet(reader.Keys());
        context.typeMeta = typeMeta;
        reader.SetKeyItr(context, DsonNull.NULL);
        reader.Attach(context);
        //
        if (reader.ContextDepth == 1) {
            ItemContext itemContext = referenceTable[_stack];
            return itemContext.header;
        }
        DsonValue dsonValue = reader.GetContainer();
        return ReadHeader(dsonValue);
    }

    public void ReadEndObject() {
        // 需要在readEndObject之前保存下来
        Context context = (Context)reader.Attachment();
        reader.SkipToEndOfObject();
        reader.ReadEndObject();
        //
        contextPool.Release(context);
        BackToPrevContext();
    }

    public SerializeHeader ReadStartArray(Type encoderType, DeserializeFeatures features) {
        TypeMeta? typeMeta = converter.TypeMetaRegistry.OfType(encoderType);
        if (typeMeta == null) {
            throw DsonCodecException.UnsupportedType(encoderType);
        }
        return ReadStartArray(typeMeta, features);
    }

    public SerializeHeader ReadStartArray(TypeMeta typeMeta, DeserializeFeatures features) {
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

    public TypeMeta? ContainerTypeMeta {
        get {
            if (reader.Attachment() is Context context) {
                return context.typeMeta;
            }
            return reader.Attachment() as TypeMeta;
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
        _rootDeclaredType = null;
        _rootFactory = null;
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

    private class PointerComparer : IEqualityComparer<ObjectPtr>
    {
        public static readonly PointerComparer Inst = new PointerComparer();

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

    #endregion
}
}