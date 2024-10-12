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
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ConcurrentMap;

/**
 * 类型id的写入策略
 * TODO 改接口
 *
 * @author wjybxx
 * date - 2023/4/27
 */
public enum ClassIdPolicy {

    /**
     * 当对象的运行时类型和声明类型相同时不写入类型信息
     * 通常我们的字段类型定义是明确的，因此可以省去大量不必要的类型信息。
     * 如果仅用于java或其它静态类型语言，建议使用该模式。
     */
    OPTIMIZED,

    /**
     * 总是写入对象的类型信息，无论运行时类型与声明类型是否相同
     * 这种方式有更好的兼容性，对跨语言友好，因为目标语言可能没有泛型，或没有注解处理器生成辅助代码等；
     * 但这种方式会大幅增加序列化后的大小，需要慎重考虑。
     */
    ALWAYS,

    /**
     * 总是不写入对象的类型信息，无论运行时类型与声明类型是否相同
     */
    NONE;

    /** 缓存信息 */
    private static final ConcurrentMap<CacheKey, Boolean> cacheDic = new ConcurrentHashMap<>();

    /**
     * 1.如果声明类型是泛型类，仅支持编码类型也是泛型类
     * 2.通常当真实类型是声明类型的默认实例类型时，可指定不编码类型信息
     *
     * @param declaredType 字段的声明类型
     * @param encoderType  运行时类型
     * @param value        是否写入类型信息
     */
    public static void addCache(Class<?> declaredType, Class<?> encoderType, boolean value) {
        CacheKey key = new CacheKey(TypeInfo.of(declaredType), TypeInfo.of(encoderType));
        cacheDic.put(key, value);
    }

    /**
     * 测试是否需要写入对象类型信息
     * 当
     *
     * @param declaredType 字段的声明类型
     * @param encoderType  运行时类型
     * @return 是否写入类型信息
     */
    public boolean test(TypeInfo declaredType, TypeInfo encoderType) {
        if (this == ClassIdPolicy.OPTIMIZED) {
            if (encoderType.equals(declaredType)) return false;
            if (declaredType.rawType == Object.class) return true;

            CacheKey key = new CacheKey(declaredType, encoderType);
            Boolean value = cacheDic.get(key);
            if (value != null) {
                return value;
            }
            if (declaredType.isGenericType() && encoderType.isGenericType()) {
                value = testGeneric(declaredType, encoderType);
                cacheDic.put(key, value);
                return value;
            }
            return true;
        }
        return this == ClassIdPolicy.ALWAYS;
    }

    private static boolean testGeneric(TypeInfo declaredType, TypeInfo encoderType) {
        // 如果泛型原型之间设置为必须写入，则必须写入；如果泛型原型之间设置为无需写入，则测试泛型参数是否相同
        {
            CacheKey key = new CacheKey(TypeInfo.of(declaredType.rawType), TypeInfo.of(encoderType.rawType));
            Boolean value = cacheDic.get(key);
            if (value != null) {
                return value || isGenericTypeArgumentsDifferent(declaredType, encoderType);
            }
        }
        // 默认类型测试
        if (encoderType.rawType == ArrayList.class
                && Collection.class.isAssignableFrom(declaredType.rawType)) {
            return isGenericTypeArgumentsDifferent(declaredType, encoderType);
        }
        if ((encoderType.rawType == HashMap.class || encoderType.rawType == LinkedHashMap.class)
                && Map.class.isAssignableFrom(declaredType.rawType)) {
            return isGenericTypeArgumentsDifferent(declaredType, encoderType);
        }
        if ((encoderType.rawType == HashSet.class || encoderType.rawType == LinkedHashSet.class)
                && Set.class.isAssignableFrom(declaredType.rawType)) {
            return isGenericTypeArgumentsDifferent(declaredType, encoderType);
        }
        return true;
    }

    private static boolean isGenericTypeArgumentsDifferent(TypeInfo declaredType, TypeInfo encoderType) {
        return !CollectionUtils.sequenceEqual(declaredType.genericArgs, encoderType.genericArgs);
    }

    private static class CacheKey {

        public final TypeInfo declaredType;
        public final TypeInfo encoderType;

        public CacheKey(TypeInfo declaredType, TypeInfo encoderType) {
            this.declaredType = declaredType;
            this.encoderType = encoderType;
        }

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            CacheKey that = (CacheKey) o;
            return declaredType.equals(that.declaredType)
                    && encoderType.equals(that.encoderType);
        }

        @Override
        public int hashCode() {
            int result = declaredType.hashCode();
            result = 31 * result + encoderType.hashCode();
            return result;
        }
    }
}