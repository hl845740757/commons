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

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.lang.reflect.TypeVariable;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 添加了缓存功能的GenericHelper
 *
 * <h3>宽松匹配</h3>
 * 默认采取宽松匹配策略，只有泛型参数声明的个数和名字相同，就认为可继承泛型参数。
 * 这可以更加灵活，如果不希望某些类型之间匹配，用户可通过扩展{@link GenericHelper}拦截。
 *
 * @author wjybxx
 * date - 2024/9/27
 */
@ThreadSafe
public final class CachedGenericHelper implements GenericHelper {

    private final List<GenericHelper> userHelpers;
    private final ConcurrentHashMap<CacheKey, TypeInfo> resultCache = new ConcurrentHashMap<>(1024);
    private final ConcurrentHashMap<ClassPair, Boolean> inheritableCache = new ConcurrentHashMap<>(1024);

    public CachedGenericHelper() {
        userHelpers = List.of();
    }

    public CachedGenericHelper(List<GenericHelper> userHelpers) {
        this.userHelpers = List.copyOf(userHelpers);
    }

    @Nullable
    @Override
    public TypeInfo inheritTypeArgs(final Class<?> runtimeType, final TypeInfo declaredType) {
        if (runtimeType == declaredType.rawType) return declaredType;

        CacheKey cacheKey = new CacheKey(runtimeType, declaredType);
        TypeInfo typeInfo = resultCache.get(cacheKey);
        if (typeInfo != null) {
            return typeInfo == TypeInfo.OBJECT ? null : typeInfo;
        }
        if (runtimeType.isArray() || declaredType.isArrayType()) {
            // 数组需要根据root元素的类型查询
            typeInfo = inheritTypeArgs(
                    runtimeType.isArray() ? ArrayUtils.getRootComponentType(runtimeType) : runtimeType,
                    declaredType.isArrayType() ? declaredType.getRootComponentType() : declaredType);
            // 若是数组，TypeInfo需要恢复
            if (typeInfo != null && runtimeType.isArray()) {
                typeInfo = TypeInfo.of(runtimeType, typeInfo.genericArgs);
            }
        } else {
            // 查询用户逻辑，逆向迭代 -- 越靠近用户，优先级越高
            for (int i = userHelpers.size() - 1; i >= 0; i--) {
                GenericHelper userHelper = userHelpers.get(i);
                typeInfo = userHelper.inheritTypeArgs(runtimeType, declaredType);
                if (typeInfo != null) break;
            }
            // 走保底逻辑
            if (typeInfo == null && canInheritTypeArgs(runtimeType, declaredType.rawType)) {
                typeInfo = TypeInfo.of(runtimeType, declaredType.genericArgs);
            }
        }
        if (typeInfo == null) {
            typeInfo = TypeInfo.OBJECT;
        }
        resultCache.put(cacheKey, typeInfo);
        return typeInfo == TypeInfo.OBJECT ? null : typeInfo;
    }

    private boolean canInheritTypeArgs(Class<?> runtimeType, Class<?> declaredType) {
        ClassPair cacheKey = new ClassPair(runtimeType, declaredType);
        Boolean r = inheritableCache.get(cacheKey);
        if (r == null) {
            r = isSameGenericTypeArguments(runtimeType, declaredType);
            inheritableCache.put(cacheKey, r);
        }
        return r;
    }

    private static boolean isSameGenericTypeArguments(Class<?> runtimeType, Class<?> declaredType) {
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
        public final TypeInfo declaredType;

        public CacheKey(Class<?> runtimeType, TypeInfo declaredType) {
            this.runtimeType = runtimeType;
            this.declaredType = declaredType;
        }

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            CacheKey cacheKey = (CacheKey) o;
            return runtimeType == cacheKey.runtimeType // class 使用 ==
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