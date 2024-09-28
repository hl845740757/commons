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

/**
 * 泛型类到泛型类的Codec的类型映射。
 * 由于泛型类的Codec不能被直接构造，因此只能先将其类型信息存储下来，待到确定泛型参数类型的时候再构造。
 * 考虑到泛型的反射构建较为复杂，因此我们不采用Type => Factory 的形式来配置，而是配置对应的Codec原型类；
 * 这可能增加类的数量，但代码的复杂度更低，更易于使用。
 * <p>
 * 注意：
 * 1. Codec编码的类型和要编码的类型有相同的泛型参数列表，比如：{@code Map<K,V>}和{@code LinkedHashMap<K,V>}，且构造函数接收一个{@link TypeInfo}参数。
 * 2. 不会频繁查询，因此不必太在意匹配算法的效率。
 * 3. 数组和泛型是不同的，数组都对应{@link ArrayCodec}，因此不需要再这里存储。
 * 4. 请避免运行时修改数据，否则可能造成线程安全问题。
 *
 * @author wjybxx
 * date - 2024/9/25
 */
@NotThreadSafe
@SuppressWarnings("rawtypes")
public final class GenericCodecConfig {

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存 */
    private final Map<Class<?>, Class<? extends DsonCodec>> encoderTypeDic;
    private final Map<Class<?>, Class<? extends DsonCodec>> decoderTypeDic;

    public GenericCodecConfig() {
        encoderTypeDic = new IdentityHashMap<>();
        decoderTypeDic = new IdentityHashMap<>();
    }

    private GenericCodecConfig(Map<Class<?>, Class<? extends DsonCodec>> encoderTypeDic,
                               Map<Class<?>, Class<? extends DsonCodec>> decoderTypeDic) {
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
        addCodec(Collection.class, CollectionCodec.class);
        addCodec(List.class, CollectionCodec.class);
        addCodec(ArrayList.class, CollectionCodec.class);
        addCodec(ArrayDeque.class, CollectionCodec.class);

        addCodec(Set.class, CollectionCodec.class);
        addCodec(HashSet.class, CollectionCodec.class);
        addCodec(LinkedHashSet.class, CollectionCodec.class);

        addCodec(Map.class, MapCodec.class);
        addCodec(HashMap.class, MapCodec.class);
        addCodec(LinkedHashMap.class, MapCodec.class);
        addCodec(ConcurrentHashMap.class, MapCodec.class);

        // 特殊组件
        addCodec(MapEncodeProxy.class, MapEncodeProxyCodec.class);
        return this;
    }

    /** 主要用于合并注解处理器生成的Config */
    public void addCodecs(GenericCodecConfig otherConfig) {
        for (Map.Entry<Class<?>, Class<? extends DsonCodec>> pair : otherConfig.encoderTypeDic.entrySet()) {
            addEncoder(pair.getKey(), pair.getValue());
        }
        for (Map.Entry<Class<?>, Class<? extends DsonCodec>> pair : otherConfig.decoderTypeDic.entrySet()) {
            addDecoder(pair.getKey(), pair.getValue());
        }
    }

    /**
     * 增加一个配置
     *
     * @param genericType 泛型类的信息
     * @param codecType   编解码器类的信息
     */
    public GenericCodecConfig addCodec(Class<?> genericType, Class<? extends DsonCodec> codecType) {
        if (genericType.getTypeParameters().length == 0) {
            throw new IllegalArgumentException("genericType is not IsGenericType");
        }
        // java端可以不如此严格，可以更灵活
//        if (genericType.getTypeParameters().length != codecType.getTypeParameters().length) {
//            throw new IllegalArgumentException("genericType.GenericTypeArguments.Length != codecType.GenericTypeArguments.Length");
//        }
        encoderTypeDic.put(genericType, codecType);
        decoderTypeDic.put(genericType, codecType);
        return this;
    }

    /**
     * 添加编码器
     *
     * @param genericType 泛型类的信息
     * @param codecType   编解码器类的信息
     * @return this
     */
    public GenericCodecConfig addEncoder(Class<?> genericType, Class<? extends DsonCodec> codecType) {
        if (genericType.getTypeParameters().length == 0) {
            throw new IllegalArgumentException("genericType is not IsGenericType");
        }
        encoderTypeDic.put(genericType, codecType);
        return this;
    }

    /**
     * 添加解码器
     *
     * @param genericType 泛型类的信息
     * @param codecType   编解码器类的信息
     * @return this
     */
    public GenericCodecConfig addDecoder(Class<?> genericType, Class<? extends DsonCodec> codecType) {
        if (genericType.getTypeParameters().length == 0) {
            throw new IllegalArgumentException("genericType is not IsGenericType");
        }
        decoderTypeDic.put(genericType, codecType);
        return this;
    }

    /** 查询编码器类 */
    public Class<? extends DsonCodec> getEncoderType(Class<?> genericTypeDefine) {
        return encoderTypeDic.get(genericTypeDefine);
    }

    /** 查询解码器类 */
    public Class<? extends DsonCodec> getDecoderType(Class<?> genericTypeDefine) {
        return decoderTypeDic.get(genericTypeDefine);
    }
}
