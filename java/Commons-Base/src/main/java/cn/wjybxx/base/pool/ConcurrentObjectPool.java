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
import cn.wjybxx.base.SystemPropsUtils;
import cn.wjybxx.base.function.FunctionUtils;

import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;
import java.util.function.Consumer;
import java.util.function.Predicate;
import java.util.function.Supplier;

/**
 * 简单的固定大小的对象池实现
 * (未鉴定归属，可归还外部对象，适用简单场景)
 *
 * @author wjybxx
 * date - 2024/1/6
 */
@ThreadSafe
public final class ConcurrentObjectPool<T> implements ObjectPool<T> {

    private static final int DEFAULT_POOL_SIZE = 64;

    private static final int SBP_MAX_CAPACITY = SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedStringBuilderPool.MaxCapacity", 64 * 1024);
    private static final int SBP_SIZE = SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedStringBuilderPool.PoolSize", 64);

    /** 全局共享的{@link StringBuilder}池 */
    public static final ConcurrentObjectPool<StringBuilder> SHARED_STRING_BUILDER_POOL = new ConcurrentObjectPool<>(
            () -> new StringBuilder(1024),
            sb -> sb.setLength(0),
            SBP_SIZE,
            sb -> sb.capacity() >= 1024 && sb.capacity() <= SBP_MAX_CAPACITY);

    private final Supplier<? extends T> factory;
    private final Consumer<? super T> resetHandler;
    private final Predicate<? super T> filter;
    private final MpmcObjectBucket<T> freeObjects;

    /**
     * @param factory      对象创建工厂
     * @param resetHandler 重置方法
     */
    public ConcurrentObjectPool(Supplier<? extends T> factory, Consumer<? super T> resetHandler) {
        this(factory, resetHandler, DEFAULT_POOL_SIZE, null);
    }

    /**
     * @param factory      对象创建工厂
     * @param resetHandler 重置方法
     * @param poolSize     缓存池大小；0表示不缓存对象
     */
    public ConcurrentObjectPool(Supplier<? extends T> factory, Consumer<? super T> resetHandler, int poolSize) {
        this(factory, resetHandler, poolSize, null);
    }

    /**
     * @param factory      对象创建工厂
     * @param resetHandler 重置方法
     * @param poolSize     缓存池大小；0表示不缓存对象
     * @param filter       对象回收过滤器
     */
    public ConcurrentObjectPool(Supplier<? extends T> factory, Consumer<? super T> resetHandler, int poolSize, Predicate<? super T> filter) {
        if (poolSize < 0) {
            throw new IllegalArgumentException("poolSize: " + poolSize);
        }
        this.factory = Objects.requireNonNull(factory, "factory");
        this.resetHandler = ObjectUtils.nullToDef(resetHandler, FunctionUtils.emptyConsumer());
        this.filter = filter;
        this.freeObjects = new MpmcObjectBucket<>(poolSize);
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
        return obj != null ? obj : factory.get();
    }

    @Override
    public void release(T obj) {
        if (obj == null) {
            throw new IllegalArgumentException("obj cannot be null.");
        }
        resetHandler.accept(obj);
        if (filter == null || filter.test(obj)) {
            freeObjects.offer(obj);
        }
    }

    @Override
    public void clear() {
        //noinspection StatementWithEmptyBody
        while (freeObjects.poll() != null) {
        }
    }
}