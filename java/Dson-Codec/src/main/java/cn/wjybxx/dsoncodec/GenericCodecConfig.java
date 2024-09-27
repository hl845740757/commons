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

import cn.wjybxx.dsoncodec.codecs.CollectionCodec;
import cn.wjybxx.dsoncodec.codecs.MapCodec;
import cn.wjybxx.dsoncodec.codecs.MapEncodeProxyCodec;

import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

/**
 * @author wjybxx
 * date - 2024/9/25
 */
@SuppressWarnings("rawtypes")
public class GenericCodecConfig implements IGenericCodecConfig {

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存 */
    protected final Map<Class<?>, Class<? extends DsonCodec>> encoderTypeDic = new IdentityHashMap<>();
    protected final Map<Class<?>, Class<? extends DsonCodec>> decoderTypeDic = new IdentityHashMap<>();

    public GenericCodecConfig() {
    }

    /** 创建一个默认配置 */
    public static GenericCodecConfig newDefaultConfig() {
        return new GenericCodecConfig().initWithDefaults();
    }

    /** 清理数据 */
    public void clear() {
        encoderTypeDic.clear();
        decoderTypeDic.clear();
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
    public void addCodec(Class<?> genericType, Class<? extends DsonCodec> codecType) {
        if (genericType == null) throw new IllegalArgumentException("genericType");
        if (codecType == null) throw new IllegalArgumentException("codecType");
        // java端可以不如此严格，可以更灵活
//        if (genericType.getTypeParameters().length != codecType.getTypeParameters().length) {
//            throw new IllegalArgumentException("genericType.GenericTypeArguments.Length != codecType.GenericTypeArguments.Length");
//        }
        encoderTypeDic.put(genericType, codecType);
        decoderTypeDic.put(genericType, codecType);
    }

    /**
     * 添加编码器
     *
     * @param genericType 泛型类的信息
     * @param codecType   编解码器类的信息
     */
    public void addEncoder(Class<?> genericType, Class<? extends DsonCodec> codecType) {
        encoderTypeDic.put(genericType, codecType);
    }

    /**
     * 添加解码器
     *
     * @param genericType 泛型类的信息
     * @param codecType   编解码器类的信息
     */
    public void addDecoder(Class<?> genericType, Class<? extends DsonCodec> codecType) {
        decoderTypeDic.put(genericType, codecType);
    }

    /** 允许子类重写该方法以实现更多的匹配 */
    @Override
    public Class<?> getEncoderType(Class<?> genericTypeDefine, IGenericCodecHelper genericCodecHelper) {
        Class<?> codecType = encoderTypeDic.get(genericTypeDefine);
        if (codecType != null) return codecType;
        // 兼容集合和字典 -- 编码时需要是集合类型的子类
        if (Collection.class.isAssignableFrom(genericTypeDefine)
                && genericCodecHelper.canInheritTypeArgs(genericTypeDefine, Collection.class)) {
            return encoderTypeDic.get(Collection.class);
        }
        if (Map.class.isAssignableFrom(genericTypeDefine)
                && genericCodecHelper.canInheritTypeArgs(genericTypeDefine, Map.class)) {
            return encoderTypeDic.get(Map.class);
        }
        return null;
    }

    @Override
    public Class<?> getDecoderType(Class<?> genericTypeDefine, IGenericCodecHelper genericCodecHelper) {
        Class<? extends DsonCodec> codecType = decoderTypeDic.get(genericTypeDefine);
        if (codecType != null) return codecType;
        // 兼容集合和字典 -- 解码时需要是默认解码类型的超类
        if (genericTypeDefine.isAssignableFrom(ArrayList.class)) {
            return encoderTypeDic.get(Collection.class);
        }
        if (genericTypeDefine.isAssignableFrom(LinkedHashMap.class)) {
            return encoderTypeDic.get(Map.class);
        }
        return null;
    }
}
