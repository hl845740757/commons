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

using System.Collections.Generic;

namespace Wjybxx.Dson.Codec
{
public class DsonConverterBuilder
{
    private readonly List<ITypeMetaRegistry> typeMetaRegistries = new(4);
    private readonly List<IDsonCodecRegistry> codecRegistries = new(4);
    private ConverterOptions options = ConverterOptions.DEFAULT;
    private bool pureMode = false;

    public DsonConverterBuilder() {
    }

    public IDsonConverter Build() {
        if (!pureMode) {
            typeMetaRegistries.Add(DsonConverterUtils.GetDefaultTypeMetaRegistry());
        }
        DynamicTypeMetaRegistry dynamicTypeMetaRegistry = new DynamicTypeMetaRegistry(
            SimpleTypeMetaRegistry.FromRegistries(typeMetaRegistries));
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

    // region type-meta

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
    // endregion

    public ConverterOptions GetOptions() {
        return options;
    }

    public DsonConverterBuilder SetOptions(ConverterOptions options) {
        this.options = options;
        return this;
    }
}
}