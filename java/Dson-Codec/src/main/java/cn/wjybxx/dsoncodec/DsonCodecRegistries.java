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
 * date 2023/4/4
 */
public class DsonCodecRegistries {

    public static DsonCodecRegistry fromCodecs(DsonCodec<?>... pojoCodecs) {
        return fromCodecs(List.of(pojoCodecs));
    }

    public static DsonCodecRegistry fromCodecs(List<? extends DsonCodec<?>> pojoCodecs) {
        final HashMap<TypeInfo, DsonCodecImpl<?>> codecMap = new HashMap<>(pojoCodecs.size());
        for (DsonCodec<?> codec : pojoCodecs) {
            if (codecMap.containsKey(codec.getEncoderType())) {
                throw new IllegalArgumentException("the type has multiple codecs :" + codec.getEncoderType());
            }
            codecMap.put(codec.getEncoderType(), new DsonCodecImpl<>(codec));
        }
        return new DefaultCodecRegistry(codecMap);
    }

    public static DsonCodecRegistry fromRegistries(DsonCodecRegistry... codecRegistries) {
        return fromRegistries(Arrays.asList(codecRegistries));
    }

    public static DsonCodecRegistry fromRegistries(List<? extends DsonCodecRegistry> codecRegistries) {
        // 合并codec
        final HashMap<TypeInfo, DsonCodecImpl<?>> type2CodecMap = new HashMap<>();
        final List<DsonCodecRegistry> unmergedRegistries = new ArrayList<>(codecRegistries.size());
        for (DsonCodecRegistry codecRegistry : codecRegistries) {
            if (!(codecRegistry instanceof DefaultCodecRegistry defaultCodecRegistry)) {
                unmergedRegistries.add(codecRegistry);
                continue;
            }
            for (DsonCodecImpl<?> codec : defaultCodecRegistry.type2CodecMap.values()) {
                if (type2CodecMap.containsKey(codec.getEncoderType())) {
                    throw new IllegalArgumentException("the type has multiple codecs :" + codec.getEncoderType());
                }
                type2CodecMap.put(codec.getEncoderType(), codec);
            }
        }
        if (unmergedRegistries.isEmpty()) {
            return new DefaultCodecRegistry(type2CodecMap);
        }
        // 简单Codec放在最前面
        unmergedRegistries.add(0, new DefaultCodecRegistry(type2CodecMap));
        return new CompositeCodecRegistry(unmergedRegistries); // 拷贝
    }

    private static class DefaultCodecRegistry implements DsonCodecRegistry {

        private final Map<TypeInfo, DsonCodecImpl<?>> type2CodecMap;
        private final IdentityHashMap<Class<?>, DsonCodecImpl<?>> class2CodecMap;

        private DefaultCodecRegistry(HashMap<TypeInfo, DsonCodecImpl<?>> type2CodecMap) {
            this.type2CodecMap = new HashMap<>(type2CodecMap); // 拷贝，压缩空间
            this.class2CodecMap = new IdentityHashMap<>(type2CodecMap.size()); // 增加非泛型缓存

            for (Map.Entry<TypeInfo, DsonCodecImpl<?>> entry : type2CodecMap.entrySet()) {
                TypeInfo typeInfo = entry.getKey();
                if (!typeInfo.hasGenericArgs()) {
                    class2CodecMap.put(typeInfo.rawType, entry.getValue());
                }
            }
        }

        @Nullable
        @Override
        public DsonCodecImpl<?> getEncoder(TypeInfo typeInfo, DsonCodecRegistry rootRegistry, IGenericCodecHelper genericCodecHelper) {
            if (typeInfo.hasGenericArgs()) {
                return type2CodecMap.get(typeInfo);
            } else {
                return class2CodecMap.get(typeInfo.rawType);
            }
        }

        @Override
        public DsonCodecImpl<?> getDecoder(TypeInfo typeInfo, DsonCodecRegistry rootRegistry, IGenericCodecHelper genericCodecHelper) {
            if (typeInfo.hasGenericArgs()) {
                return type2CodecMap.get(typeInfo);
            } else {
                return class2CodecMap.get(typeInfo.rawType);
            }
        }

    }

    private static class CompositeCodecRegistry implements DsonCodecRegistry {

        private final List<DsonCodecRegistry> registryList;

        private CompositeCodecRegistry(List<DsonCodecRegistry> registryList) {
            this.registryList = List.copyOf(registryList);
        }

        @Nullable
        @Override
        public DsonCodecImpl<?> getEncoder(TypeInfo typeInfo, DsonCodecRegistry rootRegistry, IGenericCodecHelper genericCodecHelper) {
            List<DsonCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.size(); i++) {
                DsonCodecRegistry registry = registryList.get(i);
                DsonCodecImpl<?> codec = registry.getEncoder(typeInfo, rootRegistry, genericCodecHelper);
                if (codec != null) return codec;
            }
            return null;
        }

        @Nullable
        @Override
        public DsonCodecImpl<?> getDecoder(TypeInfo typeInfo, DsonCodecRegistry rootRegistry, IGenericCodecHelper genericCodecHelper) {
            List<DsonCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.size(); i++) {
                DsonCodecRegistry registry = registryList.get(i);
                DsonCodecImpl<?> codec = registry.getDecoder(typeInfo, rootRegistry, genericCodecHelper);
                if (codec != null) return codec;
            }
            return null;
        }
    }

}