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
using System.Collections.Generic;
using System.Reflection;
using Wjybxx.Commons;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Codec.Codecs
{
public readonly struct EnumValueInfo<T>
{
    public readonly T value;
    public readonly int number;
    public readonly string name;
    public readonly string numberString;

    public EnumValueInfo(T value, int number, string name) {
        this.value = value;
        this.number = number;
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.numberString = EnumCodecUtil.ToString(number);
    }
}

/// <summary>
/// 默认枚举类的Codec
/// 注意：默认不支持序列化未在枚举中定义的枚举值 —— 其它特殊情况，建议直接使用int值。
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class EnumCodec<T> : IDsonCodec<T>, IKeyCodec<T> where T : struct, Enum
{
    private readonly Dictionary<T, EnumValueInfo<T>> _value2EnumDic;
    private readonly Dictionary<int, EnumValueInfo<T>> _number2EnumDic;
    private readonly Dictionary<string, EnumValueInfo<T>> _name2EnumDic; // 忽略大小写
    private readonly bool _isFlags;
    private readonly bool _isWriteAsString;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="valueInfos">枚举值信息，允许自定义枚举序列化数据</param>
    /// <param name="isFlags">是否是Flags类型</param>
    public EnumCodec(List<EnumValueInfo<T>> valueInfos, bool? isFlags = null) {
        _value2EnumDic = new Dictionary<T, EnumValueInfo<T>>(valueInfos.Count);
        _number2EnumDic = new Dictionary<int, EnumValueInfo<T>>(valueInfos.Count);
        _name2EnumDic = new Dictionary<string, EnumValueInfo<T>>(valueInfos.Count, StringComparer.OrdinalIgnoreCase);
        _isFlags = isFlags ?? EnumCodecUtil.IsFlags<T>();
        _isWriteAsString = (EnumCodecUtil.GetEncodeFeatures<T>() & SerializeFeatures.EnumAsString) != 0;

        foreach (EnumValueInfo<T> valueInfo in valueInfos) {
            _value2EnumDic[valueInfo.value] = valueInfo;
            _number2EnumDic[valueInfo.number] = valueInfo;
            _name2EnumDic[valueInfo.name] = valueInfo;
        }
    }

    public EnumCodec() {
        T[] values = EnumUtil.GetValues<T>();
        string[] names = EnumUtil.GetNames<T>();
        _value2EnumDic = new Dictionary<T, EnumValueInfo<T>>(values.Length);
        _number2EnumDic = new Dictionary<int, EnumValueInfo<T>>(values.Length);
        _name2EnumDic = new Dictionary<string, EnumValueInfo<T>>(values.Length);
        _isFlags = EnumCodecUtil.IsFlags<T>();
        _isWriteAsString = (EnumCodecUtil.GetEncodeFeatures<T>() & SerializeFeatures.EnumAsString) != 0;

        FieldInfo[] enumFields = typeof(T).GetFields();
        for (int idx = 0; idx < values.Length; idx++) {
            T value = values[idx];
            int number = EnumUtil.GetIntValue(value);
            // 可通过注解指定DsonName -- 第一个元素是占位符，查询枚举关联的Field时需要+1
            DsonPropertyAttribute attribute = enumFields[idx + 1].GetCustomAttribute<DsonPropertyAttribute>();
            string name = attribute != null && !string.IsNullOrWhiteSpace(attribute.Name) ? attribute.Name : names[idx];

            EnumValueInfo<T> valueInfo = new EnumValueInfo<T>(value, number, name);
            _value2EnumDic[valueInfo.value] = valueInfo;
            _number2EnumDic[valueInfo.number] = valueInfo;
            _name2EnumDic[valueInfo.name] = valueInfo;
        }
    }

    #region 避免装箱

    public string EncodeKey(T value, SerializeFeatures features) {
        // 枚举Key必须存在对应的名字
        if (_value2EnumDic.TryGetValue(value, out EnumValueInfo<T> valueInfo)) {
            return (features & SerializeFeatures.EnumKeyAsString) != 0
                ? valueInfo.name
                : valueInfo.numberString;
        }
        throw new DsonCodecException($"invalid enum key: {value}, type: {typeof(T)}");
    }

    public T DecodeKey(string keyString) {
        // 枚举Key必须存在对应的名字 - Flags不应该作为Key
        if (int.TryParse(keyString, out int number)) {
            if (number == 0) {
                return default;
            }
            if (_number2EnumDic.TryGetValue(number, out EnumValueInfo<T> valueInfo)) {
                return valueInfo.value;
            }
        } else {
            if (_name2EnumDic.TryGetValue(keyString, out EnumValueInfo<T> valueInfo)) {
                return valueInfo.value;
            }
        }
        throw new DsonCodecException($"invalid enum key: {keyString}, type: {typeof(T)}");
    }

    #endregion

    public void WriteObject(IDsonObjectWriter writer, T inst, Type declaredType, SerializeFeatures features) {
        if (!_value2EnumDic.TryGetValue(inst, out EnumValueInfo<T> valueInfo)) {
            writer.WriteInt(EnumUtil.GetIntValue(inst)); // 可能是default或flags
            return;
        }
        bool isWriteAsString = _isWriteAsString || IsWriteAsString(features, writer);
        if (isWriteAsString) {
            writer.WriteString(valueInfo.name, SerializeFeatures.StringUnquote);
        } else {
            writer.WriteInt(valueInfo.number);
        }
    }

    public T ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        if (reader.CurrentDsonType.IsNumber()) {
            int number = reader.ReadInt();
            if (number == 0) {
                return default;
            }
            if (_number2EnumDic.TryGetValue(number, out EnumValueInfo<T> valueInfo)) {
                return valueInfo.value;
            }
            if (_isFlags) {
                return (T)Enum.ToObject(typeof(T), number);
            }
            throw new DsonCodecException($"invalid enum value: {number}, type: {typeof(T)}");
        }
        if (reader.CurrentDsonType == DsonType.String) {
            string name = reader.ReadString();
            if (_name2EnumDic.TryGetValue(name, out EnumValueInfo<T> valueInfo)) {
                return valueInfo.value;
            }
            if (name.Contains('|')) { // Flags格式
                int number = ParseFlags(name);
                return (T)Enum.ToObject(typeof(T), number);
            }
            throw new DsonCodecException($"invalid enum value: {name}, type: {typeof(T)}");
        }
        if (reader.CurrentDsonType == DsonType.Array) {
            int number = ReadArray(reader);
            if (number == 0) {
                return default;
            }
            return (T)Enum.ToObject(typeof(T), number);
        }
        throw DsonIOException.InvalidDsonType(new List<DsonType>()
        {
            DsonType.Int32, DsonType.String, DsonType.Array
        }, reader.CurrentDsonType);
    }

    private int ReadArray(IDsonObjectReader reader) {
        int value = 0;
        while (reader.ReadDsonType() != DsonType.EndOfObject) {
            string name = reader.ReadString();
            if (_name2EnumDic.TryGetValue(name, out EnumValueInfo<T> valueInfo)) {
                value |= valueInfo.number;
                continue;
            }
            throw new DsonCodecException($"invalid enum value: {name}, type: {typeof(T)}");
        }
        return value;
    }

    private int ParseFlags(string str) {
        int value = 0;
        foreach (string name in ObjectUtil.SplitAndTrim(str, '|')) {
            if (_name2EnumDic.TryGetValue(name, out EnumValueInfo<T> valueInfo)) {
                value |= valueInfo.number;
                continue;
            }
            throw new DsonCodecException($"invalid enum value: {name}, type: {typeof(T)}");
        }
        return value;
    }

    private bool IsIgnoreCase(DeserializeFeatures features, IDsonObjectReader reader) {
        if ((features & DeserializeFeatures.EnumIgnoreCase) != 0) return true;
        TypeMeta typeMeta = reader.ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.decodeFeatures;
            if ((features & DeserializeFeatures.EnumIgnoreCase) != 0) return true;
        }
        features = reader.Options.decodeFeatures;
        return (features & DeserializeFeatures.EnumIgnoreCase) != 0;
    }

    private bool IsWriteAsString(SerializeFeatures features, IDsonObjectWriter writer) {
        if ((features & SerializeFeatures.EnumAsString) != 0) return true;
        if ((features & SerializeFeatures.EnumAsNumber) != 0) return false;
        TypeMeta typeMeta = writer.ContainerTypeMeta;
        if (typeMeta != null) {
            features = typeMeta.encodeFeatures;
            if ((features & SerializeFeatures.EnumAsString) != 0) return true;
            if ((features & SerializeFeatures.EnumAsNumber) != 0) return false;
        }
        features = writer.Options.encodeFeatures;
        return (features & SerializeFeatures.EnumAsString) != 0;
    }
}
}