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
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec.Codecs;

namespace Wjybxx.Dson.Codec
{
/// <summary>
///
/// <h3>泛型类Codec</h3>
/// 1. Codec需要和泛型定义类有相同的泛型参数列表。
/// 2. 如果Codec的是面向接口或抽象类的，构造函数可以接收<see cref="Type"/>和<see cref="Func{TResult}"/>参数 -- 可参考<see cref="CollectionCodec{T}"/>。
/// 3. 不会频繁查询，因此不必太在意匹配算法的效率。
/// 4. 数组和泛型是不同的，数组都对应<see cref="ArrayCodec{T}"/>，因此不需要在这里存储。
/// 5. 请避免运行时修改数据，否则可能造成线程安全问题。
/// 6. 在dotnet6/7中不支持泛型协变和逆变，因此 Codec`1[IList`1[string]] 是不能赋值给 Codec`1[List`1[String]]的。
///
/// <h3>与TypeMetaConfig的关系</h3>
/// Codec与TypeMete在配置和运行时都是分离的，它们属于不同的体系；
/// 但Codec关联的encoderType必须在<see cref="TypeMetaConfig"/>中存在。
/// 
/// <h3>合并规则</h3>
/// 多个Config合并时，越靠近用户，优先级越高 -- 因为这一定能解决冲突。
/// </summary>
public sealed class DsonCodecConfig
{
    // 一个Type可能只有encoder而没有decoder，因此需要分开缓存
    /** 非泛型Codec，或预设的特殊泛型实例Codec */
    private readonly IDictionary<Type, IDsonCodec> encoderDic;
    private readonly IDictionary<Type, IDsonCodec> decoderDic;
    /** 泛型Codec */
    private readonly IDictionary<Type, GenericCodecInfo> genericEncoderDic;
    private readonly IDictionary<Type, GenericCodecInfo> genericDecoderDic;
    /** 类型转换器 */
    private readonly IList<IDsonCodecCaster> casters;
    /** 可忽略的类型信息 */
    private readonly IDictionary<TypePair, bool> optimizedTypes;

    public DsonCodecConfig() {
        encoderDic = new Dictionary<Type, IDsonCodec>(32);
        decoderDic = new Dictionary<Type, IDsonCodec>(32);
        genericEncoderDic = new Dictionary<Type, GenericCodecInfo>(16);
        genericDecoderDic = new Dictionary<Type, GenericCodecInfo>(16);
        casters = new List<IDsonCodecCaster>();
        optimizedTypes = new Dictionary<TypePair, bool>(16);
    }

    private DsonCodecConfig(DsonCodecConfig other) {
        // 避免依赖系统库的不可变集合，导致无法引入unity
        this.encoderDic = other.encoderDic.ToImmutableLinkedDictionary();
        this.decoderDic = other.decoderDic.ToImmutableLinkedDictionary();
        this.genericEncoderDic = other.genericEncoderDic.ToImmutableLinkedDictionary();
        this.genericDecoderDic = other.genericDecoderDic.ToImmutableLinkedDictionary();
        this.casters = other.casters.ToImmutableList2();
        this.optimizedTypes = other.optimizedTypes.ToImmutableLinkedDictionary();
    }

    public IDictionary<Type, IDsonCodec> GetEncoderDic() => encoderDic;

    public IDictionary<Type, IDsonCodec> GetDecoderDic() => decoderDic;

    public IDictionary<Type, GenericCodecInfo> GetGenericEncoderDic() => genericEncoderDic;

    public IDictionary<Type, GenericCodecInfo> GetGenericDecoderDic() => genericDecoderDic;

    public IList<IDsonCodecCaster> GetCasters() => casters;

    public IDictionary<TypePair, bool> GetOptimizedTypes() => optimizedTypes;

    #region factory

    /** 根据codecs创建一个Config -- 返回的实例不可变 */
    public static DsonCodecConfig FromCodecs(IEnumerable<IDsonCodec> codecs) {
        DsonCodecConfig result = new DsonCodecConfig();
        foreach (IDsonCodec codec in codecs) {
            result.AddCodec(codec);
        }
        return result.ToImmutable();
    }

    /** 合并多个Config为单个Config -- 返回的实例不可变 */
    public static DsonCodecConfig FromConfigs(IEnumerable<DsonCodecConfig> configs) {
        DsonCodecConfig result = new DsonCodecConfig();
        foreach (DsonCodecConfig other in configs) {
            result.MergeFrom(other);
        }
        return result.ToImmutable();
    }

    /** 转换为不可变实例 */
    public DsonCodecConfig ToImmutable() {
        if (encoderDic is Dictionary<Type, IDsonCodec>) {
            return new DsonCodecConfig(this);
        }
        return this;
    }

    #endregion

    /** 压缩空间 */
    internal void TrimExcess() {
        CollectionUtil.TrimExcess(encoderDic);
        CollectionUtil.TrimExcess(decoderDic);
        CollectionUtil.TrimExcess(genericEncoderDic);
        CollectionUtil.TrimExcess(genericDecoderDic);
        CollectionUtil.TrimExcess(casters);
    }

    /** 清理数据 */
    public void Clear() {
        encoderDic.Clear();
        decoderDic.Clear();
        genericEncoderDic.Clear();
        genericDecoderDic.Clear();
        casters.Clear();
        optimizedTypes.Clear();
    }

    /** 合并配置 */
    public DsonCodecConfig MergeFrom(DsonCodecConfig other) {
        encoderDic.PutAll(other.encoderDic);
        decoderDic.PutAll(other.decoderDic);
        genericEncoderDic.PutAll(other.genericEncoderDic);
        genericDecoderDic.PutAll(other.genericDecoderDic);
        casters.AddAll(other.casters);
        optimizedTypes.PutAll(other.optimizedTypes);
        return this;
    }

    #region 非泛型Codec

    /// <summary>
    /// 添加编解码器
    /// </summary>
    public DsonCodecConfig AddCodecs(IEnumerable<IDsonCodec> codecs) {
        foreach (IDsonCodec codec in codecs) {
            AddCodec(codec.GetEncoderType(), codec);
        }
        return this;
    }

    /// <summary>
    /// 添加编解码器
    /// </summary>
    public DsonCodecConfig AddCodec(IDsonCodec codec) {
        AddCodec(codec.GetEncoderType(), codec);
        return this;
    }

    /// <summary>
    /// 添加编解码器
    /// 适用超类Codec的默认解码实例可赋值给当前类型的情况，eg：IntList => IntCollectionCodec。
    /// </summary>
    public DsonCodecConfig AddCodec(Type type, IDsonCodec codec) {
        encoderDic[type] = codec;
        decoderDic[type] = codec;
        return this;
    }

    /// <summary>
    /// 添加编码器
    /// </summary>
    /// <param name="type">要编码的类型</param>
    /// <param name="codec">编码器，codec关联的encoderType是目标类型的超类</param>
    /// <returns></returns>
    public DsonCodecConfig AddEncoder(Type type, IDsonCodec codec) {
        encoderDic[type] = codec;
        return this;
    }

    /// <summary>
    /// 添加解码器
    /// </summary>
    /// <param name="type">要解码的类型</param>
    /// <param name="codec">编码器，codec关联的encoderType是目标类型的子类</param>
    /// <returns></returns>
    public DsonCodecConfig AddDecoder(Type type, IDsonCodec codec) {
        decoderDic[type] = codec;
        return this;
    }

    /** 删除编码器 -- 用于解决冲突 */
    public IDsonCodec? RemoveEncoder(Type type) {
        encoderDic.Remove(type, out IDsonCodec? r);
        return r;
    }

    /** 删除解码器 -- 用于解决冲突 */
    public IDsonCodec? RemoveDecoder(Type type) {
        decoderDic.Remove(type, out IDsonCodec? r);
        return r;
    }

    #endregion

    #region 泛型codec

    #region add-codec

    /// <summary>
    /// 增加一个配置
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public DsonCodecConfig AddGenericCodec(Type genericType, Type codecType) {
        AddGenericCodec(GenericCodecInfo.Create(genericType, codecType));
        return this;
    }

    /// <summary>
    /// 增加一个配置，适用factory定义在codec类中的情况
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息，也是工厂类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    /// <returns></returns>
    public DsonCodecConfig AddGenericCodec(Type genericType, Type codecType, string factoryFieldName) {
        AddGenericCodec(GenericCodecInfo.Create(genericType, codecType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 增加一个配置，适用factory定义在外部类中的情况
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    /// <param name="factoryDeclaringType">定义工厂字段的类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    public DsonCodecConfig AddGenericCodec(Type genericType, Type codecType, Type factoryDeclaringType, string factoryFieldName) {
        AddGenericCodec(GenericCodecInfo.Create(genericType, codecType, factoryDeclaringType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericCodecInfo">配置项</param>
    /// <exception cref="ArgumentException"></exception>
    public DsonCodecConfig AddGenericCodec(GenericCodecInfo genericCodecInfo) {
        if (genericCodecInfo.IsNull) throw new ArgumentException("codecInfo  is null");
        genericEncoderDic[genericCodecInfo.typeInfo] = genericCodecInfo;
        genericDecoderDic[genericCodecInfo.typeInfo] = genericCodecInfo;
        return this;
    }

    #endregion

    #region add-encoder

    /// <summary>
    /// 添加编码器
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public DsonCodecConfig AddGenericEncoder(Type genericType, Type codecType) {
        AddGenericEncoder(GenericCodecInfo.Create(genericType, codecType));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息，也是工厂类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    /// <returns>this</returns>
    public DsonCodecConfig AddGenericEncoder(Type genericType, Type codecType, string factoryFieldName) {
        AddGenericEncoder(GenericCodecInfo.Create(genericType, codecType, factoryFieldName));
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
    public DsonCodecConfig AddGenericEncoder(Type genericType, Type codecType, Type factoryDeclaringType, string factoryFieldName) {
        AddGenericEncoder(GenericCodecInfo.Create(genericType, codecType, factoryDeclaringType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericCodecInfo">配置项</param>
    /// <returns></returns>
    public DsonCodecConfig AddGenericEncoder(GenericCodecInfo genericCodecInfo) {
        if (genericCodecInfo.IsNull) throw new ArgumentException("codecInfo  is null");
        genericEncoderDic[genericCodecInfo.typeInfo] = genericCodecInfo;
        return this;
    }

    #endregion

    #region add-decoder

    /// <summary>
    /// 添加解码器
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    public DsonCodecConfig AddGenericDecoder(Type genericType, Type codecType) {
        AddGenericDecoder(GenericCodecInfo.Create(genericType, codecType));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息，也是工厂类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    public DsonCodecConfig AddGenericDecoder(Type genericType, Type codecType, string factoryFieldName) {
        AddGenericDecoder(GenericCodecInfo.Create(genericType, codecType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericType">泛型类的信息</param>
    /// <param name="codecType">编解码器类的信息</param>
    /// <param name="factoryDeclaringType">定义工厂字段的类</param>
    /// <param name="factoryFieldName">工厂字段的名字</param>
    public DsonCodecConfig AddGenericDecoder(Type genericType, Type codecType, Type factoryDeclaringType, string factoryFieldName) {
        AddGenericDecoder(GenericCodecInfo.Create(genericType, codecType, factoryDeclaringType, factoryFieldName));
        return this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="genericCodecInfo">配置项</param>
    /// <returns></returns>
    public DsonCodecConfig AddGenericDecoder(GenericCodecInfo genericCodecInfo) {
        if (genericCodecInfo.IsNull) throw new ArgumentException("codecInfo  is null");
        genericDecoderDic[genericCodecInfo.typeInfo] = genericCodecInfo;
        return this;
    }

    #endregion

    #endregion

    #region 其它

    /// <summary>
    /// 添加类型转换器
    /// </summary>
    public DsonCodecConfig AddCaster(IDsonCodecCaster caster) {
        if (caster == null) throw new ArgumentNullException(nameof(caster));
        this.casters.Add(caster);
        return this;
    }

    public DsonCodecConfig AddCasters(IEnumerable<IDsonCodecCaster> casters) {
        foreach (IDsonCodecCaster? caster in casters) {
            AddCaster(caster);
        }
        return this;
    }

    /// <summary>
    /// 泛型类请配置泛型原型类。
    /// (实现想不到合适的名字了...)
    /// </summary>
    /// <param name="encoderType">编码类型</param>
    /// <param name="declaredType">声明类型</param>
    /// <param name="val">是否可优化</param>
    /// <returns></returns>
    public DsonCodecConfig AddOptimizedType(Type encoderType, Type declaredType, bool val = true) {
        optimizedTypes[new TypePair(encoderType, declaredType)] = val;
        return this;
    }

    #endregion

    #region query

    public IDsonCodec? GetEncoder(Type typeInfo) {
        encoderDic.TryGetValue(typeInfo, out IDsonCodec codecImpl);
        return codecImpl;
    }

    public IDsonCodec? GetDecoder(Type typeInfo) {
        decoderDic.TryGetValue(typeInfo, out IDsonCodec codecImpl);
        return codecImpl;
    }

    public GenericCodecInfo? GetGenericEncoderInfo(Type genericTypeDefine) {
        if (genericEncoderDic.TryGetValue(genericTypeDefine, out GenericCodecInfo item)) {
            return item;
        }
        return null;
    }

    public GenericCodecInfo? GetGenericDecoderInfo(Type genericTypeDefine) {
        if (genericDecoderDic.TryGetValue(genericTypeDefine, out GenericCodecInfo item)) {
            return item;
        }
        return null;
    }

    #endregion

    #region 默认配置

    public static DsonCodecConfig Default { get; } = NewDefaultRegistry().ToImmutable();

    public static DsonCodecConfig NewDefaultRegistry() {
        DsonCodecConfig config = new DsonCodecConfig();
        InitDefaultCodecs(config);
        InitDefaultGenericCodecs(config);
        InitDefaultOptimizedTypes(config);
        return config;
    }

    private static void InitDefaultOptimizedTypes(DsonCodecConfig config) {
        config.AddOptimizedType(typeof(List<>), typeof(IList<>));
        config.AddOptimizedType(typeof(List<>), typeof(IReadOnlyList<>));
        config.AddOptimizedType(typeof(List<>), typeof(ICollection<>));
        config.AddOptimizedType(typeof(List<>), typeof(IReadOnlyCollection<>));
        // Set
        config.AddOptimizedType(typeof(HashSet<>), typeof(ISet<>));
        config.AddOptimizedType(typeof(HashSet<>), typeof(IReadOnlySet<>));
        // Map
        config.AddOptimizedType(typeof(Dictionary<,>), typeof(IDictionary<,>));
        config.AddOptimizedType(typeof(Dictionary<,>), typeof(IReadOnlyDictionary<,>));
        // 扩展集合
        config.AddOptimizedType(typeof(LinkedHashSet<>), typeof(IGenericSet<>));
        config.AddOptimizedType(typeof(LinkedHashSet<>), typeof(ISequencedSet<>));
        config.AddOptimizedType(typeof(LinkedDictionary<,>), typeof(IGenericDictionary<,>));
        config.AddOptimizedType(typeof(LinkedDictionary<,>), typeof(ISequencedDictionary<,>));
    }

    private static void InitDefaultGenericCodecs(DsonCodecConfig config) {
        // CollectionCodec默认测试了常见的集合类型
        config.AddGenericCodec(typeof(ICollection<>), typeof(CollectionCodec<>));
        config.AddGenericCodec(typeof(IList<>), typeof(CollectionCodec<>));
        config.AddGenericCodec(typeof(List<>), typeof(CollectionCodec<>));
        // 
        config.AddGenericCodec(typeof(ISet<>), typeof(CollectionCodec<>));
        config.AddGenericCodec(typeof(HashSet<>), typeof(CollectionCodec<>));
        config.AddGenericCodec(typeof(LinkedHashSet<>), typeof(CollectionCodec<>));
        //
        config.AddGenericCodec(typeof(Stack<>), typeof(MoreCollectionCodecs.StackCodec<>));
        config.AddGenericCodec(typeof(Queue<>), typeof(MoreCollectionCodecs.QueueCodec<>));

        // IDictionary接口不指定工厂，根据options动态分配实现
        config.AddGenericCodec(typeof(IDictionary<,>), typeof(DictionaryCodec<,>));
        config.AddGenericCodec(typeof(Dictionary<,>), typeof(DictionaryCodec<,>));
        config.AddGenericCodec(typeof(LinkedDictionary<,>), typeof(DictionaryCodec<,>));
        config.AddGenericCodec(typeof(ConcurrentDictionary<,>), typeof(DictionaryCodec<,>));
        // 特殊组件
        config.AddGenericCodec(typeof(DictionaryEncodeProxy<>), typeof(DictionaryEncodeProxyCodec<>));
        config.AddGenericCodec(typeof(Nullable<>), typeof(NullableCodec<>));

        // readonly
        config.AddGenericCodec(typeof(IReadOnlyCollection<>), typeof(EnumerableCodec<>));
        config.AddGenericCodec(typeof(IReadOnlyList<>), typeof(EnumerableCodec<>));
        config.AddGenericCodec(typeof(IEnumerable<>), typeof(EnumerableCodec<>));
    }

    private static void InitDefaultCodecs(DsonCodecConfig config) {
        // dson内建结构
        config.AddCodec(new Int32Codec());
        config.AddCodec(new Int64Codec());
        config.AddCodec(new FloatCodec());
        config.AddCodec(new DoubleCodec());
        config.AddCodec(new BoolCodec());
        config.AddCodec(new StringCodec());
        config.AddCodec(new BinaryCodec());
        config.AddCodec(new ObjectPtrCodec());
        config.AddCodec(new ObjectLitePtrCodec());
        config.AddCodec(new ExtDateTimeCodec());
        config.AddCodec(new TimestampCodec());
        // 基本类型补充
        config.AddCodec(new MorePrimitiveCodecs.UInt32Codec());
        config.AddCodec(new MorePrimitiveCodecs.UInt64Codec());
        config.AddCodec(new MorePrimitiveCodecs.ShortCodec());
        config.AddCodec(new MorePrimitiveCodecs.UShortCodec());
        config.AddCodec(new MorePrimitiveCodecs.ByteCodec());
        config.AddCodec(new MorePrimitiveCodecs.SByteCodec());
        config.AddCodec(new MorePrimitiveCodecs.CharCodec());
        // 日期时间
        config.AddCodec(new DateTimeCodec());
        config.AddCodec(new DateTimeOffsetCodec());

        // 基本类型数组
        config.AddCodec(new MoreArrayCodecs.ByteArrayCodec());
        config.AddCodec(new MoreArrayCodecs.IntArrayCodec());
        config.AddCodec(new MoreArrayCodecs.LongArrayCodec());
        config.AddCodec(new MoreArrayCodecs.FloatArrayCodec());
        config.AddCodec(new MoreArrayCodecs.DoubleArrayCodec());
        config.AddCodec(new MoreArrayCodecs.BoolArrayCodec());
        config.AddCodec(new MoreArrayCodecs.StringArrayCodec());
        config.AddCodec(new MoreArrayCodecs.UIntArrayCodec());
        config.AddCodec(new MoreArrayCodecs.ULongArrayCodec());

        // 特化List
        config.AddCodec(new MoreCollectionCodecs.IntListCodec(typeof(List<int>)));
        config.AddCodec(new MoreCollectionCodecs.LongListCodec(typeof(List<long>)));
        config.AddCodec(new MoreCollectionCodecs.FloatListCodec(typeof(List<float>)));
        config.AddCodec(new MoreCollectionCodecs.DoubleListCodec(typeof(List<double>)));
        config.AddCodec(new MoreCollectionCodecs.BoolListCodec(typeof(List<bool>)));
        config.AddCodec(new MoreCollectionCodecs.StringListCodec(typeof(List<string>)));
        config.AddCodec(new MoreCollectionCodecs.UIntListCodec(typeof(List<uint>)));
        config.AddCodec(new MoreCollectionCodecs.ULongListCodec(typeof(List<ulong>)));
        // 接口类型也支持下
        config.AddCodec(new MoreCollectionCodecs.IntListCodec(typeof(IList<int>)));
        config.AddCodec(new MoreCollectionCodecs.LongListCodec(typeof(IList<long>)));
        config.AddCodec(new MoreCollectionCodecs.FloatListCodec(typeof(IList<float>)));
        config.AddCodec(new MoreCollectionCodecs.DoubleListCodec(typeof(IList<double>)));
        config.AddCodec(new MoreCollectionCodecs.BoolListCodec(typeof(IList<bool>)));
        config.AddCodec(new MoreCollectionCodecs.StringListCodec(typeof(IList<string>)));
        config.AddCodec(new MoreCollectionCodecs.UIntListCodec(typeof(IList<uint>)));
        config.AddCodec(new MoreCollectionCodecs.ULongListCodec(typeof(IList<ulong>)));

        // TODO 特殊Codec绑定
    }

    #endregion
}
}