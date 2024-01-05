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

import cn.wjybxx.base.MathCommon;

import javax.annotation.concurrent.NotThreadSafe;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Objects;
import java.util.function.Supplier;

/**
 * 对象池的默认实现
 * <h3>队列 OR 栈</h3>
 * 主要区别：栈结构会频繁使用栈顶元素，而队列结构的元素是平等的。
 * 因此栈结构有以下特性：
 * 1.如果复用对象存在bug，更容易发现。
 * 2.如果池化的对象是List这类会扩容的对象，则只有栈顶部分的对象会扩容较大。
 *
 * @author wjybxx
 * date 2023/4/1
 */
@NotThreadSafe
public final class DefaultObjectPool<T> implements ObjectPool<T> {

    /** 默认不能无限缓存 */
    private static final int DEFAULT_POOL_SIZE = 1024;

    private final Supplier<? extends T> factory;
    private final ResetPolicy<? super T> resetPolicy;
    private final int poolSize;
    private final ArrayList<T> freeObjects;

    public DefaultObjectPool(Supplier<? extends T> factory, ResetPolicy<? super T> resetPolicy) {
        this(factory, resetPolicy, DEFAULT_POOL_SIZE);
    }

    /**
     * @param factory     对象创建工厂
     * @param resetPolicy 重置方法
     * @param poolSize    缓存池大小；0表示不缓存对象
     */
    public DefaultObjectPool(Supplier<? extends T> factory, ResetPolicy<? super T> resetPolicy, int poolSize) {
        if (poolSize < 0) {
            throw new IllegalArgumentException("poolSize: " + poolSize);
        }
        this.factory = Objects.requireNonNull(factory, "factory");
        this.resetPolicy = Objects.requireNonNull(resetPolicy, "resetPolicy");
        this.poolSize = poolSize;
        this.freeObjects = new ArrayList<>(MathCommon.clamp(poolSize, 0, 10));
    }

    public int getPoolSize() {
        return poolSize;
    }

    @Override
    public T rent() {
        int size = freeObjects.size();
        if (size > 0) {
            return freeObjects.remove(size - 1); // 可避免拷贝
        }
        return factory.get();
    }

    @Override
    public void returnOne(T object) {
        if (object == null) {
            throw new IllegalArgumentException("object cannot be null.");
        }
        // 先调用reset，避免reset出现异常导致添加脏对象到缓存池中 -- 断言是否在池中还是有较大开销
        resetPolicy.reset(object);
//        assert !CollectionUtils.containsRef(freeObjects, e);
        if (freeObjects.size() < poolSize) {
            freeObjects.add(object);
        }
    }

    @Override
    public void returnAll(Collection<? extends T> objects) {
        if (objects == null) {
            throw new IllegalArgumentException("objects cannot be null.");
        }

        final ArrayList<T> freeObjects = this.freeObjects;
        final int poolSize = this.poolSize;
        final ResetPolicy<? super T> resetPolicy = this.resetPolicy;
        if (objects instanceof ArrayList<? extends T> arrayList) {
            for (int i = 0, n = arrayList.size(); i < n; i++) {
                T obj = arrayList.get(i);
                if (null == obj) {
                    continue;
                }
                resetPolicy.reset(obj);
//                assert !CollectionUtils.containsRef(freeObjects, obj);
                if (freeObjects.size() < poolSize) {
                    freeObjects.add(obj);
                }
            }
        } else {
            for (T obj : objects) {
                if (null == obj) {
                    continue;
                }
                resetPolicy.reset(obj);
//                assert !CollectionUtils.containsRef(freeObjects, obj);
                if (freeObjects.size() < poolSize) {
                    freeObjects.add(obj);
                }
            }
        }
    }

    @Override
    public void clear() {
        freeObjects.clear();
    }

}