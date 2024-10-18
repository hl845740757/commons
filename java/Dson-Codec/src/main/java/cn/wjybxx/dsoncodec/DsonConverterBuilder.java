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

import cn.wjybxx.dson.text.ObjectStyle;

import java.util.Arrays;
import java.util.Collection;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/9/28
 */
@SuppressWarnings("rawtypes")
public class DsonConverterBuilder {

    // 用于简化build
    public final TypeMetaConfig typeMetaConfig = new TypeMetaConfig();
    public final DsonCodecConfig codecConfig = new DsonCodecConfig();
    private ConverterOptions options = ConverterOptions.DEFAULT;

    public DsonConverterBuilder() {
        this(true);
    }

    /** @param includeDefaults 是否包含默认配置 */
    public DsonConverterBuilder(boolean includeDefaults) {
        if (includeDefaults) {
            typeMetaConfig.mergeFrom(TypeMetaConfig.DEFAULT);
            codecConfig.mergeFrom(DsonCodecConfig.DEFAULT);
        }
    }

    public DsonConverter build() {
        return new DefaultDsonConverter(
                new DynamicTypeMetaRegistry(typeMetaConfig),
                new DynamicCodecRegistry(codecConfig),
                new CachedGenericHelper(codecConfig.getGenericHelpers()),
                new TypeWriteHelper(codecConfig.getOptimizedTypes()),
                options);
    }

    // region type-meta

    public DsonConverterBuilder addTypeMetaConfig(TypeMetaConfig typeMetaConfig) {
        this.typeMetaConfig.mergeFrom(typeMetaConfig);
        return this;
    }

    public DsonConverterBuilder addTypeMetaConfigs(Collection<TypeMetaConfig> typeMetaConfigs) {
        for (TypeMetaConfig typeMetaConfig : typeMetaConfigs) {
            this.typeMetaConfig.mergeFrom(typeMetaConfig);
        }
        return this;
    }

    public DsonConverterBuilder addTypeMetas(Collection<TypeMeta> typeMetas) {
        typeMetaConfig.addAll(typeMetas);
        return this;
    }

    public DsonConverterBuilder addTypeMetas(TypeMeta... typeMetas) {
        typeMetaConfig.addAll(Arrays.asList(typeMetas));
        return this;
    }

    public DsonConverterBuilder addTypeMeta(TypeMeta typeMeta) {
        typeMetaConfig.add(typeMeta);
        return this;
    }

    public DsonConverterBuilder addTypeMeta(Class<?> type, String clsName) {
        typeMetaConfig.add(TypeMeta.of(type, ObjectStyle.INDENT, clsName));
        return this;
    }

    public DsonConverterBuilder addTypeMeta(Class<?> type, String... clsName) {
        typeMetaConfig.add(TypeMeta.of(type, ObjectStyle.INDENT, clsName));
        return this;
    }

    public DsonConverterBuilder addTypeMeta(Class<?> type, ObjectStyle style, String clsName) {
        typeMetaConfig.add(TypeMeta.of(type, style, clsName));
        return this;
    }

    public DsonConverterBuilder addTypeMeta(Class<?> type, ObjectStyle style, String... clsName) {
        typeMetaConfig.add(TypeMeta.of(type, style, clsName));
        return this;
    }

    // endregion

    // region codec-registry

    public DsonConverterBuilder addCodecConfig(DsonCodecConfig codecConfig) {
        this.codecConfig.mergeFrom(codecConfig);
        return this;
    }

    public DsonConverterBuilder addCodecConfigs(Collection<DsonCodecConfig> codecConfigs) {
        for (DsonCodecConfig codecConfig : codecConfigs) {
            this.codecConfig.mergeFrom(codecConfig);
        }
        return this;
    }

    public <T> DsonConverterBuilder addCodec(Class<T> clazz, DsonCodec<? super T> codec) {
        codecConfig.addCodec(clazz, codec);
        return this;
    }

    public DsonConverterBuilder addCodec(DsonCodec<?> codec) {
        codecConfig.addCodec(codec);
        return this;
    }

    public DsonConverterBuilder addCodecs(DsonCodec<?>... codecs) {
        codecConfig.addCodecs(Arrays.asList(codecs));
        return this;
    }

    public DsonConverterBuilder addCodecs(Collection<? extends DsonCodec<?>> codecs) {
        codecConfig.addCodecs(codecs);
        return this;
    }

    public DsonConverterBuilder addCodec(TypeInfo typeInfo, DsonCodec<?> codec) {
        codecConfig.addCodec(typeInfo, codec);
        return this;
    }

    public <T> DsonConverterBuilder addEncoder(Class<T> clazz, DsonCodec<? super T> codec) {
        codecConfig.addEncoder(clazz, codec);
        return this;
    }

    public DsonConverterBuilder addEncoder(TypeInfo typeInfo, DsonCodec<?> codec) {
        codecConfig.addEncoder(typeInfo, codec);
        return this;
    }

    public <T> DsonConverterBuilder addDecoder(Class<T> clazz, DsonCodec<? extends T> codec) {
        codecConfig.addDecoder(clazz, codec);
        return this;
    }

    public DsonConverterBuilder addDecoder(TypeInfo typeInfo, DsonCodec<?> codec) {
        codecConfig.addDecoder(typeInfo, codec);
        return this;
    }

    // endregion

    // region 泛型codec

    public DsonConverterBuilder addGenericCodec(TypeInfo genericType, Class<? extends DsonCodec> codecType) {
        codecConfig.addGenericCodec(genericType, codecType);
        return this;
    }

    public DsonConverterBuilder addGenericCodec(TypeInfo genericType, Class<? extends DsonCodec> codecType, Class<?> implType) {
        codecConfig.addGenericCodec(genericType, codecType, implType);
        return this;
    }

    public DsonConverterBuilder addGenericCodec(TypeInfo genericType, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        codecConfig.addGenericCodec(genericType, codecType, factory);
        return this;
    }

    public DsonConverterBuilder addGenericCodec(GenericCodecInfo genericCodecInfo) {
        codecConfig.addGenericCodec(genericCodecInfo);
        return this;
    }

    public DsonConverterBuilder addGenericEncoder(TypeInfo genericType, Class<? extends DsonCodec> codecType) {
        codecConfig.addGenericEncoder(genericType, codecType);
        return this;
    }

    public DsonConverterBuilder addGenericEncoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Class<?> implType) {
        codecConfig.addGenericEncoder(genericType, codecType, implType);
        return this;
    }

    public DsonConverterBuilder addGenericEncoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        codecConfig.addGenericEncoder(genericType, codecType, factory);
        return this;
    }

    public DsonConverterBuilder addGenericEncoder(GenericCodecInfo genericCodecInfo) {
        codecConfig.addGenericEncoder(genericCodecInfo);
        return this;
    }

    public DsonConverterBuilder addGenericDecoder(TypeInfo genericType, Class<? extends DsonCodec> codecType) {
        codecConfig.addGenericDecoder(genericType, codecType);
        return this;
    }

    public DsonConverterBuilder addGenericDecoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Class<?> implType) {
        codecConfig.addGenericDecoder(genericType, codecType, implType);
        return this;
    }

    public DsonConverterBuilder addGenericDecoder(TypeInfo genericType, Class<? extends DsonCodec> codecType, Supplier<?> factory) {
        codecConfig.addGenericDecoder(genericType, codecType, factory);
        return this;
    }

    public DsonConverterBuilder addGenericDecoder(GenericCodecInfo genericCodecInfo) {
        codecConfig.addGenericDecoder(genericCodecInfo);
        return this;
    }

    // endregion

    // region 其它

    public DsonConverterBuilder addCaster(DsonCodecCaster caster) {
        codecConfig.addCaster(caster);
        return this;
    }

    public DsonConverterBuilder addCasters(Collection<? extends DsonCodecCaster> casters) {
        codecConfig.addCasters(casters);
        return this;
    }

    public <T> DsonConverterBuilder addOptimizedType(Class<T> encoderType, Class<? super T> declaredType) {
        codecConfig.addOptimizedType(encoderType, declaredType);
        return this;
    }

    public <T> DsonConverterBuilder addOptimizedType(Class<T> encoderType, Class<? super T> declaredType, boolean val) {
        codecConfig.addOptimizedType(encoderType, declaredType, val);
        return this;
    }

    public DsonConverterBuilder addGenericHelper(GenericHelper caster) {
        codecConfig.addGenericHelper(caster);
        return this;
    }

    public DsonConverterBuilder addGenericHelpers(Collection<? extends GenericHelper> casters) {
        codecConfig.addGenericHelpers(casters);
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
