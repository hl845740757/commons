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
using System.Reflection;
using Wjybxx.Commons;
using Wjybxx.Dson.Codec.Codecs;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 为支持数组和泛型，我们根据原型类型动态创建Codec。
/// 查找Codec时，总是优先查找用户的CodecRegistry，以允许用户对特定泛型和数组进行定制编解码。
/// </summary>
public sealed class DynamicDsonCodecRegistry : IDsonCodecRegistry
{
    /** 用户的原始的类型Codec */
    private readonly IDsonCodecRegistry _basicRegistry;
    /** 类型转换器 */
    private readonly List<IDsonCodecCaster> _casters;
    /** 泛型类对应的Codec类型 */
    private readonly GenericCodecConfig _genericCodecConfig;

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存  */
    private readonly ConcurrentDictionary<Type, DsonCodecImpl> encoderDic = new ConcurrentDictionary<Type, DsonCodecImpl>();
    private readonly ConcurrentDictionary<Type, DsonCodecImpl> decoderDic = new ConcurrentDictionary<Type, DsonCodecImpl>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="basicRegistry">用户的基础类型Codec</param>
    /// <param name="casters">类型转换器</param>
    /// <param name="genericCodecConfigs">泛型Codec配置类</param>
    public DynamicDsonCodecRegistry(IDsonCodecRegistry basicRegistry, List<IDsonCodecCaster> casters, List<GenericCodecConfig> genericCodecConfigs) {
        _basicRegistry = basicRegistry ?? throw new ArgumentNullException(nameof(basicRegistry));
        _casters = new List<IDsonCodecCaster>(casters);

        // 先初始化为默认配置，然后由用户的配置进行覆盖 -- 不转不可变对象，性能更好
        _genericCodecConfig = GenericCodecConfig.NewDefaultConfig();
        foreach (GenericCodecConfig genericCodecConfig in genericCodecConfigs) {
            _genericCodecConfig.AddCodecs(genericCodecConfig);
        }
        // 初始化特化List
        AddCodec(new DsonCodecImpl<List<int>>(new MoreCollectionCodecs.IntListCodec()));
        AddCodec(new DsonCodecImpl<List<long>>(new MoreCollectionCodecs.LongListCodec()));
        AddCodec(new DsonCodecImpl<List<float>>(new MoreCollectionCodecs.FloatListCodec()));
        AddCodec(new DsonCodecImpl<List<double>>(new MoreCollectionCodecs.DoubleListCodec()));
        AddCodec(new DsonCodecImpl<List<bool>>(new MoreCollectionCodecs.BoolListCodec()));
        AddCodec(new DsonCodecImpl<List<string>>(new MoreCollectionCodecs.StringListCodec()));
        AddCodec(new DsonCodecImpl<List<uint>>(new MoreCollectionCodecs.UIntListCodec()));
        AddCodec(new DsonCodecImpl<List<ulong>>(new MoreCollectionCodecs.ULongListCodec()));
        AddCodec(new DsonCodecImpl<List<object>>(new MoreCollectionCodecs.ObjectListCodec()));
    }

    /// <summary>
    /// 预添加Codec
    /// </summary>
    /// <param name="codecImpl"></param>
    public void AddCodec(DsonCodecImpl codecImpl) {
        encoderDic.TryAdd(codecImpl.GetEncoderType(), codecImpl);
        decoderDic.TryAdd(codecImpl.GetEncoderType(), codecImpl);
    }

    /// <summary>
    /// 添加编码器
    /// </summary>
    /// <param name="codecImpl">编码器</param>
    public void AddEncoder(DsonCodecImpl codecImpl) {
        encoderDic.TryAdd(codecImpl.GetEncoderType(), codecImpl);
    }

    /// <summary>
    /// 添加编码器
    /// </summary>
    /// <param name="type">绑定的类型，需要是Codec绑定类型的子类</param>
    /// <param name="codecImpl">编码器</param>
    /// <exception cref="ArgumentException"></exception>
    public void AddEncoder(Type type, DsonCodecImpl codecImpl) {
        if (!codecImpl.GetEncoderType().IsAssignableFrom(type)) {
            throw new ArgumentException($"codecType: {codecImpl.GetEncoderType()}, argType: {type}");
        }
        encoderDic.TryAdd(type, codecImpl);
    }

    /// <summary>
    /// 添加解码器
    /// </summary>
    /// <param name="codecImpl"></param>
    public void AddDecoder(DsonCodecImpl codecImpl) {
        decoderDic.TryAdd(codecImpl.GetEncoderType(), codecImpl);
    }

    /// <summary>
    /// 查找可用的编码器，对于泛型和数组会动态生成Codec
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public DsonCodecImpl? GetEncoder(Type type) {
        // 优先查找用户的Codec，以允许用户定制优化
        DsonCodecImpl codecImpl = _basicRegistry.GetEncoder(type);
        if (codecImpl != null) return codecImpl;

        // 查缓存
        if (encoderDic.TryGetValue(type, out codecImpl)) {
            return codecImpl;
        }

        // 动态生成
        if (type.IsEnum) {
            codecImpl = MakeEnumCodec(type);
        } else if (type.IsArray) {
            codecImpl = MakeArrayCodec(type);
        } else if (type.IsGenericType) {
            codecImpl = MakeGenericCodec(type, true);
        } else {
            // 尝试转换为超类
            Type superType = CastEncoderType(type);
            if (superType != null) {
                codecImpl = GetEncoder(type);
            }
        }
        if (codecImpl != null) {
            encoderDic.TryAdd(type, codecImpl);
            // 可能是超类Encoder
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
        // 优先查找用户的Codec，以允许用户定制优化
        DsonCodecImpl codecImpl = _basicRegistry.GetDecoder(type);
        if (codecImpl != null) return codecImpl;

        // 查缓存
        if (decoderDic.TryGetValue(type, out codecImpl)) {
            return codecImpl;
        }

        // 动态生成
        if (type.IsEnum) {
            codecImpl = MakeEnumCodec(type);
        } else if (type.IsArray) {
            codecImpl = MakeArrayCodec(type);
        } else if (type.IsGenericType) {
            codecImpl = MakeGenericCodec(type, false);
        } else {
            // 尝试转换为超类或子类
            Type superType = CastEncoderType(type);
            if (superType != null) {
                codecImpl = GetDecoder(type);
            }
        }
        if (codecImpl != null) {
            // 可以解码的一定可以编码
            decoderDic.TryAdd(type, codecImpl);
            encoderDic.TryAdd(type, codecImpl);
        }
        return codecImpl;
    }

    #region make

    private DsonCodecImpl MakeArrayCodec(Type type) {
        Type genericArrayCodecType = typeof(ArrayCodec<>).MakeGenericType(type.GetElementType()!);
        IDsonCodec codec = (IDsonCodec)Activator.CreateInstance(genericArrayCodecType)!;
        return MakeCodecImpl(codec);
    }

    private DsonCodecImpl MakeEnumCodec(Type type) {
        Type enumCodecType = typeof(EnumCodec<>).MakeGenericType(type);
        IDsonCodec codec = (IDsonCodec)Activator.CreateInstance(enumCodecType)!;
        return MakeCodecImpl(codec);
    }

    private DsonCodecImpl? MakeGenericCodec(Type type, bool encoder) {
        Type genericCodecTypeDefine = encoder ? FindGenericEncoder(type) : FindGenericDecoder(type);
        if (genericCodecTypeDefine == null) {
            return null; // 未配置泛型对应的Codec类型
        }
        Type genericCodecType = genericCodecTypeDefine.MakeGenericType(type.GenericTypeArguments);
        IDsonCodec codec = (IDsonCodec)Activator.CreateInstance(genericCodecType);
        if (codec == null) {
            throw new IllegalStateException("bad generic codec: " + genericCodecTypeDefine);
        }
        return MakeCodecImpl(codec);
    }

    private Type? FindGenericEncoder(Type type) {
        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        Type codecType = _genericCodecConfig.GetEncoderType(genericTypeDefinition);
        if (codecType != null) return codecType;
        // 尝试转换为超类
        Type superClazz = CastEncoderType(genericTypeDefinition);
        if (superClazz != null) {
            return FindGenericEncoder(superClazz);
        }
        // 这段保底代码写在这里最为合适，放在用户的Config里还需要考虑冲突问题...
        // 兼容集合和字典 -- 解码时需要是默认解码类型的超类
        if (DsonConverterUtils.IsCollection(genericTypeDefinition, includeDictionary: false)) {
            return _genericCodecConfig.GetEncoderType(typeof(ICollection<>));
        }
        if (DsonConverterUtils.IsDictionary(genericTypeDefinition)) {
            return _genericCodecConfig.GetEncoderType(typeof(IDictionary<,>));
        }
        return null;
    }

    private Type? FindGenericDecoder(Type type) {
        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        Type codecType = _genericCodecConfig.GetDecoderType(genericTypeDefinition);
        if (codecType != null) return codecType;
        // 尝试转换为超类或子类
        Type? superClazz = CastDecoderType(genericTypeDefinition);
        if (superClazz != null) {
            return FindGenericEncoder(superClazz);
        }
        // c# 泛型类之间不能直接测试是否可赋值...
        return null;
    }

    private static DsonCodecImpl MakeCodecImpl(IDsonCodec codec) {
        // 存在泛型协变和逆变问题，因此不能直接使用GetEncoderClass创建泛型，需要找到IDsonCodec<>的泛型参数
        Type genericCodecType = codec.GetType().GetInterface(typeof(IDsonCodec<>).Name)!;
        Type codecImplGenericType = typeof(DsonCodecImpl<>).MakeGenericType(genericCodecType.GenericTypeArguments);
        ConstructorInfo constructor = codecImplGenericType.GetConstructors()[0];
        object dsonCodecImpl = constructor.Invoke(new object[] { codec });
        return (DsonCodecImpl)dsonCodecImpl;
    }


    private Type? CastEncoderType(Type clazz) {
        foreach (IDsonCodecCaster caster in _casters) {
            Type superClazz = caster.CastEncoderType(clazz);
            if (superClazz != null && superClazz != clazz) { // fix用户返回当前类
                return superClazz;
            }
        }
        return null;
    }

    private Type? CastDecoderType(Type clazz) {
        foreach (IDsonCodecCaster caster in _casters) {
            Type superClazz = caster.CastDecoderType(clazz);
            if (superClazz != null && superClazz != clazz) { // fix用户返回当前类
                return superClazz;
            }
        }
        return null;
    }

    #endregion
}
}