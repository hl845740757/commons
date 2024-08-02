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
using Wjybxx.Dson.Codec.Codecs;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 用于处理泛型问题
/// </summary>
public abstract class DsonCodecImpl
{
    public abstract Type GetEncoderClass();

    // 解决泛型协变逆变问题 - 不会导致装箱
    public abstract void WriteObject2(IDsonObjectWriter writer, object inst, Type declaredType, ObjectStyle style);

    public abstract object ReadObject2(IDsonObjectReader reader, Type declaredType, object? factory = null);
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class DsonCodecImpl<T> : DsonCodecImpl
{
    private readonly IDsonCodec<T> _codec;
    private readonly bool _autoStart;
    private readonly bool _writeAsArray;

    private readonly AbstractEnumCodec<T>? _enumCodec;

    public DsonCodecImpl(IDsonCodec<T> codec) {
        _codec = codec;
        _autoStart = codec.AutoStartEnd;
        _writeAsArray = codec.IsWriteAsArray;
        _enumCodec = codec as AbstractEnumCodec<T>;
    }

    public override Type GetEncoderClass() {
        return _codec.GetEncoderClass();
    }

    public override void WriteObject2(IDsonObjectWriter writer, object inst, Type declaredType, ObjectStyle style) {
        WriteObject(writer, (T)inst, declaredType, style);
    }

    public override object ReadObject2(IDsonObjectReader reader, Type declaredType, object? factory = null) {
        return ReadObject(reader, declaredType, (Func<T>?)factory); // factory不未null时一定按照declaredType查找codec，所以到这里factory应该为null
    }

    public void WriteObject(IDsonObjectWriter writer, T inst, Type declaredType, ObjectStyle style) {
        if (_autoStart) {
            if (_writeAsArray) {
                writer.WriteStartArray(in inst, declaredType, style);
                _codec.WriteObject(writer, ref inst, declaredType, style);
                writer.WriteEndArray();
            } else {
                writer.WriteStartObject(in inst, declaredType, style);
                _codec.WriteObject(writer, ref inst, declaredType, style);
                writer.WriteEndObject();
            }
        } else {
            _codec.WriteObject(writer, ref inst, declaredType, style);
        }
    }

    public T ReadObject(IDsonObjectReader reader, Type declaredType, Func<T>? factory = null) {
        if (_autoStart) {
            T result;
            if (_writeAsArray) {
                reader.ReadStartArray();
                result = _codec.ReadObject(reader, declaredType, factory);
                reader.ReadEndArray();
            } else {
                reader.ReadStartObject();
                result = _codec.ReadObject(reader, declaredType, factory);
                reader.ReadEndObject();
            }
            return result;
        } else {
            return _codec.ReadObject(reader, declaredType, factory);
        }
    }

    #region 枚举支持

    public bool IsEnumCodec => typeof(T).IsEnum; // 这个测试兼容

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