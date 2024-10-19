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
using System.Reflection;
using Wjybxx.Dson.Codec.Codecs;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 用于处理泛型问题
/// </summary>
public abstract class DsonCodecImpl
{
    public abstract IDsonCodec GetCodec();

    public abstract Type GetEncoderType();

    // 解决泛型协变逆变问题 - 不会导致装箱，但会多一次cast
    public abstract void WriteObject2(IDsonObjectWriter writer, object inst, Type declaredType, ObjectStyle style);

    public abstract object ReadObject2(IDsonObjectReader reader, object? factory);

    /** 创建Impl实例 */
    internal static DsonCodecImpl CreateInstance(IDsonCodec codec) {
        // 存在泛型协变和逆变问题，因此不能直接使用GetEncoderClass创建泛型，需要找到IDsonCodec<>的泛型参数
        Type genericCodecType = codec.GetType().GetInterface(typeof(IDsonCodec<>).Name)!;
        Type codecImplGenericType = typeof(DsonCodecImpl<>).MakeGenericType(genericCodecType.GenericTypeArguments);
        ConstructorInfo constructor = codecImplGenericType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];
        object dsonCodecImpl = constructor.Invoke(new object[] { codec });
        return (DsonCodecImpl)dsonCodecImpl;
    }
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T">实例类型，可能是EncoderType的超类</typeparam>
public sealed class DsonCodecImpl<T> : DsonCodecImpl
{
    private readonly IDsonCodec<T> _codec;
    private readonly Type _encoderType;
    private readonly bool _autoStart;
    private readonly bool _writeAsArray;
    private readonly IEnumCodec<T>? _enumCodec;

    internal DsonCodecImpl(IDsonCodec<T> codec) {
        _codec = codec;
        _encoderType = codec.GetEncoderType();
        _autoStart = codec.AutoStartEnd;
        _writeAsArray = codec.IsWriteAsArray;
        _enumCodec = codec as IEnumCodec<T>;
    }

    public override IDsonCodec GetCodec() {
        return _codec;
    }

    public override Type GetEncoderType() {
        return _encoderType;
    }

    public override void WriteObject2(IDsonObjectWriter writer, object inst, Type declaredType, ObjectStyle style) {
        WriteObject(writer, (T)inst, declaredType, style);
    }

    public override object ReadObject2(IDsonObjectReader reader, object? factory) {
        return ReadObject(reader, factory as Func<T>); // factory的cast是否可能失败？用as稳妥
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="inst">要编码的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="style">编码风格</param>
    public void WriteObject(IDsonObjectWriter writer, T inst, Type declaredType, ObjectStyle style) {
        if (_autoStart) {
            if (_writeAsArray) {
                writer.WriteStartArray(style);
                writer.WriteTypeInfo(_encoderType, declaredType);
                _codec.WriteObject(writer, ref inst, declaredType, style);
                writer.WriteEndArray();
            } else {
                writer.WriteStartObject(style);
                writer.WriteTypeInfo(_encoderType, declaredType);
                _codec.WriteObject(writer, ref inst, declaredType, style);
                writer.WriteEndObject();
            }
        } else {
            _codec.WriteObject(writer, ref inst, declaredType, style);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reader">reader</param>
    /// <param name="factory">实例工厂</param>
    /// <returns></returns>
    public T ReadObject(IDsonObjectReader reader, Func<T>? factory = null) {
        if (_autoStart) {
            T result;
            if (_writeAsArray) {
                reader.ReadStartArray();
                result = _codec.ReadObject(reader, factory);
                reader.ReadEndArray();
            } else {
                reader.ReadStartObject();
                result = _codec.ReadObject(reader, factory);
                reader.ReadEndObject();
            }
            return result;
        } else {
            return _codec.ReadObject(reader, factory);
        }
    }

    #region 枚举支持

    public bool IsEnumCodec => _enumCodec != null;

    public bool ForNumber(int number, out T result) {
        if (_enumCodec != null) {
            return _enumCodec.ForNumber(number, out result);
        }
        throw new DsonCodecException("unexpected ForNumber method call");
    }

    public bool ForName(string name, out T result) {
        if (_enumCodec != null) {
            return _enumCodec.ForName(name, out result);
        }
        throw new DsonCodecException("unexpected ForName method call");
    }

    public int GetNumber(T value) {
        if (_enumCodec != null) {
            return _enumCodec.GetNumber(value);
        }
        throw new DsonCodecException("unexpected GetNumber method call");
    }

    public string GetName(T value) {
        if (_enumCodec != null) {
            return _enumCodec.GetName(value);
        }
        throw new DsonCodecException("unexpected GetName method call");
    }

    #endregion
}
}