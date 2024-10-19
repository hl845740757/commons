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


import cn.wjybxx.base.ArrayUtils;
import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.annotation.StableName;

import javax.annotation.concurrent.Immutable;
import java.util.*;

/**
 * 类型信息
 *
 * <h3>编码-有限泛型</h3>
 * 由于Java是伪泛型，在运行时无法获得对象的完整类型信息，因此编码时无法总是精确写入完整的泛型参数信息；
 * 如果字段的泛型参数在编译期是已知的，则APT可精确生成其类型信息，eg：{@code Map<String, Task<Blackboard>}；
 * <p>
 * 如果想尽可能精确的写入泛型信息，需要扩展{@link GenericHelper}。
 *
 * <h3>解码-完整泛型</h3>
 * 虽然我们无法在运行时导出对象的完整的泛型信息，但编辑器可以 —— 编辑器中的泛型都是已构造泛型。
 * 编辑器导出的Dson文件包含完整的泛型信息是有意义的，这使得我们在Java和C#端都可以精确解析Dson文件。
 * 要精确解析Dson文件中的clsName，我们的TypeInfo必须和{@link ClassName}一样是递归的。
 *
 * <h3>数组泛型信息</h3>
 * 注意：由于{@link Class#getComponentType()}不包含泛型信息，
 * 而我们需要这部分数据，因此我们将数组的泛型信息也存储在{@link #genericArgs}中，
 * 因此不能简单根据泛型参数个数判断是否是泛型类，请通过{@link #isConstructedGenericType()}判断。
 * ps：数组不是泛型类。
 *
 * @author wjybxx
 * date 2023/3/31
 */
@Immutable
@SuppressWarnings({"unused"})
public final class TypeInfo {

    /** 原始类型 -- 可能是基础类型 */
    public final Class<?> rawType;
    /** 泛型参数信息 -- 当不为0时，应当和真实泛型参数个数相同 */
    public final List<TypeInfo> genericArgs;

    private TypeInfo(Class<?> rawType) {
        this.rawType = Objects.requireNonNull(rawType);
        this.genericArgs = List.of();
    }

    private TypeInfo(Class<?> rawType, TypeInfo typeArg1) {
        this.rawType = Objects.requireNonNull(rawType);
        this.genericArgs = List.of(typeArg1);
    }

    private TypeInfo(Class<?> rawType, TypeInfo typeArg1, TypeInfo typeArg2) {
        this.rawType = Objects.requireNonNull(rawType);
        this.genericArgs = List.of(typeArg1, typeArg2);
    }

    private TypeInfo(Class<?> rawType, List<TypeInfo> genericArgs) {
        this.rawType = Objects.requireNonNull(rawType);
        this.genericArgs = genericArgs;
    }

    // region api

    /** 是否是基础类型 */
    public boolean isPrimitive() {
        return rawType.isPrimitive();
    }

    /** 基础类型装箱 */
    public TypeInfo box() {
        if (rawType == int.class) return BOXED_INT;
        if (rawType == long.class) return BOXED_LONG;
        if (rawType == float.class) return BOXED_FLOAT;
        if (rawType == double.class) return BOXED_DOUBLE;
        if (rawType == boolean.class) return BOXED_BOOL;
        if (rawType == byte.class) return BOXED_BYTE;
        if (rawType == short.class) return BOXED_SHORT;
        if (rawType == char.class) return BOXED_CHAR;
        if (rawType == void.class) return BOXED_VOID;
        throw new RuntimeException();
    }

    /** 是否是枚举 */
    public boolean isEnum() {
        return rawType.isEnum();
    }

    /** 是否包含泛型参数 */
    public boolean hasGenericArgs() {
        return genericArgs.size() > 0;
    }

    /** 是否是泛型类 -- 不适用数组 */
    public boolean isGenericType() {
        if (rawType.isPrimitive() || rawType.isArray()) {
            return false;
        }
        return genericArgs.size() > 0 || rawType.getTypeParameters().length > 0; // 这个有点浪费，但又没有直接的API
    }

    /** 是否是已构造泛型类 -- 不适用数组 */
    public boolean isConstructedGenericType() {
        if (rawType.isPrimitive() || rawType.isArray()) {
            return false;
        }
        return genericArgs.size() > 0;
    }

    /** 获取泛型参数 */
    public TypeInfo getGenericArgument(int idx) {
        return genericArgs.get(idx);
    }

    /** 是否是数组 */
    public boolean isArrayType() {
        return rawType.isArray();
    }

    /** 获取数组的阶数 -- 非数组返回0 */
    public int getArrayRank() {
        int r = 0;
        Class<?> clazz = rawType;
        while (clazz.isArray()) {
            clazz = clazz.getComponentType();
            r++;
        }
        return r;
    }

    /** 是否是已构造泛型数组 */
    public boolean isConstructedGenericArrayType() {
        return rawType.isArray() && genericArgs.size() > 0;
    }

    /** 获取数组的元素类型 */
    public TypeInfo getComponentType() {
        if (rawType.isArray()) {
            return new TypeInfo(rawType.getComponentType(), genericArgs);  // 继承泛型信息
        }
        throw new IllegalStateException("This operation is only valid on array types");
    }

    /** 获取最底层数组的元素类型 */
    public TypeInfo getRootComponentType() {
        if (rawType.isArray()) {
            Class<?> root = ArrayUtils.getRootComponentType(rawType);
            return new TypeInfo(root, genericArgs); // 继承泛型信息
        }
        throw new IllegalStateException("This operation is only valid on array types");
    }

    /** 构建数组类型 */
    public TypeInfo makeArrayType() {
        return new TypeInfo(rawType.arrayType(), genericArgs); // 继承泛型信息
    }

    /** 构建数组类型 -- 可用于减少中间对象 */
    public TypeInfo makeArrayType(int rank) {
        Class<?> rawType = this.rawType;
        while (rank-- > 0) {
            rawType = rawType.arrayType();
        }
        return new TypeInfo(rawType, genericArgs);
    }

    // endregion

    // region equals

    @Override
    public boolean equals(Object o) {
        if (this == o) return true;
        if (o == null || getClass() != o.getClass()) return false;
        return equals((TypeInfo) o);
    }

    /** 避免走到不必要的重载 */
    public boolean equals(TypeInfo that) {
        if (that == null) return false;
        if (this == that) return true;
        if (rawType != that.rawType) { // class可用==代替equals
            return false;
        }
        if (genericArgs.isEmpty() && that.genericArgs.isEmpty()) { // 多数情况下无泛型参数
            return true;
        }
        return CollectionUtils.sequenceEqual(genericArgs, that.genericArgs);
    }

    @Override
    public int hashCode() {
        int result = rawType.hashCode();
        for (int i = 0; i < genericArgs.size(); i++) {
            result = 31 * result + genericArgs.get(i).hashCode();
        }
        return result;
    }

    @Override
    public String toString() {
        return "TypeInfo{" +
                "rawType=" + rawType +
                ", typeArgs=" + genericArgs +
                '}';
    }

    // endregion

    // region 常量

    // 非泛型常量不能使用of，of可能返回自己，导致NULL
    // String/Object
    public static final TypeInfo OBJECT = new TypeInfo(Object.class);
    public static final TypeInfo STRING = new TypeInfo(String.class);
    // 基础类型
    public static final TypeInfo INT = new TypeInfo(int.class);
    public static final TypeInfo LONG = new TypeInfo(long.class);
    public static final TypeInfo FLOAT = new TypeInfo(float.class);
    public static final TypeInfo DOUBLE = new TypeInfo(double.class);
    public static final TypeInfo BOOL = new TypeInfo(boolean.class);
    public static final TypeInfo SHORT = new TypeInfo(short.class);
    public static final TypeInfo BYTE = new TypeInfo(byte.class);
    public static final TypeInfo CHAR = new TypeInfo(char.class);
    public static final TypeInfo VOID = new TypeInfo(void.class);
    // 装箱类型
    public static final TypeInfo BOXED_INT = new TypeInfo(Integer.class);
    public static final TypeInfo BOXED_LONG = new TypeInfo(Long.class);
    public static final TypeInfo BOXED_FLOAT = new TypeInfo(Float.class);
    public static final TypeInfo BOXED_DOUBLE = new TypeInfo(Double.class);
    public static final TypeInfo BOXED_BOOL = new TypeInfo(Boolean.class);
    public static final TypeInfo BOXED_SHORT = new TypeInfo(Short.class);
    public static final TypeInfo BOXED_BYTE = new TypeInfo(Byte.class);
    public static final TypeInfo BOXED_CHAR = new TypeInfo(Character.class);
    public static final TypeInfo BOXED_VOID = new TypeInfo(Void.class);
    // 数组类型
    public static final TypeInfo ARRAY_INT = new TypeInfo(int[].class);
    public static final TypeInfo ARRAY_LONG = new TypeInfo(long[].class);
    public static final TypeInfo ARRAY_FLOAT = new TypeInfo(float[].class);
    public static final TypeInfo ARRAY_DOUBLE = new TypeInfo(double[].class);
    public static final TypeInfo ARRAY_BOOL = new TypeInfo(boolean[].class);
    public static final TypeInfo ARRAY_SHORT = new TypeInfo(short[].class);
    public static final TypeInfo ARRAY_BYTE = new TypeInfo(byte[].class);
    public static final TypeInfo ARRAY_CHAR = new TypeInfo(char[].class);
    // 数组类型 -- String/Object
    public static final TypeInfo ARRAY_STRING = new TypeInfo(String[].class);
    public static final TypeInfo ARRAY_OBJECT = new TypeInfo(Object[].class);

    // 常用集合--泛型类其实可使用Of
    public static final TypeInfo ARRAYLIST = new TypeInfo(ArrayList.class, OBJECT);
    public static final TypeInfo LINKED_HASHSET = new TypeInfo(LinkedHashSet.class, OBJECT);

    public static final TypeInfo HASHMAP = new TypeInfo(HashMap.class, OBJECT, OBJECT);
    public static final TypeInfo STRING_HASHMAP = new TypeInfo(HashMap.class, STRING, OBJECT);

    public static final TypeInfo LINKED_HASHMAP = new TypeInfo(LinkedHashMap.class, OBJECT, OBJECT);
    public static final TypeInfo STRING_LINKED_HASHMAP = new TypeInfo(LinkedHashMap.class, STRING, OBJECT);

    // endregion

    // region 工厂方法：of-type-info

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType) {
        // 避免过多的测试，以免浪费性能 -- 生成的代码不会走到基础类型
        if (rawType == Integer.class) return BOXED_INT;
        if (rawType == Long.class) return BOXED_LONG;
        if (rawType == String.class) return STRING;
        if (rawType == Object.class) return OBJECT;
        return new TypeInfo(rawType);
    }

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType, TypeInfo typeArg1) {
        return new TypeInfo(rawType, typeArg1);
    }

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType, TypeInfo typeArg1, TypeInfo typeArg2) {
        return new TypeInfo(rawType, typeArg1, typeArg2);
    }

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType, TypeInfo... typeArgs) {
        return new TypeInfo(rawType, List.of(typeArgs));
    }

    @StableName(comment = "生成的代码会调用")
    public static TypeInfo of(Class<?> rawType, List<TypeInfo> typeArgs) {
        return new TypeInfo(rawType, List.copyOf(typeArgs));
    }

    /** 用于继承其它类型的泛型参数 */
    public static TypeInfo of(Class<?> rawType, List<TypeInfo> typeArgs, TypeInfo typeArg1) {
        TypeInfo[] newTypeInfo = new TypeInfo[typeArgs.size() + 2];
        typeArgs.toArray(newTypeInfo);
        newTypeInfo[typeArgs.size()] = Objects.requireNonNull(typeArg1);
        return new TypeInfo(rawType, List.of(newTypeInfo)); // 会多一次拷贝，但不想依赖外部库
    }

    /** 用于继承其它类型的泛型参数 */
    public static TypeInfo of(Class<?> rawType, List<TypeInfo> typeArgs, TypeInfo typeArg1, TypeInfo typeArg2) {
        TypeInfo[] newTypeInfo = new TypeInfo[typeArgs.size() + 2];
        typeArgs.toArray(newTypeInfo);
        newTypeInfo[typeArgs.size()] = Objects.requireNonNull(typeArg1);
        newTypeInfo[typeArgs.size() + 1] = Objects.requireNonNull(typeArg2);
        return new TypeInfo(rawType, List.of(newTypeInfo));
    }

    // endregion

    // region 工厂方法: of-class#方便手写

    public static TypeInfo of(Class<?> rawType, Class<?> typeArg1) {
        return new TypeInfo(rawType, of(typeArg1));
    }

    public static TypeInfo of(Class<?> rawType, Class<?> typeArg1, Class<?> typeArg2) {
        return new TypeInfo(rawType, of(typeArg1), of(typeArg2));
    }

    public static TypeInfo of(Class<?> rawType, Class<?>... typeArgs) {
        TypeInfo[] typeInfos = new TypeInfo[typeArgs.length]; // 使用数组可避免额外拷贝
        for (int i = 0; i < typeArgs.length; i++) {
            typeInfos[i] = of(typeArgs[i]);
        }
        return new TypeInfo(rawType, List.of(typeInfos));
    }

    // endregion
}