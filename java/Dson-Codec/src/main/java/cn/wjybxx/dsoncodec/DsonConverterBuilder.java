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
import java.util.List;

/**
 * @author wjybxx
 * date - 2024/9/28
 */
public class DsonConverterBuilder {

    private final List<TypeMetaRegistry> typeMetaRegistries = new ArrayList<>(4);

    private final List<DsonCodecRegistry> codecRegistries = new ArrayList<>(4);
    private final List<DsonCodecCaster> casters = new ArrayList<>(4);
    private final List<GenericCodecConfig> genericCodecConfigs = new ArrayList<>(4);
    private GenericCodecHelper genericCodecHelper;

    private ConverterOptions options = ConverterOptions.DEFAULT;
    private boolean pureMode = false;

    public DsonConverterBuilder() {
    }

    public DsonConverter build() {
        if (!pureMode) {
            typeMetaRegistries.addLast(DsonConverterUtils.getDefaultTypeMetaRegistry());
        }
        DynamicTypeMetaRegistry dynamicTypeMetaRegistry = new DynamicTypeMetaRegistry(
                TypeMetaRegistries.fromRegistries(typeMetaRegistries));
        if (!pureMode) {
            typeMetaRegistries.removeLast();
        }
        if (genericCodecHelper == null) {
            genericCodecHelper = new GenericCodecHelper();
        }
        DynamicCodecRegistry dynamicDsonCodecRegistry;
        codecRegistries.add(DsonConverterUtils.getDefaultCodecRegistry());
        {
            dynamicDsonCodecRegistry = new DynamicCodecRegistry(
                    DsonCodecRegistries.fromRegistries(codecRegistries), casters,
                    genericCodecConfigs, genericCodecHelper);
        }
        codecRegistries.removeLast();

        return new DefaultDsonConverter(dynamicTypeMetaRegistry,
                dynamicDsonCodecRegistry,
                genericCodecHelper,
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

    public DsonConverterBuilder addTypeMetaRegistries(List<? extends TypeMetaRegistry> typeMetaRegistries) {
        this.typeMetaRegistries.addAll(typeMetaRegistries);
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

    public DsonConverterBuilder addCodecRegistries(List<? extends DsonCodecRegistry> codecRegistries) {
        this.codecRegistries.addAll(codecRegistries);
        return this;
    }

    // endregion

    // region generic
    public List<GenericCodecConfig> getGenericCodecConfigs() {
        return genericCodecConfigs;
    }

    public DsonConverterBuilder addGenericCodecConfig(GenericCodecConfig genericCodecConfig) {
        this.genericCodecConfigs.add(genericCodecConfig);
        return this;
    }

    public DsonConverterBuilder addGenericCodecConfigs(List<? extends GenericCodecConfig> genericCodecConfigs) {
        this.genericCodecConfigs.addAll(genericCodecConfigs);
        return this;
    }

    public GenericCodecHelper getGenericCodecHelper() {
        return genericCodecHelper;
    }

    public DsonConverterBuilder setGenericCodecHelper(GenericCodecHelper genericCodecHelper) {
        this.genericCodecHelper = genericCodecHelper;
        return this;
    }

    // endregion

    public List<DsonCodecCaster> getCasters() {
        return casters;
    }

    public DsonConverterBuilder addCaster(DsonCodecCaster caster) {
        this.casters.add(caster);
        return this;
    }

    public DsonConverterBuilder addCasters(List<? extends DsonCodecCaster> casters) {
        this.casters.addAll(casters);
        return this;
    }

    public ConverterOptions getOptions() {
        return options;
    }

    public DsonConverterBuilder setOptions(ConverterOptions options) {
        this.options = options;
        return this;
    }
}
