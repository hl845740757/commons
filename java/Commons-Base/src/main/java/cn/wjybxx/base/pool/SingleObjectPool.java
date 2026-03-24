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

import javax.annotation.concurrent.ThreadSafe;
import java.lang.invoke.MethodHandles;
import java.lang.invoke.VarHandle;
import java.util.Objects;
import java.util.concurrent.locks.LockSupport;
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
@ThreadSafe
public class SingleObjectPool<T> implements ObjectPool<T> {

    private final Supplier<? extends T> factory;
    private final Consumer<? super T> cleaner;
    private final Predicate<? super T> filter;
    private volatile T value;

    public SingleObjectPool(Supplier<? extends T> factory, Consumer<? super T> cleaner) {
        this(factory, cleaner, null);
    }

    public SingleObjectPool(Supplier<? extends T> factory, Consumer<? super T> cleaner, Predicate<? super T> filter) {
        this.factory = Objects.requireNonNull(factory, "factory");
        this.cleaner = ObjectUtils.nullToDef(cleaner, FunctionUtils.emptyConsumer());
        this.filter = filter;
    }

    @Override
    public T get() {
        return acquire();
    }

    @Override
    public T acquire() {
        T result = value;
        if (VH_VALUE.compareAndSet(this, result, null)) {
            return result;
        }
        return factory.get();
    }

    @Override
    public void release(T obj) {
        if (obj == null) {
            throw new IllegalArgumentException("object cannot be null.");
        }
//        assert obj != this.value;
        cleaner.accept(obj);
        if (filter == null || filter.test(obj)) {
            this.value = obj; // 覆盖式写入
        }
    }

    @Override
    public void clear() {
        value = null;
    }

    private static final VarHandle VH_VALUE;

    static {
        try {
            MethodHandles.Lookup l = MethodHandles.lookup();
            VH_VALUE = l.findVarHandle(SingleObjectPool.class, "value", Object.class);
        } catch (ReflectiveOperationException e) {
            throw new ExceptionInInitializerError(e);
        }
        // Reduce the risk of rare disastrous classloading in first call to
        // LockSupport.park: https://bugs.openjdk.java.net/browse/JDK-8074773
        Class<?> ensureLoaded = LockSupport.class;
    }
}