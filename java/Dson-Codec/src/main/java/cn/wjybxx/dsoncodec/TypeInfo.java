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
 * <h3>编码-有限泛型</h3>
 * 由于Java是伪泛型，在编译时会擦除类型信息，因此在运行时无法获得对象的完整类型信息 —— APT也无法做到。
 * 因此编码时无法写入完整的泛型参数信息，因此在跨语言通信时，应当避免【泛型递归】，即禁止泛型参数也是泛型 -- 最好彻底避免泛型。
 *
 * <h3>解码-完整泛型</h3>
 * 虽然我们无法在运行时导出对象的完整的泛型信息，但编辑器可以 —— 编辑器中的泛型都是已构造泛型。
 * 编辑器导出的Dson文件包含完整的泛型信息是有意义的，这使得我们在Java和C#端都可以精确解析Dson文件。
 * 要精确解析Dson文件中的clsName，我们的TypeInfo必须和{@link ClassName}一样是递归的。
 *
 * <h3>数组泛型信息</h3>
 * 注意：由于{@link Class#getComponentType()}不包含泛型信息，
 * 而我们需要这部分数据，因此我们将数组的泛型信息也存储在{@link #typeArgs}中，
 * 因此不能简单根据泛型参数个数判断是否是泛型类，请通过{@link #isGenericType()}判断。
 * ps：数组不是泛型类。
 *
 * @author wjybxx
 * date 2023/3/31
 */
@Immutable
@SuppressWarnings({"unused"})
public final class TypeInfo {

    /** 原始类型 */
    public final Class<?> rawType;
    /** 泛型参数信息 */
    public final List<TypeInfo> typeArgs;

    private TypeInfo(Class<?> rawType) {
        this.rawType = Objects.requireNonNull(rawType);
        this.typeArgs = List.of();
    }

    private TypeInfo(Class<?> rawType, boolean mutable) {
        this.rawType = Objects.requireNonNull(rawType);
        this.typeArgs = mutable ? new ArrayList<>() : List.of();
    }

    private TypeInfo(Class<?> rawType, List<TypeInfo> typeArgs) {
        this.rawType = Objects.requireNonNull(rawType);
        this.typeArgs = List.copyOf(typeArgs);
    }

    private TypeInfo(Class<?> rawType, Class<?> typeArg1) {
        this.rawType = Objects.requireNonNull(rawType);
        this.typeArgs = List.of(TypeInfo.of(typeArg1));
    }

    private TypeInfo(Class<?> rawType, Class<?> typeArg1, Class<?> typeArg2) {
        this.rawType = Objects.requireNonNull(rawType);
        this.typeArgs = List.of(TypeInfo.of(typeArg1), TypeInfo.of(typeArg2));
    }

    // 用于动态解析时
    static TypeInfo newMutable(Class<?> rawType) {
        return new TypeInfo(rawType, true);
    }

    TypeInfo toImmutable() {
        if (typeArgs.isEmpty()) {
            return new TypeInfo(rawType);
        }
        for (int i = 0; i < typeArgs.size(); i++) {
            typeArgs.set(i, typeArgs.get(i).toImmutable());
        }
        return new TypeInfo(rawType, List.copyOf(typeArgs));
    }

    // region api

    /** 是否是基础类型 */
    public boolean isPrimitive() {
        return rawType.isPrimitive();
    }

    /** 是否是枚举 */
    public boolean isEnum() {
        return rawType.isEnum();
    }

    /**
     * 是否是泛型类
     * 注意：这不代表{@link #rawType}是泛型类，使用时务必小心。
     */
    public boolean isGenericType() {
        return !rawType.isArray() && !typeArgs.isEmpty();
    }

    /** 获取泛型原型 */
    public TypeInfo getGenericTypeDefinition() {
        if (isGenericType()) {
            return new TypeInfo(rawType);
        }
        throw new IllegalStateException("This operation is only valid on generic types");
    }

    /** 获取泛型参数 */
    public TypeInfo getGenericArgument(int idx) {
        return typeArgs.get(idx);
    }

    /** 是否是数组 */
    public boolean isArray() {
        return rawType.isArray();
    }

    /** 获取数组的元素类型 */
    public TypeInfo getComponentType() {
        if (rawType.isArray()) {
            return new TypeInfo(rawType.getComponentType(), typeArgs);  // 继承泛型信息
        }
        return null;
    }

    /** 构建数组类型 */
    public TypeInfo makeArrayType() {
        return new TypeInfo(rawType.arrayType(), typeArgs); // 继承泛型信息
    }

    // endregion

    // region 常量

    /** 这里不能调用of...因为of可能返回该对象 */
    public static final TypeInfo OBJECT = new TypeInfo(Object.class);
    public static final TypeInfo INTEGER = new TypeInfo(Integer.class);
    public static final TypeInfo LONG = new TypeInfo(Long.class);
    public static final TypeInfo STRING = new TypeInfo(String.class);

    public static final TypeInfo ARRAYLIST =
            new TypeInfo(ArrayList.class, Object.class);
    public static final TypeInfo LINKED_HASHSET =
            new TypeInfo(LinkedHashSet.class, Object.class);

    public static final TypeInfo LINKED_HASHMAP =
            new TypeInfo(LinkedHashMap.class, Object.class, Object.class);
    public static final TypeInfo STRING_LINKED_HASHMAP =
            new TypeInfo(LinkedHashMap.class, String.class, Object.class);

    public static final TypeInfo HASHMAP =
            new TypeInfo(HashMap.class, Object.class, Object.class);
    public static final TypeInfo STRING_HASHMAP =
            new TypeInfo(HashMap.class, String.class, Object.class);

    // endregion

    // region 工厂方法

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType) {
        if (rawType == Object.class) {
            return OBJECT;
        }
        if (rawType == String.class) {
            return STRING;
        }
        if (rawType == Integer.class) {
            return INTEGER;
        }
        if (rawType == Long.class) {
            return LONG;
        }
        return new TypeInfo(rawType);
    }

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType, Class<?> typeArg1) {
        return new TypeInfo(rawType, List.of(typeArg1));
    }

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType, Class<?> typeArg1, Class<?> typeArg2) {
        return ofGeneric(rawType, TypeInfo.of(typeArg1), TypeInfo.of(typeArg2))
    }

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType, List<Class<?>> typeArgs) {
        List<TypeInfo> typeInfos = new ArrayList<>(typeArgs.size());
        for (int i = 0; i < typeArgs.size(); i++) {
            typeInfos.add(TypeInfo.of(typeArgs.get(i)));
        }
        return new TypeInfo(rawType, List.copyOf(typeInfos));
    }

    public static TypeInfo ofGeneric(Class<?> rawType, TypeInfo typeArg1) {
        return new TypeInfo(rawType, List.of(typeArg1));
    }

    public static TypeInfo ofGeneric(Class<?> rawType, TypeInfo typeArg1, TypeInfo typeArg2) {
        return new TypeInfo(rawType, List.of(typeArg1, typeArg2));
    }

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo ofGeneric(Class<?> rawType, List<TypeInfo<?>> typeArgs) {
        return new TypeInfo(rawType, List.copyOf(typeArgs));
    }


    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType, Class<?> typeArg1, Class<?> typeArg2, Class<?> typeArg3) {
        return new TypeInfo(rawType, List.of(typeArg1, typeArg2, typeArg3));
    }

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType, Class<?>... typeArgs) {
        return new TypeInfo(rawType, List.of(typeArgs));
    }


    // endregion

    // region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;

        TypeInfo typeInfo = (TypeInfo) o;

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