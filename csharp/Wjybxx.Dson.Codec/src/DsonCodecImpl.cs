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
using System.Runtime.CompilerServices;
using Wjybxx.Dson.Codec.Codecs;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 用于处理泛型问题
/// </summary>
public abstract class DsonCodecImpl
{
    /** 是否禁止序列化引用 */
    internal abstract bool DisableSerializeReference { get; }
    /** 是否是可内联的Codec -- 用于集合类型性能优化 */
    internal abstract bool IsInlinableCodec { get; }

    public abstract Type GetEncoderType();

    // 解决泛型协变逆变问题 - 不会导致装箱，但会多一次cast
    public abstract void WriteObject2(IDsonObjectWriter writer, object inst, Type declaredType, SerializeFeatures features);

    public abstract object ReadObject2(IDsonObjectReader reader, Type declaredType, Func<object>? factory);

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
    private readonly INullableCodec<T>? _nullableCodec;
    private readonly IKeyCodec<T>? _keyCodec;
    private readonly bool _disableSerilizeReference;
    private readonly bool _inlinableCodec;

    internal DsonCodecImpl(IDsonCodec<T> codec) {
        _codec = codec;
        _encoderType = codec.GetEncoderType();
        _nullableCodec = codec as INullableCodec<T>;
        _keyCodec = codec as IKeyCodec<T>;
        //
        _disableSerilizeReference = _encoderType.IsValueType
                                    || _encoderType == typeof(string)
                                    || _encoderType.IsArray
                                    || DsonConverterUtils.IsCollection(_encoderType)
                                    || DsonConverterUtils.IsDictionary(_encoderType);
        // codec需要能正确处理null
        _inlinableCodec = _encoderType.IsValueType
                          || _encoderType == typeof(string);
    }

    internal override bool DisableSerializeReference => _disableSerilizeReference;

    internal override bool IsInlinableCodec => _inlinableCodec;

    public override Type GetEncoderType() {
        return _encoderType;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void WriteObject2(IDsonObjectWriter writer, object inst, Type declaredType, SerializeFeatures features) {
        WriteObject(writer, (T)inst, declaredType, features);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override object ReadObject2(IDsonObjectReader reader, Type declaredType, Func<object>? factory) {
        return ReadObject(reader, declaredType, factory);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="inst">要编码的对象</param>
    /// <param name="declaredType">对象的声明类型</param>
    /// <param name="features">特征值</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteObject(IDsonObjectWriter writer, in T inst, Type declaredType, SerializeFeatures features) {
        _codec.WriteObject(writer, inst, declaredType, features);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="reader">reader</param>
    /// <param name="declaredType"></param>
    /// <param name="factory">实例工厂</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T ReadObject(IDsonObjectReader reader, Type declaredType, Func<object>? factory) {
        return _codec.ReadObject(reader, declaredType, factory);
    }

    #region nullabel支持

    public bool IsNullableCodec => _nullableCodec != null;

    public bool HasValue(in T value) {
        if (_nullableCodec != null) {
            return _nullableCodec.HasValue(in value);
        }
        throw new DsonCodecException("unexpected HasValue method call");
    }

    #endregion

    #region 字典特殊支持

    /// <summary>
    /// 是否是字典的Key编解码器
    /// </summary>
    public bool IsKeyCodec => _keyCodec != null;

    public string EncodeKey(T value, SerializeFeatures features) {
        if (_keyCodec != null) {
            return _keyCodec.EncodeKey(value, features);
        }
        throw new DsonCodecException("unexpected EncodeKey method call");
    }

    public T DecodeKey(string keyString) {
        if (_keyCodec != null) {
            return _keyCodec.DecodeKey(keyString);
        }
        throw new DsonCodecException("unexpected DecodeKey method call");
    }

    #endregion
}
}