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
/// 泛型类到泛型类的Codec的类型映射。
/// 由于泛型类的Codec不能被直接构造，因此只能先将其类型信息存储下来，待到确定泛型参数类型的时候再构造。
/// 考虑到泛型的反射构建较为复杂，因此我们不采用Type => Factory 的形式来配置，而是配置对应的Codec原型类；
/// 这可能增加类的数量，但代码的复杂度更低，更易于使用。
/// 
/// 注意：
/// 1. Codec需要和泛型定义类有相同的泛型参数列表。
/// 2. 不会频繁查询，因此不必太在意匹配算法的效率。
/// 3. 数组和泛型是不同的，数组都对应<see cref="ArrayCodec{T}"/>，因此不需要在这里存储。
/// 4. 在dotnet6/7中不支持泛型协变和逆变，因此 Codec`1[IList`1[string]] 是不能赋值给 Codec`1[List`1[String]]的。
/// </summary>
[NotThreadSafe]
public sealed class GenericCodecConfig
{
    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存  */
    private readonly IDictionary<Type, Type> encoderTypeDic;
    private readonly IDictionary<Type, Type> decoderTypeDic;

    public GenericCodecConfig() {
        encoderTypeDic = new Dictionary<Type, Type>();
        decoderTypeDic = new Dictionary<Type, Type>();
    }

    private GenericCodecConfig(IDictionary<Type, Type> encoderTypeDic, IDictionary<Type, Type> decoderTypeDic) {
        this.encoderTypeDic = encoderTypeDic.ToImmutableLinkedDictionary();
        this.decoderTypeDic = decoderTypeDic.ToImmutableLinkedDictionary(); // 避免系统库依赖，无法引入Unity
    }

    /** 清理数据 */
    public void Clear() {
        encoderTypeDic.Clear();
        decoderTypeDic.Clear();
    }

    /** 转换为不可变配置 */
    public GenericCodecConfig ToImmutable() {
        return new GenericCodecConfig(encoderTypeDic, decoderTypeDic);
    }

    /** 创建一个默认配置 */
    public static GenericCodecConfig NewDefaultConfig() {
        return new GenericCodecConfig().InitWithDefaults();
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
    /// 增加一个配置
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public void AddCodec(Type genericType, Type codecType) {
        if (!genericType.IsGenericType) throw new ArgumentException($"genericType is not IsGenericType");
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
        if (!genericType.IsGenericType) throw new ArgumentException($"genericType is not IsGenericType");
        if (genericType.GenericTypeArguments.Length != codecType.GenericTypeArguments.Length) {
            throw new ArgumentException("genericType.GenericTypeArguments.Length != codecType.GenericTypeArguments.Length");
        }
        encoderTypeDic[genericType] = codecType;
    }

    /// <summary>
    /// 添加解码器
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public void AddDecoder(Type genericType, Type codecType) {
        if (!genericType.IsGenericType) throw new ArgumentException($"genericType is not IsGenericType");
        if (genericType.GenericTypeArguments.Length != codecType.GenericTypeArguments.Length) {
            throw new ArgumentException("genericType.GenericTypeArguments.Length != codecType.GenericTypeArguments.Length");
        }
        decoderTypeDic[genericType] = codecType;
    }

    /// <summary>
    /// 获取编码器类型
    /// </summary>
    /// <param name="genericTypeDefine"></param>
    /// <returns></returns>
    public Type? GetEncoderType(Type genericTypeDefine) {
        encoderTypeDic.TryGetValue(genericTypeDefine, out Type codecType);
        return codecType;
    }

    /// <summary>
    /// 获取解码器类型
    /// </summary>
    /// <param name="genericTypeDefine"></param>
    /// <returns></returns>
    public Type? GetDecoderType(Type genericTypeDefine) {
        decoderTypeDic.TryGetValue(genericTypeDefine, out Type codecType);
        return codecType;
    }
}
}