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

package cn.wjybxx.base.io;

import java.util.Objects;

/**
 * @author wjybxx
 * date - 2024/5/22
 */
public abstract class ArrayPoolBuilder<T> {

    private static final int DEFAULT_MAX_CAPACITY = 1024 * 1024;

    private final Class<T> arrayType;
    private boolean clear;
    private int defCapacity = 8192;
    private int maxCapacity = DEFAULT_MAX_CAPACITY;

    public ArrayPoolBuilder(Class<T> arrayType) {
        this.arrayType = Objects.requireNonNull(arrayType, "arrayType");
    }

    public abstract ArrayPool<T> build();

    // region

    public Class<T> getArrayType() {
        return arrayType;
    }

    /** 数组在归还时是否清理数组内容 */
    public boolean isClear() {
        return clear;
    }

    public ArrayPoolBuilder<T> setClear(boolean clear) {
        this.clear = clear;
        return this;
    }

    /** 默认分配的数组空间大小 */
    public int getDefCapacity() {
        return defCapacity;
    }

    public ArrayPoolBuilder<T> setDefCapacity(int defCapacity) {
        this.defCapacity = defCapacity;
        return this;
    }

    /** 可缓存的数组的最大空间 -- 超过大小的数组销毁 */
    public int getMaxCapacity() {
        return maxCapacity;
    }

    public ArrayPoolBuilder<T> setMaxCapacity(int maxCapacity) {
        this.maxCapacity = maxCapacity;
        return this;
    }

    // endregion

    public static <T> SimpleArrayPoolBuilder<T> newSimpleBuilder(Class<T> arrayType) {
        return new SimpleArrayPoolBuilder<>(arrayType);
    }

    public static <T> ConcurrentArrayPoolBuilder<T> newConcurrentBuilder(Class<T> arrayType) {
        return new ConcurrentArrayPoolBuilder<>(arrayType);
    }

    // region

    public static class SimpleArrayPoolBuilder<T> extends ArrayPoolBuilder<T> {

        private int poolSize = 16;

        public SimpleArrayPoolBuilder(Class<T> arrayType) {
            super(arrayType);
        }

        @Override
        public SimpleArrayPool<T> build() {
            return new SimpleArrayPool<>(this);
        }

        @Override
        public SimpleArrayPoolBuilder<T> setDefCapacity(int defCapacity) {
            super.setDefCapacity(defCapacity);
            return this;
        }

        @Override
        public SimpleArrayPoolBuilder<T> setMaxCapacity(int maxCapacity) {
            super.setMaxCapacity(maxCapacity);
            return this;
        }

        @Override
        public SimpleArrayPoolBuilder<T> setClear(boolean clear) {
            super.setClear(clear);
            return this;
        }

        /** 对象池大小 - 等于0则不缓存 */
        public int getPoolSize() {
            return poolSize;
        }

        public SimpleArrayPoolBuilder<T> setPoolSize(int poolSize) {
            this.poolSize = poolSize;
            return this;
        }
    }

    public static class ConcurrentArrayPoolBuilder<T> extends ArrayPoolBuilder<T> {

        public ConcurrentArrayPoolBuilder(Class<T> arrayType) {
            super(arrayType);
        }

        @Override
        public ConcurrentArrayPool<T> build() {
            return new ConcurrentArrayPool<>(this);
        }

        @Override
        public ConcurrentArrayPoolBuilder<T> setDefCapacity(int defCapacity) {
            super.setDefCapacity(defCapacity);
            return this;
        }

        @Override
        public ConcurrentArrayPoolBuilder<T> setMaxCapacity(int maxCapacity) {
            super.setMaxCapacity(maxCapacity);
            return this;
        }

        @Override
        public ConcurrentArrayPoolBuilder<T> setClear(boolean clear) {
            super.setClear(clear);
            return this;
        }
    }

    // endregion

}
