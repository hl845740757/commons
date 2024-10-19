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

import cn.wjybxx.dson.text.DsonTexts;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.types.*;

import java.util.*;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 类型元数据配置
 * <p>
 * 用户在初始化Config时无需处理泛型类的TypeMeta，底层会动态生成对应的TypeMeta，
 * 用户只需要保证使用到的所有原始类型都注册了即可。
 *
 * <h3>合并规则</h3>
 * 多个Config合并时，越靠近用户，优先级越高 -- 因为这一定能解决冲突。
 *
 * @author wjybxx
 * date - 2024/10/13
 */
public final class TypeMetaConfig {

    private final Map<TypeInfo, TypeMeta> type2MetaMap;
    private final Map<String, TypeMeta> name2MetaMap; // 主要用于检测冲突

    public TypeMetaConfig() {
        type2MetaMap = new HashMap<>(32);
        name2MetaMap = new HashMap<>(32);
    }

    private TypeMetaConfig(TypeMetaConfig other) {
        this.type2MetaMap = Map.copyOf(other.type2MetaMap);
        this.name2MetaMap = Map.copyOf(other.name2MetaMap);
    }

    public Map<TypeInfo, TypeMeta> getType2MetaMap() {
        return type2MetaMap;
    }

    public Map<String, TypeMeta> getName2MetaMap() {
        return name2MetaMap;
    }

    // region factory

    public static TypeMetaConfig fromTypeMetas(TypeMeta... typeMetas) {
        return new TypeMetaConfig().addAll(Arrays.asList(typeMetas))
                .toImmutable();
    }

    public static TypeMetaConfig fromTypeMetas(Collection<TypeMeta> typeMetas) {
        return new TypeMetaConfig().addAll(typeMetas)
                .toImmutable();
    }

    public static TypeMetaConfig fromConfigs(Collection<? extends TypeMetaConfig> configs) {
        TypeMetaConfig result = new TypeMetaConfig();
        for (TypeMetaConfig other : configs) {
            result.mergeFrom(other);
        }
        return result.toImmutable();
    }

    /** 转为不可变实例 */
    public TypeMetaConfig toImmutable() {
        if (type2MetaMap instanceof HashMap<TypeInfo, TypeMeta>) {
            return new TypeMetaConfig(this);
        }
        return this;
    }

    // endregion

    // region update

    public void clear() {
        type2MetaMap.clear();
        name2MetaMap.clear();
    }

    public TypeMetaConfig mergeFrom(TypeMetaConfig other) {
        if (this == other) {
            throw new IllegalArgumentException();
        }
        for (TypeMeta typeMeta : other.type2MetaMap.values()) {
            add(typeMeta);
        }
        return this;
    }

    public TypeMetaConfig addAll(Collection<TypeMeta> typeMetas) {
        for (TypeMeta typeMeta : typeMetas) {
            add(typeMeta);
        }
        return this;
    }


    /** 添加TypeMeta，会检测冲突 */
    public TypeMetaConfig add(TypeMeta typeMeta) {
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

    // 以下为快捷方法
    public TypeMetaConfig add(Class<?> type, String clsName) {
        add(TypeMeta.of(type, ObjectStyle.INDENT, clsName));
        return this;
    }

    public TypeMetaConfig add(Class<?> type, String... clsName) {
        add(TypeMeta.of(type, ObjectStyle.INDENT, clsName));
        return this;
    }

    public TypeMetaConfig add(Class<?> type, ObjectStyle style, String clsName) {
        add(TypeMeta.of(type, style, clsName));
        return this;
    }

    public TypeMetaConfig add(Class<?> type, ObjectStyle style, String... clsName) {
        add(TypeMeta.of(type, style, clsName));
        return this;
    }

    // endregion

    // region query

    public TypeMeta ofType(TypeInfo type) {
        return type2MetaMap.get(type);
    }

    public TypeMeta ofName(String clsName) {
        return name2MetaMap.get(clsName);
    }
    // endregion

    // region 默认配置

    public static final TypeMetaConfig DEFAULT = newDefaultConfig().toImmutable();

    /**
     * 创建一个默认配置
     * 1.只包含基础的类型，其它都需要用户分配
     * 2.clsName并不总是等于类型名，以方便跨语言交互
     */
    public static TypeMetaConfig newDefaultConfig() {
        return newDefaultConfig(true);
    }

    /**
     * 创建一个默认配置
     * 由于集合的命名难以统一，因此作为可选项。
     *
     * @param includeCollections 是否包含集合数据
     */
    public static TypeMetaConfig newDefaultConfig(boolean includeCollections) {
        TypeMetaConfig config = new TypeMetaConfig();
        // dson内建结构
        config.add(int.class, DsonTexts.LABEL_INT32, "int", "int32", "ui", "uint", "uint32");
        config.add(long.class, DsonTexts.LABEL_INT64, "long", "int64", "uL", "ulong", "uint64");
        config.add(float.class, DsonTexts.LABEL_FLOAT, "float");
        config.add(double.class, DsonTexts.LABEL_DOUBLE, "double");
        config.add(boolean.class, DsonTexts.LABEL_BOOL, "bool", "boolean");
        config.add(String.class, DsonTexts.LABEL_STRING, "string");
        config.add(Binary.class, DsonTexts.LABEL_BINARY, "bytes");
        config.add(ObjectPtr.class, DsonTexts.LABEL_PTR);
        config.add(ObjectLitePtr.class, DsonTexts.LABEL_LITE_PTR);
        config.add(ExtDateTime.class, DsonTexts.LABEL_DATETIME);
        config.add(Timestamp.class, DsonTexts.LABEL_TIMESTAMP);
        // 基础类型
        config.add(short.class, "short", "int16", "uint16");
        config.add(byte.class, "byte", "sbyte");
        config.add(char.class, "char");
        // 装箱类型--要和c#互通
        config.add(Integer.class, "Int");
        config.add(Long.class, "Long");
        config.add(Float.class, "Float");
        config.add(Double.class, "Double");
        config.add(Boolean.class, "Bool");
        config.add(Short.class, "Short");
        config.add(Byte.class, "Byte");
        config.add(Character.class, "Char");
        config.add(Number.class, "Number");
        config.add(Object.class, "object", "Object"); // object会作为泛型参数...

        // 基础集合
        if (includeCollections) {
            config.add(Collection.class, "ICollection", "ICollection`1");
            config.add(List.class, "IList", "IList`1");
            config.add(ArrayList.class, "List", "List`1");

            config.add(Map.class, "IDictionary", "IDictionary`2");
            config.add(HashMap.class, "HashMap", "HashMap`2"); // c#的字典有毒，不删除的情况下有序，导致Java映射困难
            config.add(LinkedHashMap.class, "Dictionary", "Dictionary`2", "LinkedDictionary", "LinkedDictionary`2");
            config.add(ConcurrentHashMap.class, "ConcurrentDictionary", "ConcurrentDictionary`2");

            config.add(MapEncodeProxy.class, "DictionaryEncodeProxy", "MapEncodeProxy"); // 字典读写代理
        }
        return config;
    }

    // endregion
}