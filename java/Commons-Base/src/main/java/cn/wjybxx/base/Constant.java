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

package cn.wjybxx.base;

import javax.annotation.Nonnull;

/**
 * 常量
 * <p>
 * Q: 常量的含义？
 * A: 常量是枚举的扩展，是动态数量的枚举，它同枚举一样使用 == 判断相等性，一般由{@link ConstantPool}创建。
 * 常量是为了解决枚举的一些限制而创建的，包括：不能动态创建，不能有泛型参数。
 * <p>
 * Q：接口的作用？
 * A：这允许用户使用代理，不占用继承位。
 * <p>
 * Q: 使用常量时需要注意的地方？
 * A: 1. 一般由{@link ConstantPool}创建。
 * 2. 其使用方式与{@link ThreadLocal}非常相似，优先定义静态属性，只有有足够理由的时候才定义非静态属性。
 *
 * @author wjybxx
 * date 2023/4/1
 */
public interface Constant extends Comparable<Constant> {

    /**
     * 注意：
     * 1. 该id仅仅在其所属的{@link ConstantPool}下唯一。
     * 2. 如果常量的创建存在竞争，那么其id可能并不稳定，也不能保证连续。
     * 3. 如果常量的创建是无竞争的，那么常量之间的id应是连续的。
     * 4. 可类比{@link Enum#ordinal()}
     *
     * @return 常量的数字id。
     */
    int id();

    /**
     * 注意：
     * 1. 即使名字相同，也不代表是同一个同一个常量，只有同一个引用时才一定相等。
     * 2. 可类比{@link Enum#name()}
     *
     * @return 常量的名字。
     */
    @Nonnull
    String name();

    /**
     * 声明常量的池的id
     */
    @Nonnull
    String poolId();

    // region builder

    abstract class Builder<T extends Constant> {

        private String poolId;
        private Integer id;
        private final String name;

        private int cacheIndex = -1;
        private boolean requireCacheIndex;

        public Builder(String name) {
            this.name = checkName(name);
        }

        /**
         * 设置常量的id - 该方法由常量池调用
         *
         * @param poolId     声明常量的池
         * @param id         分配的常量id
         * @param cacheIndex 分配的缓存索引，-1表示未设置
         */
        public void setId(String poolId, int id, int cacheIndex) {
            if (this.id != null) {
                throw new IllegalStateException("id cannot be initialized repeatedly");
            }
            this.poolId = poolId;
            this.id = id;
            this.cacheIndex = cacheIndex;
        }

        public int getIdOrThrow() {
            if (this.id == null) {
                throw new IllegalStateException("id has not been initialized");
            }
            return id;
        }

        public Integer getId() {
            return id;
        }

        public String getName() {
            return name;
        }

        public String getPoolId() {
            return poolId;
        }

        /**
         * 获取分配的高速缓存索引 -- -1表示未设置。
         * 注意：{@link ConstantPool}仅仅分配index，而真正的实现在于常量的使用者。
         */
        public int getCacheIndex() {
            return cacheIndex;
        }

        public boolean isRequireCacheIndex() {
            return requireCacheIndex;
        }

        /** 设置是否需要分配高速缓存索引 */
        public Builder<T> setRequireCacheIndex(boolean requireCacheIndex) {
            this.requireCacheIndex = requireCacheIndex;
            return this;
        }

        public abstract T build();

    }

    /** 检查name的合法性 */
    static String checkName(String name) {
        if (name == null || name.isEmpty()) {
            throw new IllegalArgumentException("name is empty ");
        }
        return name;
    }

    // endregion

}