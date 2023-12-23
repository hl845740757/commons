/*
 * Copyright 2023 wjybxx(845740757@qq.com)
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

package cn.wjybxx.common.codec.binary;

import cn.wjybxx.common.codec.*;
import cn.wjybxx.common.codec.codecs.*;

import javax.annotation.Nullable;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

/**
 * @author wjybxx
 * date 2023/4/1
 */
public class BinaryConverterUtils extends ConverterUtils {

    /** 类型id注册表 */
    private static final TypeMetaRegistry TYPE_META_REGISTRY;
    /** 默认codec注册表 */
    private static final BinaryCodecRegistry CODEC_REGISTRY;

    static {
        // 基础类型的数组codec用于避免拆装箱，提高性能
        List<BinaryPojoCodec<?>> entryList = List.of(
                newCodec(new IntArrayCodec()),
                newCodec(new LongArrayCodec()),
                newCodec(new FloatArrayCodec()),
                newCodec(new DoubleArrayCodec()),
                newCodec(new BooleanArrayCodec()),
                newCodec(new StringArrayCodec()),
                newCodec(new ShortArrayCodec()),
                newCodec(new CharArrayCodec()),

                newCodec(new ObjectArrayCodec()),
                newCodec(new CollectionCodec<>(Collection.class, null)),
                newCodec(new MapCodec<>(Map.class, null)),

                // 常用具体类型集合 -- 不含默认解码类型的超类
                newCodec(new CollectionCodec<>(LinkedList.class, LinkedList::new)),
                newCodec(new CollectionCodec<>(ArrayDeque.class, ArrayDeque::new)),
                newCodec(new MapCodec<>(IdentityHashMap.class, IdentityHashMap::new)),
                newCodec(new MapCodec<>(ConcurrentHashMap.class, ConcurrentHashMap::new))
        );

        Map<Class<?>, BinaryPojoCodec<?>> codecMap = BinaryCodecRegistries.newCodecMap(entryList);
        CODEC_REGISTRY = new DefaultCodecRegistry(codecMap);

        TYPE_META_REGISTRY = TypeMetaRegistries.fromMetas(
                TypeMeta.of(int[].class, ClassId.ofDefaultNameSpace(1)),
                TypeMeta.of(long[].class, ClassId.ofDefaultNameSpace(2)),
                TypeMeta.of(float[].class, ClassId.ofDefaultNameSpace(3)),
                TypeMeta.of(double[].class, ClassId.ofDefaultNameSpace(4)),
                TypeMeta.of(boolean[].class, ClassId.ofDefaultNameSpace(5)),
                TypeMeta.of(String[].class, ClassId.ofDefaultNameSpace(6)),
                TypeMeta.of(short[].class, ClassId.ofDefaultNameSpace(7)),
                TypeMeta.of(char[].class, ClassId.ofDefaultNameSpace(8)),

                TypeMeta.of(Object[].class, ClassId.ofDefaultNameSpace(11)),
                TypeMeta.of(Collection.class, ClassId.ofDefaultNameSpace(12)),
                TypeMeta.of(Map.class, ClassId.ofDefaultNameSpace(13)),

                // 常用具体类型集合
                TypeMeta.of(LinkedList.class, ClassId.ofDefaultNameSpace(21)),
                TypeMeta.of(ArrayDeque.class, ClassId.ofDefaultNameSpace(22)),
                TypeMeta.of(IdentityHashMap.class, ClassId.ofDefaultNameSpace(23)),
                TypeMeta.of(ConcurrentHashMap.class, ClassId.ofDefaultNameSpace(24))

        );
    }

    private static <T> BinaryPojoCodec<T> newCodec(BinaryPojoCodecImpl<T> codecImpl) {
        return new BinaryPojoCodec<>(codecImpl);
    }

    public static BinaryCodecRegistry getDefaultCodecRegistry() {
        return CODEC_REGISTRY;
    }

    public static TypeMetaRegistry getDefaultTypeMetaRegistry() {
        return TYPE_META_REGISTRY;
    }

    @SuppressWarnings({"rawtypes", "unchecked"})
    private static class DefaultCodecRegistry implements BinaryCodecRegistry {

        final Map<Class<?>, BinaryPojoCodec<?>> codecMap;

        final BinaryPojoCodec<Object[]> objectArrayCodec;
        final BinaryPojoCodec<Collection> collectionCodec;
        final BinaryPojoCodec<Map> mapCodec;

        private DefaultCodecRegistry(Map<Class<?>, BinaryPojoCodec<?>> codecMap) {
            this.codecMap = codecMap;

            this.objectArrayCodec = getCodec(codecMap, Object[].class);
            this.collectionCodec = getCodec(codecMap, Collection.class);
            this.mapCodec = getCodec(codecMap, Map.class);
        }

        private static <T> BinaryPojoCodec<T> getCodec(Map<Class<?>, BinaryPojoCodec<?>> codecMap, Class<T> clazz) {
            BinaryPojoCodec<T> codec = (BinaryPojoCodec<T>) codecMap.get(clazz);
            if (codec == null) throw new IllegalArgumentException(clazz.getName());
            return codec;
        }

        @Nullable
        @Override
        public <T> BinaryPojoCodec<T> get(Class<T> clazz) {
            BinaryPojoCodec<?> codec = codecMap.get(clazz);
            if (codec != null) return (BinaryPojoCodec<T>) codec;

            if (clazz.isArray()) return (BinaryPojoCodec<T>) objectArrayCodec;
            if (Collection.class.isAssignableFrom(clazz)) return (BinaryPojoCodec<T>) collectionCodec;
            if (Map.class.isAssignableFrom(clazz)) return (BinaryPojoCodec<T>) mapCodec;
            return null;
        }
    }

}