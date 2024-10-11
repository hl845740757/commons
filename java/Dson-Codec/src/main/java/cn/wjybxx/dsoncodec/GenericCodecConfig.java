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

import cn.wjybxx.dsoncodec.codecs.ArrayCodec;
import cn.wjybxx.dsoncodec.codecs.CollectionCodec;
import cn.wjybxx.dsoncodec.codecs.MapCodec;
import cn.wjybxx.dsoncodec.codecs.MapEncodeProxyCodec;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;
import java.util.function.Supplier;

/**
 * 泛型类到泛型类的Codec的类型映射。
 * 由于泛型类的Codec不能被直接构造，因此只能先将其类型信息存储下来，待到确定泛型参数类型的时候再构造。
 * 考虑到泛型的反射构建较为复杂，因此我们不采用Type => Factory 的形式来配置，而是配置对应的Codec原型类；
 * 这可能增加类的数量，但代码的复杂度更低，更易于使用。
 * <p>
 * 注意：
 * 1. Codec编码的类型和要编码的类型有相同的泛型参数列表。且必须包含接收{@link TypeInfo}的构造函数 --  可参考{@link CollectionCodec}。
 * 2. 如果Codec的是面向接口或抽象类的，构造函数还可接收一个{@link Supplier}的参数。
 * 3. 不会频繁查询，因此不必太在意匹配算法的效率。
 * 4. 数组和泛型是不同的，数组都对应{@link ArrayCodec}，因此不需要再这里存储。
 * 5. 请避免运行时修改数据，否则可能造成线程安全问题。
 * 6. 反射难以确定泛型擦参数除后的类型，为避免增加Codec和运行时的复杂度，泛型原型的信息在注册时指定。
 *
 * @author wjybxx
 * date - 2024/9/25
 */
@NotThreadSafe
@SuppressWarnings("rawtypes")
public final class GenericCodecConfig {

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存 */
    private final Map<Class<?>, GenericCodecInfo> encoderTypeDic;
    private final Map<Class<?>, GenericCodecInfo> decoderTypeDic;

    public GenericCodecConfig() {
        encoderTypeDic = new IdentityHashMap<>();
        decoderTypeDic = new IdentityHashMap<>();
    }

    private GenericCodecConfig(Map<Class<?>, GenericCodecInfo> encoderTypeDic,
                               Map<Class<?>, GenericCodecInfo> decoderTypeDic) {
        this.encoderTypeDic = encoderTypeDic;
        this.decoderTypeDic = decoderTypeDic;
    }

    /** 清理数据 */
    public void clear() {
        encoderTypeDic.clear();
        decoderTypeDic.clear();
    }

    /** 转换为不可变配置 */
    public GenericCodecConfig toImmutable() {
        return new GenericCodecConfig(Map.copyOf(encoderTypeDic), Map.copyOf(decoderTypeDic));
    }

    /** 创建一个默认配置 */
    public static GenericCodecConfig newDefaultConfig() {
        return new GenericCodecConfig().initWithDefaults();
    }

    /** 通过默认的泛型类Codec初始化 */
    public GenericCodecConfig initWithDefaults() {
        addCodec(TypeInfo.of(Collection.class, Object.class), CollectionCodec.class, ArrayList.class);
        // List
        addCodec(TypeInfo.of(List.class, Object.class), CollectionCodec.class, ArrayList.class);
        addCodec(TypeInfo.of(ArrayList.class, Object.class), CollectionCodec.class, ArrayList.class);
        //
        addCodec(TypeInfo.of(LinkedList.class, Object.class), CollectionCodec.class, LinkedList.class);
        addCodec(TypeInfo.of(ArrayDeque.class, Object.class), CollectionCodec.class, ArrayDeque.class);
        // Set -- 如果是接口类型，则默认保持有序；如果是具体类型，则默认具体类型
        addCodec(TypeInfo.of(Set.class, Object.class), CollectionCodec.class, LinkedHashSet.class);
        addCodec(TypeInfo.of(HashSet.class, Object.class), CollectionCodec.class, HashSet.class);
        addCodec(TypeInfo.of(LinkedHashSet.class, Object.class), CollectionCodec.class, LinkedHashSet.class);
        // Map -- 如果是接口类型，则默认保持有序
        addCodec(TypeInfo.of(Map.class, Object.class, Object.class), MapCodec.class, LinkedHashMap.class);
        addCodec(TypeInfo.of(HashMap.class, Object.class, Object.class), MapCodec.class, HashMap.class);
        addCodec(TypeInfo.of(LinkedHashMap.class, Object.class, Object.class), MapCodec.class, LinkedHashMap.class);
        //
        addCodec(TypeInfo.of(ConcurrentMap.class, Object.class, Object.class), MapCodec.class, ConcurrentHashMap.class);
        addCodec(TypeInfo.of(ConcurrentHashMap.class, Object.class, Object.class), MapCodec.class, ConcurrentHashMap.class);
        // 特殊组件
        addCodec(TypeInfo.of(MapEncodeProxy.class, Object.class), MapEncodeProxyCodec.class);
        return this;
    }

    /** 主要用于合并注解处理器生成的Config */
    public GenericCodecConfig addCodecs(GenericCodecConfig otherConfig) {
        for (GenericCodecInfo genericCodecInfo : otherConfig.encoderTypeDic.values()) {
            addEncoder(genericCodecInfo);
        }
        for (GenericCodecInfo genericCodecInfo : otherConfig.decoderTypeDic.values()) {
            addDecoder(genericCodecInfo);
        }
        return this;
    }

    // 进行重载以方便使用
    // region add-codec

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @return this
     */
    public GenericCodecConfig addCodec(TypeInfo genericType, Class<? extends DsonCodec> codecType) {
        addCodec(GenericCodecInfo.create(genericType, codecType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param implType    实现类类型
     * @return this
     */
    public GenericCodecConfig addCodec(TypeInfo genericType, Class<? extends DsonCodec> codecType, Class<?> implType) {
        addCodec(GenericCodecInfo.create(genericType, codecType, implType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param factory     实例工厂
     * @return this
     */
    public GenericCodecConfig addCodec(TypeInfo genericType, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        addCodec(GenericCodecInfo.create(genericType, codecType, factory));
        return this;
    }

    /**
     * 增加一个codec配置
     *
     * @param genericCodecInfo 编解码器类的信息
     */
    public GenericCodecConfig addCodec(GenericCodecInfo genericCodecInfo) {
        Objects.requireNonNull(genericCodecInfo);
        encoderTypeDic.put(genericCodecInfo.typeInfo.rawType, genericCodecInfo);
        decoderTypeDic.put(genericCodecInfo.typeInfo.rawType, genericCodecInfo);
        return this;
    }
    // endregion

    // region add-encoder

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @return this
     */
    public GenericCodecConfig addEncoder(TypeInfo genericType, Class<? extends DsonCodec> codecType) {
        addEncoder(GenericCodecInfo.create(genericType, codecType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param implType    实现类类型
     * @return this
     */
    public GenericCodecConfig addEncoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Class<?> implType) {
        addEncoder(GenericCodecInfo.create(genericType, codecType, implType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param factory     实例工厂
     * @return this
     */
    public GenericCodecConfig addEncoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        addEncoder(GenericCodecInfo.create(genericType, codecType, factory));
        return this;
    }

    /**
     * 添加编码器
     *
     * @param genericCodecInfo 编解码器类的信息
     * @return this
     */
    public GenericCodecConfig addEncoder(GenericCodecInfo genericCodecInfo) {
        Objects.requireNonNull(genericCodecInfo);
        encoderTypeDic.put(genericCodecInfo.typeInfo.rawType, genericCodecInfo);
        return this;
    }
    // endregion

    // region add-decoder

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @return this
     */
    public GenericCodecConfig addDecoder(TypeInfo genericType, Class<? extends DsonCodec> codecType) {
        addDecoder(GenericCodecInfo.create(genericType, codecType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param implType    实现类类型
     * @return this
     */
    public GenericCodecConfig addDecoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Class<?> implType) {
        addDecoder(GenericCodecInfo.create(genericType, codecType, implType));
        return this;
    }

    /**
     * @param genericType 泛型定义类
     * @param codecType   解码器类型
     * @param factory     实例工厂
     * @return this
     */
    public GenericCodecConfig addDecoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        addDecoder(GenericCodecInfo.create(genericType, codecType, factory));
        return this;
    }

    /**
     * 添加解码器
     *
     * @param genericCodecInfo 编解码器类的信息
     * @return this
     */
    public GenericCodecConfig addDecoder(GenericCodecInfo genericCodecInfo) {
        Objects.requireNonNull(genericCodecInfo);
        decoderTypeDic.put(genericCodecInfo.typeInfo.rawType, genericCodecInfo);
        return this;
    }

    // endregion

    /** 查询编码器类 */
    public GenericCodecInfo getEncoderInfo(Class<?> genericTypeDefine) {
        return encoderTypeDic.get(genericTypeDefine);
    }

    /** 查询解码器类 */
    public GenericCodecInfo getDecoderInfo(Class<?> genericTypeDefine) {
        return decoderTypeDic.get(genericTypeDefine);
    }

}
