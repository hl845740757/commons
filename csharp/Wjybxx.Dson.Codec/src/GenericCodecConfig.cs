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
using Wjybxx.Commons;
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
/// 2. 如果Codec的是面向接口或抽象类的，构造函数可以接收<see cref="Type"/>和<see cref="Func{TResult}"/>参数 -- 可参考<see cref="CollectionCodec{T}"/>。
/// 3. 不会频繁查询，因此不必太在意匹配算法的效率。
/// 4. 数组和泛型是不同的，数组都对应<see cref="ArrayCodec{T}"/>，因此不需要在这里存储。
/// 5. 请避免运行时修改数据，否则可能造成线程安全问题。
/// 6. 在dotnet6/7中不支持泛型协变和逆变，因此 Codec`1[IList`1[string]] 是不能赋值给 Codec`1[List`1[String]]的。
/// </summary>
[NotThreadSafe]
public sealed class GenericCodecConfig
{
    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存  */
    private readonly IDictionary<Type, GenericCodecInfo> encoderTypeDic;
    private readonly IDictionary<Type, GenericCodecInfo> decoderTypeDic;

    public GenericCodecConfig() {
        encoderTypeDic = new Dictionary<Type, GenericCodecInfo>();
        decoderTypeDic = new Dictionary<Type, GenericCodecInfo>();
    }

    private GenericCodecConfig(IDictionary<Type, GenericCodecInfo> encoderTypeDic, IDictionary<Type, GenericCodecInfo> decoderTypeDic) {
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
        // 艹，readonly系列集合和普通集合之间没有交集...
        // CollectionCodec默认测试了常见的集合类型
        AddCodec(typeof(ICollection<>), typeof(CollectionCodec<>));
        AddCodec(typeof(IList<>), typeof(CollectionCodec<>));
        AddCodec(typeof(List<>), typeof(CollectionCodec<>));
        // 
        AddCodec(typeof(ISet<>), typeof(CollectionCodec<>));
        AddCodec(typeof(HashSet<>), typeof(CollectionCodec<>));
        AddCodec(typeof(LinkedHashSet<>), typeof(CollectionCodec<>));
        //
        AddCodec(typeof(Stack<>), typeof(MoreCollectionCodecs.StackCodec<>));
        AddCodec(typeof(Queue<>), typeof(MoreCollectionCodecs.QueueCodec<>));

        // IDictionary接口不指定工厂，根据options动态分配实现
        AddCodec(typeof(IDictionary<,>), typeof(DictionaryCodec<,>));
        AddCodec(typeof(Dictionary<,>), typeof(DictionaryCodec<,>));
        AddCodec(typeof(LinkedDictionary<,>), typeof(DictionaryCodec<,>));
        AddCodec(typeof(ConcurrentDictionary<,>), typeof(DictionaryCodec<,>));
        // 特殊组件
        AddCodec(typeof(DictionaryEncodeProxy<>), typeof(DictionaryEncodeProxyCodec<>));
        AddCodec(typeof(Nullable<>), typeof(NullableCodec<>));
        return this;
    }

    /// <summary>
    /// 主要用于合并注解处理器生成的Config
    /// </summary>
    /// <param name="otherConfig"></param>
    public GenericCodecConfig AddCodecs(GenericCodecConfig otherConfig) {
        foreach (GenericCodecInfo item in otherConfig.encoderTypeDic.Values) {
            AddEncoder(item);
        }
        foreach (GenericCodecInfo item in otherConfig.decoderTypeDic.Values) {
            AddDecoder(item);
        }
        return this;
    }

    #region add-codec

    /// <summary>
    /// 增加一个配置
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public GenericCodecConfig AddCodec(Type genericType, Type codecType) {
        AddCodec(GenericCodecInfo.Create(genericType, codecType));
        return this;
    }

    /// <summary>
    /// 增加一个配置，适用factory定义在codec类中的情况
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息，也是工厂类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    /// <returns></returns>
    public GenericCodecConfig AddCodec(Type genericType, Type codecType, string factoryFieldName) {
        AddCodec(GenericCodecInfo.Create(genericType, codecType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 增加一个配置，适用factory定义在外部类中的情况
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    /// <param name="factoryDeclaringType">定义工厂字段的类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    public GenericCodecConfig AddCodec(Type genericType, Type codecType, Type factoryDeclaringType, string factoryFieldName) {
        AddCodec(GenericCodecInfo.Create(genericType, codecType, factoryDeclaringType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericCodecInfo">配置项</param>
    /// <exception cref="ArgumentException"></exception>
    public GenericCodecConfig AddCodec(GenericCodecInfo genericCodecInfo) {
        if (genericCodecInfo.IsNull) throw new ArgumentException("codecInfo  is null");
        encoderTypeDic[genericCodecInfo.typeInfo] = genericCodecInfo;
        decoderTypeDic[genericCodecInfo.typeInfo] = genericCodecInfo;
        return this;
    }

    #endregion

    #region add-encoder

    /// <summary>
    /// 添加编码器
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public GenericCodecConfig AddEncoder(Type genericType, Type codecType) {
        AddEncoder(GenericCodecInfo.Create(genericType, codecType));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息，也是工厂类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    /// <returns>this</returns>
    public GenericCodecConfig AddEncoder(Type genericType, Type codecType, string factoryFieldName) {
        AddEncoder(GenericCodecInfo.Create(genericType, codecType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    /// <param name="factoryDeclaringType">定义工厂字段的类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    /// <returns>this</returns>
    public GenericCodecConfig AddEncoder(Type genericType, Type codecType, Type factoryDeclaringType, string factoryFieldName) {
        AddEncoder(GenericCodecInfo.Create(genericType, codecType, factoryDeclaringType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericCodecInfo">配置项</param>
    /// <returns></returns>
    public GenericCodecConfig AddEncoder(GenericCodecInfo genericCodecInfo) {
        if (genericCodecInfo.IsNull) throw new ArgumentException("codecInfo  is null");
        encoderTypeDic[genericCodecInfo.typeInfo] = genericCodecInfo;
        return this;
    }

    #endregion

    #region add-decoder

    /// <summary>
    /// 添加解码器
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public GenericCodecConfig AddDecoder(Type genericType, Type codecType) {
        AddDecoder(GenericCodecInfo.Create(genericType, codecType));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息，也是工厂类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    public GenericCodecConfig AddDecoder(Type genericType, Type codecType, string factoryFieldName) {
        AddDecoder(GenericCodecInfo.Create(genericType, codecType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    /// <param name="factoryDeclaringType">定义工厂字段的类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    public GenericCodecConfig AddDecoder(Type genericType, Type codecType, Type factoryDeclaringType, string factoryFieldName) {
        AddDecoder(GenericCodecInfo.Create(genericType, codecType, factoryDeclaringType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericCodecInfo">配置项</param>
    /// <returns></returns>
    public GenericCodecConfig AddDecoder(GenericCodecInfo genericCodecInfo) {
        if (genericCodecInfo.IsNull) throw new ArgumentException("codecInfo  is null");
        decoderTypeDic[genericCodecInfo.typeInfo] = genericCodecInfo;
        return this;
    }

    #endregion

    /// <summary>
    /// 获取编码器类型
    /// </summary>
    /// <param name="genericTypeDefine"></param>
    /// <returns></returns>
    public GenericCodecInfo? GetEncoderInfo(Type genericTypeDefine) {
        if (encoderTypeDic.TryGetValue(genericTypeDefine, out GenericCodecInfo item)) {
            return item;
        }
        return null;
    }

    /// <summary>
    /// 获取解码器类型
    /// </summary>
    /// <param name="genericTypeDefine"></param>
    /// <returns></returns>
    public GenericCodecInfo? GetDecoderInfo(Type genericTypeDefine) {
        if (decoderTypeDic.TryGetValue(genericTypeDefine, out GenericCodecInfo item)) {
            return item;
        }
        return null;
    }
}
}