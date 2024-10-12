#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
public static class DsonCodecRegistries
{
    public static IDsonCodecRegistry FromCodecs(params IDsonCodec[] pojoCodecs) {
        return FromCodecs((IEnumerable<IDsonCodec>)pojoCodecs);
    }

    public static IDsonCodecRegistry FromCodecs(IEnumerable<IDsonCodec> pojoCodecs) {
        return SimpleCodecRegistry.FromCodecs(pojoCodecs);
    }

    public static IDsonCodecRegistry FromRegistries(params IDsonCodecRegistry[] codecRegistries) {
        return FromRegistries((IEnumerable<IDsonCodecRegistry>)codecRegistries);
    }

    public static IDsonCodecRegistry FromRegistries(IEnumerable<IDsonCodecRegistry> codecRegistries) {
        SimpleCodecRegistry simpleCodecRegistry = new SimpleCodecRegistry();
        List<IDsonCodecRegistry> unmergedRegistries = new List<IDsonCodecRegistry>(4);
        // 合并codec
        foreach (IDsonCodecRegistry codecRegistry in codecRegistries) {
            if (codecRegistry is SimpleCodecRegistry other) {
                simpleCodecRegistry.MergeFrom(other);
            } else {
                unmergedRegistries.Add(codecRegistry);
            }
        }
        if (unmergedRegistries.Count == 0) {
            return simpleCodecRegistry.ToImmutable();
        }
        // 简单Codec放在最前面
        unmergedRegistries.Insert(0, simpleCodecRegistry.ToImmutable());
        return new CompositeCodecRegistry(unmergedRegistries); // 拷贝
    }


    private class CompositeCodecRegistry : IDsonCodecRegistry
    {
        private readonly List<IDsonCodecRegistry> registryList;

        internal CompositeCodecRegistry(List<IDsonCodecRegistry> registryList) {
            this.registryList = new List<IDsonCodecRegistry>(registryList);
        }

        public DsonCodecImpl? GetEncoder(Type clazz) {
            List<IDsonCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.Count; i++) {
                IDsonCodecRegistry registry = registryList[i];
                DsonCodecImpl codec = registry.GetEncoder(clazz);
                if (codec != null) return codec;
            }
            return null;
        }

        public DsonCodecImpl? GetDecoder(Type clazz) {
            List<IDsonCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.Count; i++) {
                IDsonCodecRegistry registry = registryList[i];
                DsonCodecImpl codec = registry.GetDecoder(clazz);
                if (codec != null) return codec;
            }
            return null;
        }
    }
}
}