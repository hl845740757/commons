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
 *
 * @author wjybxx
 * date 2023/3/31
 */
public class LocalByteArrayPool implements ObjectPool<byte[]> {

    public static final LocalByteArrayPool INSTANCE = new LocalByteArrayPool();

    @Override
    public byte[] rent() {
        return THREAD_LOCAL_INST.get().rent();
    }

    @Override
    public void returnOne(byte[] buffer) {
        THREAD_LOCAL_INST.get().returnOne(buffer);
    }

    @Override
    public void clear() {

    }

    /** 获取线程本地实例 - 慎用 */
    public static SimpleByteArrayPool localInst() {
        return THREAD_LOCAL_INST.get();
    }

    /** 池化数量 */
    private static final int POOL_SIZE;
    /** 字节数组不能扩容，因此需要提前规划 */
    private static final int BUFFER_SIZE;
    /** 封装以便我们可以在某些时候去除包装 */
    private static final ThreadLocal<SimpleByteArrayPool> THREAD_LOCAL_INST;

    static {
        Properties properties = System.getProperties();
        POOL_SIZE = PropertiesUtils.getInt(properties, "Wjybxx.Commons.IO.LocalByteArrayPool.PoolSize", 4);
        BUFFER_SIZE = PropertiesUtils.getInt(properties, "Wjybxx.Commons.IO.LocalByteArrayPool.BufferSize", 64 * 1024);
        THREAD_LOCAL_INST = ThreadLocal.withInitial(() -> new SimpleByteArrayPool(POOL_SIZE, BUFFER_SIZE));
    }

}