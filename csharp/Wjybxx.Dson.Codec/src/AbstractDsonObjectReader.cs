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
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 
/// </summary>
public abstract class AbstractDsonObjectReader : IDsonObjectReader
{
    protected readonly IDsonConverter converter;
    protected readonly IDsonReader<string> reader;

    protected AbstractDsonObjectReader(IDsonConverter converter, IDsonReader<string> reader) {
        this.converter = converter;
        this.reader = reader;
    }

    #region 简单值

    public int ReadInt(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadInt(reader, name) : 0;
    }

    public long ReadLong(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadLong(reader, name) : 0;
    }

    public float ReadFloat(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadFloat(reader, name) : 0;
    }

    public double ReadDouble(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadDouble(reader, name) : 0;
    }

    public bool ReadBool(string? name) {
        return ReadName(name) && DsonCodecHelper.ReadBool(reader, name);
    }

    public string? ReadString(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadString(reader, name) : null;
    }

    public void ReadNull(string? name) {
        if (ReadName(name)) {
            DsonCodecHelper.ReadNull(reader, name);
        }
    }

    public Binary ReadBinary(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadBinary(reader, name) : default;
    }

    public ObjectPtr ReadPtr(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadPtr(reader, name) : default;
    }

    public ObjectLitePtr ReadLitePtr(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadLitePtr(reader, name) : default;
    }

    public DateTime ReadDateTime(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadDateTime(reader, name).ToDateTime() : default;
    }

    public ExtDateTime ReadExtDateTime(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadDateTime(reader, name) : default;
    }

    public Timestamp ReadTimestamp(string? name) {
        return ReadName(name) ? DsonCodecHelper.ReadTimestamp(reader, name) : default;
    }

    #endregion

    #region object处理

    public T ReadObject<T>(string? name, Type declaredType, Func<T>? factory = null) {
        if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
        if (!ReadName(name)) { // 顺带读取了DsonType
            return default;
        }
        IDsonReader<string> reader = this.reader;
        if (declaredType.IsPrimitive) {
            return (T)DsonCodecHelper.ReadPrimitive(reader, name, declaredType);
        }
        if (declaredType == typeof(string)) {
            return (T)(object)DsonCodecHelper.ReadString(reader, name);
        }
        if (declaredType == typeof(byte[])) {
            Binary binary = DsonCodecHelper.ReadBinary(reader, name);
            return (T)(object)binary.UnsafeBuffer;
        }

        DsonType dsonType = reader.CurrentDsonType;
        if (dsonType == DsonType.Null) { // null直接返回
            reader.ReadNull(name);
            return default;
        }
        if (dsonType.IsContainer()) { // 容器类型只能通过codec解码
            return ReadContainer(declaredType, factory, dsonType);
        }

        // 考虑枚举类型--可转换为基础值类型的Object
        IDsonCodecRegistry rootRegistry = converter.CodecRegistry;
        DsonCodecImpl<T> codec = rootRegistry.GetDecoder(declaredType, rootRegistry) as DsonCodecImpl<T>;
        if (codec != null && codec.IsEnumCodec) {
            return codec.ReadObject(this, declaredType);
        }
        if (typeof(DsonValue).IsAssignableFrom(declaredType)) {
            return (T)(object)Dsons.ReadDsonValue(reader);
        }
        // 默认类型转换-声明类型可能是个抽象类型，eg：Number
        return (T)DsonCodecHelper.ReadDsonValue(reader, dsonType, name);
    }

    private T ReadContainer<T>(Type declaredType, Func<T>? factory, DsonType dsonType) {
        string classId = ReadClassId(dsonType);
        DsonCodecImpl codec = FindObjectDecoder(declaredType, factory, classId);
        if (codec == null) {
            throw DsonCodecException.Incompatible(declaredType, classId);
        }
        if (codec.GetEncoderClass() == typeof(T)) {
            DsonCodecImpl<T> codecImpl = (DsonCodecImpl<T>)codec;
            return codecImpl.ReadObject(this, declaredType, factory);
        }
        // codec可能是实际类型(子类型)的codec
        return (T)codec.ReadObject2(this, declaredType, factory);
    }

    #endregion

    #region 流程

    public ConverterOptions Options => converter.Options;
    public DsonContextType ContextType => reader.ContextType;

    public DsonType ReadDsonType() {
        return reader.IsAtType ? reader.ReadDsonType() : reader.CurrentDsonType;
    }

    public string ReadName() {
        return reader.ReadName();
    }

    public abstract bool ReadName(string? name);

    public DsonType CurrentDsonType => reader.CurrentDsonType;
    public string CurrentName => reader.CurrentName;

    public virtual void ReadStartObject() {
        if (reader.IsAtType) { // 顶层对象适配
            reader.ReadDsonType();
        }
        reader.ReadStartObject();
    }

    public virtual void ReadEndObject() {
        reader.SkipToEndOfObject();
        reader.ReadEndObject();
    }

    public virtual void ReadStartArray() {
        if (reader.IsAtType) { // 顶层对象适配
            reader.ReadDsonType();
        }
        reader.ReadStartArray();
    }

    public virtual void ReadEndArray() {
        reader.SkipToEndOfObject();
        reader.ReadEndArray();
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

    public T DecodeKey<T>(string keyString) {
        Type type = typeof(T);
        if (type == typeof(int)) {
            int r = int.Parse(keyString); // 会导致装箱，外部需要优化
            return (T)(object)r;
        }
        if (type == typeof(long)) {
            long r = long.Parse(keyString);
            return (T)(object)r;
        }
        if (type == typeof(string)) {
            return (T)(object)keyString;
        }
        IDsonCodecRegistry rootRegistry = converter.CodecRegistry;
        DsonCodecImpl<T> codec = rootRegistry.GetDecoder(type, rootRegistry) as DsonCodecImpl<T>;
        if (codec == null || !codec.IsEnumCodec) {
            throw DsonCodecException.UnsupportedKeyType(type);
        }
        // 处理枚举类型
        T result;
        if (converter.Options.writeEnumAsString) {
            if (codec.ForName(keyString, out result)) {
                return result;
            }
        } else {
            int number = int.Parse(keyString);
            if (codec.ForNumber(number, out result)) {
                return result;
            }
        }
        throw DsonCodecException.EnumAbsent(type, keyString);
    }

    public void SetComponentType(DsonType dsonType) {
        if (reader is DsonTextReader textReader) {
            DsonToken token = DsonTexts.ClsNameTokenOfType(dsonType);
            textReader.SetCompClsNameToken(token);
        }
    }

    public void Dispose() {
        reader.Dispose();
    }

    private string ReadClassId(DsonType dsonType) {
        IDsonReader<string> reader = this.reader;
        if (dsonType == DsonType.Object) {
            reader.ReadStartObject();
        } else {
            reader.ReadStartArray();
        }
        String clsName;
        DsonType nextDsonType = reader.PeekDsonType();
        if (nextDsonType == DsonType.Header) {
            reader.ReadDsonType();
            reader.ReadStartHeader();
            clsName = reader.ReadString(DsonHeaders.Names_ClassName);
            if (clsName.LastIndexOf(' ') < 0) {
                clsName = string.Intern(clsName); // 池化
            }
            reader.SkipToEndOfObject();
            reader.ReadEndHeader();
        } else {
            clsName = "";
        }
        reader.BackToWaitStart();
        return clsName;
    }

    private DsonCodecImpl? FindObjectDecoder<T>(Type declaredType, Func<T>? factory, string classId) {
        // factory不为null时，直接按照声明类型查找，因为factory的优先级最高
        IDsonCodecRegistry rootRegistry = converter.CodecRegistry;
        if (factory != null) {
            return rootRegistry.GetDecoder(declaredType, rootRegistry);
        }
        // 尝试按真实类型读 -- IsAssignableFrom 支持 Nullable
        if (!string.IsNullOrWhiteSpace(classId)) {
            TypeMeta typeMeta = converter.TypeMetaRegistry.OfName(classId);
            if (typeMeta != null && declaredType.IsAssignableFrom(typeMeta.type)) {
                return rootRegistry.GetDecoder(typeMeta.type, rootRegistry);
            }
        }
        // 尝试按照声明类型读 - 读的时候两者可能是无继承关系的(投影)
        return rootRegistry.GetDecoder(declaredType, rootRegistry);
    }

    #endregion
}
}