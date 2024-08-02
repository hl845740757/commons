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
public class DynamicDsonCodecRegistry : IDsonCodecRegistry
{
    /** 用户的原始的类型Codec */
    private readonly IDsonCodecRegistry _basicRegistry;
    /** 泛型类对应的Codec类型 */
    private readonly IGenericCodecConfig _genericCodecConfig;

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存  */
    private readonly ConcurrentDictionary<Type, DsonCodecImpl> encoderDic = new ConcurrentDictionary<Type, DsonCodecImpl>();
    private readonly ConcurrentDictionary<Type, DsonCodecImpl> decoderDic = new ConcurrentDictionary<Type, DsonCodecImpl>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="basicRegistry">用户的基础类型Codec</param>
    /// <param name="genericCodecConfig">泛型Codec配置类</param>
    public DynamicDsonCodecRegistry(IDsonCodecRegistry basicRegistry, IGenericCodecConfig genericCodecConfig) {
        _basicRegistry = basicRegistry;
        _genericCodecConfig = genericCodecConfig;

        // 初始化特化数组
        AddCodec(new DsonCodecImpl<int[]>(new MoreArrayCodecs.IntArrayCodec()));
        AddCodec(new DsonCodecImpl<long[]>(new MoreArrayCodecs.LongArrayCodec()));
        AddCodec(new DsonCodecImpl<float[]>(new MoreArrayCodecs.FloatArrayCodec()));
        AddCodec(new DsonCodecImpl<double[]>(new MoreArrayCodecs.DoubleArrayCodec()));
        AddCodec(new DsonCodecImpl<bool[]>(new MoreArrayCodecs.BoolArrayCodec()));
        AddCodec(new DsonCodecImpl<string[]>(new MoreArrayCodecs.StringArrayCodec()));
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
        encoderDic.TryAdd(codecImpl.GetEncoderClass(), codecImpl);
        decoderDic.TryAdd(codecImpl.GetEncoderClass(), codecImpl);
    }

    /// <summary>
    /// 添加编码器
    /// </summary>
    /// <param name="codecImpl">编码器</param>
    public void AddEncoder(DsonCodecImpl codecImpl) {
        encoderDic.TryAdd(codecImpl.GetEncoderClass(), codecImpl);
    }

    /// <summary>
    /// 添加编码器
    /// </summary>
    /// <param name="type">绑定的类型，需要是Codec绑定类型的子类</param>
    /// <param name="codecImpl">编码器</param>
    /// <exception cref="ArgumentException"></exception>
    public void AddEncoder(Type type, DsonCodecImpl codecImpl) {
        if (!codecImpl.GetEncoderClass().IsAssignableFrom(type)) {
            throw new ArgumentException($"codecType: {codecImpl.GetEncoderClass()}, argType: {type}");
        }
        encoderDic.TryAdd(type, codecImpl);
    }

    /// <summary>
    /// 添加解码器
    /// </summary>
    /// <param name="codecImpl"></param>
    public void AddDecoder(DsonCodecImpl codecImpl) {
        decoderDic.TryAdd(codecImpl.GetEncoderClass(), codecImpl);
    }

    /// <summary>
    /// 查找可用的编码器，对于泛型和数组会动态生成Codec
    /// </summary>
    /// <param name="type"></param>
    /// <param name="rootRegistry"></param>
    /// <returns></returns>
    public DsonCodecImpl? GetEncoder(Type type, IDsonCodecRegistry rootRegistry) {
        // 优先查找用户的Codec，以允许用户定制优化
        DsonCodecImpl codecImpl = _basicRegistry.GetEncoder(type, rootRegistry);
        if (codecImpl != null) return codecImpl;
        if (encoderDic.TryGetValue(type, out codecImpl)) {
            return codecImpl;
        }
        if (type.IsArray) {
            codecImpl = MakeArrayCodec(type);
        } else if (type.IsGenericType) {
            codecImpl = MakeGenericCodec(type, true);
        } else if (type.IsEnum) {
            codecImpl = MakeEnumCodec(type);
        }
        if (codecImpl != null) {
            encoderDic.TryAdd(type, codecImpl);
        }
        return codecImpl; // 不存在的类型
    }

    /// <summary>
    /// 查找可用的解码器，对于泛型和数组会动态生成Codec
    /// </summary>
    /// <param name="type"></param>
    /// <param name="rootRegistry"></param>
    /// <returns></returns>
    public DsonCodecImpl? GetDecoder(Type type, IDsonCodecRegistry rootRegistry) {
        // 优先查找用户的Codec，以允许用户定制优化
        DsonCodecImpl codecImpl = _basicRegistry.GetDecoder(type, rootRegistry);
        if (codecImpl != null) return codecImpl;
        if (decoderDic.TryGetValue(type, out codecImpl)) {
            return codecImpl;
        }
        if (type.IsArray) {
            codecImpl = MakeArrayCodec(type);
        } else if (type.IsGenericType) {
            codecImpl = MakeGenericCodec(type, false);
        } else if (type.IsEnum) {
            codecImpl = MakeEnumCodec(type);
        }
        if (codecImpl != null) {
            decoderDic.TryAdd(type, codecImpl);
        }
        return codecImpl; // 不存在的类型
    }

    #region make

    private DsonCodecImpl MakeArrayCodec(Type type) {
        Type genericArrayCodecType = typeof(ArrayCodec<>).MakeGenericType(type.GetElementType()!);
        IDsonCodec codec = (IDsonCodec)Activator.CreateInstance(genericArrayCodecType)!;

        DsonCodecImpl codecImpl = MakeCodecImpl(codec);
        encoderDic.TryAdd(type, codecImpl);
        return codecImpl;
    }

    private DsonCodecImpl? MakeGenericCodec(Type type, bool encoder) {
        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        Type genericCodecTypeDefine = encoder
            ? _genericCodecConfig.GetEncoderType(genericTypeDefinition)
            : _genericCodecConfig.GetDecoderType(genericTypeDefinition);
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

    private DsonCodecImpl MakeEnumCodec(Type type) {
        Type enumCodecType = typeof(EnumCodec<>).MakeGenericType(type);
        IDsonCodec codec = (IDsonCodec)Activator.CreateInstance(enumCodecType)!;
        return MakeCodecImpl(codec);
    }

    private static DsonCodecImpl MakeCodecImpl(IDsonCodec codec) {
        // 存在泛型协变和逆变问题，因此不能直接使用GetEncoderClass创建泛型，需要找到IDsonCodec<>的泛型参数
        Type genericCodecType = codec.GetType().GetInterface(typeof(IDsonCodec<>).Name)!;
        Type codecImplGenericType = typeof(DsonCodecImpl<>).MakeGenericType(genericCodecType.GenericTypeArguments);
        ConstructorInfo constructor = codecImplGenericType.GetConstructors()[0];
        object dsonCodecImpl = constructor.Invoke(new object[] { codec });
        return (DsonCodecImpl)dsonCodecImpl;
    }

    #endregion
}
}