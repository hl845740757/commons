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
        Dictionary<Type, DsonCodecImpl> codecMap = new Dictionary<Type, DsonCodecImpl>();
        foreach (IDsonCodec codec in pojoCodecs) {
            if (codecMap.ContainsKey(codec.GetEncoderType())) {
                throw new ArgumentException("the class has multiple codecs :" + codec.GetEncoderType());
            }
            // 反射创建...
            Type genericType = typeof(DsonCodecImpl<>).MakeGenericType(codec.GetEncoderType());
            DsonCodecImpl codecImpl = (DsonCodecImpl)Activator.CreateInstance(genericType, codec);
            codecMap[codec.GetEncoderType()] = codecImpl;
        }
        return new DefaultCodecRegistry(codecMap);
    }

    public static IDsonCodecRegistry FromRegistries(params IDsonCodecRegistry[] codecRegistries) {
        return FromRegistries((IEnumerable<IDsonCodecRegistry>)codecRegistries);
    }

    public static IDsonCodecRegistry FromRegistries(IEnumerable<IDsonCodecRegistry> codecRegistries) {
        // 合并codec
        Dictionary<Type, DsonCodecImpl> type2CodecMap = new Dictionary<Type, DsonCodecImpl>();
        List<IDsonCodecRegistry> unmergedRegistries = new List<IDsonCodecRegistry>(4);
        foreach (IDsonCodecRegistry codecRegistry in codecRegistries) {
            if (codecRegistry is not DefaultCodecRegistry defaultCodecRegistry) {
                unmergedRegistries.Add(codecRegistry);
                continue;
            }
            foreach (DsonCodecImpl codec in defaultCodecRegistry.type2CodecMap.Values) {
                if (type2CodecMap.ContainsKey(codec.GetEncoderType())) {
                    throw new ArgumentException("the type has multiple codecs :" + codec.GetEncoderType());
                }
                type2CodecMap[codec.GetEncoderType()] = codec;
            }
        }
        if (unmergedRegistries.Count == 0) {
            return new DefaultCodecRegistry(type2CodecMap);
        }
        // 简单Codec放在最前面
        unmergedRegistries.Insert(0, new DefaultCodecRegistry(type2CodecMap));
        return new CompositeCodecRegistry(unmergedRegistries); // 拷贝
    }

    private class DefaultCodecRegistry : IDsonCodecRegistry
    {
        internal readonly Dictionary<Type, DsonCodecImpl> type2CodecMap;

        internal DefaultCodecRegistry(Dictionary<Type, DsonCodecImpl> type2CodecMap) {
            this.type2CodecMap = new Dictionary<Type, DsonCodecImpl>(type2CodecMap); // copy 压缩空间
        }

        public DsonCodecImpl? GetEncoder(Type clazz) {
            type2CodecMap.TryGetValue(clazz, out DsonCodecImpl r);
            return r;
        }

        public DsonCodecImpl? GetDecoder(Type clazz) {
            type2CodecMap.TryGetValue(clazz, out DsonCodecImpl r);
            return r;
        }
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