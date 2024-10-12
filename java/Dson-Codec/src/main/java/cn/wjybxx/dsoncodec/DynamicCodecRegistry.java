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
import cn.wjybxx.dsoncodec.codecs.EnumCodec;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import javax.annotation.Nullable;
import java.lang.reflect.Constructor;
import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.Supplier;

/**
 * 动态Codec注册表
 *
 * @author wjybxx
 * date - 2024/9/25
 */
public final class DynamicCodecRegistry implements DsonCodecRegistry {

    private static final Logger logger = LoggerFactory.getLogger(DynamicCodecRegistry.class);
    /** 用户的原始的类型Codec */
    private final DsonCodecRegistry basicRegistry;
    /** 类型转换器 */
    private final List<DsonCodecCaster> casters;
    /** 泛型codec配置 */
    private final GenericCodecConfig genericCodecConfig;
    /** 泛型Codec辅助工具类 */
    private final GenericCodecHelper genericCodecHelper;

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存 */
    private final ConcurrentHashMap<TypeInfo, DsonCodecImpl<?>> encoderDic = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<TypeInfo, DsonCodecImpl<?>> decoderDic = new ConcurrentHashMap<>();

    public DynamicCodecRegistry(DsonCodecRegistry basicRegistry,
                                List<? extends DsonCodecCaster> casters,
                                List<GenericCodecConfig> genericCodecConfigs,
                                GenericCodecHelper genericCodecHelper) {
        this.basicRegistry = Objects.requireNonNull(basicRegistry);
        this.casters = List.copyOf(casters);
        this.genericCodecHelper = Objects.requireNonNull(genericCodecHelper);

        // 先初始化为默认配置，然后由用户的配置进行覆盖 -- 不转不可变对象，性能更好
        this.genericCodecConfig = GenericCodecConfig.newDefaultConfig();
        for (GenericCodecConfig genericCodecConfig : genericCodecConfigs) {
            this.genericCodecConfig.mergeFrom(genericCodecConfig);
        }
    }

    /** 预添加Codec(可覆盖) */
    public void addCodec(DsonCodecImpl<?> codecImpl) {
        encoderDic.put(codecImpl.getEncoderType(), codecImpl);
        decoderDic.put(codecImpl.getEncoderType(), codecImpl);
    }

    /** 添加编码器 */
    public void addEncoder(DsonCodecImpl<?> codecImpl) {
        encoderDic.put(codecImpl.getEncoderType(), codecImpl);
    }

    /**
     * 添加编码器
     *
     * @param type      绑定的类型，需要是Codec绑定类型的子类
     * @param codecImpl 编码器
     */
    public <T> void addEncoder(TypeInfo type, DsonCodecImpl<? super T> codecImpl) {
        encoderDic.put(type, codecImpl);
    }

    /** 添加解码器 */
    public void addDecoder(DsonCodecImpl<?> codecImpl) {
        decoderDic.put(codecImpl.getEncoderType(), codecImpl);
    }

    @Nullable
    @Override
    public DsonCodecImpl<?> getEncoder(TypeInfo type) {
        // 优先查找用户的Codec，以允许用户定制优化
        DsonCodecImpl<?> codecImpl = basicRegistry.getEncoder(type);
        if (codecImpl != null) return codecImpl;

        // 查缓存
        codecImpl = encoderDic.get(type);
        if (codecImpl != null) return codecImpl;

        // 动态生成--java端无法处理泛型
        if (type.isEnum()) {
            codecImpl = makeEnumCodec(type);
        } else if (type.isArrayType()) {
            codecImpl = makeArrayCodec(type);
        } else if (type.isGenericType()) {
            codecImpl = makeGenericCodec(type, true);
        } else {
            // 尝试转换为超类
            Class<?> superType = castEncoderType(type.rawType);
            if (superType != null) {
                codecImpl = getEncoder(TypeInfo.of(superType, type.genericArgs));
            }
        }
        if (codecImpl != null) {
            encoderDic.putIfAbsent(type, codecImpl);
            // 可能是超类Encoder
            if (type == codecImpl.getEncoderType()) {
                decoderDic.putIfAbsent(type, codecImpl);
            }
        }
        return codecImpl;
    }

    @Nullable
    @Override
    public DsonCodecImpl<?> getDecoder(TypeInfo type) {
        // 优先查找用户的Codec，以允许用户定制优化
        DsonCodecImpl<?> codecImpl = basicRegistry.getDecoder(type);
        if (codecImpl != null) return codecImpl;

        // 查缓存
        codecImpl = decoderDic.get(type);
        if (codecImpl != null) return codecImpl;

        // 动态生成--java端无法处理泛型
        if (type.isEnum()) {
            codecImpl = makeEnumCodec(type);
        } else if (type.isArrayType()) {
            codecImpl = makeArrayCodec(type);
        } else if (type.isGenericType()) {
            codecImpl = makeGenericCodec(type, false);
        } else {
            // 尝试转换为子类
            Class<?> subType = castDecoderType(type.rawType);
            if (subType != null) {
                codecImpl = getDecoder(TypeInfo.of(subType, type.genericArgs));
            }
        }
        if (codecImpl != null) {
            // 可以解码的一定可以编码
            decoderDic.putIfAbsent(type, codecImpl);
            encoderDic.putIfAbsent(type, codecImpl);
        }
        return codecImpl;
    }

    private DsonCodecImpl<?> makeArrayCodec(TypeInfo type) {
        return new DsonCodecImpl<>(new ArrayCodec<>(type));
    }

    @SuppressWarnings({"unchecked", "rawtypes"})
    private DsonCodecImpl<?> makeEnumCodec(TypeInfo type) {
        return new DsonCodecImpl<>(new EnumCodec<>((Class) type.rawType)); // forget
    }

    private DsonCodecImpl<?> makeGenericCodec(TypeInfo type, boolean encoder) {
        GenericCodecInfo genericCodecInfo = encoder ? findGenericEncoder(type) : findGenericDecoder(type);
        if (genericCodecInfo == null) {
            return null; // 不存在对应的泛型类Codec
        }
        if (type.genericArgs.isEmpty()) { // 修正为擦除后的类型
            type = genericCodecInfo.typeInfo;
        }
        Class<?> genericCodecTypeDefine = genericCodecInfo.codecType;
        // 先尝试包含TypeInfo和Factory的构造函数
        try {
            Constructor<?> constructor = genericCodecTypeDefine.getConstructor(TypeInfo.class, Supplier.class);
            DsonCodec<?> codec = (DsonCodec<?>) constructor.newInstance(type, genericCodecInfo.factory);
            return new DsonCodecImpl<>(codec);
        } catch (NoSuchMethodException ignore) {

        } catch (ReflectiveOperationException ex) {
            logger.warn("create instance caught exception, type: " + type, ex);
            return null;
        }
        // 再尝试仅包含TypeInfo的构造函数
        try {
            Constructor<?> constructor = genericCodecTypeDefine.getConstructor(TypeInfo.class);
            DsonCodec<?> codec = (DsonCodec<?>) constructor.newInstance(type);
            return new DsonCodecImpl<>(codec);
        } catch (NoSuchMethodException ignore) {
            return null;
        } catch (ReflectiveOperationException ex) {
            logger.warn("create instance caught exception, type: " + type, ex);
            return null;
        }
    }

    private GenericCodecInfo findGenericEncoder(TypeInfo type) {
        GenericCodecInfo genericCodecInfo = genericCodecConfig.getEncoderInfo(type.rawType);
        if (genericCodecInfo != null) return genericCodecInfo;
        // 尝试转换为超类
        Class<?> superClazz = castEncoderType(type.rawType);
        if (superClazz != null) {
            return findGenericEncoder(TypeInfo.of(superClazz, type.genericArgs));
        }
        // 这段保底代码写在这里最为合适，放在用户的Config里还需要考虑冲突问题...
        // 兼容集合和字典
        if (Collection.class.isAssignableFrom(type.rawType)
                && genericCodecHelper.canInheritTypeArgs(type.rawType, Collection.class)) {
            return genericCodecConfig.getEncoderInfo(Collection.class);
        }
        if (Map.class.isAssignableFrom(type.rawType)
                && genericCodecHelper.canInheritTypeArgs(type.rawType, Map.class)) {
            return genericCodecConfig.getEncoderInfo(Map.class);
        }
        return null;
    }

    private GenericCodecInfo findGenericDecoder(TypeInfo type) {
        GenericCodecInfo genericCodecInfo = genericCodecConfig.getDecoderInfo(type.rawType);
        if (genericCodecInfo != null) return genericCodecInfo;
        // 尝试转换为超类或子类
        Class<?> superClazz = castDecoderType(type.rawType);
        if (superClazz != null) {
            return findGenericDecoder(TypeInfo.of(superClazz, type.genericArgs));
        }
        return null;
    }

    private <T> Class<? super T> castEncoderType(Class<T> clazz) {
        for (DsonCodecCaster caster : casters) {
            Class<? super T> superClazz = caster.castEncoderType(clazz, genericCodecHelper);
            if (superClazz != null && superClazz != clazz) { // fix用户返回当前类
                return superClazz;
            }
        }
        return null;
    }

    private Class<?> castDecoderType(Class<?> clazz) {
        for (DsonCodecCaster caster : casters) {
            Class<?> superClazz = caster.castDecoderType(clazz, genericCodecHelper);
            if (superClazz != null && superClazz != clazz) { // fix用户返回当前类
                return superClazz;
            }
        }
        return null;
    }

}
