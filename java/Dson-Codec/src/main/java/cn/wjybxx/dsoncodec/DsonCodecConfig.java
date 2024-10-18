/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.dsoncodec;

import cn.wjybxx.dsoncodec.codecs.*;

import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import java.util.function.Supplier;

/**
 * <h3>泛型类Codec</h3>
 * 由于泛型类的Codec不能被直接构造，因此只能先将其类型信息存储下来，在运行时根据泛型参数动态构造。
 * <p>
 * 1. Codec编码的类型和要编码的类型有相同的泛型参数列表。且必须包含接收{@link TypeInfo}的构造函数 --  可参考{@link CollectionCodec}。
 * 2. 如果Codec的是面向接口或抽象类的，构造函数还可接收一个{@link Supplier}的参数。
 * 3. 不会频繁查询，因此不必太在意匹配算法的效率。
 * 4. 数组和泛型是不同的，数组都对应{@link ArrayCodec}，因此不需要再这里存储。
 * 5. 请避免运行时修改数据，否则可能造成线程安全问题。
 * 6. 反射难以确定泛型擦参数除后的类型，为避免增加Codec和运行时的复杂度，泛型原型的信息在注册时指定。
 *
 * <h3>与TypeMetaConfig的关系</h3>
 * Codec与TypeMete在配置和运行时都是分离的，它们属于不同的体系；
 * 但Codec关联的encoderType必须在{@link TypeMetaConfig}中存在。
 *
 * <h3>合并规则</h3>
 * 多个Config合并时，越靠近用户，优先级越高 -- 因为这一定能解决冲突。
 *
 * @author wjybxx
 * date - 2024/10/11
 */
@SuppressWarnings({"rawtypes"})
public final class DsonCodecConfig {

    // 一个Type可能只有encoder而没有decoder，因此需要分开缓存
    /** 非泛型Codec，或预设的特殊泛型实例Codec */
    private final Map<TypeInfo, DsonCodec<?>> encoderDic;
    private final Map<TypeInfo, DsonCodec<?>> decoderDic;
    /** 泛型Codec */
    private final Map<Class<?>, GenericCodecInfo> genericEncoderDic;
    private final Map<Class<?>, GenericCodecInfo> genericDecoderDic;
    /** 类型转换器 */
    private final List<DsonCodecCaster> casters;
    /** 可忽略的类型信息 */
    private final Map<ClassPair, Boolean> optimizedTypes;
    /** java端专属，用于运行时从声明类型捕获泛型参数 */
    private final List<GenericHelper> genericHelpers;

    public DsonCodecConfig() {
        encoderDic = new HashMap<>(32);
        decoderDic = new HashMap<>(32);
        genericEncoderDic = new IdentityHashMap<>(16);
        genericDecoderDic = new IdentityHashMap<>(16);
        casters = new ArrayList<>(4);
        optimizedTypes = new HashMap<>(16);
        genericHelpers = new ArrayList<>(4);
    }

    private DsonCodecConfig(DsonCodecConfig other) {
        this.encoderDic = Map.copyOf(other.encoderDic);
        this.decoderDic = Map.copyOf(other.decoderDic);
        this.genericEncoderDic = Map.copyOf(other.genericEncoderDic);
        this.genericDecoderDic = Map.copyOf(other.genericDecoderDic);
        this.casters = List.copyOf(other.casters);
        this.optimizedTypes = Map.copyOf(other.optimizedTypes);
        this.genericHelpers = List.copyOf(other.genericHelpers);
    }

    public Map<TypeInfo, DsonCodec<?>> getEncoderDic() {
        return encoderDic;
    }

    public Map<TypeInfo, DsonCodec<?>> getDecoderDic() {
        return decoderDic;
    }

    public Map<Class<?>, GenericCodecInfo> getGenericEncoderDic() {
        return genericEncoderDic;
    }

    public Map<Class<?>, GenericCodecInfo> getGenericDecoderDic() {
        return genericDecoderDic;
    }

    public List<DsonCodecCaster> getCasters() {
        return casters;
    }

    public Map<ClassPair, Boolean> getOptimizedTypes() {
        return optimizedTypes;
    }

    public List<GenericHelper> getGenericHelpers() {
        return genericHelpers;
    }

    // region factory

    /** 根据codecs创建一个Config -- 返回的实例不可变 */
    public static DsonCodecConfig fromCodecs(DsonCodec<?>... codecs) {
        return fromCodecs(Arrays.asList(codecs));
    }

    /** 根据codecs创建一个Config -- 返回的实例不可变 */
    public static DsonCodecConfig fromCodecs(Collection<? extends DsonCodec<?>> codecs) {
        DsonCodecConfig result = new DsonCodecConfig();
        for (DsonCodec<?> codec : codecs) {
            result.addCodec(codec);
        }
        return result.toImmutable();
    }

    /** 合并多个Config为单个Config -- 返回的实例不可变 */
    public static DsonCodecConfig fromConfigs(Collection<? extends DsonCodecConfig> configs) {
        DsonCodecConfig result = new DsonCodecConfig();
        for (DsonCodecConfig other : configs) {
            result.mergeFrom(other);
        }
        return result.toImmutable();
    }

    /** 转换为不可变实例 */
    public DsonCodecConfig toImmutable() {
        if (encoderDic instanceof HashMap<TypeInfo, DsonCodec<?>>) {
            return new DsonCodecConfig(this);
        }
        return this;
    }

    // endregion

    /** 清理数据 */
    public void clear() {
        encoderDic.clear();
        decoderDic.clear();
        genericEncoderDic.clear();
        genericDecoderDic.clear();
        casters.clear();
        optimizedTypes.clear();
        genericHelpers.clear();
    }

    /** 合并配置 */
    public DsonCodecConfig mergeFrom(DsonCodecConfig other) {
        if (this == other) {
            throw new IllegalArgumentException();
        }
        encoderDic.putAll(other.encoderDic);
        decoderDic.putAll(other.decoderDic);
        genericEncoderDic.putAll(other.genericEncoderDic);
        genericDecoderDic.putAll(other.genericDecoderDic);
        casters.addAll(other.casters);
        optimizedTypes.putAll(other.optimizedTypes);
        genericHelpers.addAll(other.genericHelpers);
        return this;
    }

    // region 非泛型coded

    /** 配置编解码器 */
    public DsonCodecConfig addCodecs(DsonCodec<?>... codecs) {
        for (DsonCodec<?> codec : codecs) {
            addCodec(codec.getEncoderType(), codec);
        }
        return this;
    }

    /** 配置编解码器 */
    public DsonCodecConfig addCodecs(Collection<? extends DsonCodec<?>> codecs) {
        for (DsonCodec<?> codec : codecs) {
            addCodec(codec.getEncoderType(), codec);
        }
        return this;
    }

    /** 配置编解码器 */
    public DsonCodecConfig addCodec(DsonCodec<?> codec) {
        addCodec(codec.getEncoderType(), codec);
        return this;
    }

    /**
     * 配置编解码器
     * 适用超类Codec的默认解码实例可赋值给当前类型的情况，eg：IntList => IntCollectionCodec。
     */
    public <T> DsonCodecConfig addCodec(Class<T> clazz, DsonCodec<? super T> codec) {
        return addCodec(TypeInfo.of(clazz), codec);
    }

    /** 配置编解码器 */
    public DsonCodecConfig addCodec(TypeInfo typeInfo, DsonCodec<?> codec) {
        encoderDic.put(typeInfo, codec);
        decoderDic.put(typeInfo, codec);
        return this;
    }

    /** 配置编码器 */
    public <T> DsonCodecConfig addEncoder(Class<T> clazz, DsonCodec<? super T> codec) {
        addEncoder(TypeInfo.of(clazz), codec);
        return this;
    }

    /** 配置编码器 -- 适用已构造泛型 */
    public DsonCodecConfig addEncoder(TypeInfo typeInfo, DsonCodec<?> codec) {
        encoderDic.put(typeInfo, codec);
        return this;
    }

    /** 配置解码器 */
    public <T> DsonCodecConfig addDecoder(Class<T> clazz, DsonCodec<? extends T> codec) {
        addDecoder(TypeInfo.of(clazz), codec);
        return this;
    }

    /** 配置解码器 -- 适用已构造泛型 */
    public DsonCodecConfig addDecoder(TypeInfo typeInfo, DsonCodec<?> codec) {
        decoderDic.put(typeInfo, codec);
        return this;
    }

    /** 删除编码器 -- 用于解决冲突 */
    public DsonCodec<?> removeEncoder(Class<?> clazz) {
        return encoderDic.remove(TypeInfo.of(clazz));
    }

    /** 删除编码器 -- 适用已构造泛型 */
    public DsonCodec<?> removeEncoder(TypeInfo typeInfo) {
        return encoderDic.remove(typeInfo);
    }

    /** 删除解码器 -- 用于解决冲突 */
    public DsonCodec<?> removeDecoder(Class<?> clazz) {
        return decoderDic.remove(TypeInfo.of(clazz));
    }

    /** 删除解码器 -- 适用已构造泛型 */
    public DsonCodec<?> removeDecoder(TypeInfo typeInfo) {
        return decoderDic.remove(typeInfo);
    }

    // endregion

    // region 泛型codec

    // 进行重载以方便使用
    // region add-codec

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @return this
     */
    public DsonCodecConfig addGenericCodec(TypeInfo genericType, Class<? extends DsonCodec> codecType) {
        addGenericCodec(GenericCodecInfo.create(genericType, codecType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param implType    实现类类型
     * @return this
     */
    public DsonCodecConfig addGenericCodec(TypeInfo genericType, Class<? extends DsonCodec> codecType, Class<?> implType) {
        addGenericCodec(GenericCodecInfo.create(genericType, codecType, implType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param factory     实例工厂
     * @return this
     */
    public DsonCodecConfig addGenericCodec(TypeInfo genericType, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        addGenericCodec(GenericCodecInfo.create(genericType, codecType, factory));
        return this;
    }

    /**
     * 增加一个codec配置
     *
     * @param genericCodecInfo 编解码器类的信息
     */
    public DsonCodecConfig addGenericCodec(GenericCodecInfo genericCodecInfo) {
        Objects.requireNonNull(genericCodecInfo);
        genericEncoderDic.put(genericCodecInfo.typeInfo.rawType, genericCodecInfo);
        genericDecoderDic.put(genericCodecInfo.typeInfo.rawType, genericCodecInfo);
        return this;
    }
    // endregion

    // region add-encoder

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @return this
     */
    public DsonCodecConfig addGenericEncoder(TypeInfo genericType, Class<? extends DsonCodec> codecType) {
        addGenericEncoder(GenericCodecInfo.create(genericType, codecType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param implType    实现类类型
     * @return this
     */
    public DsonCodecConfig addGenericEncoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Class<?> implType) {
        addGenericEncoder(GenericCodecInfo.create(genericType, codecType, implType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param factory     实例工厂
     * @return this
     */
    public DsonCodecConfig addGenericEncoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        addGenericEncoder(GenericCodecInfo.create(genericType, codecType, factory));
        return this;
    }

    /**
     * 添加编码器
     *
     * @param genericCodecInfo 编解码器类的信息
     * @return this
     */
    public DsonCodecConfig addGenericEncoder(GenericCodecInfo genericCodecInfo) {
        Objects.requireNonNull(genericCodecInfo);
        genericEncoderDic.put(genericCodecInfo.typeInfo.rawType, genericCodecInfo);
        return this;
    }
    // endregion

    // region add-decoder

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @return this
     */
    public DsonCodecConfig addGenericDecoder(TypeInfo genericType, Class<? extends DsonCodec> codecType) {
        addGenericDecoder(GenericCodecInfo.create(genericType, codecType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param implType    实现类类型
     * @return this
     */
    public DsonCodecConfig addGenericDecoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Class<?> implType) {
        addGenericDecoder(GenericCodecInfo.create(genericType, codecType, implType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param factory     实例工厂
     * @return this
     */
    public DsonCodecConfig addGenericDecoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        addGenericDecoder(GenericCodecInfo.create(genericType, codecType, factory));
        return this;
    }

    /**
     * 添加解码器
     *
     * @param genericCodecInfo 编解码器类的信息
     * @return this
     */
    public DsonCodecConfig addGenericDecoder(GenericCodecInfo genericCodecInfo) {
        genericDecoderDic.put(genericCodecInfo.typeInfo.rawType, genericCodecInfo);
        return this;
    }

    // endregion
    // endregion

    // region 其它

    /** 添加类型转换器 */
    public DsonCodecConfig addCaster(DsonCodecCaster caster) {
        this.casters.add(caster);
        return this;
    }

    public DsonCodecConfig addCasters(Collection<? extends DsonCodecCaster> casters) {
        this.casters.addAll(casters);
        return this;
    }

    /**
     * 添加可优化编码类型（无需写入类型信息的情况）
     *
     * @param encoderType  编码类型
     * @param declaredType 声明类型
     */
    public <T> DsonCodecConfig addOptimizedType(Class<T> encoderType, Class<? super T> declaredType) {
        optimizedTypes.put(new ClassPair(encoderType, declaredType), true);
        return this;
    }

    /**
     * 添加可优化编码类型（无需写入类型信息的情况）
     * 泛型类请配置泛型原型类
     *
     * @param encoderType  编码类型
     * @param declaredType 声明类型
     * @param val          是否可优化
     */
    public <T> DsonCodecConfig addOptimizedType(Class<T> encoderType, Class<? super T> declaredType, boolean val) {
        optimizedTypes.put(new ClassPair(encoderType, declaredType), val);
        return this;
    }

    /** 添加泛型工具类 */
    public DsonCodecConfig addGenericHelper(GenericHelper genericHelper) {
        this.genericHelpers.add(genericHelper);
        return this;
    }

    /** 添加泛型工具类 */
    public DsonCodecConfig addGenericHelpers(Collection<? extends GenericHelper> genericHelpers) {
        this.genericHelpers.addAll(genericHelpers);
        return this;
    }

    // endregion

    // region query

    public DsonCodec<?> getEncoder(TypeInfo typeInfo) {
        return encoderDic.get(typeInfo);
    }

    public DsonCodec<?> getDecoder(TypeInfo typeInfo) {
        return decoderDic.get(typeInfo);
    }

    public GenericCodecInfo getGenericEncoderInfo(Class<?> genericTypeDefine) {
        return genericEncoderDic.get(genericTypeDefine);
    }

    public GenericCodecInfo getGenericDecoderInfo(Class<?> genericTypeDefine) {
        return genericDecoderDic.get(genericTypeDefine);
    }

    // endregion

    // default实例

    /** 全局默认配置 */
    public static final DsonCodecConfig DEFAULT = newDefaultRegistry().toImmutable();

    /** 创建一个默认配置 */
    public static DsonCodecConfig newDefaultRegistry() {
        DsonCodecConfig config = new DsonCodecConfig();
        initDefaultCodecs(config);
        initDefaultGenericCodecs(config);
        initDefaultOptimizedTypes(config);
        return config;
    }

    private static void initDefaultOptimizedTypes(DsonCodecConfig config) {
        // List
        config.addOptimizedType(ArrayList.class, List.class);
        config.addOptimizedType(ArrayList.class, Collection.class);
        // Set
        config.addOptimizedType(HashSet.class, Set.class);
        config.addOptimizedType(LinkedHashSet.class, Set.class);
        config.addOptimizedType(LinkedHashSet.class, HashSet.class);
        // Map
        config.addOptimizedType(HashMap.class, Map.class);
        config.addOptimizedType(LinkedHashMap.class, Map.class);
        config.addOptimizedType(LinkedHashMap.class, HashMap.class);
    }

    private static void initDefaultGenericCodecs(DsonCodecConfig config) {
        config.addGenericCodec(TypeInfo.of(Collection.class, Object.class), CollectionCodec.class, ArrayList.class);
        config.addGenericCodec(TypeInfo.of(List.class, Object.class), CollectionCodec.class, ArrayList.class);
        config.addGenericCodec(TypeInfo.of(ArrayList.class, Object.class), CollectionCodec.class, ArrayList.class);
        config.addGenericCodec(TypeInfo.of(LinkedList.class, Object.class), CollectionCodec.class, LinkedList.class);
        config.addGenericCodec(TypeInfo.of(ArrayDeque.class, Object.class), CollectionCodec.class, ArrayDeque.class);

        // Set -- 如果是接口类型，则默认保持有序；如果是具体类型，则默认具体类型
        config.addGenericCodec(TypeInfo.of(Set.class, Object.class), CollectionCodec.class, LinkedHashSet.class);
        config.addGenericCodec(TypeInfo.of(HashSet.class, Object.class), CollectionCodec.class, HashSet.class);
        config.addGenericCodec(TypeInfo.of(LinkedHashSet.class, Object.class), CollectionCodec.class, LinkedHashSet.class);
        config.addGenericCodec(TypeInfo.of(EnumSet.class, Enum.class), CollectionCodec.class); // EnumSet需要动态构建

        // Map -- 如果是接口类型，则默认保持有序
        config.addGenericCodec(TypeInfo.of(Map.class, Object.class, Object.class), MapCodec.class, LinkedHashMap.class);
        config.addGenericCodec(TypeInfo.of(HashMap.class, Object.class, Object.class), MapCodec.class, HashMap.class);
        config.addGenericCodec(TypeInfo.of(LinkedHashMap.class, Object.class, Object.class), MapCodec.class, LinkedHashMap.class);
        config.addGenericCodec(TypeInfo.of(EnumMap.class, Enum.class, Object.class), MapCodec.class); // EnumSet需要动态构建
        config.addGenericCodec(TypeInfo.of(ConcurrentMap.class, Object.class, Object.class), MapCodec.class, ConcurrentHashMap.class);
        config.addGenericCodec(TypeInfo.of(ConcurrentHashMap.class, Object.class, Object.class), MapCodec.class, ConcurrentHashMap.class);

        // 特殊组件
        config.addGenericCodec(TypeInfo.of(MapEncodeProxy.class, Object.class), MapEncodeProxyCodec.class);
    }

    private static void initDefaultCodecs(DsonCodecConfig config) {
        config.addCodec(new Int32Codec());
        config.addCodec(new Int64Codec());
        config.addCodec(new FloatCodec());
        config.addCodec(new DoubleCodec());
        config.addCodec(new BooleanCodec());
        config.addCodec(new StringCodec());
        config.addCodec(new BinaryCodec());
        config.addCodec(new ObjectPtrCodec());
        config.addCodec(new ObjectLitePtrCodec());
        config.addCodec(new ExtDateTimeCodec());
        config.addCodec(new TimestampCodec());
        // 基本类型补充
        config.addCodec(new MorePrimitiveCodecs.ShortCodec());
        config.addCodec(new MorePrimitiveCodecs.ByteCodec());
        config.addCodec(new MorePrimitiveCodecs.CharacterCodec());
        // 基本类型补充
        config.addCodec(int.class, new Int32Codec());
        config.addCodec(long.class, new Int64Codec());
        config.addCodec(float.class, new FloatCodec());
        config.addCodec(double.class, new DoubleCodec());
        config.addCodec(boolean.class, new BooleanCodec());
        config.addCodec(short.class, new MorePrimitiveCodecs.ShortCodec());
        config.addCodec(byte.class, new MorePrimitiveCodecs.ByteCodec());
        config.addCodec(char.class, new MorePrimitiveCodecs.CharacterCodec());
        // 基本类型数组
        config.addCodec(new MoreArrayCodecs.ByteArrayCodec());
        config.addCodec(new MoreArrayCodecs.IntArrayCodec());
        config.addCodec(new MoreArrayCodecs.LongArrayCodec());
        config.addCodec(new MoreArrayCodecs.FloatArrayCodec());
        config.addCodec(new MoreArrayCodecs.DoubleArrayCodec());
        config.addCodec(new MoreArrayCodecs.BooleanArrayCodec());
        config.addCodec(new MoreArrayCodecs.StringArrayCodec());
        config.addCodec(new MoreArrayCodecs.ShortArrayCodec());
        config.addCodec(new MoreArrayCodecs.CharArrayCodec());

        // 日期时间
        config.addCodec(new LocalDateTimeCodec());
        config.addCodec(new LocalDateCodec());
        config.addCodec(new LocalTimeCodec());
        config.addCodec(new InstantCodec());
        config.addCodec(new DurationCodec());
        // TODO 特殊Codec绑定
    }
    // endregion
}