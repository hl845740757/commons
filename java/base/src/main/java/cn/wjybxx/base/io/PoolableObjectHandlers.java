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

import cn.wjybxx.base.pool.ObjectPool;

import javax.annotation.Nullable;
import java.util.Objects;
import java.util.function.Consumer;
import java.util.function.IntFunction;
import java.util.function.Predicate;
import java.util.function.Supplier;

/**
 * @author wjybxx
 * date - 2024/7/19
 */
public class PoolableObjectHandlers {

    public static <T> PoolableObjectHandler<T> of(Supplier<? extends T> factory,
                                                  @Nullable Consumer<? super T> resetHandler) {
        return new PoolableObjectHandler1<>(factory, resetHandler, null);
    }

    public static <T> PoolableObjectHandler<T> of(Supplier<? extends T> factory,
                                                  @Nullable Consumer<? super T> resetHandler,
                                                  @Nullable Predicate<? super T> filter) {
        return new PoolableObjectHandler1<>(factory, resetHandler, filter);
    }

    public static <T> PoolableObjectHandler<T> of(IntFunction<? extends T> factory,
                                                  @Nullable Consumer<? super T> resetHandler) {
        return new PoolableObjectHandler2<>(factory, resetHandler, null);
    }


    public static <T> PoolableObjectHandler<T> of(IntFunction<? extends T> factory,
                                                  @Nullable Consumer<? super T> resetHandler,
                                                  @Nullable Predicate<? super T> filter) {
        return new PoolableObjectHandler2<>(factory, resetHandler, filter);
    }

    public static PoolableObjectHandler<StringBuilder> newStringBuilderHandler(int minCapacity, int maxCapacity) {
        return new StringBuilderHandler(minCapacity, maxCapacity);
    }

    // region impl

    private static class PoolableObjectHandler1<T> implements PoolableObjectHandler<T> {

        private final Supplier<? extends T> factory;
        private final Consumer<? super T> resetHandler;
        private final Predicate<? super T> filter;

        public PoolableObjectHandler1(Supplier<? extends T> factory,
                                      Consumer<? super T> resetHandler,
                                      Predicate<? super T> filter) {
            this.factory = Objects.requireNonNull(factory, "factory");
            this.resetHandler = resetHandler;
            this.filter = filter;
        }

        @Override
        public T create(ObjectPool<? super T> pool, int capacity) {
            return factory.get();
        }

        @Override
        public boolean test(T obj) {
            return filter == null || filter.test(obj);
        }

        @Override
        public void reset(T obj) {
            if (resetHandler != null) {
                resetHandler.accept(obj);
            }
        }

        @Override
        public void destroy(T obj) {

        }
    }

    private static class PoolableObjectHandler2<T> implements PoolableObjectHandler<T> {

        private final IntFunction<? extends T> factory;
        private final Consumer<? super T> resetHandler;
        private final Predicate<? super T> filter;

        public PoolableObjectHandler2(IntFunction<? extends T> factory,
                                      Consumer<? super T> resetHandler,
                                      Predicate<? super T> filter) {
            this.factory = Objects.requireNonNull(factory, "factory");
            this.resetHandler = resetHandler;
            this.filter = filter;
        }

        @Override
        public T create(ObjectPool<? super T> pool, int capacity) {
            return factory.apply(capacity);
        }

        @Override
        public boolean test(T obj) {
            return filter == null || filter.test(obj);
        }

        @Override
        public void reset(T obj) {
            if (resetHandler != null) {
                resetHandler.accept(obj);
            }
        }

        @Override
        public void destroy(T obj) {

        }
    }

    private static class StringBuilderHandler implements PoolableObjectHandler<StringBuilder> {

        final int minCapacity;
        final int maxCapacity;

        StringBuilderHandler(int minCapacity, int maxCapacity) {
            if (minCapacity < 0 || maxCapacity < minCapacity) {
                throw new IllegalArgumentException();
            }
            this.minCapacity = minCapacity;
            this.maxCapacity = maxCapacity;
        }

        @Override
        public StringBuilder create(ObjectPool<? super StringBuilder> pool, int capacity) {
            return new StringBuilder(capacity > 0 ? capacity : minCapacity);
        }

        @Override
        public boolean test(StringBuilder obj) {
            return obj.capacity() >= minCapacity && obj.capacity() <= maxCapacity;
        }

        @Override
        public void reset(StringBuilder obj) {
            obj.setLength(0);
        }

        @Override
        public void destroy(StringBuilder obj) {

        }
    }
    // endregion
}