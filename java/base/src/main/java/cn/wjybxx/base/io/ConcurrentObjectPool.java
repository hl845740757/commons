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

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.base.pool.ObjectPool;
import cn.wjybxx.base.pool.ResetPolicy;

import javax.annotation.concurrent.ThreadSafe;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Objects;
import java.util.function.Predicate;
import java.util.function.Supplier;

/**
 * 固定大小的普通对象池实现
 * (未鉴定归属，可归还外部对象)
 *
 * @author wjybxx
 * date - 2024/1/6
 */
@ThreadSafe
public final class ConcurrentObjectPool<T> implements ObjectPool<T> {

    private static final int DEFAULT_POOL_SIZE = 64;

    /** 全局共享的{@link StringBuilder}池 */
    public static final ConcurrentObjectPool<StringBuilder> SHARED_STRING_BUILDER_POOL = new ConcurrentObjectPool<>(
            () -> new StringBuilder(1024),
            sb -> sb.setLength(0),
            64,
            sb -> sb.capacity() >= 1024 && sb.capacity() <= 64 * 1024);

    private final Supplier<? extends T> factory;
    private final ResetPolicy<? super T> resetPolicy;
    private final Predicate<? super T> filter;
    private final MpmcArrayQueue<T> freeObjects;

    /**
     * @param factory     对象创建工厂
     * @param resetPolicy 重置方法
     */
    public ConcurrentObjectPool(Supplier<? extends T> factory, ResetPolicy<? super T> resetPolicy) {
        this(factory, resetPolicy, DEFAULT_POOL_SIZE, null);
    }

    /**
     * @param factory     对象创建工厂
     * @param resetPolicy 重置方法
     * @param poolSize    缓存池大小；0表示不缓存对象
     */
    public ConcurrentObjectPool(Supplier<? extends T> factory, ResetPolicy<? super T> resetPolicy, int poolSize) {
        this(factory, resetPolicy, poolSize, null);
    }

    /**
     * @param factory     对象创建工厂
     * @param resetPolicy 重置方法
     * @param poolSize    缓存池大小；0表示不缓存对象
     * @param filter      对象回收过滤器
     */
    public ConcurrentObjectPool(Supplier<? extends T> factory, ResetPolicy<? super T> resetPolicy, int poolSize, Predicate<? super T> filter) {
        if (poolSize < 0) {
            throw new IllegalArgumentException("poolSize: " + poolSize);
        }
        this.factory = Objects.requireNonNull(factory, "factory");
        this.resetPolicy = ObjectUtils.nullToDef(resetPolicy, ResetPolicy.DO_NOTHING);
        this.filter = filter;
        this.freeObjects = new MpmcArrayQueue<>(poolSize);
    }

    /** 获取池大小 */
    public int getPoolSize() {
        return freeObjects.getLength();
    }

    /**
     * 可用对象数
     * 注意：这只是一个估值，通常仅用于debug和测试用例
     */
    public int getAvailableCount() {
        return freeObjects.size();
    }

    @Deprecated
    @Override
    public T get() {
        return acquire();
    }

    @Override
    public T acquire() {
        T obj = freeObjects.poll();
        return obj == null ? factory.get() : obj;
    }

    @Override
    public void release(T object) {
        if (object == null) {
            throw new IllegalArgumentException("object cannot be null.");
        }
        resetPolicy.reset(object);
        if (filter == null || filter.test(object)) {
            freeObjects.offer(object);
        }
    }

    @Override
    public void releaseAll(Collection<? extends T> objects) {
        if (objects == null) {
            throw new IllegalArgumentException("objects cannot be null.");
        }

        final MpmcArrayQueue<T> freeObjects = this.freeObjects;
        final ResetPolicy<? super T> resetPolicy = this.resetPolicy;
        final Predicate<? super T> filter = this.filter;
        if (objects instanceof ArrayList<? extends T> arrayList) {
            for (int i = 0, n = arrayList.size(); i < n; i++) {
                T obj = arrayList.get(i);
                if (null == obj) {
                    continue;
                }
                resetPolicy.reset(obj);
                if (filter == null || filter.test(obj)) {
                    freeObjects.offer(obj);
                }
            }
        } else {
            for (T obj : objects) {
                if (null == obj) {
                    continue;
                }
                resetPolicy.reset(obj);
                if (filter == null || filter.test(obj)) {
                    freeObjects.offer(obj);
                }
            }
        }
    }

    @Override
    public void clear() {
        //noinspection StatementWithEmptyBody
        while (freeObjects.poll() != null) {

        }
    }

}