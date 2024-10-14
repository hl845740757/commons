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
    private readonly SimpleCodecRegistry _basicRegistry;
    /** 泛型类对应的Codec类型 */
    private readonly GenericCodecConfig _genericCodecConfig;
    /** 类型转换器 */
    private readonly List<IDsonCodecCaster> _casters;

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存  */
    private readonly ConcurrentDictionary<Type, DsonCodecImpl> encoderDic = new ConcurrentDictionary<Type, DsonCodecImpl>();
    private readonly ConcurrentDictionary<Type, DsonCodecImpl> decoderDic = new ConcurrentDictionary<Type, DsonCodecImpl>();

    /// <summary>
    /// 
    /// </summary>
    public DynamicCodecRegistry(List<IDsonCodecRegistry> basicRegistries) {
        _basicRegistry = new SimpleCodecRegistry();
        foreach (IDsonCodecRegistry other in basicRegistries) {
            _basicRegistry.MergeFrom(other.Export());
        }
        // 先初始化为默认配置，然后由用户的配置进行覆盖 -- 不转不可变对象，性能更好
        _genericCodecConfig = GenericCodecConfig.NewDefaultConfig();
        foreach (GenericCodecConfig genericCodecConfig in _basicRegistry.GetGenericCodecConfigs()) {
            _genericCodecConfig.MergeFrom(genericCodecConfig);
        }
        // 拷贝，避免不必要的虚方法调用
        _casters = new List<IDsonCodecCaster>(_basicRegistry.GetCasters());

        // 初始化特化List
        AddCodec(new DsonCodecImpl<IList<int>>(new MoreCollectionCodecs.IntListCodec(typeof(IList<int>))));
        AddCodec(new DsonCodecImpl<IList<long>>(new MoreCollectionCodecs.LongListCodec(typeof(IList<long>))));
        AddCodec(new DsonCodecImpl<IList<float>>(new MoreCollectionCodecs.FloatListCodec(typeof(IList<float>))));
        AddCodec(new DsonCodecImpl<IList<double>>(new MoreCollectionCodecs.DoubleListCodec(typeof(IList<double>))));
        AddCodec(new DsonCodecImpl<IList<bool>>(new MoreCollectionCodecs.BoolListCodec(typeof(IList<bool>))));
        AddCodec(new DsonCodecImpl<IList<string>>(new MoreCollectionCodecs.StringListCodec(typeof(IList<string>))));
        AddCodec(new DsonCodecImpl<IList<uint>>(new MoreCollectionCodecs.UIntListCodec(typeof(IList<uint>))));
        AddCodec(new DsonCodecImpl<IList<ulong>>(new MoreCollectionCodecs.ULongListCodec(typeof(IList<ulong>))));

        AddCodec(new DsonCodecImpl<IList<int>>(new MoreCollectionCodecs.IntListCodec(typeof(List<int>))));
        AddCodec(new DsonCodecImpl<IList<long>>(new MoreCollectionCodecs.LongListCodec(typeof(List<long>))));
        AddCodec(new DsonCodecImpl<IList<float>>(new MoreCollectionCodecs.FloatListCodec(typeof(List<float>))));
        AddCodec(new DsonCodecImpl<IList<double>>(new MoreCollectionCodecs.DoubleListCodec(typeof(List<double>))));
        AddCodec(new DsonCodecImpl<IList<bool>>(new MoreCollectionCodecs.BoolListCodec(typeof(List<bool>))));
        AddCodec(new DsonCodecImpl<IList<string>>(new MoreCollectionCodecs.StringListCodec(typeof(List<string>))));
        AddCodec(new DsonCodecImpl<IList<uint>>(new MoreCollectionCodecs.UIntListCodec(typeof(List<uint>))));
        AddCodec(new DsonCodecImpl<IList<ulong>>(new MoreCollectionCodecs.ULongListCodec(typeof(List<ulong>))));
    }

    public SimpleCodecRegistry Export() {
        SimpleCodecRegistry result = new SimpleCodecRegistry();
        result.MergeFrom(_basicRegistry);
        result.GetEncoderDic().AddAll(encoderDic);
        result.GetDecoderDic().AddAll(decoderDic);
        return result;
    }

    #region add-cache

    /// <summary>
    /// 预添加Codec(可覆盖)
    /// </summary>
    /// <param name="codecImpl"></param>
    public void AddCodec(DsonCodecImpl codecImpl) {
        encoderDic[codecImpl.GetEncoderType()] = codecImpl;
        decoderDic[codecImpl.GetEncoderType()] = codecImpl;
    }

    /// <summary>
    /// 添加编码器
    /// </summary>
    /// <param name="codecImpl">编码器</param>
    public void AddEncoder(DsonCodecImpl codecImpl) {
        encoderDic[codecImpl.GetEncoderType()] = codecImpl;
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
        encoderDic[type] = codecImpl;
    }

    /// <summary>
    /// 添加解码器
    /// </summary>
    /// <param name="codecImpl"></param>
    public void AddDecoder(DsonCodecImpl codecImpl) {
        decoderDic[codecImpl.GetEncoderType()] = codecImpl;
    }

    #endregion

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
            GenericCodecInfo? genericCodecInfo = _genericCodecConfig.GetEncoderInfo(type.GetGenericTypeDefinition());
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
            GenericCodecInfo? genericCodecInfo = _genericCodecConfig.GetDecoderInfo(type.GetGenericTypeDefinition());
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
        return null;
    }

    #endregion
}
}