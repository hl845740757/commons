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
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 抽象类不添加Struct和Enum限制，该接口用于避免拆装箱等问题
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IEnumCodec<T> : IDsonCodec<T>
{
    bool ForNumber(int number, out T result);

    bool ForName(string name, out T result);

    int GetNumber(T value);

    string GetName(T value);
}

/// <summary>
/// 单个枚举值信息
/// </summary>
/// <typeparam name="T"></typeparam>
public readonly struct EnumValueInfo<T>
{
    public readonly T value;
    public readonly int number;
    public readonly string name;

    public EnumValueInfo(T value, int number, string name) {
        this.value = value;
        this.number = number;
        this.name = name ?? throw new ArgumentNullException(nameof(name));
    }
}

/// <summary>
/// 默认枚举类的Codec
/// 注意：默认不支持序列化未在枚举中定义的枚举值 —— 其它特殊情况，建议直接使用int值。
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class EnumCodec<T> : IEnumCodec<T> where T : struct, Enum
{
    private readonly Dictionary<T, EnumValueInfo<T>> _value2EnumDic;
    private readonly Dictionary<int, EnumValueInfo<T>> _number2EnumDic;
    private readonly Dictionary<string, EnumValueInfo<T>> _name2EnumDic;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="valueInfos">枚举值信息，允许自定义枚举序列化数据</param>
    public EnumCodec(List<EnumValueInfo<T>> valueInfos) {
        _value2EnumDic = new Dictionary<T, EnumValueInfo<T>>(valueInfos.Count);
        _number2EnumDic = new Dictionary<int, EnumValueInfo<T>>(valueInfos.Count);
        _name2EnumDic = new Dictionary<string, EnumValueInfo<T>>(valueInfos.Count);

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

        FieldInfo[] enumFields = typeof(T).GetFields();
        for (int idx = 0; idx < values.Length; idx++) {
            T value = values[idx];
            int number = EnumUtil.GetIntValue(value);

            // 可通过注解指定DsonName -- 第一个元素是占位符，查询枚举关联的Field时需要+1
            DsonPropertyAttribute attribute = enumFields[idx + 1].GetCustomAttribute<DsonPropertyAttribute>();
            string name;
            if (attribute != null && !string.IsNullOrWhiteSpace(attribute.Name)) {
                name = attribute.Name;
            } else {
                name = names[idx];
            }

            EnumValueInfo<T> valueInfo = new EnumValueInfo<T>(value, number, name);
            _value2EnumDic[valueInfo.value] = valueInfo;
            _number2EnumDic[valueInfo.number] = valueInfo;
            _name2EnumDic[valueInfo.name] = valueInfo;
        }
    }

    #region 避免装箱

    public bool ForNumber(int number, out T result) {
        if (_number2EnumDic.TryGetValue(number, out EnumValueInfo<T> valueInfo)) {
            result = valueInfo.value;
            return true;
        }
        result = default;
        return false;
    }

    public bool ForName(string name, out T result) {
        if (_name2EnumDic.TryGetValue(name, out EnumValueInfo<T> valueInfo)) {
            result = valueInfo.value;
            return true;
        }
        result = default;
        return false;
    }

    public int GetNumber(T value) {
        if (!_value2EnumDic.TryGetValue(value, out EnumValueInfo<T> valueInfo)) {
            throw new DsonCodecException($"invalid enum value: {value}, type: {typeof(T)}");
        }
        return valueInfo.number;
    }

    public string GetName(T value) {
        if (!_value2EnumDic.TryGetValue(value, out EnumValueInfo<T> valueInfo)) {
            throw new DsonCodecException($"invalid enum value: {value}, type: {typeof(T)}");
        }
        return valueInfo.name;
    }

    #endregion

    /// <summary>
    /// false 可以将枚举简单写为整数
    /// </summary>
    public bool AutoStartEnd => false;

    public void WriteObject(IDsonObjectWriter writer, ref T inst, Type declaredType, ObjectStyle style) {
        if (!_value2EnumDic.TryGetValue(inst, out EnumValueInfo<T> valueInfo)) {
            throw new DsonCodecException($"invalid enum value: {inst}, type: {typeof(T)}");
        }
        if (writer.Options.writeEnumAsString) {
            writer.WriteString(null, valueInfo.name, StringStyle.Unquote);
        } else {
            writer.WriteInt(null, valueInfo.number);
        }
    }

    public T ReadObject(IDsonObjectReader reader, Func<T>? factory = null) {
        if (reader.Options.writeEnumAsString) {
            string name = reader.ReadString(null);
            if (_name2EnumDic.TryGetValue(name, out EnumValueInfo<T> valueInfo)) {
                return valueInfo.value;
            }
            throw new DsonCodecException($"invalid enum value: {name}, type: {typeof(T)}");
        } else {
            int number = reader.ReadInt(null);
            if (_number2EnumDic.TryGetValue(number, out EnumValueInfo<T> valueInfo)) {
                return valueInfo.value;
            }
            // 不做number转enum支持 -- 存在跨语言兼容性问题
            throw new DsonCodecException($"invalid enum value: {number}, type: {typeof(T)}");
        }
    }
}
}