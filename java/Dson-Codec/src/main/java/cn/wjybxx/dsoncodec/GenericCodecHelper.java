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

import cn.wjybxx.base.annotation.Stateless;

import javax.annotation.Nullable;
import javax.annotation.concurrent.ThreadSafe;
import java.lang.reflect.TypeVariable;
import java.util.List;
import java.util.concurrent.ConcurrentHashMap;

/**
 * 泛型编解码工具类
 *
 * @author wjybxx
 * date - 2024/9/27
 */
@ThreadSafe
public final class GenericCodecHelper {

    /** 用户扩展逻辑 */
    private final List<Handler> userHelpers;
    /** 由于查询频率极高，因此需要缓存结果 */
    private final ConcurrentHashMap<CacheKey, Boolean> inheritableResultCache = new ConcurrentHashMap<>();

    public GenericCodecHelper() {
        userHelpers = List.of();
    }

    public GenericCodecHelper(List<? extends Handler> userHelpers) {
        this.userHelpers = List.copyOf(userHelpers);
    }

    /** 获取用户的Helpers -- 用于创建其它的Helper */
    public List<Handler> getUserHelpers() {
        return userHelpers;
    }

    /** 用于扩展 -- 实在不知道怎么命名了，还是内部类吧.. */
    @Stateless
    public interface Handler {

        /**
         * 运行时类型是否可以继承声明类型的泛型参数
         *
         * @param runtimeType  运行时类型
         * @param declaredType 声明类型
         * @return 结果；如果返回null，则表示未得出结果，外部则继续查询另一个Handler
         */
        @Nullable
        Boolean canInheritTypeArgs(Class<?> runtimeType, Class<?> declaredType);
    }

    /**
     * 添加缓存
     *
     * @param clazz    运行时类型
     * @param declared 声明类型
     */
    public <T> GenericCodecHelper addCache(Class<T> clazz, Class<? super T> declared, boolean val) {
        inheritableResultCache.put(new CacheKey(clazz, declared), val);
        return this;
    }

    /**
     * 添加缓存。
     * 允许非继承关系的class，主要用于类型投影。
     *
     * @param clazz    运行时类型
     * @param declared 声明类型
     */
    public GenericCodecHelper addCacheUnsafe(Class<?> clazz, Class<?> declared, boolean val) {
        inheritableResultCache.put(new CacheKey(clazz, declared), val);
        return this;
    }

    /**
     * 测试运行时类型是否可继承声明类型的泛型参数
     * 如果可继承泛型参数，则会将声明类型的泛型参数传递给运行时类型，以查询对象的编解码器（可以写入更完整的泛型信息）。
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
        if (r != null) {
            return r;
        }
        if (runtimeType.isArray()) {
            // 数组只可以和数组匹配，不开放给用户扩展
            if (declaredType.isArray()) {
                Class<?> runtimeElementType = DsonConverterUtils.getRootComponentType(runtimeType);
                Class<?> declaredElementType = DsonConverterUtils.getRootComponentType(declaredType);
                r = canInheritTypeArgs0(runtimeElementType, declaredElementType);
            } else {
                r = false;
            }
        } else {
            r = canInheritTypeArgs0(runtimeType, declaredType);
        }
        inheritableResultCache.put(cacheKey, r);
        return r;
    }

    private boolean canInheritTypeArgs0(Class<?> runtimeType, Class<?> declaredType) {
        // 用户扩展逻辑
        for (Handler userHelper : userHelpers) {
            Boolean r = userHelper.canInheritTypeArgs(runtimeType, declaredType);
            if (r != null) return r;
        }
        // 默认逻辑
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

        public final Class<?> first;
        public final Class<?> second;

        public CacheKey(Class<?> first, Class<?> second) {
            this.first = first;
            this.second = second;
        }

        @Override
        public boolean equals(Object o) {
            if (this == o) return true;
            if (o == null || getClass() != o.getClass()) return false;

            CacheKey cacheKey = (CacheKey) o;
            return first.equals(cacheKey.first)
                    && second.equals(cacheKey.second);
        }

        @Override
        public int hashCode() {
            int result = first.hashCode();
            result = 31 * result + second.hashCode();
            return result;
        }
    }
}
