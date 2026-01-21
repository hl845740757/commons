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

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 单个枚举值信息
/// </summary>
/// <typeparam name="T"></typeparam>
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
        this.numberString = string.Intern(number.ToString());
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
    private readonly Dictionary<string, EnumValueInfo<T>> _name2EnumDic;
    private readonly bool _isFlags;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="valueInfos">枚举值信息，允许自定义枚举序列化数据</param>
    /// <param name="isFlags">是否是Flags类型</param>
    public EnumCodec(List<EnumValueInfo<T>> valueInfos, bool? isFlags = null) {
        _value2EnumDic = new Dictionary<T, EnumValueInfo<T>>(valueInfos.Count);
        _number2EnumDic = new Dictionary<int, EnumValueInfo<T>>(valueInfos.Count);
        _name2EnumDic = new Dictionary<string, EnumValueInfo<T>>(valueInfos.Count, StringComparer.OrdinalIgnoreCase);
        _isFlags = isFlags ?? typeof(T).IsDefined(typeof(FlagsAttribute));

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
        _isFlags = typeof(T).IsDefined(typeof(FlagsAttribute));

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
        if (_value2EnumDic.TryGetValue(value, out EnumValueInfo<T> valueInfo)) {
            return (features & SerializeFeatures.EnumKeyAsString) != 0
                ? valueInfo.name
                : valueInfo.numberString;
        }
        throw new DsonCodecException($"invalid enum key: {value}, type: {typeof(T)}");
    }

    public T DecodeKey(string keyString) {
        // 枚举Key必须存在对应的名字
        if (int.TryParse(keyString, out int number)) {
            if (_number2EnumDic.TryGetValue(number, out EnumValueInfo<T> valueInfo)) {
                return valueInfo.value;
            }
        } else {
            if (_name2EnumDic.TryGetValue(keyString, out EnumValueInfo<T> valueInfo)) {
                return valueInfo.value;
            }
            if (Enum.TryParse(keyString, true, out T value)) {
                return value;
            }
        }
        throw new DsonCodecException($"invalid enum key: {keyString}, type: {typeof(T)}");
    }

    #endregion

    /// <summary>
    /// false 可以将枚举简单写为整数
    /// </summary>
    public void WriteObject(IDsonObjectWriter writer, T inst, Type declaredType, SerializeFeatures features) {
        if (!_value2EnumDic.TryGetValue(inst, out EnumValueInfo<T> valueInfo)) {
            if (_isFlags) {
                writer.WriteInt(EnumUtil.GetIntValue(inst));
                return;
            }
            throw new DsonCodecException($"invalid enum value: {inst}, type: {typeof(T)}");
        }
        bool isWriteAsString = IsWriteAsString(features, writer);
        if (isWriteAsString) {
            writer.WriteString(valueInfo.name, SerializeFeatures.StringUnquote);
        } else {
            writer.WriteInt(valueInfo.number);
        }
    }

    public T ReadObject(IDsonObjectReader reader, Type declaredType, DeserializeFeatures features, Func<object>? factory = null) {
        if (reader.CurrentDsonType == DsonType.String) {
            string name = reader.ReadString();
            if (_name2EnumDic.TryGetValue(name, out EnumValueInfo<T> valueInfo)) {
                return valueInfo.value;
            }
            if (name.Contains('|')) { // Flags格式
                int number = ParseFlags(name);
                return (T)Enum.ToObject(typeof(T), number);
            }
            if (Enum.TryParse(name, true, out T value)) {
                return value;
            }
            throw new DsonCodecException($"invalid enum value: {name}, type: {typeof(T)}");
        } else {
            int number = reader.ReadInt();
            if (_number2EnumDic.TryGetValue(number, out EnumValueInfo<T> valueInfo)) {
                return valueInfo.value;
            }
            if (_isFlags) { // Flags
                return (T)Enum.ToObject(typeof(T), number);
            }
            // 不做number转enum支持 -- 存在跨语言兼容性问题
            throw new DsonCodecException($"invalid enum value: {number}, type: {typeof(T)}");
        }
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

    private int ParseFlags(string str) {
        int value = 0;
        foreach (string e in ObjectUtil.SplitAndTrim(str, '|')) {
            // 枚举Flags更常见的应当是name
            if (_name2EnumDic.TryGetValue(e, out EnumValueInfo<T> valueInfo)) {
                value |= valueInfo.number;
                continue;
            }
            if (int.TryParse(e, out int number)) {
                value |= number;
                continue;
            }
            throw new DsonCodecException($"invalid enum value: {e}, type: {typeof(T)}");
        }
        return value;
    }
}
}