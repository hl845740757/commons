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
    /** 用户配置信息 */
    private final DsonCodecConfig config;
    /** 类型转换器 */
    private final List<DsonCodecCaster> casters;

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存 */
    private final ConcurrentHashMap<TypeInfo, DsonCodecImpl<?>> encoderDic = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<TypeInfo, DsonCodecImpl<?>> decoderDic = new ConcurrentHashMap<>();

    public DynamicCodecRegistry(DsonCodecConfig config) {
        config = config.toImmutable();
        this.config = config;
        this.casters = config.getCasters();

        // 构建DsonCodecImpl实例
        config.getEncoderDic().forEach((typeInfo, dsonCodec) -> {
            encoderDic.put(typeInfo, new DsonCodecImpl<>(dsonCodec));
        });
        config.getDecoderDic().forEach((typeInfo, dsonCodec) -> {
            decoderDic.put(typeInfo, new DsonCodecImpl<>(dsonCodec));
        });
    }


    @Nullable
    @Override
    public DsonCodecImpl<?> getEncoder(final TypeInfo type) {
        DsonCodecImpl<?> codecImpl = encoderDic.get(type);
        if (codecImpl != null) return codecImpl;

        // 动态生成--java端无法处理泛型
        if (type.isEnum()) {
            codecImpl = makeEnumCodec(type);
        } else if (type.isArrayType()) {
            codecImpl = makeArrayCodec(type);
        } else if (type.isGenericType()) {
            GenericCodecInfo genericCodecInfo = config.getGenericEncoderInfo(type.rawType);
            if (genericCodecInfo != null) {
                codecImpl = makeGenericCodec(type, genericCodecInfo);
            } else {
                // 尝试转换为超类编码，写入超类的TypeInfo
                TypeInfo superType = castEncoderType(type);
                if (superType != null) {
                    codecImpl = getEncoder(superType);
                }
            }
        } else {
            // 非泛型类，也尝试转换为超类，写入超类的TypeInfo
            TypeInfo superType = castEncoderType(type);
            if (superType != null) {
                codecImpl = getEncoder(superType);
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
        DsonCodecImpl<?> codecImpl = decoderDic.get(type);
        if (codecImpl != null) return codecImpl;

        // 动态生成--java端无法处理泛型
        if (type.isEnum()) {
            codecImpl = makeEnumCodec(type);
        } else if (type.isArrayType()) {
            codecImpl = makeArrayCodec(type);
        } else if (type.isGenericType()) {
            GenericCodecInfo genericCodecInfo = config.getGenericDecoderInfo(type.rawType);
            if (genericCodecInfo != null) {
                codecImpl = makeGenericCodec(type, genericCodecInfo);
            } else {
                // 尝试转换为子类解码，解码不涉及到写入TypeInfo
                TypeInfo subType = castDecoderType(type);
                if (subType != null) {
                    codecImpl = getDecoder(subType);
                }
            }
        } else {
            // 尝试转换为子类解码，解码不涉及到写入TypeInfo
            TypeInfo subType = castDecoderType(type);
            if (subType != null) {
                codecImpl = getDecoder(subType);
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
        // 参数可能是丢失了泛型参数的原始类型，亦或是逻辑错误导致泛型参数个数不匹配
        if (type.genericArgs.size() != genericCodecInfo.typeInfo.genericArgs.size()) {
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

    private TypeInfo castEncoderType(TypeInfo type) {
        // caster逆向迭代，越靠近用户优先级越高，才能保证一定能解决冲突
        Class<?> rawType = type.rawType;
        for (int i = casters.size() - 1; i >= 0; i--) {
            DsonCodecCaster caster = casters.get(i);
            TypeInfo superType = caster.castEncoderType(type);
            if (superType == null) continue;
            return superType.rawType == rawType ? null : superType; // fix用户返回当前类
        }
        // 这段保底代码写在这里最为合适，放在用户的Config里还需要考虑冲突问题...
        // 这里其实也需要测试是否可以继承泛型参数，但不想增加复杂度了，用户通过Caster解决
        if (List.class.isAssignableFrom(rawType)) {
            return TypeInfo.of(List.class, type.genericArgs);
        }
        if (Set.class.isAssignableFrom(rawType)) {
            return TypeInfo.of(Set.class, type.genericArgs);
        }
        if (Collection.class.isAssignableFrom(rawType)) {
            return TypeInfo.of(Collection.class, type.genericArgs);
        }
        if (Map.class.isAssignableFrom(rawType)) {
            return TypeInfo.of(Map.class, type.genericArgs);
        }
        return null;
    }

    private TypeInfo castDecoderType(TypeInfo type) {
        // caster逆向迭代，越靠近用户优先级越高，才能保证一定能解决冲突
        for (int i = casters.size() - 1; i >= 0; i--) {
            DsonCodecCaster caster = casters.get(i);
            TypeInfo subType = caster.castDecoderType(type);
            if (subType == null) continue;
            return subType.rawType == type.rawType ? null : subType; // fix用户返回当前类
        }
        return null;
    }

}
