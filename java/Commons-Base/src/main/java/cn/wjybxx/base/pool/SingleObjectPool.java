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
package cn.wjybxx.base.pool;

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.base.function.FunctionUtils;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.Objects;
import java.util.function.Consumer;
import java.util.function.Predicate;
import java.util.function.Supplier;

/**
 * 只缓存单个对象对象池
 * 相比直接使用共享对象，使用该缓存池可避免递归调用带来的bug
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public class SingleObjectPool<T> implements ObjectPool<T> {

    private final Supplier<? extends T> factory;
    private final Consumer<? super T> resetHandler;
    private final Predicate<? super T> filter;
    private T value;

    public SingleObjectPool(Supplier<? extends T> factory, Consumer<? super T> resetHandler) {
        this(factory, resetHandler, null);
    }

    public SingleObjectPool(Supplier<? extends T> factory, Consumer<? super T> resetHandler, Predicate<? super T> filter) {
        this.factory = Objects.requireNonNull(factory, "factory");
        this.resetHandler = ObjectUtils.nullToDef(resetHandler, FunctionUtils.emptyConsumer());
        this.filter = filter;
    }

    @Override
    public T get() {
        return acquire();
    }

    @Override
    public T acquire() {
        T result = this.value;
        if (result != null) {
            this.value = null;
        } else {
            result = factory.get();
        }
        return result;
    }

    @Override
    public void release(T obj) {
        if (obj == null) {
            throw new IllegalArgumentException("object cannot be null.");
        }
        assert obj != this.value;
        resetHandler.accept(obj);
        if (filter == null || filter.test(obj)) {
            this.value = obj;
        }
    }

    @Override
    public void clear() {
        value = null;
    }

}