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
import java.util.function.Function;

/**
 * @author wjybxx
 * date - 2023/4/26
 */
public class TypeMetaRegistries {

    /**
     * @param typeSet 类型集合
     * @param mapper  类型到元数据的映射函数
     */
    public static TypeMetaRegistry fromMapper(final Set<Class<?>> typeSet, Function<Class<?>, TypeMeta> mapper) {
        List<TypeMeta> typeMetaList = new ArrayList<>();
        for (Class<?> clazz : typeSet) {
            TypeMeta typeMeta = mapper.apply(clazz);
            assert typeMeta.typeInfo.rawType == clazz;
            typeMetaList.add(typeMeta);
        }
        return fromMetas(typeMetaList);
    }

    public static TypeMetaRegistry fromRegistries(TypeMetaRegistry... registries) {
        List<TypeMeta> typeMetaList = new ArrayList<>();
        for (TypeMetaRegistry e : registries) {
            typeMetaList.addAll(e.export());
        }
        return fromMetas(typeMetaList);
    }

    public static TypeMetaRegistry fromMetas(TypeMeta... typeMetas) {
        return fromMetas(Arrays.asList(typeMetas));
    }

    public static TypeMetaRegistry fromMetas(List<TypeMeta> typeMetaList) {
        final IdentityHashMap<TypeInfo<?>, TypeMeta> type2MetaMap = new IdentityHashMap<>(typeMetaList.size());
        final IdentityHashMap<Class<?>, TypeMeta> clazz2MetaMap = new IdentityHashMap<>(typeMetaList.size());
        final HashMap<String, TypeMeta> name2MetaMap = HashMap.newHashMap(typeMetaList.size());
        for (TypeMeta typeMeta : typeMetaList) {
            TypeInfo<?> typeInfo = typeMeta.typeInfo;
            if (type2MetaMap.containsKey(typeInfo)) {
                throw new IllegalArgumentException("type %s is duplicate".formatted(typeInfo));
            }
            type2MetaMap.put(typeInfo, typeMeta);
            // 非泛型额外注册
            if (!typeInfo.isGenericType()) {
                if (clazz2MetaMap.containsKey(typeInfo.rawType)) {
                    throw new IllegalArgumentException("type %s is duplicate".formatted(typeInfo));
                }
                clazz2MetaMap.put(typeInfo.rawType, typeMeta);
            }
            // 注册className
            for (String clsName : typeMeta.clsNames) {
                if (name2MetaMap.containsKey(clsName)) {
                    throw new IllegalArgumentException("clsName %s is duplicate".formatted(clsName));
                }
                name2MetaMap.put(clsName, typeMeta);
            }
        }
        return new TypeMetaRegistryImpl(type2MetaMap, clazz2MetaMap, name2MetaMap);
    }

    private static class TypeMetaRegistryImpl implements TypeMetaRegistry {


        private final Map<TypeInfo<?>, TypeMeta> type2MetaMap;
        private final Map<Class<?>, TypeMeta> clazz2MetaMap;
        private final Map<String, TypeMeta> name2MetaMap;

        TypeMetaRegistryImpl(Map<TypeInfo<?>, TypeMeta> type2MetaMap, Map<Class<?>, TypeMeta> clazz2MetaMap,
                             Map<String, TypeMeta> name2MetaMap) {
            this.type2MetaMap = type2MetaMap;
            this.clazz2MetaMap = clazz2MetaMap;
            this.name2MetaMap = name2MetaMap;
        }

        @Nullable
        @Override
        public TypeMeta ofType(TypeInfo<?> type) {
            return type2MetaMap.get(type);
        }

        @Nullable
        @Override
        public TypeMeta ofClass(Class<?> clazz) {
            return clazz2MetaMap.get(clazz);
        }

        @Override
        public TypeMeta ofName(String clsName) {
            return name2MetaMap.get(clsName);
        }

        @Override
        public List<TypeMeta> export() {
            return new ArrayList<>(type2MetaMap.values());
        }
    }
}