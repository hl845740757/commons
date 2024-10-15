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

import javax.annotation.Nullable;
import java.lang.invoke.CallSite;
import java.lang.invoke.LambdaMetafactory;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.MethodType;
import java.lang.reflect.Constructor;
import java.lang.reflect.Modifier;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public final class DsonConverterUtils {

    /** 类型原数据注册表 */
    private static final TypeMetaRegistry BUILTIN_TYPE_METAS;

    private static final MethodType SUPPLIER_INVOKE_TYPE = MethodType.methodType(Supplier.class);
    private static final MethodType SUPPLIER_GET_METHOD_TYPE = MethodType.methodType(Object.class);

    private static final Map<Class<?>, Class<?>> wrapperToPrimitiveTypeMap = new IdentityHashMap<>(9);
    private static final Map<Class<?>, Class<?>> primitiveTypeToWrapperMap = new IdentityHashMap<>(9);
    private static final Map<Class<?>, Object> primitiveTypeDefaultValueMap = new IdentityHashMap<>(9);

    static {
        wrapperToPrimitiveTypeMap.put(Boolean.class, boolean.class);
        wrapperToPrimitiveTypeMap.put(Byte.class, byte.class);
        wrapperToPrimitiveTypeMap.put(Character.class, char.class);
        wrapperToPrimitiveTypeMap.put(Double.class, double.class);
        wrapperToPrimitiveTypeMap.put(Float.class, float.class);
        wrapperToPrimitiveTypeMap.put(Integer.class, int.class);
        wrapperToPrimitiveTypeMap.put(Long.class, long.class);
        wrapperToPrimitiveTypeMap.put(Short.class, short.class);
        wrapperToPrimitiveTypeMap.put(Void.class, void.class);

        for (Map.Entry<Class<?>, Class<?>> entry : wrapperToPrimitiveTypeMap.entrySet()) {
            primitiveTypeToWrapperMap.put(entry.getValue(), entry.getKey());
        }

        primitiveTypeDefaultValueMap.put(Boolean.class, Boolean.FALSE);
        primitiveTypeDefaultValueMap.put(Byte.class, (byte) 0);
        primitiveTypeDefaultValueMap.put(Character.class, (char) 0);
        primitiveTypeDefaultValueMap.put(Double.class, 0d);
        primitiveTypeDefaultValueMap.put(Float.class, 0f);
        primitiveTypeDefaultValueMap.put(Integer.class, 0);
        primitiveTypeDefaultValueMap.put(Long.class, 0L);
        primitiveTypeDefaultValueMap.put(Short.class, (short) 0);
        primitiveTypeDefaultValueMap.put(Void.class, null);

        // 内置codec类型
        List<TypeMeta> typeMetaList = builtinTypeMetas();
        BUILTIN_TYPE_METAS = SimpleTypeMetaRegistry.fromTypeMetas(typeMetaList);
    }

    // region 内建codec

    private static TypeMeta typeMetaOf(Class<?> clazz, String... clsNames) {
        if (clsNames.length == 0) {
            clsNames = new String[]{clazz.getSimpleName()};
        }
        return TypeMeta.of(clazz, ObjectStyle.INDENT, List.of(clsNames));
    }

    /**
     * 内建基本类型的codec
     * 1.只包含基础的类型，其它都需要用户分配
     * 2.clsName并不总是等于类型名，以方便跨语言交互
     */
    private static List<TypeMeta> builtinTypeMetas() {
        return List.of(
                // dson内建结构
                typeMetaOf(int.class, DsonTexts.LABEL_INT32, "int", "int32", "ui", "uint", "uint32"),
                typeMetaOf(long.class, DsonTexts.LABEL_INT64, "long", "int64", "uL", "ulong", "uint64"),
                typeMetaOf(float.class, DsonTexts.LABEL_FLOAT, "float"),
                typeMetaOf(double.class, DsonTexts.LABEL_DOUBLE, "double"),
                typeMetaOf(boolean.class, DsonTexts.LABEL_BOOL, "bool", "boolean"),
                typeMetaOf(String.class, DsonTexts.LABEL_STRING, "string"),
                typeMetaOf(Binary.class, DsonTexts.LABEL_BINARY, "bytes"),
                typeMetaOf(ObjectPtr.class, DsonTexts.LABEL_PTR),
                typeMetaOf(ObjectLitePtr.class, DsonTexts.LABEL_LITE_PTR),
                typeMetaOf(ExtDateTime.class, DsonTexts.LABEL_DATETIME),
                typeMetaOf(Timestamp.class, DsonTexts.LABEL_TIMESTAMP),
                // 基础类型
                typeMetaOf(short.class, "int16", "uint16", "short"),
                typeMetaOf(byte.class, "byte", "sbyte"),
                typeMetaOf(char.class, "char"),
                // 装箱类型--要和c#互通
                typeMetaOf(Integer.class, "Int"),
                typeMetaOf(Long.class, "Long"),
                typeMetaOf(Float.class, "Float"),
                typeMetaOf(Double.class, "Double"),
                typeMetaOf(Boolean.class, "Bool"),
                typeMetaOf(Short.class, "Short"),
                typeMetaOf(Byte.class, "Byte"),
                typeMetaOf(Character.class, "Char"),

                // 特殊组件
                typeMetaOf(Object.class, "object", "Object"), // object会作为泛型参数...
                typeMetaOf(MapEncodeProxy.class, "DictionaryEncodeProxy", "MapEncodeProxy"),

                // 基础集合
                typeMetaOf(Collection.class, "ICollection", "ICollection`1"),
                typeMetaOf(List.class, "IList", "IList`1"),
                typeMetaOf(ArrayList.class, "List", "List`1"),

                typeMetaOf(Map.class, "IDictionary", "IDictionary`2"),
                typeMetaOf(HashMap.class, "HashMap", "HashMap`2"), // c#的字典有毒，不删除的情况下有序，导致Java映射困难
                typeMetaOf(LinkedHashMap.class, "Dictionary", "Dictionary`2", "LinkedDictionary", "LinkedDictionary`2"),
                typeMetaOf(ConcurrentHashMap.class, "ConcurrentDictionary", "ConcurrentDictionary`2")
        );
    }

    // endregion

    // region codec utils

    /** 获取默认的元数据注册表 */
    public static TypeMetaRegistry getDefaultTypeMetaRegistry() {
        return BUILTIN_TYPE_METAS;
    }

    /** 获取给定类型的默认值 */
    public static Object getDefaultValue(Class<?> type) {
        return type.isPrimitive() ? primitiveTypeDefaultValueMap.get(type) : null;
    }

    public static Class<?> boxIfPrimitiveType(Class<?> type) {
        return type.isPrimitive() ? primitiveTypeToWrapperMap.get(type) : type;
    }

    public static Class<?> unboxIfWrapperType(Class<?> type) {
        final Class<?> result = wrapperToPrimitiveTypeMap.get(type);
        return result == null ? type : result;
    }

    public static boolean isBoxType(Class<?> type) {
        return wrapperToPrimitiveTypeMap.containsKey(type);
    }

    public static boolean isPrimitiveType(Class<?> type) {
        return type.isPrimitive();
    }

    /**
     * 测试右手边的类型是否可以赋值给左边的类型。
     * 基本类型和其包装类型之间将认为是可赋值的。
     *
     * @param lhsType 基类型
     * @param rhsType 测试的类型
     * @return 如果测试的类型可以赋值给基类型则返回true，否则返回false
     */
    public static boolean isAssignableFrom(Class<?> lhsType, Class<?> rhsType) {
        Objects.requireNonNull(lhsType, "Left-hand side type must not be null");
        Objects.requireNonNull(rhsType, "Right-hand side type must not be null");
        if (lhsType.isAssignableFrom(rhsType)) {
            return true;
        }
        if (lhsType.isPrimitive()) {
            Class<?> resolvedPrimitive = wrapperToPrimitiveTypeMap.get(rhsType);
            return (lhsType == resolvedPrimitive);
        } else {
            // rhsType.isPrimitive
            Class<?> resolvedWrapper = primitiveTypeToWrapperMap.get(rhsType);
            return (resolvedWrapper != null && lhsType.isAssignableFrom(resolvedWrapper));
        }
    }

    /**
     * 测试给定的值是否可以赋值给定的类型。
     * 基本类型和其包装类型之间将认为是可赋值的，但null值不可以赋值给基本类型。
     *
     * @param type  目标类型
     * @param value 测试的值
     * @return 如果目标值可以赋值给目标类型则返回true
     */
    public static boolean isAssignableValue(Class<?> type, @Nullable Object value) {
        Objects.requireNonNull(type, "Type must not be null");
        return (value == null) ? !type.isPrimitive() : isAssignableFrom(type, value.getClass());
    }

    /**
     * {@code java.lang.ClassCastException: Cannot cast java.lang.Integer to int}
     * {@link Class#cast(Object)}对基本类型有坑。。。。
     */
    public static <T> T castValue(Class<T> type, Object value) {
        if (type.isPrimitive()) {
            @SuppressWarnings("unchecked") final Class<T> boxedType = (Class<T>) primitiveTypeToWrapperMap.get(type);
            return boxedType.cast(value);
        } else {
            return type.cast(value);
        }
    }

    /** 枚举实例可能是枚举类的子类，如果枚举实例声明了代码块{}，在编解码时需要转换为声明类 */
    public static Class<?> getEncodeClass(Object value) {
        Class<?> clazz = value.getClass();
        if (clazz.isEnum()) {
            return clazz;
        }
        Class<?> superclass = clazz.getSuperclass();
        if (superclass.isEnum()) {
            return superclass;
        }
        return clazz;
//        if (value instanceof Enum<?> e) {
//            return e.getDeclaringClass();
//        } else {
//            return value.getClass();
//        }
    }

    /** 注意：默认情况下map是一个数组对象，而不是普通的对象 */
    public static <T> boolean isEncodeAsArray(Class<T> encoderClass) {
        return encoderClass.isArray()
                || Collection.class.isAssignableFrom(encoderClass)
                || Map.class.isAssignableFrom(encoderClass);
    }

    /** 无参构造函数转lambda实例 -- 可避免解码过程中的反射 */
    public static <T> Supplier<T> noArgConstructorToSupplier(MethodHandles.Lookup lookup, Constructor<T> constructor) throws Throwable {
        Class<T> returnType = constructor.getDeclaringClass();
        CallSite callSite = LambdaMetafactory.metafactory(lookup,
                "get", SUPPLIER_INVOKE_TYPE, SUPPLIER_GET_METHOD_TYPE,
                lookup.unreflectConstructor(constructor),
                MethodType.methodType(returnType));

        @SuppressWarnings("unchecked") Supplier<T> supplier = (Supplier<T>) callSite.getTarget().invoke();
        return supplier;
    }

    /** 尝试将无参构造函数转换为Supplier */
    @Nullable
    public static <T> Supplier<T> tryNoArgConstructorToSupplier(Class<T> clazz) {
        if (Modifier.isAbstract(clazz.getModifiers()) || clazz.isInterface()) {
            return null; // 抽象类或接口
        }
        try {
            Constructor<T> constructor = clazz.getConstructor();
            if (!Modifier.isPublic(constructor.getModifiers())) {
                return null;
            }
            MethodHandles.Lookup lookup = MethodHandles.lookup();
            return noArgConstructorToSupplier(lookup, constructor);
        } catch (Throwable ex) {
            return null;
        }
    }

    // endregion

}