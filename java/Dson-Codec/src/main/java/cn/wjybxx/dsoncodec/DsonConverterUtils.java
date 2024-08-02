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

import cn.wjybxx.base.reflect.TypeParameterFinder;
import cn.wjybxx.dson.text.DsonTexts;
import cn.wjybxx.dson.text.ObjectStyle;
import cn.wjybxx.dson.types.*;
import cn.wjybxx.dsoncodec.codecs.*;

import javax.annotation.Nullable;
import java.lang.invoke.CallSite;
import java.lang.invoke.LambdaMetafactory;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.MethodType;
import java.lang.reflect.*;
import java.time.Instant;
import java.time.LocalDate;
import java.time.LocalDateTime;
import java.time.LocalTime;
import java.util.*;
import java.util.concurrent.ConcurrentHashMap;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date 2023/4/4
 */
public final class DsonConverterUtils {

    /** 类型id注册表 */
    private static final TypeMetaRegistry TYPE_META_REGISTRY;
    /** 默认codec注册表 */
    private static final DsonCodecRegistry CODEC_REGISTRY;

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
        TYPE_META_REGISTRY = TypeMetaRegistries.fromMetas(builtinTypeMetas());
        CODEC_REGISTRY = new DefaultCodecRegistry(DsonCodecRegistries.newCodecMap(builtinCodecs().stream()
                .map(DsonCodecImpl::new)
                .toList()));
    }

    // region 内建codec

    private static TypeMeta typeMetaOf(Class<?> clazz, String... clsNames) {
        if (clsNames.length == 0) {
            clsNames = new String[]{clazz.getSimpleName()};
        }
        return TypeMeta.of(clazz, ObjectStyle.INDENT, List.of(clsNames));
    }

    private static List<TypeMeta> builtinTypeMetas() {
        return List.of(
                // dson内建结构
                typeMetaOf(int.class, DsonTexts.LABEL_INT32, "int", "int32", "ui", "uint", "uint32"),
                typeMetaOf(long.class, DsonTexts.LABEL_INT64, "long", "int64", "uL", "ulong", "uint64"),
                typeMetaOf(float.class, DsonTexts.LABEL_FLOAT, "float"),
                typeMetaOf(double.class, DsonTexts.LABEL_DOUBLE, "double"),
                typeMetaOf(boolean.class, DsonTexts.LABEL_BOOL, "bool", "boolean"),
                typeMetaOf(String.class, DsonTexts.LABEL_STRING, "string"),
                typeMetaOf(Binary.class, DsonTexts.LABEL_BINARY),
                typeMetaOf(ObjectPtr.class, DsonTexts.LABEL_PTR),
                typeMetaOf(ObjectLitePtr.class, DsonTexts.LABEL_LITE_PTR),
                typeMetaOf(ExtDateTime.class, DsonTexts.LABEL_DATETIME),
                typeMetaOf(Timestamp.class, DsonTexts.LABEL_TIMESTAMP),

                // 特殊组件
                typeMetaOf(MapEncodeProxy.class, "MapEncodeProxy", "DictionaryEncodeProxy"),

                // 基本类型数组
                typeMetaOf(int[].class),
                typeMetaOf(long[].class),
                typeMetaOf(float[].class),
                typeMetaOf(double[].class),
                typeMetaOf(boolean[].class),
                typeMetaOf(String[].class),
                typeMetaOf(short[].class),
                typeMetaOf(char[].class),
                typeMetaOf(Object[].class),

                // 抽象集合类型
                typeMetaOf(Collection.class),
                typeMetaOf(Map.class),
                // 常用具体类型集合
                typeMetaOf(LinkedList.class),
                typeMetaOf(ArrayDeque.class),
                typeMetaOf(IdentityHashMap.class),
                typeMetaOf(ConcurrentHashMap.class),

                // 日期
                typeMetaOf(LocalDateTime.class),
                typeMetaOf(LocalDate.class),
                typeMetaOf(LocalTime.class),
                typeMetaOf(Instant.class),
                typeMetaOf(DurationCodec.class)
        );
    }

    private static List<DsonCodec<?>> builtinCodecs() {
        return List.of(
                // dson内建结构
                new Int32Codec(),
                new Int64Codec(),
                new FloatCodec(),
                new DoubleCodec(),
                new BooleanCodec(),
                new StringCodec(),
                new BinaryCodec(),
                new ObjectPtrCodec(),
                new ObjectLitePtrCodec(),
                new ExtDateTimeCodec(),
                new TimestampCodec(),

                // 特殊组件
                new MapEncodeProxyCodec(),

                // 基本类型数组
                new MoreArrayCodecs.IntArrayCodec(),
                new MoreArrayCodecs.LongArrayCodec(),
                new MoreArrayCodecs.FloatArrayCodec(),
                new MoreArrayCodecs.DoubleArrayCodec(),
                new MoreArrayCodecs.BooleanArrayCodec(),
                new MoreArrayCodecs.StringArrayCodec(),
                new MoreArrayCodecs.ShortArrayCodec(),
                new MoreArrayCodecs.CharArrayCodec(),
                new MoreArrayCodecs.ObjectArrayCodec(),

                // 抽象集合类型
                new CollectionCodec<>(Collection.class, null),
                new MapCodec<>(Map.class, null),
                // 常用具体类型集合 -- 不含默认解码类型的超类
                new CollectionCodec<>(LinkedList.class, LinkedList::new),
                new CollectionCodec<>(ArrayDeque.class, ArrayDeque::new),
                new MapCodec<>(IdentityHashMap.class, IdentityHashMap::new),
                new MapCodec<>(ConcurrentHashMap.class, ConcurrentHashMap::new),

                // 日期类型
                new LocalDateTimeCodec(),
                new LocalDateCodec(),
                new LocalTimeCodec(),
                new InstantCodec(),
                new DurationCodec()
        );
    }

    @SuppressWarnings({"rawtypes", "unchecked"})
    private static class DefaultCodecRegistry implements DsonCodecRegistry {

        final Map<Class<?>, DsonCodecImpl<?>> codecMap;

        final DsonCodecImpl<Object[]> objectArrayCodec;
        final DsonCodecImpl<Collection> collectionCodec;
        final DsonCodecImpl<Map> mapCodec;

        private DefaultCodecRegistry(Map<Class<?>, DsonCodecImpl<?>> codecMap) {
            this.codecMap = codecMap;

            this.objectArrayCodec = getCodec(codecMap, Object[].class);
            this.collectionCodec = getCodec(codecMap, Collection.class);
            this.mapCodec = getCodec(codecMap, Map.class);
        }

        private static <T> DsonCodecImpl<T> getCodec(Map<Class<?>, DsonCodecImpl<?>> codecMap, Class<T> clazz) {
            DsonCodecImpl<T> codec = (DsonCodecImpl<T>) codecMap.get(clazz);
            if (codec == null) throw new IllegalArgumentException(clazz.getName());
            return codec;
        }

        @Nullable
        @Override
        public <T> DsonCodecImpl<? super T> getEncoder(Class<T> clazz, DsonCodecRegistry rootRegistry) {
            DsonCodecImpl<?> codec = codecMap.get(clazz);
            if (codec != null) return (DsonCodecImpl<T>) codec;

            if (clazz.isArray()) return (DsonCodecImpl<T>) objectArrayCodec;
            if (Collection.class.isAssignableFrom(clazz)) return (DsonCodecImpl<? super T>) collectionCodec;
            if (Map.class.isAssignableFrom(clazz)) return (DsonCodecImpl<? super T>) mapCodec;
            return null;
        }

        @Nullable
        @Override
        public <T> DsonCodecImpl<T> getDecoder(Class<T> clazz, DsonCodecRegistry rootRegistry) {
            DsonCodecImpl<?> codec = codecMap.get(clazz);
            if (codec != null) return (DsonCodecImpl<T>) codec;
            // java是泛型擦擦，因此我们可以这么干 -- Codec创建的实例可以线下转型目标类型
            if (clazz.isAssignableFrom(LinkedHashMap.class)) return (DsonCodecImpl<T>) mapCodec;
            if (clazz.isAssignableFrom(ArrayList.class)) return (DsonCodecImpl<T>) collectionCodec;
            return null;
        }
    }

    // endregion

    // region array

    /** 最大支持9阶 - 我都没见过3阶以上的数组... */
    private static final String[] arrayRankSymbols = {
            "[]",
            "[][]",
            "[][][]",
            "[][][][]",
            "[][][][][]",
            "[][][][][][]",
            "[][][][][][][]",
            "[][][][][][][][]",
            "[][][][][][][][][]"
    };

    public static String arrayRankSymbol(int rank) {
        if (rank < 1 || rank > 9) {
            throw new IllegalArgumentException("rank: " + rank);
        }
        return arrayRankSymbols[rank - 1];
    }

    /** 获取根元素的类型 -- 如果Type是数组，则返回最底层的元素类型；如果不是数组，则返回type */
    public static Class<?> getRootComponentType(Class<?> clz) {
        while (clz.isArray()) {
            clz = clz.getComponentType();
        }
        return clz;
    }

    /** 获取数组的阶数 -- 如果不是数组，则返回0 */
    public static int getArrayRank(Class<?> clz) {
        int r = 0;
        while (clz.isArray()) {
            r++;
            clz = clz.getComponentType();
        }
        return r;
    }
    // endregion

    // region 泛型

    /** 判断是否可继承的开销比较大，我们需要缓存测试结果 */
    private static final ConcurrentHashMap<ClassPair, Boolean> inheritableResultCache = new ConcurrentHashMap<>();

    private static final TypeInfo<?> Nil = TypeInfo.of(void.class);
    private static final ConcurrentHashMap<Class<?>, TypeInfo<?>> actualTypeArgCache = new ConcurrentHashMap<>();

    /** 判断要编码的类型是否可继承声明类型的泛型参数 */
    public static <T> boolean canInheritTypeArgs(Class<T> encoderClass, TypeInfo<?> typeInfo) {
        return typeInfo.typeArgs.size() > 0 && canInheritTypeArgs(encoderClass, typeInfo.rawType);
    }

    /** 测试当前类是否可继承目标类的泛型参数 */
    public static <T> boolean canInheritTypeArgs(Class<T> thisClass, Class<?> targetClass) {
        if (thisClass == targetClass) {
            return true;
        }
        if (!targetClass.isAssignableFrom(thisClass)) {
            return false;
        }
        ClassPair classPair = new ClassPair(thisClass, targetClass);
        Boolean r = inheritableResultCache.get(classPair);
        if (r != null) {
            return r;
        }
        if (thisClass.isArray()) {
            // 数组由根元素类型决定
            Class<?> thisElementType = getRootComponentType(thisClass);
            Class<?> targetElementType = getRootComponentType(targetClass);
            r = canInheritTypeArgs(thisElementType, targetElementType);
        } else {
            TypeVariable<? extends Class<?>>[] thisTypeParameters = thisClass.getTypeParameters();
            if (thisTypeParameters.length > 0) {
                // 当前类是泛型类 -- 查找传递给超类的泛型变量
                @SuppressWarnings("unchecked") Class<? super T> superClassOrInterface = (Class<? super T>) targetClass;
                Type genericSuperType = TypeParameterFinder.getGenericSuperType(thisClass, superClassOrInterface);
                if (genericSuperType instanceof ParameterizedType parameterizedType) {
                    Type[] actualTypeArguments = parameterizedType.getActualTypeArguments();
                    r = Arrays.equals(thisTypeParameters, actualTypeArguments);
                }
            }
        }
        if (r == null) {
            r = false;
        }
        inheritableResultCache.put(classPair, r);
        return r;
    }

    /** 获取传递给集合的元素类型；不存在则返回nuLl */
    @SuppressWarnings("unchecked")
    public static <T> TypeInfo<?> getElementActualTypeInfo(Class<T> rawType) {
        if (!Collection.class.isAssignableFrom(rawType)) {
            return null;
        }
        return findActualTypeImpl(rawType, (Class<? super T>) Collection.class, "E");
    }

    /** 获取传递给字典的Key类型；不存在则返回nuLl */
    @SuppressWarnings("unchecked")
    public static <T> TypeInfo<?> getKeyActualTypeInfo(Class<T> rawType) {
        if (!Map.class.isAssignableFrom(rawType)) {
            return null;
        }
        return findActualTypeImpl(rawType, (Class<? super T>) Map.class, "K");
    }

    private static <T> TypeInfo<?> findActualTypeImpl(Class<T> rawType, Class<? super T> targetType, String typeVarName) {
        if (rawType == targetType) {
            return null;
        }
        TypeInfo<?> typeInfo = actualTypeArgCache.get(rawType);
        if (typeInfo == null) {
            try {
                Class<?> actualType = TypeParameterFinder.findTypeParameterUnsafe(rawType, targetType, typeVarName);
                if (actualType != null) {
                    typeInfo = TypeInfo.of(actualType);
                }
            } catch (Throwable ignore) {
            }
            if (typeInfo == null) {
                typeInfo = Nil;
            }
            actualTypeArgCache.put(rawType, typeInfo);
        }
        return typeInfo == Nil ? null : typeInfo;
    }

    private static class ClassPair {

        final Class<?> first;
        final Class<?> second;

        public ClassPair(Class<?> first, Class<?> second) {
            this.first = first;
            this.second = second;
        }

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            ClassPair classPair = (ClassPair) o;

            if (!first.equals(classPair.first)) return false;
            return second.equals(classPair.second);
        }

        @Override
        public int hashCode() {
            int result = first.hashCode();
            result = 31 * result + second.hashCode();
            return result;
        }
    }
    // endregion

    // region codec utils

    /** 获取默认的编解码器 */
    public static DsonCodecRegistry getDefaultCodecRegistry() {
        return CODEC_REGISTRY;
    }

    /** 获取默认的元数据注册表 */
    public static TypeMetaRegistry getDefaultTypeMetaRegistry() {
        return TYPE_META_REGISTRY;
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
    public static boolean isAssignable(Class<?> lhsType, Class<?> rhsType) {
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
        return (value != null ? isAssignable(type, value.getClass()) : !type.isPrimitive());
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
        if (value instanceof Enum<?> e) {
            return e.getDeclaringClass();
        } else {
            return value.getClass();
        }
    }

    /** 注意：默认情况下map是一个数组对象，而不是普通的对象 */
    public static <T> boolean isEncodeAsArray(Class<T> encoderClass) {
        return encoderClass.isArray()
                || Collection.class.isAssignableFrom(encoderClass)
                || Map.class.isAssignableFrom(encoderClass);
    }

    /** List转Array */
    @SuppressWarnings("unchecked")
    public static <T, E> T convertList2Array(List<? extends E> list, Class<T> arrayType) {
        final Class<?> componentType = arrayType.getComponentType();
        final int length = list.size();

        if (list.getClass() == ArrayList.class && !componentType.isPrimitive()) {
            final E[] tempArray = (E[]) Array.newInstance(componentType, length);
            return (T) list.toArray(tempArray);
        }
        // System.arrayCopy并不支持对象数组到基础类型数组
        final T tempArray = (T) Array.newInstance(componentType, length);
        for (int index = 0; index < length; index++) {
            Object element = list.get(index);
            Array.set(tempArray, index, element);
        }
        return tempArray;
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

    /** @param lookup 外部缓存实例，避免每次创建的开销 */
    public static <T extends Collection<?>> CollectionCodec<T> createCollectionCodec(MethodHandles.Lookup lookup, Class<T> clazz) {
        try {
            Constructor<T> constructor = clazz.getConstructor();
            Supplier<T> factory = noArgConstructorToSupplier(lookup, constructor);
            return new CollectionCodec<>(clazz, factory);
        } catch (RuntimeException e) {
            throw e;
        } catch (Throwable e) {
            throw new RuntimeException(e);
        }
    }

    public static <T extends Map<?, ?>> MapCodec<T> createMapCodec(MethodHandles.Lookup lookup, Class<T> clazz) {
        try {
            Constructor<T> constructor = clazz.getConstructor();
            Supplier<T> factory = noArgConstructorToSupplier(lookup, constructor);
            return new MapCodec<>(clazz, factory);
        } catch (RuntimeException e) {
            throw e;
        } catch (Throwable e) {
            throw new RuntimeException(e);
        }
    }
    // endregion

}