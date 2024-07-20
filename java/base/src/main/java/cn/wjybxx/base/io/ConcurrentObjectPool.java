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

import cn.wjybxx.base.SystemPropsUtils;
import cn.wjybxx.base.pool.ObjectPool;

import javax.annotation.concurrent.ThreadSafe;
import java.util.Objects;

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

    private static final int SBP_MAX_CAPACITY = SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedStringBuilderPool.MaxCapacity", 64 * 1024);
    private static final int SBP_SIZE = SystemPropsUtils.getInt("Wjybxx.Commons.IO.SharedStringBuilderPool.PoolSize", 64);

    /** 全局共享的{@link StringBuilder}池 */
    public static final ConcurrentObjectPool<StringBuilder> SHARED_STRING_BUILDER_POOL = new ConcurrentObjectPool<>(
            PoolableObjectHandlers.newStringBuilderHandler(1024, SBP_MAX_CAPACITY), SBP_SIZE);

    private final PoolableObjectHandler<T> handler;
    private final MpmcArrayQueue<T> freeObjects;

    /**
     * @param handler 对象处理器
     */
    public ConcurrentObjectPool(PoolableObjectHandler<T> handler) {
        this(handler, DEFAULT_POOL_SIZE);
    }

    /**
     * @param handler  对象处理器
     * @param poolSize 缓存池大小；0表示不缓存对象
     */
    public ConcurrentObjectPool(PoolableObjectHandler<T> handler, int poolSize) {
        if (poolSize < 0) {
            throw new IllegalArgumentException("poolSize: " + poolSize);
        }
        this.handler = Objects.requireNonNull(handler, "factory");
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
        return obj == null ? handler.create(this, 0) : obj;
    }

    @Override
    public void release(T obj) {
        if (obj == null) {
            throw new IllegalArgumentException("obj cannot be null.");
        }
        if (!handler.test(obj)) {
            handler.destroy(obj);
            return;
        }
        handler.reset(obj);
        if (!freeObjects.offer(obj)) {
            handler.destroy(obj);
        }
    }

    @Override
    public void clear() {
        T obj;
        while ((obj = freeObjects.poll()) != null) {
            handler.destroy(obj);
        }
    }

    public void fill(int count) {
        for (int i = 0; i < count; i++) {
            T obj = handler.create(this, 0);
            if (!freeObjects.offer(obj)) {
                handler.destroy(obj);
                return;
            }
        }
    }

}