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

import java.util.ArrayList;
import java.util.Arrays;
import java.util.Collection;
import java.util.List;

/**
 * @author wjybxx
 * date - 2024/9/28
 */
public class DsonConverterBuilder {

    private final SimpleTypeMetaRegistry tempTypeMetaRegistry = new SimpleTypeMetaRegistry();
    private final SimpleCodecRegistry tempCodecRegistry = new SimpleCodecRegistry(); // 用于简化build

    private final List<TypeMetaRegistry> typeMetaRegistries = new ArrayList<>(4);
    private final List<DsonCodecRegistry> codecRegistries = new ArrayList<>(4);
    private GenericHelper genericHelper;

    private ConverterOptions options = ConverterOptions.DEFAULT;
    private boolean pureMode = false;

    public DsonConverterBuilder() {
        typeMetaRegistries.add(tempTypeMetaRegistry);
        codecRegistries.add(tempCodecRegistry);
    }

    public DsonConverter build() {
        if (!pureMode) {
            typeMetaRegistries.addLast(DsonConverterUtils.getDefaultTypeMetaRegistry());
        }
        DynamicTypeMetaRegistry dynamicTypeMetaRegistry = new DynamicTypeMetaRegistry(typeMetaRegistries);
        if (!pureMode) {
            typeMetaRegistries.removeLast();
        }
        if (genericHelper == null) {
            genericHelper = new GenericHelper();
        }
        DynamicCodecRegistry dynamicCodecRegistry;
        codecRegistries.add(DsonConverterUtils.getDefaultCodecRegistry());
        {
            dynamicCodecRegistry = new DynamicCodecRegistry(codecRegistries);
        }
        codecRegistries.removeLast();

        return new DefaultDsonConverter(dynamicTypeMetaRegistry,
                dynamicCodecRegistry,
                genericHelper,
                options);
    }

    /**
     * 是否纯净模式
     * 纯净模式是指{@link #build()}时不使用默认的{@link TypeMeta}，完全由用户分配。
     * 主要用于解析跨语言Dson文本。
     */
    public boolean isPureMode() {
        return pureMode;
    }

    public DsonConverterBuilder setPureMode(boolean pureMode) {
        this.pureMode = pureMode;
        return this;
    }

    // region type-meta

    public List<TypeMetaRegistry> getTypeMetaRegistries() {
        return typeMetaRegistries;
    }

    public DsonConverterBuilder addTypeMetaRegistry(TypeMetaRegistry typeMetaRegistry) {
        this.typeMetaRegistries.add(typeMetaRegistry);
        return this;
    }

    public DsonConverterBuilder addTypeMetaRegistries(Collection<? extends TypeMetaRegistry> typeMetaRegistries) {
        this.typeMetaRegistries.addAll(typeMetaRegistries);
        return this;
    }

    public DsonConverterBuilder addTypeMetas(Collection<TypeMeta> typeMetas) {
        tempTypeMetaRegistry.addAll(typeMetas);
        return this;
    }

    public DsonConverterBuilder addTypeMetas(TypeMeta... typeMetas) {
        tempTypeMetaRegistry.addAll(Arrays.asList(typeMetas));
        return this;
    }

    public DsonConverterBuilder addTypeMeta(TypeMeta typeMeta) {
        tempTypeMetaRegistry.add(typeMeta);
        return this;
    }

    // endregion

    // region codec-registry
    public List<DsonCodecRegistry> getCodecRegistries() {
        return codecRegistries;
    }

    public DsonConverterBuilder addCodecRegistry(DsonCodecRegistry codecRegistry) {
        this.codecRegistries.add(codecRegistry);
        return this;
    }

    public DsonConverterBuilder addCodecRegistries(Collection<? extends DsonCodecRegistry> codecRegistries) {
        this.codecRegistries.addAll(codecRegistries);
        return this;
    }

    public <T> DsonConverterBuilder addCodec(Class<T> clazz, DsonCodec<? super T> codec) {
        tempCodecRegistry.addCodec(clazz, codec);
        return this;
    }

    public DsonConverterBuilder addCodec(DsonCodec<?> codec) {
        tempCodecRegistry.addCodec(codec);
        return this;
    }

    public DsonConverterBuilder addCodecs(DsonCodec<?>... codecs) {
        tempCodecRegistry.addCodecs(Arrays.asList(codecs));
        return this;
    }

    public DsonConverterBuilder addCodecs(Collection<? extends DsonCodec<?>> codecs) {
        tempCodecRegistry.addCodecs(codecs);
        return this;
    }

    public DsonConverterBuilder addCodec(TypeInfo typeInfo, DsonCodec<?> codec) {
        tempCodecRegistry.addCodec(typeInfo, codec);
        return this;
    }

    public <T> DsonConverterBuilder addEncoder(Class<T> clazz, DsonCodec<? super T> codec) {
        tempCodecRegistry.addEncoder(clazz, codec);
        return this;
    }

    public DsonConverterBuilder addEncoder(TypeInfo typeInfo, DsonCodec<?> codec) {
        tempCodecRegistry.addEncoder(typeInfo, codec);
        return this;
    }

    public <T> DsonConverterBuilder addDecoder(Class<T> clazz, DsonCodec<? extends T> codec) {
        tempCodecRegistry.addDecoder(clazz, codec);
        return this;
    }

    public DsonConverterBuilder addDecoder(TypeInfo typeInfo, DsonCodec<?> codec) {
        tempCodecRegistry.addDecoder(typeInfo, codec);
        return this;
    }

    public DsonConverterBuilder addGenericCodecConfig(GenericCodecConfig genericCodecConfig) {
        tempCodecRegistry.addGenericCodecConfig(genericCodecConfig);
        return this;
    }

    public DsonConverterBuilder addGenericCodecConfigs(Collection<? extends GenericCodecConfig> genericCodecConfigs) {
        tempCodecRegistry.addGenericCodecConfigs(genericCodecConfigs);
        return this;
    }

    public DsonConverterBuilder addCaster(DsonCodecCaster caster) {
        tempCodecRegistry.addCaster(caster);
        return this;
    }

    public DsonConverterBuilder addCasters(Collection<? extends DsonCodecCaster> casters) {
        tempCodecRegistry.addCasters(casters);
        return this;
    }

    // endregion

    public ConverterOptions getOptions() {
        return options;
    }

    public DsonConverterBuilder setOptions(ConverterOptions options) {
        this.options = options;
        return this;
    }
}
