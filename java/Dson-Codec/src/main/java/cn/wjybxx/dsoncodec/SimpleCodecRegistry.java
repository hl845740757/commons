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
import java.util.*;

/**
 * @author wjybxx
 * date - 2024/10/11
 */
public final class SimpleCodecRegistry implements DsonCodecRegistry {

    private final Map<TypeInfo, DsonCodecImpl<?>> encoderDic;
    private final Map<TypeInfo, DsonCodecImpl<?>> decoderDic;
    private final List<GenericCodecConfig> genericCodecConfigs;
    private final List<DsonCodecCaster> casters;

    public SimpleCodecRegistry() {
        encoderDic = new HashMap<>(32);
        decoderDic = new HashMap<>(32);
        genericCodecConfigs = new ArrayList<>();
        casters = new ArrayList<>();
    }

    private SimpleCodecRegistry(SimpleCodecRegistry other, boolean immutable) {
        if (immutable) {
            this.encoderDic = Map.copyOf(other.encoderDic);
            this.decoderDic = Map.copyOf(other.decoderDic);
            this.genericCodecConfigs = List.copyOf(other.genericCodecConfigs);
            this.casters = List.copyOf(other.casters);
        } else {
            this.encoderDic = new HashMap<>(other.encoderDic);
            this.decoderDic = new HashMap<>(other.decoderDic);
            this.genericCodecConfigs = new ArrayList<>(other.genericCodecConfigs);
            this.casters = new ArrayList<>(other.casters);
        }
    }

    public Map<TypeInfo, DsonCodecImpl<?>> getEncoderDic() {
        return encoderDic;
    }

    public Map<TypeInfo, DsonCodecImpl<?>> getDecoderDic() {
        return decoderDic;
    }

    public List<GenericCodecConfig> getGenericCodecConfigs() {
        return genericCodecConfigs;
    }

    public List<DsonCodecCaster> getCasters() {
        return casters;
    }

    // region factory

    /** 根据codecs创建一个Registry -- 返回的实例不可变 */
    public static SimpleCodecRegistry fromCodecs(DsonCodec<?>... codecs) {
        return fromCodecs(Arrays.asList(codecs));
    }

    /** 根据codecs创建一个Registry -- 返回的实例不可变 */
    public static SimpleCodecRegistry fromCodecs(Collection<? extends DsonCodec<?>> codecs) {
        SimpleCodecRegistry result = new SimpleCodecRegistry();
        for (DsonCodec<?> codec : codecs) {
            result.addCodec(codec);
        }
        return result.toImmutable();
    }

    /** 根据codecs创建一个Registry -- 返回的实例不可变 */
    public static SimpleCodecRegistry fromRegistries(Collection<? extends DsonCodecRegistry> registries) {
        SimpleCodecRegistry result = new SimpleCodecRegistry();
        for (DsonCodecRegistry other : registries) {
            result.mergeFrom(other);
        }
        return result.toImmutable();
    }

    /** 转换为不可变实例 */
    public SimpleCodecRegistry toImmutable() {
        return new SimpleCodecRegistry(this, true);
    }

    // endregion

    /** 清理数据 */
    public void clear() {
        encoderDic.clear();
        decoderDic.clear();
        genericCodecConfigs.clear();
        casters.clear();
    }

    /** 合并配置 */
    public SimpleCodecRegistry mergeFrom(DsonCodecRegistry registry) {
        SimpleCodecRegistry other = registry instanceof SimpleCodecRegistry
                ? (SimpleCodecRegistry) registry : registry.export();
        encoderDic.putAll(other.encoderDic);
        decoderDic.putAll(other.decoderDic);
        genericCodecConfigs.addAll(other.genericCodecConfigs);
        casters.addAll(other.casters);
        return this;
    }

    /** 添加泛型codec配置 */
    public SimpleCodecRegistry addGenericCodecConfig(GenericCodecConfig genericCodecConfig) {
        Objects.requireNonNull(genericCodecConfig);
        this.genericCodecConfigs.add(genericCodecConfig);
        return this;
    }

    /** 添加泛型codec配置 */
    public SimpleCodecRegistry addGenericCodecConfigs(Collection<? extends GenericCodecConfig> genericCodecConfigs) {
        for (GenericCodecConfig genericCodecConfig : genericCodecConfigs) {
            addGenericCodecConfig(genericCodecConfig);
        }
        return this;
    }

    /** 添加类型转换器 */
    public SimpleCodecRegistry addCaster(DsonCodecCaster caster) {
        Objects.requireNonNull(caster);
        this.casters.add(caster);
        return this;
    }

    public SimpleCodecRegistry addCasters(Collection<? extends DsonCodecCaster> casters) {
        for (DsonCodecCaster caster : casters) {
            addCaster(caster);
        }
        return this;
    }

    // region add-codec

    /** 配置编解码器 */
    public SimpleCodecRegistry addCodecs(Collection<? extends DsonCodec<?>> codecs) {
        for (DsonCodec<?> codec : codecs) {
            addCodec(codec.getEncoderType(), codec);
        }
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

    // 合并其它Registry
    public SimpleCodecRegistry addEncoder(TypeInfo typeInfo, DsonCodecImpl<?> codecImpl) {
        encoderDic.put(typeInfo, codecImpl);
        return this;
    }

    /** 配置解码器 -- 适用已构造泛型 */
    public SimpleCodecRegistry addDecoder(TypeInfo typeInfo, DsonCodecImpl<?> codecImpl) {
        decoderDic.put(typeInfo, codecImpl);
        return this;
    }

    // endregion

    @Nullable
    @Override
    public DsonCodecImpl<?> getEncoder(TypeInfo typeInfo) {
        return encoderDic.get(typeInfo);
    }

    @Override
    public DsonCodecImpl<?> getDecoder(TypeInfo typeInfo) {
        return decoderDic.get(typeInfo);
    }

    @Override
    public SimpleCodecRegistry export() {
        return new SimpleCodecRegistry(this, false);
    }
}