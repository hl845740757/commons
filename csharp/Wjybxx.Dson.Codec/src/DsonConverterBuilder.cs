#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using Wjybxx.Commons;
using Wjybxx.Dson.Codec.Attributes;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Codec
{
public class DsonConverterBuilder
{
    // 用于简化build
    private readonly TypeMetaConfig typeMetaConfig = new TypeMetaConfig();
    private readonly DsonCodecConfig codecConfig = new DsonCodecConfig();
    private ConverterOptions options = ConverterOptions.DEFAULT;

    public DsonConverterBuilder(bool includeDefaults = true) {
        if (includeDefaults) {
            typeMetaConfig.MergeFrom(TypeMetaConfig.Default);
            codecConfig.MergeFrom(DsonCodecConfig.Default);
        }
    }

    public IDsonConverter Build() {
        return new DefaultDsonConverter(
            new DynamicTypeMetaRegistry(typeMetaConfig),
            new DynamicCodecRegistry(codecConfig),
            new TypeWriteHelper(codecConfig.GetOptimizedTypes()),
            options);
    }

    #region type-meta

    public DsonConverterBuilder AddTypeMeta(TypeMeta typeMeta) {
        typeMetaConfig.Add(typeMeta);
        return this;
    }

    public DsonConverterBuilder AddTypeMetas(IEnumerable<TypeMeta> typeMetas) {
        typeMetaConfig.AddAll(typeMetas);
        return this;
    }

    public DsonConverterBuilder AddTypeMetaConfig(TypeMetaConfig typeMetaConfig) {
        this.typeMetaConfig.MergeFrom(typeMetaConfig);
        return this;
    }

    public DsonConverterBuilder AddTypeMetaConfigs(IEnumerable<TypeMetaConfig> typeMetaConfigs) {
        foreach (TypeMetaConfig typeMetaConfig in typeMetaConfigs) {
            this.typeMetaConfig.MergeFrom(typeMetaConfig);
        }
        return this;
    }

    #endregion

    # region 非泛型codec

    public DsonConverterBuilder AddCodecConfig(DsonCodecConfig codecConfig) {
        this.codecConfig.MergeFrom(codecConfig);
        return this;
    }

    public DsonConverterBuilder AddCodecConfigs(IEnumerable<DsonCodecConfig> codecConfigs) {
        foreach (DsonCodecConfig codecConfig in codecConfigs) {
            this.codecConfig.MergeFrom(codecConfig);
        }
        return this;
    }

    public DsonConverterBuilder AddCodecs(IEnumerable<IDsonCodec> codecs) {
        codecConfig.AddCodecs(codecs);
        return this;
    }

    public DsonConverterBuilder AddCodec(IDsonCodec codec) {
        codecConfig.AddCodec(codec);
        return this;
    }

    public DsonConverterBuilder AddCodec(Type type, IDsonCodec codec) {
        codecConfig.AddCodec(type, codec);
        return this;
    }

    public DsonConverterBuilder AddEncoder(Type type, IDsonCodec codec) {
        codecConfig.AddEncoder(type, codec);
        return this;
    }

    public DsonConverterBuilder AddDecoder(Type type, IDsonCodec codec) {
        codecConfig.AddDecoder(type, codec);
        return this;
    }

    # endregion

    #region 泛型codec

    public DsonConverterBuilder AddGenericCodec(Type genericType, Type codecType) {
        codecConfig.AddGenericCodec(genericType, codecType);
        return this;
    }

    public DsonConverterBuilder AddGenericCodec(GenericCodecInfo genericCodecInfo) {
        codecConfig.AddGenericCodec(genericCodecInfo);
        return this;
    }

    public DsonConverterBuilder AddGenericEncoder(Type genericType, Type codecType) {
        codecConfig.AddGenericEncoder(genericType, codecType);
        return this;
    }

    public DsonConverterBuilder AddGenericEncoder(GenericCodecInfo genericCodecInfo) {
        codecConfig.AddGenericEncoder(genericCodecInfo);
        return this;
    }

    public DsonConverterBuilder AddGenericDecoder(Type genericType, Type codecType) {
        codecConfig.AddGenericDecoder(genericType, codecType);
        return this;
    }

    public DsonConverterBuilder AddGenericDecoder(GenericCodecInfo genericCodecInfo) {
        codecConfig.AddGenericDecoder(genericCodecInfo);
        return this;
    }

    #endregion

    #region other

    public DsonConverterBuilder AddCaster(IDsonCodecCaster caster) {
        codecConfig.AddCaster(caster);
        return this;
    }

    public DsonConverterBuilder AddCasters(IEnumerable<IDsonCodecCaster> casters) {
        codecConfig.AddCasters(casters);
        return this;
    }

    public DsonConverterBuilder AddOptimizedType(Type encoderType, Type declaredType, bool val = true) {
        codecConfig.AddOptimizedType(encoderType, declaredType, val);
        return this;
    }

    public ConverterOptions GetOptions() {
        return options;
    }

    public DsonConverterBuilder SetOptions(ConverterOptions options) {
        this.options = options;
        return this;
    }

    #endregion

    #region util

    /// <summary>
    /// 通过CodecType添加<see cref="TypeMeta"/>和<see cref="IDsonCodec"/>
    /// </summary>
    /// <param name="codecType">Codec类型</param>
    /// <returns></returns>
    public DsonConverterBuilder AddByCodecType(Type codecType) {
        Type encoderType = DsonConverterUtils.GetEncoderType(codecType);
        if (codecType.IsGenericType) {
            encoderType = encoderType.GetGenericTypeDefinition();
            GetFeatures(encoderType, out SerializeFeatures features1, out DeserializeFeatures features2);
            //
            AddGenericCodec(encoderType, codecType);
            AddTypeMeta(TypeMeta.Of(encoderType, features1, features2, ObjectUtil.GetSimpleName(encoderType)));
        } else {
            GetFeatures(encoderType, out SerializeFeatures features1, out DeserializeFeatures features2);
            //
            AddCodec((IDsonCodec)Activator.CreateInstance(codecType)!);
            AddTypeMeta(TypeMeta.Of(encoderType, features1, features2, encoderType.Name));
        }
        return this;
    }

    private static void GetFeatures(Type encoderType,
                                    out SerializeFeatures serializeFeatures,
                                    out DeserializeFeatures deserializeFeatures) {
        DsonSerializableAttribute attribute = encoderType.GetCustomAttribute<DsonSerializableAttribute>();
        if (attribute != null) {
            serializeFeatures = attribute.EncodeFeatures;
            deserializeFeatures = attribute.DecodeFeatures;
        } else {
            serializeFeatures = default;
            deserializeFeatures = default;
        }
    }

    #endregion
}
}