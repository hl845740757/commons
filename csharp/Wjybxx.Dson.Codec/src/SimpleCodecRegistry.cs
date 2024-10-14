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
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec.Codecs;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 
/// </summary>
public sealed class SimpleCodecRegistry : IDsonCodecRegistry
{
    private readonly IDictionary<Type, DsonCodecImpl> encoderDic;
    private readonly IDictionary<Type, DsonCodecImpl> decoderDic;
    private readonly IList<GenericCodecConfig> genericCodecConfigs;
    private readonly IList<IDsonCodecCaster> casters;

    public SimpleCodecRegistry() {
        encoderDic = new Dictionary<Type, DsonCodecImpl>(32);
        decoderDic = new Dictionary<Type, DsonCodecImpl>(32);
        genericCodecConfigs = new List<GenericCodecConfig>();
        casters = new List<IDsonCodecCaster>();
    }

    private SimpleCodecRegistry(SimpleCodecRegistry other, bool immutable = true) {
        if (immutable) {
            this.encoderDic = ImmutableLinkedDictionary<Type, DsonCodecImpl>.CreateRange(other.encoderDic);
            this.decoderDic = ImmutableLinkedDictionary<Type, DsonCodecImpl>.CreateRange(other.decoderDic);
            this.genericCodecConfigs = ImmutableList<GenericCodecConfig>.CreateRange(other.genericCodecConfigs);
            this.casters = ImmutableList<IDsonCodecCaster>.CreateRange(other.casters);
        } else {
            this.encoderDic = new Dictionary<Type, DsonCodecImpl>(other.encoderDic);
            this.decoderDic = new Dictionary<Type, DsonCodecImpl>(other.decoderDic);
            this.genericCodecConfigs = new List<GenericCodecConfig>(other.genericCodecConfigs);
            this.casters = new List<IDsonCodecCaster>(other.casters);
        }
    }

    public IDictionary<Type, DsonCodecImpl> GetEncoderDic() => encoderDic;

    public IDictionary<Type, DsonCodecImpl> GetDecoderDic() => decoderDic;

    public IList<GenericCodecConfig> GetGenericCodecConfigs() => genericCodecConfigs;

    public IList<IDsonCodecCaster> GetCasters() => casters;

    #region factory

    internal static SimpleCodecRegistry NewDefaultRegistry() {
        SimpleCodecRegistry registry = new SimpleCodecRegistry();
        // 初始化特化List
        registry.AddCodec(new MoreCollectionCodecs.IntListCodec(typeof(IList<int>)));
        registry.AddCodec(new MoreCollectionCodecs.LongListCodec(typeof(IList<long>)));
        registry.AddCodec(new MoreCollectionCodecs.FloatListCodec(typeof(IList<float>)));
        registry.AddCodec(new MoreCollectionCodecs.DoubleListCodec(typeof(IList<double>)));
        registry.AddCodec(new MoreCollectionCodecs.BoolListCodec(typeof(IList<bool>)));
        registry.AddCodec(new MoreCollectionCodecs.StringListCodec(typeof(IList<string>)));
        registry.AddCodec(new MoreCollectionCodecs.UIntListCodec(typeof(IList<uint>)));
        registry.AddCodec(new MoreCollectionCodecs.ULongListCodec(typeof(IList<ulong>)));

        registry.AddCodec(new MoreCollectionCodecs.IntListCodec(typeof(List<int>)));
        registry.AddCodec(new MoreCollectionCodecs.LongListCodec(typeof(List<long>)));
        registry.AddCodec(new MoreCollectionCodecs.FloatListCodec(typeof(List<float>)));
        registry.AddCodec(new MoreCollectionCodecs.DoubleListCodec(typeof(List<double>)));
        registry.AddCodec(new MoreCollectionCodecs.BoolListCodec(typeof(List<bool>)));
        registry.AddCodec(new MoreCollectionCodecs.StringListCodec(typeof(List<string>)));
        registry.AddCodec(new MoreCollectionCodecs.UIntListCodec(typeof(List<uint>)));
        registry.AddCodec(new MoreCollectionCodecs.ULongListCodec(typeof(List<ulong>)));
        return registry;
    }

    /** 根据codecs创建一个Registry -- 返回的实例不可变 */
    public static SimpleCodecRegistry FromCodecs(IEnumerable<IDsonCodec> codecs) {
        SimpleCodecRegistry result = new SimpleCodecRegistry();
        foreach (IDsonCodec codec in codecs) {
            result.AddCodec(codec);
        }
        return result.ToImmutable();
    }

    /** 合并多个Registry为单个Registry -- 返回的实例不可变 */
    public static SimpleCodecRegistry FromRegistries(IEnumerable<IDsonCodecRegistry> registries) {
        SimpleCodecRegistry result = new SimpleCodecRegistry();
        foreach (IDsonCodecRegistry other in registries) {
            result.MergeFrom(other);
        }
        return result.ToImmutable();
    }

    /** 转换为不可变实例 */
    public SimpleCodecRegistry ToImmutable() {
        return new SimpleCodecRegistry(this);
    }

    #endregion

    /** 压缩空间 */
    internal void TrimExcess() {
        CollectionUtil.TrimExcess(encoderDic);
        CollectionUtil.TrimExcess(decoderDic);
        CollectionUtil.TrimExcess(genericCodecConfigs);
        CollectionUtil.TrimExcess(casters);
    }

    /** 清理数据 */
    public void Clear() {
        encoderDic.Clear();
        decoderDic.Clear();
        genericCodecConfigs.Clear();
        casters.Clear();
    }

    /** 合并配置 */
    public SimpleCodecRegistry MergeFrom(IDsonCodecRegistry other) {
        SimpleCodecRegistry castOther = other as SimpleCodecRegistry;
        if (castOther == null) {
            castOther = other.Export();
        }
        foreach (KeyValuePair<Type, DsonCodecImpl> pair in castOther.encoderDic) {
            encoderDic[pair.Key] = pair.Value;
        }
        foreach (KeyValuePair<Type, DsonCodecImpl> pair in castOther.decoderDic) {
            decoderDic[pair.Key] = pair.Value;
        }
        AddGenericCodecConfigs(castOther.genericCodecConfigs);
        AddCasters(castOther.casters);
        return this;
    }

    /// <summary>
    /// 添加泛型配置
    /// </summary>
    public SimpleCodecRegistry AddGenericCodecConfig(GenericCodecConfig genericCodecConfig) {
        this.genericCodecConfigs.Add(genericCodecConfig);
        return this;
    }

    public SimpleCodecRegistry AddGenericCodecConfigs(IEnumerable<GenericCodecConfig> genericCodecConfigs) {
        foreach (GenericCodecConfig genericCodecConfig in genericCodecConfigs) {
            AddGenericCodecConfig(genericCodecConfig);
        }
        return this;
    }

    /// <summary>
    /// 添加类型转换器
    /// </summary>
    public SimpleCodecRegistry AddCaster(IDsonCodecCaster caster) {
        if (caster == null) throw new ArgumentNullException(nameof(caster));
        this.casters.Add(caster);
        return this;
    }

    public SimpleCodecRegistry AddCasters(IEnumerable<IDsonCodecCaster> casters) {
        foreach (IDsonCodecCaster? caster in casters) {
            AddCaster(caster);
        }
        return this;
    }

    #region add-codec

    /// <summary>
    /// 添加编解码器
    /// </summary>
    public SimpleCodecRegistry AddCodecs(IEnumerable<IDsonCodec> codecs) {
        foreach (IDsonCodec codec in codecs) {
            AddCodec(codec.GetEncoderType(), codec);
        }
        return this;
    }

    /// <summary>
    /// 添加编解码器
    /// </summary>
    public SimpleCodecRegistry AddCodec(IDsonCodec codec) {
        AddCodec(codec.GetEncoderType(), codec);
        return this;
    }

    /// <summary>
    /// 添加编解码器
    /// 适用超类Codec的默认解码实例可赋值给当前类型的情况，eg：IntList => IntCollectionCodec。
    /// </summary>
    public SimpleCodecRegistry AddCodec(Type type, IDsonCodec codec) {
        DsonCodecImpl codecImpl = DsonCodecImpl.CreateInstance(codec);
        encoderDic[type] = codecImpl;
        decoderDic[type] = codecImpl;
        return this;
    }

    /// <summary>
    /// 添加编码器
    /// </summary>
    /// <param name="type">要编码的类型</param>
    /// <param name="codec">编码器，codec关联的encoderType是目标类型的超类</param>
    /// <returns></returns>
    public SimpleCodecRegistry AddEncoder(Type type, IDsonCodec codec) {
        DsonCodecImpl codecImpl = DsonCodecImpl.CreateInstance(codec);
        encoderDic[type] = codecImpl;
        return this;
    }

    /// <summary>
    /// 添加解码器
    /// </summary>
    /// <param name="type">要解码的类型</param>
    /// <param name="codec">编码器，codec关联的encoderType是目标类型的子类</param>
    /// <returns></returns>
    public SimpleCodecRegistry AddDecoder(Type type, IDsonCodec codec) {
        DsonCodecImpl codecImpl = DsonCodecImpl.CreateInstance(codec);
        decoderDic[type] = codecImpl;
        return this;
    }

    // 用于合并其它Registry
    public SimpleCodecRegistry AddEncoder(Type type, DsonCodecImpl codecImpl) {
        encoderDic[type] = codecImpl;
        return this;
    }

    public SimpleCodecRegistry AddDecoder(Type type, DsonCodecImpl codecImpl) {
        decoderDic[type] = codecImpl;
        return this;
    }

    #endregion


    public DsonCodecImpl? GetEncoder(Type typeInfo) {
        encoderDic.TryGetValue(typeInfo, out DsonCodecImpl codecImpl);
        return codecImpl;
    }

    public DsonCodecImpl? GetDecoder(Type typeInfo) {
        decoderDic.TryGetValue(typeInfo, out DsonCodecImpl codecImpl);
        return codecImpl;
    }

    public SimpleCodecRegistry Export() {
        return new SimpleCodecRegistry(this, false);
    }
}
}