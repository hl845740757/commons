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
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec.Codecs;

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 默认的泛型Codec配置类实现，该配置应当在使用前初始化，因此默认是非线程安全的。
/// </summary>
[NotThreadSafe]
public class GenericCodecConfig : IGenericCodecConfig
{
    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存  */
    private readonly Dictionary<Type, Type> encoderTypeDic = new Dictionary<Type, Type>();
    private readonly Dictionary<Type, Type> decoderTypeDic = new Dictionary<Type, Type>();

    public GenericCodecConfig() {
    }

    /** 创建一个默认配置 */
    public static GenericCodecConfig NewDefaultConfig() {
        return new GenericCodecConfig().InitWithDefaults();
    }

    /// <summary>
    /// 主要用于合并注解处理器生成的Config
    /// </summary>
    /// <param name="otherConfig"></param>
    public void AddCodecs(GenericCodecConfig otherConfig) {
        foreach (KeyValuePair<Type, Type> pair in otherConfig.encoderTypeDic) {
            AddEncoder(pair.Key, pair.Value);
        }
        foreach (KeyValuePair<Type, Type> pair in otherConfig.decoderTypeDic) {
            AddDecoder(pair.Key, pair.Value);
        }
    }

    /// <summary>
    /// 通过默认的泛型类Codec初始化
    /// </summary>
    public GenericCodecConfig InitWithDefaults() {
        AddCodec(typeof(ICollection<>), typeof(ICollectionCodec<>));
        AddCodec(typeof(IList<>), typeof(MoreCollectionCodecs.IListCodec<>));
        AddCodec(typeof(List<>), typeof(MoreCollectionCodecs.ListCodec<>));
        AddCodec(typeof(Stack<>), typeof(MoreCollectionCodecs.StackCodec<>));
        AddCodec(typeof(Queue<>), typeof(MoreCollectionCodecs.QueueCodec<>));

        AddCodec(typeof(ISet<>), typeof(MoreCollectionCodecs.ISetCodec<>));
        AddCodec(typeof(HashSet<>), typeof(MoreCollectionCodecs.HashSetCodec<>));
        AddCodec(typeof(LinkedHashSet<>), typeof(MoreCollectionCodecs.LinkedHashSetCodec<>));

        AddCodec(typeof(IDictionary<,>), typeof(IDictionaryCodec<,>));
        AddCodec(typeof(Dictionary<,>), typeof(MoreCollectionCodecs.DictionaryCodec<,>));
        AddCodec(typeof(LinkedDictionary<,>), typeof(MoreCollectionCodecs.LinkedDictionaryCodec<,>));
        AddCodec(typeof(ConcurrentDictionary<,>), typeof(MoreCollectionCodecs.ConcurrentDictionaryCodec<,>));

        // 特殊组件
        AddCodec(typeof(DictionaryEncodeProxy<>), typeof(DictionaryEncodeProxyCodec<>));
        AddCodec(typeof(Nullable<>), typeof(NullableCodec<>));
        return this;
    }

    /// <summary>
    /// 增加一个配置
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public void AddCodec(Type genericType, Type codecType) {
        if (genericType == null) throw new ArgumentNullException(nameof(genericType));
        if (codecType == null) throw new ArgumentNullException(nameof(codecType));
        if (genericType.GenericTypeArguments.Length != codecType.GenericTypeArguments.Length) {
            throw new ArgumentException("genericType.GenericTypeArguments.Length != codecType.GenericTypeArguments.Length");
        }
        encoderTypeDic[genericType] = codecType;
        decoderTypeDic[genericType] = codecType;
    }

    /// <summary>
    /// 添加编码器
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public void AddEncoder(Type genericType, Type codecType) {
        encoderTypeDic[genericType] = codecType;
    }

    /// <summary>
    /// 添加解码器
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public void AddDecoder(Type genericType, Type codecType) {
        decoderTypeDic[genericType] = codecType;
    }

    /// <summary>
    /// <inheritdoc cref="IGenericCodecConfig.GetEncoderType"/>
    /// </summary>
    public virtual Type? GetEncoderType(Type genericTypeDefine) {
        if (!encoderTypeDic.TryGetValue(genericTypeDefine, out Type codecType)) {
            // 集合和字典兼容
            if (DsonConverterUtils.IsCollection(genericTypeDefine)) {
                encoderTypeDic.TryGetValue(typeof(ICollection<>), out codecType);
            } else if (DsonConverterUtils.IsDictionary(genericTypeDefine)) {
                encoderTypeDic.TryGetValue(typeof(IDictionary<,>), out codecType);
            }
        }
        return codecType;
    }

    /// <summary>
    /// <inheritdoc cref="IGenericCodecConfig.GetDecoderType"/>
    /// </summary>
    public virtual Type? GetDecoderType(Type genericTypeDefine) {
        decoderTypeDic.TryGetValue(genericTypeDefine, out Type codecType);
        return codecType;
    }
}
}