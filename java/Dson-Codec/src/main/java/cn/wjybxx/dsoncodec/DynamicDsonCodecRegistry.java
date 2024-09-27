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

import javax.annotation.Nullable;
import java.lang.reflect.Constructor;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;

/**
 * @author wjybxx
 * date - 2024/9/25
 */
public class DynamicDsonCodecRegistry implements DsonCodecRegistry {

    /** 用户的原始的类型Codec */
    private final DsonCodecRegistry basicRegistry;
    /** 泛型codec配置 */
    private final IGenericCodecConfig genericCodecConfig;

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存 */
    private final ConcurrentHashMap<TypeInfo, DsonCodecImpl<?>> encoderDic = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<TypeInfo, DsonCodecImpl<?>> decoderDic = new ConcurrentHashMap<>();

    public DynamicDsonCodecRegistry(DsonCodecRegistry basicRegistry, IGenericCodecConfig genericCodecConfig) {
        this.basicRegistry = Objects.requireNonNull(basicRegistry);
        this.genericCodecConfig = Objects.requireNonNull(genericCodecConfig);
    }

    /** 预添加Codec */
    public void addCodec(DsonCodecImpl<?> codecImpl) {
        encoderDic.putIfAbsent(codecImpl.getEncoderType(), codecImpl);
        decoderDic.putIfAbsent(codecImpl.getEncoderType(), codecImpl);
    }

    /** 添加编码器 */
    public void addEncoder(DsonCodecImpl<?> codecImpl) {
        encoderDic.putIfAbsent(codecImpl.getEncoderType(), codecImpl);
    }

    /**
     * 添加编码器
     *
     * @param type      绑定的类型，需要是Codec绑定类型的子类
     * @param codecImpl 编码器
     */
    public <T> void addEncoder(TypeInfo type, DsonCodecImpl<? super T> codecImpl) {
        encoderDic.putIfAbsent(type, codecImpl);
    }

    /** 添加解码器 */
    public void addDecoder(DsonCodecImpl<?> codecImpl) {
        decoderDic.putIfAbsent(codecImpl.getEncoderType(), codecImpl);
    }

    @Nullable
    @Override
    public DsonCodecImpl<?> getEncoder(TypeInfo type, DsonCodecRegistry rootRegistry, IGenericCodecHelper genericCodecHelper) {
        // 优先查找用户的Codec，以允许用户定制优化
        DsonCodecImpl<?> codecImpl = basicRegistry.getEncoder(type, rootRegistry, genericCodecHelper);
        if (codecImpl != null) return codecImpl;

        // 查缓存
        codecImpl = encoderDic.get(type);
        if (codecImpl != null) return codecImpl;

        // 动态生成--java端无法处理泛型
        if (type.isArrayType()) {
            codecImpl = makeArrayCodec(type);
        } else if (type.isGenericType()) {
            codecImpl = makeGenericCodec(type, true, genericCodecHelper);
        } else if (type.isEnum()) {
            codecImpl = makeEnumCodec(type);
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
    public DsonCodecImpl<?> getDecoder(TypeInfo type, DsonCodecRegistry rootRegistry, IGenericCodecHelper genericCodecHelper) {
        // 优先查找用户的Codec，以允许用户定制优化
        DsonCodecImpl<?> codecImpl = basicRegistry.getDecoder(type, rootRegistry, genericCodecHelper);
        if (codecImpl != null) return codecImpl;

        // 查缓存
        codecImpl = decoderDic.get(type);
        if (codecImpl != null) return codecImpl;

        // 动态生成--java端无法处理泛型
        if (type.isArrayType()) {
            codecImpl = makeArrayCodec(type);
        } else if (type.isGenericType()) {
            codecImpl = makeGenericCodec(type, false, genericCodecHelper);
        } else if (type.isEnum()) {
            codecImpl = makeEnumCodec(type);
        }
        if (codecImpl != null) {
            decoderDic.putIfAbsent(type, codecImpl);
            encoderDic.putIfAbsent(type, codecImpl);
        }
        return codecImpl;
    }

    private <T> DsonCodecImpl<T> makeArrayCodec(TypeInfo type) {
        return new DsonCodecImpl<>(new ArrayCodec<>(type));
    }

    @SuppressWarnings({"unchecked", "rawtypes"})
    private DsonCodecImpl<?> makeEnumCodec(TypeInfo type) {
        return new DsonCodecImpl<>(new EnumCodec<>((Class) type.rawType)); // forget
    }

    private DsonCodecImpl<?> makeGenericCodec(TypeInfo type, boolean encoder, IGenericCodecHelper genericCodecHelper) {
        Class<?> genericCodecTypeDefine = encoder
                ? genericCodecConfig.getEncoderType(type.rawType, genericCodecHelper)
                : genericCodecConfig.getDecoderType(type.rawType, genericCodecHelper);
        if (genericCodecTypeDefine == null) {
            return null; // 未配置泛型对应的Codec类型
        }
        try {
            // 需要有一个包含TypeInfo的构造函数
            Constructor<?> constructor = genericCodecTypeDefine.getConstructor(TypeInfo.class);
            DsonCodec<?> codec = (DsonCodec<?>) constructor.newInstance(type);
            return new DsonCodecImpl<>(codec);
        } catch (ReflectiveOperationException ignore) {
            return null;
        }
    }
}
