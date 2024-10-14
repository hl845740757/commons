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

namespace Wjybxx.Dson.Codec
{
public class DsonConverterBuilder
{
    private readonly SimpleTypeMetaRegistry tempTypeMetaRegistry = new SimpleTypeMetaRegistry();
    private readonly SimpleCodecRegistry tempCodecRegistry = new SimpleCodecRegistry(); // 用于简化build

    private readonly List<ITypeMetaRegistry> typeMetaRegistries = new(4);
    private readonly List<IDsonCodecRegistry> codecRegistries = new(4);
    private ConverterOptions options = ConverterOptions.DEFAULT;
    private bool pureMode = false;

    public DsonConverterBuilder() {
        typeMetaRegistries.Add(tempTypeMetaRegistry);
        codecRegistries.Add(tempCodecRegistry);
    }

    public IDsonConverter Build() {
        if (!pureMode) {
            typeMetaRegistries.Add(DsonConverterUtils.GetDefaultTypeMetaRegistry());
        }
        DynamicTypeMetaRegistry dynamicTypeMetaRegistry = new DynamicTypeMetaRegistry(typeMetaRegistries);
        if (!pureMode) {
            typeMetaRegistries.RemoveAt(typeMetaRegistries.Count - 1);
        }

        DynamicCodecRegistry dynamicCodecRegistry;
        codecRegistries.Add(DsonConverterUtils.GetDefaultCodecRegistry());
        {
            dynamicCodecRegistry = new DynamicCodecRegistry(codecRegistries);
        }
        codecRegistries.RemoveAt(codecRegistries.Count - 1);

        return new DefaultDsonConverter(dynamicTypeMetaRegistry,
            dynamicCodecRegistry,
            options);
    }

    /**
     * 是否纯净模式
     * 纯净模式是指Build时不使用默认的TypeMeta，完全由用户分配。
     * 主要用于解析跨语言Dson文本。
     */
    public bool IsPureMode() {
        return pureMode;
    }

    public DsonConverterBuilder SetPureMode(bool pureMode) {
        this.pureMode = pureMode;
        return this;
    }

    #region type-meta

    public List<ITypeMetaRegistry> GetTypeMetaRegistries() {
        return typeMetaRegistries;
    }

    public DsonConverterBuilder AddTypeMetaRegistry(ITypeMetaRegistry typeMetaRegistry) {
        this.typeMetaRegistries.Add(typeMetaRegistry);
        return this;
    }

    public DsonConverterBuilder AddTypeMetaRegistries(List<ITypeMetaRegistry> typeMetaRegistries) {
        this.typeMetaRegistries.AddRange(typeMetaRegistries);
        return this;
    }

    public DsonConverterBuilder AddTypeMeta(TypeMeta typeMeta) {
        tempTypeMetaRegistry.Add(typeMeta);
        return this;
    }

    public DsonConverterBuilder AddTypeMetas(IEnumerable<TypeMeta> typeMetas) {
        tempTypeMetaRegistry.AddAll(typeMetas);
        return this;
    }

    public DsonConverterBuilder AddTypeMetas(params TypeMeta[] typeMetas) {
        tempTypeMetaRegistry.AddAll(typeMetas);
        return this;
    }

    #endregion

    # region codec-registry

    public List<IDsonCodecRegistry> GetCodecRegistries() {
        return codecRegistries;
    }

    public DsonConverterBuilder AddCodecRegistry(IDsonCodecRegistry codecRegistry) {
        this.codecRegistries.Add(codecRegistry);
        return this;
    }

    public DsonConverterBuilder AddCodecRegistries(List<IDsonCodecRegistry> codecRegistries) {
        this.codecRegistries.AddRange(codecRegistries);
        return this;
    }

    public DsonConverterBuilder AddCodecs(IEnumerable<IDsonCodec> codecs) {
        tempCodecRegistry.AddCodecs(codecs);
        return this;
    }

    public DsonConverterBuilder AddCodec(IDsonCodec codec) {
        tempCodecRegistry.AddCodec(codec);
        return this;
    }

    public DsonConverterBuilder AddCodec(Type type, IDsonCodec codec) {
        tempCodecRegistry.AddCodec(type, codec);
        return this;
    }

    public DsonConverterBuilder AddEncoder(Type type, IDsonCodec codec) {
        tempCodecRegistry.AddEncoder(type, codec);
        return this;
    }

    public DsonConverterBuilder AddDecoder(Type type, IDsonCodec codec) {
        tempCodecRegistry.AddDecoder(type, codec);
        return this;
    }

    public DsonConverterBuilder AddGenericCodecConfig(GenericCodecConfig genericCodecConfig) {
        tempCodecRegistry.AddGenericCodecConfig(genericCodecConfig);
        return this;
    }

    public DsonConverterBuilder AddGenericCodecConfigs(IEnumerable<GenericCodecConfig> genericCodecConfigs) {
        tempCodecRegistry.AddGenericCodecConfigs(genericCodecConfigs);
        return this;
    }

    public DsonConverterBuilder AddCaster(IDsonCodecCaster caster) {
        tempCodecRegistry.AddCaster(caster);
        return this;
    }

    public DsonConverterBuilder AddCasters(IEnumerable<IDsonCodecCaster> casters) {
        tempCodecRegistry.AddCasters(casters);
        return this;
    }

    # endregion

    public ConverterOptions GetOptions() {
        return options;
    }

    public DsonConverterBuilder SetOptions(ConverterOptions options) {
        this.options = options;
        return this;
    }
}
}