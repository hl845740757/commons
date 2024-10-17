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
        if (!ReadName(name)) { //  字段不存在，返回默认值
            return default;
        }
        IDsonReader<string> reader = this.reader;
        DsonType dsonType = reader.CurrentDsonType;
        if (dsonType == DsonType.Null) { // null直接返回
            reader.ReadNull(name);
            return default;
        }
        if (dsonType.IsContainer()) { // 容器类型只能通过codec解码
            return ReadContainer(declaredType, factory, dsonType);
        }
        // 非容器类型 -- Dson内建结构，基础值类型，Enum，String等
        if (declaredType.IsEnum) {
            DsonCodecImpl<T> codec = (DsonCodecImpl<T>)converter.CodecRegistry.GetDecoder(declaredType)!;
            return codec.ReadObject(this);
        }
        // 考虑DsonValue
        if (typeof(DsonValue).IsAssignableFrom(declaredType)) {
            return (T)(object)Dsons.ReadDsonValue(reader);
        }
        // 默认类型转换-声明类型可能是个抽象类型，eg：Number
        return (T)DsonCodecHelper.ReadDsonValue(reader, dsonType, name);
    }

    private T ReadContainer<T>(Type declaredType, Func<T>? factory, DsonType dsonType) {
        string clsName = ReadClsName(dsonType);
        DsonCodecImpl codec = FindObjectDecoder(declaredType, factory, clsName);
        if (codec == null) {
            throw DsonCodecException.Incompatible(declaredType, clsName);
        }
        // 避免结构体装箱
        if (codec is DsonCodecImpl<T> codecImpl) {
            return codecImpl.ReadObject(this, factory);
        } else {
            return (T)codec.ReadObject2(this, factory);
        }
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

    private static readonly Delegate parseInt = new Func<string, int>(int.Parse);
    private static readonly Delegate parseLong = new Func<string, long>(long.Parse);
    private static readonly Delegate parseUint = new Func<string, uint>(uint.Parse);
    private static readonly Delegate parseUlong = new Func<string, ulong>(ulong.Parse);

    public T DecodeKey<T>(string keyString) {
        Type type = typeof(T);
        if (type == typeof(string) || type == typeof(object)) {
            return (T)(object)keyString;
        }
        // 使用func以避免装箱
        if (type == typeof(int)) {
            Func<string, T> func = (Func<string, T>)parseInt;
            return func.Invoke(keyString);
        }
        if (type == typeof(long)) {
            Func<string, T> func = (Func<string, T>)parseLong;
            return func.Invoke(keyString);
        }
        if (type == typeof(uint)) {
            Func<string, T> func = (Func<string, T>)parseUint;
            return func.Invoke(keyString);
        }
        if (type == typeof(ulong)) {
            Func<string, T> func = (Func<string, T>)parseUlong;
            return func.Invoke(keyString);
        }
        // 处理枚举类型
        DsonCodecImpl<T> codec = (DsonCodecImpl<T>)converter.CodecRegistry.GetDecoder(type)!;
        if (codec == null || !codec.IsEnumCodec) {
            throw DsonCodecException.UnsupportedKeyType(type);
        }
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

    private string ReadClsName(DsonType dsonType) {
        IDsonReader<string> reader = this.reader;
        if (reader.HasWaitingStartContext()) {
            return ""; // 已读取header，当前可能触发了读代理
        }
        if (dsonType == DsonType.Object) {
            reader.ReadStartObject();
        } else {
            reader.ReadStartArray();
        }
        string clsName;
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

    private DsonCodecImpl? FindObjectDecoder<T>(Type declaredType, Func<T>? factory, string clsName) {
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
}
}