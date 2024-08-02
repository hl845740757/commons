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
using System.Linq;

namespace Wjybxx.Dson.Codec
{
public static class DsonCodecRegistries
{
    public static Dictionary<Type, DsonCodecImpl> NewCodecMap(IList<DsonCodecImpl> pojoCodecs) {
        Dictionary<Type, DsonCodecImpl> codecMap = new Dictionary<Type, DsonCodecImpl>();
        foreach (DsonCodecImpl codec in pojoCodecs) {
            if (codecMap.ContainsKey(codec.GetEncoderClass())) {
                throw new ArgumentException("the class has multiple codecs :" + codec.GetEncoderClass());
            }
            codecMap[codec.GetEncoderClass()] = codec;
        }
        return codecMap;
    }

    public static IDsonCodecRegistry FromCodecs(params DsonCodecImpl[] pojoCodecs) {
        return FromCodecs((IEnumerable<DsonCodecImpl>)pojoCodecs);
    }

    public static IDsonCodecRegistry FromCodecs(IEnumerable<DsonCodecImpl> pojoCodecs) {
        Dictionary<Type, DsonCodecImpl> codecMap = new Dictionary<Type, DsonCodecImpl>();
        foreach (DsonCodecImpl codec in pojoCodecs) {
            if (codecMap.ContainsKey(codec.GetEncoderClass())) {
                throw new ArgumentException("the class has multiple codecs :" + codec.GetEncoderClass());
            }
            codecMap[codec.GetEncoderClass()] = codec;
        }
        return new DefaultCodecRegistry(codecMap);
    }

    public static IDsonCodecRegistry FromRegistries(params IDsonCodecRegistry[] codecRegistry) {
        return new CompositeCodecRegistry(codecRegistry.ToList()); // 拷贝
    }

    public static IDsonCodecRegistry FromRegistries(IList<IDsonCodecRegistry> codecRegistries) {
        return new CompositeCodecRegistry(new List<IDsonCodecRegistry>(codecRegistries)); // 拷贝
    }

    private class DefaultCodecRegistry : IDsonCodecRegistry
    {
        private readonly Dictionary<Type, DsonCodecImpl> type2CodecMap;

        internal DefaultCodecRegistry(Dictionary<Type, DsonCodecImpl> type2CodecMap) {
            this.type2CodecMap = type2CodecMap;
        }

        public DsonCodecImpl? GetEncoder(Type clazz, IDsonCodecRegistry rootRegistry) {
            type2CodecMap.TryGetValue(clazz, out DsonCodecImpl r);
            return r;
        }

        public DsonCodecImpl? GetDecoder(Type clazz, IDsonCodecRegistry rootRegistry) {
            type2CodecMap.TryGetValue(clazz, out DsonCodecImpl r);
            return r;
        }
    }

    private class CompositeCodecRegistry : IDsonCodecRegistry
    {
        private readonly List<IDsonCodecRegistry> registryList;

        internal CompositeCodecRegistry(List<IDsonCodecRegistry> registryList) {
            this.registryList = registryList;
        }

        public DsonCodecImpl? GetEncoder(Type clazz, IDsonCodecRegistry rootRegistry) {
            List<IDsonCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.Count; i++) {
                IDsonCodecRegistry registry = registryList[i];
                DsonCodecImpl codec = registry.GetEncoder(clazz, rootRegistry);
                if (codec != null) return codec;
            }
            return null;
        }

        public DsonCodecImpl? GetDecoder(Type clazz, IDsonCodecRegistry rootRegistry) {
            List<IDsonCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.Count; i++) {
                IDsonCodecRegistry registry = registryList[i];
                DsonCodecImpl codec = registry.GetDecoder(clazz, rootRegistry);
                if (codec != null) return codec;
            }
            return null;
        }
    }
}
}