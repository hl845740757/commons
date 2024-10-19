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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec.Codecs;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 为支持数组和泛型，我们根据原型类型动态创建Codec。
/// 查找Codec时，总是优先查找用户的CodecRegistry，以允许用户对特定泛型和数组进行定制编解码。
/// </summary>
public sealed class DynamicCodecRegistry : IDsonCodecRegistry
{
    /** 用户的原始的类型Codec */
    private readonly DsonCodecConfig _config;
    /** 类型转换器 */
    private readonly List<IDsonCodecCaster> _casters;

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存  */
    private readonly ConcurrentDictionary<Type, DsonCodecImpl> encoderDic = new ConcurrentDictionary<Type, DsonCodecImpl>();
    private readonly ConcurrentDictionary<Type, DsonCodecImpl> decoderDic = new ConcurrentDictionary<Type, DsonCodecImpl>();

    /// <summary>
    /// 
    /// </summary>
    public DynamicCodecRegistry(DsonCodecConfig config) {
        config = config.ToImmutable();
        this._config = config;
        this._casters = new List<IDsonCodecCaster>(_config.GetCasters()); // 拷贝，避免不必要的虚方法调用

        // 构建DsonImpl实例
        foreach (KeyValuePair<Type, IDsonCodec> pair in config.GetEncoderDic()) {
            encoderDic[pair.Key] = DsonCodecImpl.CreateInstance(pair.Value);
        }
        foreach (KeyValuePair<Type, IDsonCodec> pair in config.GetDecoderDic()) {
            decoderDic[pair.Key] = DsonCodecImpl.CreateInstance(pair.Value);
        }
    }

    /// <summary>
    /// 查找可用的编码器，对于泛型和数组会动态生成Codec
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public DsonCodecImpl? GetEncoder(Type type) {
        if (encoderDic.TryGetValue(type, out DsonCodecImpl? codecImpl)) {
            return codecImpl;
        }

        // 动态生成
        if (type.IsEnum) {
            codecImpl = MakeEnumCodec(type);
        } else if (type.IsArray) {
            codecImpl = MakeArrayCodec(type);
        } else if (type.IsGenericType) {
            GenericCodecInfo? genericCodecInfo = _config.GetGenericEncoderInfo(type.GetGenericTypeDefinition());
            if (genericCodecInfo != null) {
                codecImpl = MakeGenericCodec(type, genericCodecInfo.Value);
            } else {
                // 尝试转换为超类编码，写入超类的TypeInfo
                Type superType = CastEncoderType(type);
                if (superType != null) {
                    codecImpl = GetEncoder(superType);
                }
            }
        } else {
            // 尝试转换为超类编码，写入超类的TypeInfo
            Type superType = CastEncoderType(type);
            if (superType != null) {
                codecImpl = GetEncoder(type);
            }
        }
        if (codecImpl != null) {
            encoderDic.TryAdd(type, codecImpl);
            if (type == codecImpl.GetEncoderType()) {
                decoderDic.TryAdd(type, codecImpl);
            }
        }
        return codecImpl;
    }

    /// <summary>
    /// 查找可用的解码器，对于泛型和数组会动态生成Codec
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public DsonCodecImpl? GetDecoder(Type type) {
        if (decoderDic.TryGetValue(type, out DsonCodecImpl? codecImpl)) {
            return codecImpl;
        }

        // 动态生成
        if (type.IsEnum) {
            codecImpl = MakeEnumCodec(type);
        } else if (type.IsArray) {
            codecImpl = MakeArrayCodec(type);
        } else if (type.IsGenericType) {
            GenericCodecInfo? genericCodecInfo = _config.GetGenericDecoderInfo(type.GetGenericTypeDefinition());
            if (genericCodecInfo != null) {
                codecImpl = MakeGenericCodec(type, genericCodecInfo.Value);
            } else {
                // 尝试转换为子类解码，解码不涉及到写入TypeInfo
                Type subType = CastDecoderType(type);
                if (subType != null) {
                    codecImpl = GetDecoder(subType);
                }
            }
        } else {
            // 尝试转换为子类解码，解码不涉及到写入TypeInfo
            Type subType = CastDecoderType(type);
            if (subType != null) {
                codecImpl = GetDecoder(type);
            }
        }
        if (codecImpl != null) {
            decoderDic.TryAdd(type, codecImpl);
            if (type == codecImpl.GetEncoderType()) {
                encoderDic.TryAdd(type, codecImpl);
            }
        }
        return codecImpl;
    }

    #region make

    private DsonCodecImpl MakeArrayCodec(Type type) {
        Type genericArrayCodecType = typeof(ArrayCodec<>).MakeGenericType(type.GetElementType()!);
        IDsonCodec codec = (IDsonCodec)Activator.CreateInstance(genericArrayCodecType)!;
        return DsonCodecImpl.CreateInstance(codec);
    }

    private DsonCodecImpl MakeEnumCodec(Type type) {
        Type enumCodecType = typeof(EnumCodec<>).MakeGenericType(type);
        IDsonCodec codec = (IDsonCodec)Activator.CreateInstance(enumCodecType)!;
        return DsonCodecImpl.CreateInstance(codec);
    }

    private DsonCodecImpl MakeGenericCodec(Type type, GenericCodecInfo genericCodecInfo) {
        Debug.Assert(type.GetGenericTypeDefinition() == genericCodecInfo.typeInfo);
        Type genericCodecType = genericCodecInfo.codecType.MakeGenericType(type.GenericTypeArguments);

        IDsonCodec codec;
        // 先查找包含Type和Func的构造函数 -- factory的泛型参数是传递给DsonCodec泛型参数的类型
        Type interfaceType = genericCodecType.GetInterface(typeof(IDsonCodec<>).Name)!;
        Type factoryType = typeof(Func<>).MakeGenericType(interfaceType.GenericTypeArguments[0]);
        ConstructorInfo constructorInfo = genericCodecType.GetConstructor(new[] { typeof(Type), factoryType });
        if (constructorInfo != null) {
            // 如果用户指定了Factory，则获取具体实例，否则传入null
            if (genericCodecInfo.factoryDeclaringType != null) {
                Type factoryDeclaringType = genericCodecInfo.factoryDeclaringType.MakeGenericType(type.GenericTypeArguments);
                FieldInfo fieldInfo = factoryDeclaringType.GetField(genericCodecInfo.factoryField!, GenericCodecInfo.FactoryBindFlags)!;
                object factory = fieldInfo.GetValue(null); // Func<T>
                codec = (IDsonCodec)constructorInfo.Invoke(new object[] { type, factory });
            } else {
                codec = (IDsonCodec)constructorInfo.Invoke(new object[] { type, null });
            }
        } else {
            // 再查找包含Type的构造函数
            constructorInfo = genericCodecType.GetConstructor(new[] { typeof(Type) });
            if (constructorInfo != null) {
                codec = (IDsonCodec)constructorInfo.Invoke(new object[] { type });
            } else {
                codec = (IDsonCodec)Activator.CreateInstance(genericCodecType); // 无参构造函数
            }
        }
        if (codec == null) {
            throw new DsonCodecException("bad generic codec: " + genericCodecType);
        }
        return DsonCodecImpl.CreateInstance(codec);
    }

    private Type? CastEncoderType(Type type) {
        // caster逆向迭代，越靠近用户优先级越高，才能保证一定能解决冲突
        for (int index = _casters.Count - 1; index >= 0; index--) {
            IDsonCodecCaster caster = _casters[index];
            Type superType = caster.CastEncoderType(type);
            if (superType == null) continue;
            return superType == type ? null : superType; // fix用户返回当前类
        }
        // 这段保底代码写在这里最为合适，放在用户的Config里还需要考虑冲突问题...
        Type castType = type.GetInterface(typeof(IList<>).Name);
        if (castType != null) return castType;

        castType = type.GetInterface(typeof(ISet<>).Name);
        if (castType != null) return castType;

        castType = type.GetInterface(typeof(IGenericSet<>).Name);
        if (castType != null) return castType;

        castType = type.GetInterface(typeof(ICollection<>).Name);
        if (castType != null) return castType;

        castType = type.GetInterface(typeof(IDictionary<,>).Name);
        if (castType != null) return castType;

        // readonly系列集合...
        castType = type.GetInterface(typeof(IEnumerable<>).Name);
        return castType;
    }

    private Type? CastDecoderType(Type type) {
        // caster逆向迭代，越靠近用户优先级越高，才能保证一定能解决冲突
        for (int index = _casters.Count - 1; index >= 0; index--) {
            IDsonCodecCaster caster = _casters[index];
            Type subType = caster.CastDecoderType(type);
            if (subType == null) continue;
            return subType == type ? null : subType; // fix用户返回当前类
        }
        // readonly系列集合...
        if (type.IsGenericType) {
            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(IReadOnlyList<>)
                || genericTypeDefinition == typeof(IReadOnlyCollection<>)
                || genericTypeDefinition == typeof(IEnumerable<>)) {
                return typeof(List<>).MakeGenericType(type.GenericTypeArguments);
            }
            if (genericTypeDefinition == typeof(IReadOnlySet<>)) {
                return typeof(HashSet<>).MakeGenericType(type.GenericTypeArguments);
            }
            if (genericTypeDefinition == typeof(IReadOnlyDictionary<,>)) {
                return typeof(Dictionary<,>).MakeGenericType(type.GenericTypeArguments);
            }
        }
        return null;
    }

    #endregion
}
}