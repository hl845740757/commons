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
 *
 * @author wjybxx
 * date 2023/3/31
 */
public class LocalCharArrayPool implements ArrayPool<char[]> {

    public static final LocalCharArrayPool INSTANCE = new LocalCharArrayPool();

    @Override
    public char[] rent() {
        return THREAD_LOCAL_INST.get().rent();
    }

    @Override
    public char[] rent(int minimumLength) {
        return THREAD_LOCAL_INST.get().rent(minimumLength);
    }

    @Override
    public char[] rent(int minimumLength, boolean clear) {
        return THREAD_LOCAL_INST.get().rent(minimumLength, clear);
    }

    @Override
    public void returnOne(char[] array) {
        THREAD_LOCAL_INST.get().returnOne(array);
    }

    @Override
    public void returnOne(char[] array, boolean clear) {
        THREAD_LOCAL_INST.get().returnOne(array, clear);
    }

    @Override
    public void freeAll() {

    }

    /** 获取线程本地实例 - 慎用；定义为实例方法，以免和{@link #INSTANCE}的提示冲突 */
    public SimpleArrayPool<char[]> localInst() {
        return THREAD_LOCAL_INST.get();
    }

    /** 池化数量 */
    private static final int POOL_SIZE;
    /** 池中创建的char数组的初始大小 */
    private static final int INIT_CAPACITY;
    /** 池中可放入的最大char数组 */
    private static final int MAX_CAPACITY;
    /** 封装以便我们可以在某些时候去除包装 */
    private static final ThreadLocal<SimpleArrayPool<char[]>> THREAD_LOCAL_INST;

    static {
        POOL_SIZE = SystemPropsUtils.getInt("Wjybxx.Commons.IO.LocalCharArrayPool.PoolSize", 4);
        INIT_CAPACITY = SystemPropsUtils.getInt("Wjybxx.Commons.IO.LocalCharArrayPool.InitCapacity", 1024);
        MAX_CAPACITY = SystemPropsUtils.getInt("Wjybxx.Commons.IO.LocalCharArrayPool.MaxCapacity", 64 * 1024);
        THREAD_LOCAL_INST = ThreadLocal.withInitial(() -> new SimpleArrayPool<>(char[].class, POOL_SIZE, INIT_CAPACITY, MAX_CAPACITY));
    }

}