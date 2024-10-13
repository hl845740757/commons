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
import java.util.Set;
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
    private final SimpleCodecRegistry basicRegistry;
    /** 泛型codec配置 */
    private final GenericCodecConfig genericCodecConfig;
    /** 类型转换器 */
    private final List<DsonCodecCaster> casters;
    /** 泛型Codec辅助工具类 */
    private final GenericHelper genericHelper;

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存 */
    private final ConcurrentHashMap<TypeInfo, DsonCodecImpl<?>> encoderDic = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<TypeInfo, DsonCodecImpl<?>> decoderDic = new ConcurrentHashMap<>();

    public DynamicCodecRegistry(List<DsonCodecRegistry> basicRegistries) {
        this.basicRegistry = new SimpleCodecRegistry();
        for (DsonCodecRegistry other : basicRegistries) {
            this.basicRegistry.mergeFrom(other.export());
        }
        // 先初始化为默认配置，然后由用户的配置进行覆盖 -- 不转不可变对象，性能更好
        this.genericCodecConfig = GenericCodecConfig.newDefaultConfig();
        for (GenericCodecConfig genericCodecConfig : basicRegistry.getGenericCodecConfigs()) {
            this.genericCodecConfig.mergeFrom(genericCodecConfig);
        }
        this.casters = List.copyOf(basicRegistry.getCasters());
        this.genericHelper = new GenericHelper();
    }

    /** 需要暴露给Converter */
    public GenericHelper getGenericHelper() {
        return genericHelper;
    }

    @Override
    public SimpleCodecRegistry export() {
        SimpleCodecRegistry result = new SimpleCodecRegistry();
        result.mergeFrom(basicRegistry);
        result.getEncoderDic().putAll(encoderDic);
        result.getDecoderDic().putAll(decoderDic);
        return result;
    }

    /** 预添加Codec(可覆盖) */
    public void addCodec(DsonCodec<?> codec) {
        DsonCodecImpl<?> codecImpl = new DsonCodecImpl<>(codec);
        encoderDic.put(codecImpl.getEncoderType(), codecImpl);
        decoderDic.put(codecImpl.getEncoderType(), codecImpl);
    }

    /**
     * 预添加Codec
     * 适用超类Codec的默认解码实例可赋值给当前类型的情况，eg：IntList => IntCollectionCodec。
     */
    public void addCodec(TypeInfo type, DsonCodec<?> codec) {
        DsonCodecImpl<?> codecImpl = new DsonCodecImpl<>(codec);
        encoderDic.put(type, codecImpl);
        decoderDic.put(type, codecImpl);
    }

    /**
     * 添加编码器
     *
     * @param type  要编码的类型
     * @param codec 编码器，codec关联的encoderType是目标类型的超类
     */
    public void addEncoder(TypeInfo type, DsonCodec<?> codec) {
        DsonCodecImpl<?> codecImpl = new DsonCodecImpl<>(codec);
        encoderDic.put(type, codecImpl);
    }

    /**
     * 添加解码器
     *
     * @param type  要解码的类型
     * @param codec 编码器，codec关联的encoderType是目标类型的子类
     */
    public void addDecoder(TypeInfo type, DsonCodec<?> codec) {
        DsonCodecImpl<?> codecImpl = new DsonCodecImpl<>(codec);
        decoderDic.put(type, codecImpl);
    }

    @Nullable
    @Override
    public DsonCodecImpl<?> getEncoder(final TypeInfo type) {
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
            GenericCodecInfo genericCodecInfo = genericCodecConfig.getEncoderInfo(type.rawType);
            if (genericCodecInfo != null) {
                codecImpl = makeGenericCodec(type, genericCodecInfo);
            } else {
                // 尝试转换为超类编码，写入超类的TypeInfo
                Class<?> superType = castEncoderType(type.rawType);
                if (superType != null) {
                    codecImpl = getEncoder(TypeInfo.of(superType, type.genericArgs));
                }
            }
        } else {
            // 非泛型类，也尝试转换为超类，写入超类的TypeInfo
            Class<?> superType = castEncoderType(type.rawType);
            if (superType != null) {
                codecImpl = getEncoder(TypeInfo.of(superType, type.genericArgs));
            }
        }
        if (codecImpl != null) {
            encoderDic.putIfAbsent(type, codecImpl);
            // 可能是超类Encoder
            if (type.rawType == codecImpl.getEncoderType().rawType) {
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
            GenericCodecInfo genericCodecInfo = genericCodecConfig.getDecoderInfo(type.rawType);
            if (genericCodecInfo != null) {
                codecImpl = makeGenericCodec(type, genericCodecInfo);
            } else {
                // 尝试转换为子类解码，解码不涉及到写入TypeInfo
                Class<?> subType = castDecoderType(type.rawType);
                if (subType != null) {
                    codecImpl = getDecoder(TypeInfo.of(subType, type.genericArgs));
                }
            }
        } else {
            // 尝试转换为子类解码，解码不涉及到写入TypeInfo
            Class<?> subType = castDecoderType(type.rawType);
            if (subType != null) {
                codecImpl = getDecoder(TypeInfo.of(subType, type.genericArgs));
            }
        }
        if (codecImpl != null) {
            decoderDic.putIfAbsent(type, codecImpl);
            // 可能是子类Decoder
            if (type.rawType == codecImpl.getEncoderType().rawType) {
                encoderDic.putIfAbsent(type, codecImpl);
            }
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

    private DsonCodecImpl<?> makeGenericCodec(TypeInfo type, GenericCodecInfo genericCodecInfo) {
        assert type.rawType == genericCodecInfo.typeInfo.rawType;
        if (type.genericArgs.isEmpty()) { // 参数可能是丢失了泛型参数的原始类型
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

        } catch (ReflectiveOperationException ex) {
            logger.warn("create instance caught exception, type: " + type, ex);
            return null;
        }
        throw new DsonCodecException("bad generic codec: " + genericCodecTypeDefine);
    }


    private Class<?> castEncoderType(Class<?> clazz) {
        // caster逆向迭代，越靠近用户优先级越高，才能保证一定能解决冲突
        for (int i = casters.size() - 1; i >= 0; i--) {
            DsonCodecCaster caster = casters.get(i);
            Class<?> superClazz = caster.castEncoderType(clazz, genericHelper);
            if (superClazz != null && superClazz != clazz) { // fix用户返回当前类
                return superClazz;
            }
        }
        // 这段保底代码写在这里最为合适，放在用户的Config里还需要考虑冲突问题...
        // 具体到抽象
        if (List.class.isAssignableFrom(clazz)
                && genericHelper.canInheritTypeArgs(clazz, List.class)) {
            return List.class;
        }
        if (Set.class.isAssignableFrom(clazz)
                && genericHelper.canInheritTypeArgs(clazz, Set.class)) {
            return Set.class;
        }
        if (Collection.class.isAssignableFrom(clazz)
                && genericHelper.canInheritTypeArgs(clazz, Collection.class)) {
            return Collection.class;
        }
        if (Map.class.isAssignableFrom(clazz)
                && genericHelper.canInheritTypeArgs(clazz, Map.class)) {
            return Map.class;
        }
        return null;
    }

    private Class<?> castDecoderType(Class<?> clazz) {
        // caster逆向迭代，越靠近用户优先级越高，才能保证一定能解决冲突
        for (int i = casters.size() - 1; i >= 0; i--) {
            DsonCodecCaster caster = casters.get(i);
            Class<?> superClazz = caster.castDecoderType(clazz, genericHelper);
            if (superClazz != null && superClazz != clazz) { // fix用户返回当前类
                return superClazz;
            }
        }
        return null;
    }

}
