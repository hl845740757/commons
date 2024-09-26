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
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;

/**
 * @author wjybxx
 * date - 2024/9/25
 */
public class DynamicDsonCodecRegistry implements DsonCodecRegistry {

    /** 用户的原始的类型Codec */
    private final DsonCodecRegistry basicRegistry;

    /** 一个Type可能只有encoder而没有decoder，因此需要分开缓存 */
    private final ConcurrentHashMap<Class<?>, DsonCodecImpl<?>> encoderDic = new ConcurrentHashMap<>();
    private final ConcurrentHashMap<Class<?>, DsonCodecImpl<?>> decoderDic = new ConcurrentHashMap<>();

    public DynamicDsonCodecRegistry(DsonCodecRegistry basicRegistry) {
        this.basicRegistry = Objects.requireNonNull(basicRegistry);
    }

    /** 预添加Codec */
    public void addCodec(DsonCodecImpl<?> codecImpl) {
        encoderDic.putIfAbsent(codecImpl.getEncoderClass(), codecImpl);
        decoderDic.putIfAbsent(codecImpl.getEncoderClass(), codecImpl);
    }

    /** 添加编码器 */
    public void addEncoder(DsonCodecImpl<?> codecImpl) {
        encoderDic.putIfAbsent(codecImpl.getEncoderClass(), codecImpl);
    }

    /**
     * 添加编码器
     *
     * @param type      绑定的类型，需要是Codec绑定类型的子类
     * @param codecImpl 编码器
     */
    public <T> void addEncoder(Class<T> type, DsonCodecImpl<? super T> codecImpl) {
        encoderDic.putIfAbsent(type, codecImpl);
    }

    /** 添加解码器 */
    public void addDecoder(DsonCodecImpl<?> codecImpl) {
        decoderDic.putIfAbsent(codecImpl.getEncoderClass(), codecImpl);
    }

    @SuppressWarnings("unchecked")
    @Nullable
    @Override
    public <T> DsonCodecImpl<? super T> getEncoder(TypeInfo type, DsonCodecRegistry rootRegistry) {
        // 优先查找用户的Codec，以允许用户定制优化
        DsonCodecImpl<? super T> codecImpl = basicRegistry.getEncoder(type, rootRegistry);
        if (codecImpl != null) return codecImpl;

        // 查缓存
        codecImpl = (DsonCodecImpl<? super T>) encoderDic.get(type);
        if (codecImpl != null) return codecImpl;

        // 动态生成--java端无法处理泛型
        if (type.isArray()) {
            codecImpl = MakeArrayCodec(type);
        } else if (type.isEnum()) {
            codecImpl = MakeEnumCodec(type);
        }
        if (codecImpl != null) {
            encoderDic.putIfAbsent(type, codecImpl);
            // 可能是超类Encoder
            if (type == codecImpl.getEncoderClass()) {
                decoderDic.putIfAbsent(type, codecImpl);
            }
        }
        return codecImpl;
    }

    @SuppressWarnings("unchecked")
    @Nullable
    @Override
    public <T> DsonCodecImpl<T> getDecoder(TypeInfo type, DsonCodecRegistry rootRegistry) {
        // 优先查找用户的Codec，以允许用户定制优化
        DsonCodecImpl<T> codecImpl = basicRegistry.getDecoder(type, rootRegistry);
        if (codecImpl != null) return codecImpl;

        // 查缓存
        codecImpl = (DsonCodecImpl<T>) decoderDic.get(type);
        if (codecImpl != null) return codecImpl;

        // 动态生成--java端无法处理泛型
        if (type.isArray()) {
            codecImpl = MakeArrayCodec(type);
        } else if (type.isEnum()) {
            codecImpl = MakeEnumCodec(type);
        }
        if (codecImpl != null) {
            decoderDic.putIfAbsent(type, codecImpl);
            encoderDic.putIfAbsent(type, codecImpl);
        }
        return codecImpl;
    }

    private <T> DsonCodecImpl<T> MakeArrayCodec(Class<T> type) {
        return new DsonCodecImpl<>(new ArrayCodec<>(type));
    }

    @SuppressWarnings({"unchecked", "rawtypes"})
    private <T> DsonCodecImpl<T> MakeEnumCodec(Class<T> type) {
        return new DsonCodecImpl<>(new EnumCodec<>((Class) type)); // forget
    }
}
