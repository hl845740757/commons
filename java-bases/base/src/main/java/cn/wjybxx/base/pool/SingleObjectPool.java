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

import javax.annotation.concurrent.NotThreadSafe;
import java.util.Collection;
import java.util.Objects;
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
    private final ResetPolicy<? super T> resetPolicy;
    private T value;

    public SingleObjectPool(Supplier<? extends T> factory, ResetPolicy<? super T> resetPolicy) {
        this.factory = Objects.requireNonNull(factory, "factory");
        this.resetPolicy = Objects.requireNonNull(resetPolicy, "resetPolicy");
    }

    @Override
    public T rent() {
        T result = this.value;
        if (result != null) {
            this.value = null;
        } else {
            result = factory.get();
        }
        return result;
    }

    @Override
    public void returnOne(T object) {
        if (object == null) {
            throw new IllegalArgumentException("object cannot be null.");
        }
        assert object != this.value;
        resetPolicy.reset(object);
        this.value = object;
    }

    @Override
    public void returnAll(Collection<? extends T> objects) {
        if (objects == null) {
            throw new IllegalArgumentException("objects cannot be null.");
        }
        for (T obj : objects) {
            if (null == obj) {
                continue;
            }
            assert obj != this.value;
            resetPolicy.reset(obj);
            this.value = obj;
        }
    }

    @Override
    public void freeAll() {
        value = null;
    }

}