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

import java.util.Properties;

/**
 * 基于ThreadLocal的简单buffer池
 * (默认不支持扩容，因此用途有限 -- 外部可以归还扩容后的数组)
 *
 * @author wjybxx
 * date 2023/3/31
 */
public class LocalCharArrayPool implements ObjectPool<char[]> {

    public static final LocalCharArrayPool INSTANCE = new LocalCharArrayPool();

    @Override
    public char[] rent() {
        return THREAD_LOCAL_INST.get().rent();
    }

    @Override
    public void returnOne(char[] buffer) {
        THREAD_LOCAL_INST.get().returnOne(buffer);
    }

    @Override
    public void clear() {

    }

    /** 获取线程本地实例 - 慎用 */
    public static SimpleCharArrayPool localInst() {
        return THREAD_LOCAL_INST.get();
    }

    /** 池化数量 */
    private static final int POOL_SIZE;
    /** 池中创建的char数组的初始大小 */
    private static final int INIT_CAPACITY;
    /** 池中可放入的最大char数组 */
    private static final int MAX_CAPACITY;
    /** 封装以便我们可以在某些时候去除包装 */
    private static final ThreadLocal<SimpleCharArrayPool> THREAD_LOCAL_INST;

    static {
        Properties properties = System.getProperties();
        POOL_SIZE = PropertiesUtils.getInt(properties, "Wjybxx.Commons.IO.LocalCharArrayPool.PoolSize", 4);
        INIT_CAPACITY = PropertiesUtils.getInt(properties, "Wjybxx.Commons.IO.LocalCharArrayPool.InitCapacity", 1024);
        MAX_CAPACITY = PropertiesUtils.getInt(properties, "Wjybxx.Commons.IO.LocalCharArrayPool.MaxCapacity", 64 * 1024);
        THREAD_LOCAL_INST = ThreadLocal.withInitial(() -> new SimpleCharArrayPool(POOL_SIZE, INIT_CAPACITY, MAX_CAPACITY));
    }

}