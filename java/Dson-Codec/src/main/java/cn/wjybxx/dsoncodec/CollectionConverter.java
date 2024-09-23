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

import cn.wjybxx.base.CollectionUtils;

import java.util.*;

/**
 * 集合转换器
 * 主要用于实现读取为不可变集合。
 *
 * @author wjybxx
 * date - 2024/9/23
 */
public interface CollectionConverter {

    /**
     * 转换字典
     *
     * @param typeInfo 类型声明信息
     * @param map      待转换的字典
     * @return 转换后的map
     */
    <K, V> Map<K, V> convertMap(TypeInfo<?> typeInfo, Map<K, V> map);

    /**
     * 转换集合
     *
     * @param typeInfo   类型声明信息
     * @param collection 待转换的集合
     * @return 转换后的map
     */
    <E> Collection<E> convertCollection(TypeInfo<?> typeInfo, Collection<E> collection);


    static CollectionConverter immutableConverter() {
        return ImmutableConverter.INST;
    }

    class ImmutableConverter implements CollectionConverter {

        public static final ImmutableConverter INST = new ImmutableConverter();

        private ImmutableConverter() {
        }

        @Override
        public <K, V> Map<K, V> convertMap(TypeInfo<?> typeInfo, Map<K, V> map) {
            if (map instanceof LinkedHashMap<K, V> hashMap) {
                return Collections.unmodifiableMap(hashMap); // 保持插入序
            }
            return CollectionUtils.toImmutableLinkedHashMap(map);
        }

        @Override
        public <E> Collection<E> convertCollection(TypeInfo<?> typeInfo, Collection<E> collection) {
            if (collection instanceof LinkedHashSet<E> hashSet) {
                return Collections.unmodifiableSet(hashSet); // 保持插入序
            }
            if (typeInfo.rawType == Set.class) {
                return CollectionUtils.toImmutableLinkedHashSet(collection);
            }
            return List.copyOf(collection);
        }
    }
}