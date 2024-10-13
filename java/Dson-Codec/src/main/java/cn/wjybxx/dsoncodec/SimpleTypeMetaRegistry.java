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
 * date - 2024/10/13
 */
public final class SimpleTypeMetaRegistry implements TypeMetaRegistry {

    private final Map<TypeInfo, TypeMeta> type2MetaMap;
    private final Map<String, TypeMeta> name2MetaMap;

    public SimpleTypeMetaRegistry() {
        type2MetaMap = HashMap.newHashMap(64);
        name2MetaMap = HashMap.newHashMap(64);
    }

    private SimpleTypeMetaRegistry(Map<TypeInfo, TypeMeta> type2MetaMap,
                                   Map<String, TypeMeta> name2MetaMap) {
        this.type2MetaMap = Map.copyOf(type2MetaMap);
        this.name2MetaMap = Map.copyOf(name2MetaMap);
    }

    // region factory

    public static SimpleTypeMetaRegistry fromMapper(Set<Class<?>> typeSet, Function<Class<?>, TypeMeta> mapper) {
        SimpleTypeMetaRegistry registry = new SimpleTypeMetaRegistry();
        for (Class<?> clazz : typeSet) {
            TypeMeta typeMeta = mapper.apply(clazz);
            if (typeMeta.typeInfo.rawType != clazz) {
                throw new RuntimeException("type: " + clazz);
            }
            registry.add(typeMeta);
        }
        return registry.toImmutable();
    }

    public static SimpleTypeMetaRegistry fromMetas(TypeMeta... typeMetas) {
        return new SimpleTypeMetaRegistry().addAll(Arrays.asList(typeMetas))
                .toImmutable();
    }

    public static SimpleTypeMetaRegistry fromMetas(List<TypeMeta> typeMetas) {
        return new SimpleTypeMetaRegistry().addAll(typeMetas)
                .toImmutable();
    }

    public static SimpleTypeMetaRegistry fromRegistries(List<? extends TypeMetaRegistry> registries) {
        SimpleTypeMetaRegistry result = new SimpleTypeMetaRegistry();
        for (TypeMetaRegistry other : registries) {
            result.addAll(other.export());
        }
        return result.toImmutable();
    }

    /** 转为不可变实例 */
    public SimpleTypeMetaRegistry toImmutable() {
        return new SimpleTypeMetaRegistry(type2MetaMap, name2MetaMap);
    }

    // endregion

    // region update

    public void clear() {
        type2MetaMap.clear();
        name2MetaMap.clear();
    }

    public SimpleTypeMetaRegistry mergeFrom(TypeMetaRegistry other) {
        for (TypeMeta typeMeta : other.export()) {
            add(typeMeta);
        }
        return this;
    }

    public SimpleTypeMetaRegistry addAll(List<TypeMeta> typeMetas) {
        for (TypeMeta typeMeta : typeMetas) {
            add(typeMeta);
        }
        return this;
    }

    public SimpleTypeMetaRegistry add(TypeMeta typeMeta) {
        TypeInfo typeInfo = typeMeta.typeInfo;
        TypeMeta exist = type2MetaMap.get(typeInfo);
        if (exist != null) {
            if (exist.equals(typeMeta)) {
                return this;
            }
            // TypeMeta冲突需要用户解决 -- Codec的冲突是无害的，而TypeMeta的冲突是有害的
            throw new IllegalArgumentException("type conflict, type: %s".formatted(typeInfo));
        }
        type2MetaMap.put(typeInfo, typeMeta);

        for (String clsName : typeMeta.clsNames) {
            if (name2MetaMap.containsKey(clsName)) {
                throw new IllegalArgumentException("clsName conflict, type: %s, clsName: %s".formatted(typeInfo, clsName));
            }
            name2MetaMap.put(clsName, typeMeta);
        }
        return this;
    }

    /** 删除给定类型的TypeMeta，主要用于解决冲突 */
    public TypeMeta remove(TypeInfo typeInfo) {
        TypeMeta typeMeta = type2MetaMap.remove(typeInfo);
        if (typeMeta != null) {
            for (String clsName : typeMeta.clsNames) {
                name2MetaMap.remove(clsName);
            }
        }
        return typeMeta;
    }

    // endregion

    @Nullable
    @Override
    public TypeMeta ofType(TypeInfo type) {
        return type2MetaMap.get(type);
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