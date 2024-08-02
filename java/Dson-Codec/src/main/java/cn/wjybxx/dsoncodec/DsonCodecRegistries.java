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
import java.util.IdentityHashMap;
import java.util.List;
import java.util.Map;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public class DsonCodecRegistries {

    public static Map<Class<?>, DsonCodecImpl<?>> newCodecMap(List<? extends DsonCodecImpl<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, DsonCodecImpl<?>> codecMap = new IdentityHashMap<>(pojoCodecs.size());
        for (DsonCodecImpl<?> codec : pojoCodecs) {
            if (codecMap.containsKey(codec.getEncoderClass())) {
                throw new IllegalArgumentException("the class has multiple codecs :" + codec.getEncoderClass().getName());
            }
            codecMap.put(codec.getEncoderClass(), codec);
        }
        return codecMap;
    }

    public static DsonCodecRegistry fromCodecs(DsonCodecImpl<?>... pojoCodecs) {
        return fromCodecs(List.of(pojoCodecs));
    }

    public static DsonCodecRegistry fromCodecs(List<? extends DsonCodecImpl<?>> pojoCodecs) {
        final IdentityHashMap<Class<?>, DsonCodecImpl<?>> codecMap = new IdentityHashMap<>(pojoCodecs.size());
        for (DsonCodecImpl<?> codec : pojoCodecs) {
            if (codecMap.containsKey(codec.getEncoderClass())) {
                throw new IllegalArgumentException("the class has multiple codecs :" + codec.getEncoderClass().getName());
            }
            codecMap.put(codec.getEncoderClass(), codec);
        }
        return new DefaultCodecRegistry(codecMap);
    }

    public static DsonCodecRegistry fromRegistries(DsonCodecRegistry... codecRegistry) {
        return new CompositeCodecRegistry(List.of(codecRegistry)); // 拷贝
    }

    public static DsonCodecRegistry fromRegistries(List<DsonCodecRegistry> codecRegistry) {
        return new CompositeCodecRegistry(List.copyOf(codecRegistry)); // 拷贝
    }

    private static class DefaultCodecRegistry implements DsonCodecRegistry {

        private final IdentityHashMap<Class<?>, DsonCodecImpl<?>> type2CodecMap;

        private DefaultCodecRegistry(IdentityHashMap<Class<?>, DsonCodecImpl<?>> type2CodecMap) {
            this.type2CodecMap = type2CodecMap;
        }

        @SuppressWarnings("unchecked")
        @Nullable
        @Override
        public <T> DsonCodecImpl<? super T> getEncoder(Class<T> clazz, DsonCodecRegistry rootRegistry) {
            return (DsonCodecImpl<? super T>) type2CodecMap.get(clazz);
        }

        @SuppressWarnings("unchecked")
        @Override
        public <T> DsonCodecImpl<T> getDecoder(Class<T> clazz, DsonCodecRegistry rootRegistry) {
            return (DsonCodecImpl<T>) type2CodecMap.get(clazz);
        }

    }

    private static class CompositeCodecRegistry implements DsonCodecRegistry {

        private final List<DsonCodecRegistry> registryList;

        private CompositeCodecRegistry(List<DsonCodecRegistry> registryList) {
            this.registryList = registryList;
        }

        @Nullable
        @Override
        public <T> DsonCodecImpl<? super T> getEncoder(Class<T> clazz, DsonCodecRegistry rootRegistry) {
            List<DsonCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.size(); i++) {
                DsonCodecRegistry registry = registryList.get(i);
                DsonCodecImpl<? super T> codec = registry.getEncoder(clazz, rootRegistry);
                if (codec != null) return codec;
            }
            return null;
        }

        @Nullable
        @Override
        public <T> DsonCodecImpl<T> getDecoder(Class<T> clazz, DsonCodecRegistry rootRegistry) {
            List<DsonCodecRegistry> registryList = this.registryList;
            for (int i = 0; i < registryList.size(); i++) {
                DsonCodecRegistry registry = registryList.get(i);
                DsonCodecImpl<T> codec = registry.getDecoder(clazz, rootRegistry);
                if (codec != null) return codec;
            }
            return null;
        }
    }

}