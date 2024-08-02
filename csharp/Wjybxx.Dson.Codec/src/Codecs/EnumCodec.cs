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
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec.Codecs
{
/// <summary>
/// 抽象类不添加Struct和Enum限制，该接口用于避免拆装箱等问题
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class AbstractEnumCodec<T>
{
    public abstract bool ForNumber(int number, out T result);

    public abstract bool ForName(string name, out T result);

    public abstract int GetNumber(T value);

    public abstract string GetName(T value);
}

/// <summary>
/// 枚举类的Codec
/// 注意：默认不支持序列化未在枚举中定义的枚举值 —— 其它特殊情况，建议直接使用int值。
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class EnumCodec<T> : AbstractEnumCodec<T>, IDsonCodec<T> where T : struct, Enum
{
    private static readonly Dictionary<T, EnumValueInfo> _value2ConstDic = new Dictionary<T, EnumValueInfo>();
    private static readonly Dictionary<int, EnumValueInfo> _number2ConstDic = new Dictionary<int, EnumValueInfo>();
    private static readonly Dictionary<string, EnumValueInfo> _name2ConstDic = new Dictionary<string, EnumValueInfo>();

    static EnumCodec() {
#if UNITY_EDITOR
        Array values = Enum.GetValues(typeof(T));
        string[] names = Enum.GetNames(typeof(T));
        for (int i = 0; i < values.Length; i++)
        {
            T value = (T)values.GetValue(i);
            int number = value.GetHashCode(); // 奇巧淫技：int32/uint32/byte/sybte的hashcode是自身，可避免装箱
            string name = names[i];

            EnumValueInfo enumValueInfo = new EnumValueInfo(value, number, name);
            _value2ConstDic[value] = enumValueInfo;
            _number2ConstDic[number] = enumValueInfo;
            _name2ConstDic[name] = enumValueInfo;
        }
#else
        T[] values = Enum.GetValues<T>();
        string[] names = Enum.GetNames<T>();
        for (int i = 0; i < values.Length; i++) {
            T value = values[i];
            int number = value.GetHashCode(); // 奇巧淫技：int32/uint32/byte/sybte的hashcode是自身，可避免装箱
            string name = names[i];

            EnumValueInfo enumValueInfo = new EnumValueInfo(value, number, name);
            _value2ConstDic[value] = enumValueInfo;
            _number2ConstDic[number] = enumValueInfo;
            _name2ConstDic[name] = enumValueInfo;
        }
#endif
    }

    #region 避免装箱

    public override bool ForNumber(int number, out T result) {
        if (_number2ConstDic.TryGetValue(number, out EnumValueInfo valueInfo)) {
            result = valueInfo.value;
            return true;
        }
        result = default;
        return false;
    }

    public override bool ForName(string name, out T result) {
        if (_name2ConstDic.TryGetValue(name, out EnumValueInfo valueInfo)) {
            result = valueInfo.value;
            return true;
        }
        result = default;
        return false;
    }

    public override int GetNumber(T value) {
        if (!_value2ConstDic.TryGetValue(value, out EnumValueInfo valueInfo)) {
            throw new DsonCodecException($"invalid enum value: {value}, type: {typeof(T)}");
        }
        return valueInfo.number;
    }

    public override string GetName(T value) {
        if (!_value2ConstDic.TryGetValue(value, out EnumValueInfo valueInfo)) {
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
        if (!_value2ConstDic.TryGetValue(inst, out EnumValueInfo valueInfo)) {
            throw new DsonCodecException($"invalid enum value: {inst}, type: {typeof(T)}");
        }
        if (writer.Options.writeEnumAsString) {
            writer.WriteString(null, valueInfo.name, StringStyle.Unquote);
        } else {
            writer.WriteInt(null, valueInfo.number);
        }
    }

    public T ReadObject(IDsonObjectReader reader, Type declaredType, Func<T>? factory = null) {
        if (reader.Options.writeEnumAsString) {
            string name = reader.ReadString(reader.CurrentName);
            if (_name2ConstDic.TryGetValue(name, out EnumValueInfo valueInfo)) {
                return valueInfo.value;
            }
        } else {
            int number = reader.ReadInt(reader.CurrentName);
            if (_number2ConstDic.TryGetValue(number, out EnumValueInfo valueInfo)) {
                return valueInfo.value;
            }
            // 不做number转enum支持 -- ToObject会装箱，另外还存在跨语言兼容性问题
        }
        return default;
    }

    private readonly struct EnumValueInfo
    {
        internal readonly T value;
        internal readonly int number;
        internal readonly string name;

        public EnumValueInfo(T value, int number, string name) {
            this.value = value;
            this.number = number;
            this.name = name;
        }
    }
}
}