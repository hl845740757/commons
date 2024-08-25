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


import cn.wjybxx.base.annotation.StableName;

import javax.annotation.concurrent.Immutable;
import java.util.*;

/**
 * 类型信息
 *
 * <h3>有限泛型</h3>
 * 由于Java是伪泛型，而想通过外部库将类型信息省略做到极致，要付出极高的代价。
 * 因此我们禁止泛型递归，即禁止泛型参数也是泛型，因此{@link #typeArgs}不是{@link TypeInfo}类型。
 * 这种程度的支持已经够我们解决大部分问题了，特殊情况下由用户封装一层即可解决。
 *
 * <h3>数组泛型信息</h3>
 * 注意：由于{@link Class#getComponentType()}不包含泛型信息，
 * 而我们需要这部分数据，因此我们将数组的泛型信息也存储在{@link #typeArgs}中，
 * 因此不能简单根据泛型参数个数判断是否是泛型类，请通过{@link #isGenericType()}判断。
 * ps：数组不是泛型类。
 *
 * @param <T> T最好使用原始类型，不要再带有泛型，否则你会痛苦的 -- 因为 xxx.class是不包含泛型信息的。
 * @author wjybxx
 * date 2023/3/31
 */
@Immutable
@SuppressWarnings({"rawtypes", "unused"})
public final class TypeInfo<T> {

    /** 原始类型 */
    public final Class<T> rawType;
    /** 泛型参数信息 -- 不再递归 */
    public final List<Class<?>> typeArgs;

    private TypeInfo(Class<T> rawType) {
        this.rawType = Objects.requireNonNull(rawType);
        this.typeArgs = List.of();
    }

    private TypeInfo(Class<T> rawType, List<Class<?>> typeArgs) {
        this.rawType = Objects.requireNonNull(rawType);
        this.typeArgs = typeArgs;
    }

    private TypeInfo(Class<T> rawType, Class<?> typeArg1) {
        this.rawType = Objects.requireNonNull(rawType);
        this.typeArgs = List.of(typeArg1);
    }

    private TypeInfo(Class<T> rawType, Class<?> typeArg1, Class<?> typeArg2) {
        this.rawType = Objects.requireNonNull(rawType);
        this.typeArgs = List.of(typeArg1, typeArg2);
    }

    // region api

    /** 是否是基础类型 */
    public boolean isPrimitive() {
        return rawType.isPrimitive();
    }

    /**
     * 是否是泛型类
     * 注意：这不代表{@link #rawType}是泛型类，使用时务必小心。
     */
    public boolean isGenericType() {
        return !rawType.isArray() && !typeArgs.isEmpty();
    }

    /** 获取泛型原型 */
    public TypeInfo<?> getGenericTypeDefinition() {
        if (isGenericType()) {
            return new TypeInfo<>(rawType);
        }
        return null;
    }

    /** 获取泛型参数 */
    public TypeInfo<?> getGenericArgument(int idx) {
        return of(typeArgs.get(idx));
    }

    /** 是否是数组 */
    public boolean isArray() {
        return rawType.isArray();
    }

    /** 获取数组的元素类型 */
    public TypeInfo<?> getComponentType() {
        if (rawType.isArray()) {
            return new TypeInfo<>(rawType.getComponentType(), typeArgs);  // 继承泛型信息
        }
        return null;
    }

    /** 构建数组类型 */
    public TypeInfo<?> makeArrayType() {
        return new TypeInfo<>(rawType.arrayType(), typeArgs); // 继承泛型信息
    }

    // endregion

    // region 常量

    /** 这里不能调用of...因为of可能返回该对象 */
    public static final TypeInfo<Object> OBJECT = new TypeInfo<>(Object.class);
    public static final TypeInfo<Integer> INTEGER = new TypeInfo<>(Integer.class);
    public static final TypeInfo<Long> LONG = new TypeInfo<>(Long.class);
    public static final TypeInfo<String> STRING = new TypeInfo<>(String.class);
    /** 表示不写入类型信息 */
    public static final TypeInfo<Object> NONE = new TypeInfo<>(Object.class);

    public static final TypeInfo<ArrayList> ARRAYLIST =
            new TypeInfo<>(ArrayList.class, Object.class);
    public static final TypeInfo<LinkedHashSet> LINKED_HASHSET =
            new TypeInfo<>(LinkedHashSet.class, Object.class);

    public static final TypeInfo<LinkedHashMap> LINKED_HASHMAP =
            new TypeInfo<>(LinkedHashMap.class, Object.class, Object.class);
    public static final TypeInfo<LinkedHashMap> STRING_LINKED_HASHMAP =
            new TypeInfo<>(LinkedHashMap.class, String.class, Object.class);

    public static final TypeInfo<HashMap> HASHMAP =
            new TypeInfo<>(HashMap.class, Object.class, Object.class);
    public static final TypeInfo<HashMap> STRING_HASHMAP =
            new TypeInfo<>(HashMap.class, String.class, Object.class);

    // endregion

    // region 工厂方法

    @SuppressWarnings("unchecked")
    @StableName(comment = "生成的代码会调用")
    public static <T> TypeInfo<T> of(Class<T> rawType) {
        if (rawType == Object.class) {
            return (TypeInfo<T>) OBJECT;
        }
        if (rawType == String.class) {
            return (TypeInfo<T>) STRING;
        }
        if (rawType == Integer.class) {
            return (TypeInfo<T>) INTEGER;
        }
        if (rawType == Long.class) {
            return (TypeInfo<T>) LONG;
        }
        return new TypeInfo<>(rawType);
    }

    @StableName(comment = "生成的代码会调用")
    public static <T> TypeInfo<T> of(Class<T> rawType, Class<?> typeArg1) {
        return new TypeInfo<>(rawType, List.of(typeArg1));
    }

    @StableName(comment = "生成的代码会调用")
    public static <T> TypeInfo<T> of(Class<T> rawType, Class<?> typeArg1, Class<?> typeArg2) {
        return new TypeInfo<>(rawType, List.of(typeArg1, typeArg2));
    }

    @StableName(comment = "生成的代码会调用")
    public static <T> TypeInfo<T> of(Class<T> rawType, Class<?> typeArg1, Class<?> typeArg2, Class<?> typeArg3) {
        return new TypeInfo<>(rawType, List.of(typeArg1, typeArg2, typeArg3));
    }

    @StableName(comment = "生成的代码会调用")
    public static <T> TypeInfo<T> of(Class<T> rawType, Class<?>... typeArgs) {
        return new TypeInfo<>(rawType, List.of(typeArgs));
    }

    @StableName(comment = "生成的代码会调用")
    public static <T> TypeInfo<T> of(Class<T> rawType, List<Class<?>> typeArgs) {
        return new TypeInfo<>(rawType, List.copyOf(typeArgs));
    }

    // endregion

    // region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        TypeInfo<?> typeInfo = (TypeInfo<?>) o;

        if (!rawType.equals(typeInfo.rawType)) return false;
        return typeArgs.equals(typeInfo.typeArgs);
    }

    @Override
    public int hashCode() {
        int result = rawType.hashCode();
        result = 31 * result + typeArgs.hashCode();
        return result;
    }

    @Override
    public String toString() {
        return "TypeInfo{" +
                "rawType=" + rawType +
                ", typeArgs=" + typeArgs +
                '}';
    }

// endregion
}