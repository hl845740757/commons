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

import cn.wjybxx.base.PropertiesUtils;
import cn.wjybxx.base.pool.ObjectPool;

import javax.annotation.Nonnull;
import java.util.Properties;

/**
 * 基于ThreadLocal的Builder池
 *
 * @author wjybxx
 * date - 2023/8/9
 */
public class LocalStringBuilderPool implements ObjectPool<StringBuilder> {

    public static final LocalStringBuilderPool INSTANCE = new LocalStringBuilderPool();

    @Nonnull
    @Override
    public StringBuilder rent() {
        return THREAD_LOCAL_INST.get().rent();
    }

    @Override
    public void returnOne(StringBuilder builder) {
        THREAD_LOCAL_INST.get().returnOne(builder);
    }

    @Override
    public void clear() {

    }

    /** 获取线程本地实例 - 慎用 */
    public static StringBuilderPool localInst() {
        return THREAD_LOCAL_INST.get();
    }

    /**
     * 每个线程缓存的StringBuilder数量，
     * 同时使用多个Builder实例的情况很少，因此只缓存少量实例即可
     * */
    private static final int POOL_SIZE;
    /**
     * StringBuilder的初始空间，
     * IO操作通常需要较大缓存空间，初始值给大一些
     */
    private static final int INIT_CAPACITY;
    /**
     * StringBuilder的最大空间，
     * 超过限定值的Builder不会被复用
     */
    private static final int MAX_CAPACITY;

    static {
        // 全小写看着费劲...因此使用大驼峰
        Properties properties = System.getProperties();
        POOL_SIZE = PropertiesUtils.getInt(properties, "Wjybxx.Commons.IO.LocalStringBuilderPool.PoolSize", 8);
        INIT_CAPACITY = PropertiesUtils.getInt(properties, "Wjybxx.Commons.IO.LocalStringBuilderPool.InitCapacity", 4096);
        MAX_CAPACITY = PropertiesUtils.getInt(properties, "Wjybxx.Commons.IO.LocalStringBuilderPool.MaxCapacity", Integer.MAX_VALUE);
    }

    /** 封装以便我们可以在某些时候去除包装 */
    private static final ThreadLocal<StringBuilderPool> THREAD_LOCAL_INST = ThreadLocal.withInitial(
            () -> new StringBuilderPool(POOL_SIZE, INIT_CAPACITY));

}
