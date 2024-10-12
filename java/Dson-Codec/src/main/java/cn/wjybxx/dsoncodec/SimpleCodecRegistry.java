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

import javax.annotation.Nullable;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

/**
 * 注意：这里不适用泛型类的Codec，泛型类型的配置请使用{@link GenericCodecConfig}
 *
 * @author wjybxx
 * date - 2024/10/11
 */
public final class SimpleCodecRegistry implements DsonCodecRegistry {

    private final Map<TypeInfo, DsonCodecImpl<?>> encoderDic;
    private final Map<TypeInfo, DsonCodecImpl<?>> decoderDic;

    public SimpleCodecRegistry() {
        encoderDic = new HashMap<>();
        decoderDic = new HashMap<>();
    }

    public SimpleCodecRegistry(Map<TypeInfo, DsonCodecImpl<?>> encoderDic,
                               Map<TypeInfo, DsonCodecImpl<?>> decoderDic) {
        this.encoderDic = Map.copyOf(encoderDic);
        this.decoderDic = Map.copyOf(decoderDic);
    }

    /** 根据codecs创建一个Registry -- 返回的实例不可变 */
    public static SimpleCodecRegistry fromCodecs(List<? extends DsonCodec<?>> codecs) {
        SimpleCodecRegistry result = new SimpleCodecRegistry();
        for (DsonCodec<?> codec : codecs) {
            result.addCodec(codec);
        }
        return result.toImmutable();
    }

    /** 转换为不可变实例 */
    public SimpleCodecRegistry toImmutable() {
        return new SimpleCodecRegistry(encoderDic, decoderDic);
    }

    /** 清理数据 */
    public void clear() {
        encoderDic.clear();
        decoderDic.clear();
    }

    /** 合并配置 */
    public SimpleCodecRegistry mergeFrom(SimpleCodecRegistry other) {
        encoderDic.putAll(other.encoderDic);
        decoderDic.putAll(other.decoderDic);
        return this;
    }

    /** 配置编解码器 */
    public SimpleCodecRegistry addCodec(DsonCodec<?> codec) {
        addCodec(codec.getEncoderType(), codec);
        return this;
    }

    /**
     * 配置编解码器
     * 适用超类Codec的默认解码实例可赋值给当前类型的情况，eg：IntList => IntCollectionCodec。
     */
    public <T> SimpleCodecRegistry addCodec(Class<T> clazz, DsonCodec<? super T> codec) {
        return addCodec(TypeInfo.of(clazz), codec);
    }

    /** 配置编解码器 */
    public SimpleCodecRegistry addCodec(TypeInfo typeInfo, DsonCodec<?> codec) {
        DsonCodecImpl<?> codecImpl = new DsonCodecImpl<>(codec);
        encoderDic.put(typeInfo, codecImpl);
        decoderDic.put(typeInfo, codecImpl);
        return this;
    }

    /** 配置编码器 */
    public <T> SimpleCodecRegistry addEncoder(Class<T> clazz, DsonCodec<? super T> codec) {
        addEncoder(TypeInfo.of(clazz), codec);
        return this;
    }

    /** 配置编码器 -- 适用已构造泛型 */
    public SimpleCodecRegistry addEncoder(TypeInfo typeInfo, DsonCodec<?> codec) {
        DsonCodecImpl<?> codecImpl = new DsonCodecImpl<>(codec);
        encoderDic.put(typeInfo, codecImpl);
        return this;
    }

    /** 配置解码器 */
    public <T> SimpleCodecRegistry addDecoder(Class<T> clazz, DsonCodec<? extends T> codec) {
        addDecoder(TypeInfo.of(clazz), codec);
        return this;
    }

    /** 配置解码器 -- 适用已构造泛型 */
    public SimpleCodecRegistry addDecoder(TypeInfo typeInfo, DsonCodec<?> codec) {
        DsonCodecImpl<?> codecImpl = new DsonCodecImpl<>(codec);
        decoderDic.put(typeInfo, codecImpl);
        return this;
    }

    @Nullable
    @Override
    public DsonCodecImpl<?> getEncoder(TypeInfo typeInfo) {
        return encoderDic.get(typeInfo);
    }

    @Override
    public DsonCodecImpl<?> getDecoder(TypeInfo typeInfo) {
        return decoderDic.get(typeInfo);
    }

}
