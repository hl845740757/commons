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

import javax.annotation.concurrent.ThreadSafe;
import java.lang.reflect.TypeVariable;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 泛型编解码工具类，测试是否可继承泛型参数。
 * 该类进行了简化，不想整得太复杂了--维护负担有点大。
 *
 * @author wjybxx
 * date - 2024/9/27
 */
@ThreadSafe
public final class GenericHelper {

    /** 由于查询频率极高，因此需要缓存结果 */
    private final ConcurrentHashMap<CacheKey, Boolean> inheritableResultCache = new ConcurrentHashMap<>(1024);

    public GenericHelper() {
    }

    /**
     * 添加缓存
     *
     * @param clazz    运行时类型
     * @param declared 声明类型
     * @param val      是否可继承泛型参数
     */
    public <T> GenericHelper addCache(Class<T> clazz, Class<? super T> declared, boolean val) {
        inheritableResultCache.put(new CacheKey(clazz, declared), val);
        return this;
    }

    /**
     * 添加缓存。
     * 允许非继承关系的class，主要用于类型投影。
     *
     * @param clazz    运行时类型
     * @param declared 声明类型
     * @param val      是否可继承泛型参数
     */
    public GenericHelper addCacheUnsafe(Class<?> clazz, Class<?> declared, boolean val) {
        inheritableResultCache.put(new CacheKey(clazz, declared), val);
        return this;
    }

    /**
     * 尝试继承声明类型的泛型参数（可以写入更完整的泛型信息）
     *
     * @param runtimeType  运行时类型
     * @param declaredType 声明类型，可能和运行时类型一致，也可能毫无关系（投影）
     * @return 返回处理继承关系后的TypeInfo
     */
    public TypeInfo inheritTypeArgs(Class<?> runtimeType, TypeInfo declaredType) {
        if (runtimeType == declaredType.rawType) return declaredType;
        if (declaredType.rawType == Object.class) return TypeInfo.of(runtimeType);

        CacheKey cacheKey = new CacheKey(runtimeType, declaredType.rawType);
        Boolean r = inheritableResultCache.get(cacheKey);
        if (r == null) {
            r = test(runtimeType, declaredType.rawType);
            inheritableResultCache.put(cacheKey, r);
        }
        return r ? TypeInfo.of(runtimeType, declaredType.genericArgs) : TypeInfo.of(runtimeType);
    }

    /**
     * 测试运行时类型是否可继承声明类型的泛型参数
     *
     * @param runtimeType  运行时类型
     * @param declaredType 声明类型，可能和运行时类型一致，也可能毫无关系（投影）
     * @return 如果可以继承泛型参数则返回true
     */
    public boolean canInheritTypeArgs(Class<?> runtimeType, Class<?> declaredType) {
        if (runtimeType == declaredType) return true;
        if (declaredType == Object.class) return false;

        CacheKey cacheKey = new CacheKey(runtimeType, declaredType);
        Boolean r = inheritableResultCache.get(cacheKey);
        if (r == null) {
            r = test(runtimeType, declaredType);
            inheritableResultCache.put(cacheKey, r);
        }
        return r;
    }

    private boolean test(Class<?> runtimeType, Class<?> declaredType) {
        boolean runtimeIsArray = runtimeType.isArray();
        boolean declaredIsArray = declaredType.isArray();
        if (runtimeIsArray && declaredIsArray) {
            // 强制规则：数组只可以和数组匹配
            runtimeType = ArrayUtils.getRootComponentType(runtimeType);
            declaredType = ArrayUtils.getRootComponentType(declaredType);
        } else if (runtimeIsArray || declaredIsArray) {
            return false;
        }
        // 除非在缓存中，否则默认不可从非超类型继承
        if (!declaredType.isAssignableFrom(runtimeType)) {
            return false;
        }
        TypeVariable<? extends Class<?>>[] thisTypeParameters = runtimeType.getTypeParameters();
        TypeVariable<? extends Class<?>>[] declaredTypeParameters = declaredType.getTypeParameters();
        if (thisTypeParameters.length != declaredTypeParameters.length) {
            return false;
        }
        // 子类声明相同的泛型变量是声明了新的变量，因此无法直接Equals比较数组元素，我们只能简单的比较名字
        for (int i = 0; i < thisTypeParameters.length; i++) {
            TypeVariable<? extends Class<?>> thisTypeParameter = thisTypeParameters[i];
            TypeVariable<? extends Class<?>> declaredTypeParameter = declaredTypeParameters[i];
            if (!thisTypeParameter.getName().equals(declaredTypeParameter.getName())) {
                return false;
            }
        }
        return true;
    }

    private static class CacheKey {

        public final Class<?> runtimeType;
        public final Class<?> declaredType;

        public CacheKey(Class<?> runtimeType, Class<?> declaredType) {
            this.runtimeType = runtimeType;
            this.declaredType = declaredType;
        }

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            CacheKey cacheKey = (CacheKey) o;
            return runtimeType.equals(cacheKey.runtimeType)
                    && declaredType.equals(cacheKey.declaredType);
        }

        @Override
        public int hashCode() {
            int result = runtimeType.hashCode();
            result = 31 * result + declaredType.hashCode();
            return result;
        }
    }
}
