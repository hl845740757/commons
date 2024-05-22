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

/**
 * 基于ThreadLocal的简单buffer池
 * (默认不支持扩容，因此用途有限 -- 外部可以归还扩容后的数组)
 * (netty的ByteBuf池化是做得比较好的)
 *
 * @author wjybxx
 * date 2023/3/31
 */
public class LocalByteArrayPool implements ArrayPool<byte[]> {

    public static final LocalByteArrayPool INSTANCE = new LocalByteArrayPool();

    @Override
    public byte[] acquire() {
        return THREAD_LOCAL_INST.get().acquire();
    }

    @Override
    public byte[] acquire(int minimumLength) {
        return THREAD_LOCAL_INST.get().acquire(minimumLength);
    }

    @Override
    public byte[] acquire(int minimumLength, boolean clear) {
        return THREAD_LOCAL_INST.get().acquire(minimumLength, clear);
    }

    @Override
    public void release(byte[] array) {
        THREAD_LOCAL_INST.get().release(array);
    }

    @Override
    public void release(byte[] array, boolean clear) {
        THREAD_LOCAL_INST.get().release(array, clear);
    }

    @Override
    public void clear() {

    }

    /** 获取线程本地实例 - 慎用；定义为实例方法，以免和{@link #INSTANCE}的提示冲突 */
    public SimpleArrayPool<byte[]> localInst() {
        return THREAD_LOCAL_INST.get();
    }

    /** 池化数量 */
    private static final int POOL_SIZE;
    /** 池中创建的字节数组的初始大小 */
    private static final int INIT_CAPACITY;
    /** 池中可放入的最大字节数组 */
    private static final int MAX_CAPACITY;
    /** 封装以便我们可以在某些时候去除包装 */
    private static final ThreadLocal<SimpleArrayPool<byte[]>> THREAD_LOCAL_INST;

    static {
        // 全小写看着费劲...因此使用大驼峰
        POOL_SIZE = SystemPropsUtils.getInt("Wjybxx.Commons.IO.LocalByteArrayPool.PoolSize", 16);
        INIT_CAPACITY = SystemPropsUtils.getInt("Wjybxx.Commons.IO.LocalByteArrayPool.InitCapacity", 1024);
        MAX_CAPACITY = SystemPropsUtils.getInt("Wjybxx.Commons.IO.LocalByteArrayPool.MaxCapacity", 512 * 1024);
        THREAD_LOCAL_INST = ThreadLocal.withInitial(() -> new SimpleArrayPool<>(byte[].class, POOL_SIZE, INIT_CAPACITY, MAX_CAPACITY));
    }

}