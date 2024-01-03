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

import java.util.ArrayDeque;
import java.util.Objects;
import java.util.Properties;

/**
 * 基于ThreadLocal的简单buffer池
 *
 * @author wjybxx
 * date 2023/3/31
 */
public class LocalByteArrayPool implements ObjectPool<byte[]> {

    public static final LocalByteArrayPool INSTANCE = new LocalByteArrayPool();

    /** 字节数组不能扩容，因此需要提前规划 */
    private static final int BUFFER_SIZE;
    /** 池化数量 */
    private static final int POOL_SIZE;
    /** 不再额外封装 */
    private static final ThreadLocal<ArrayDeque<byte[]>> LOCAL_BUFFER_QUEUE;

    static {
        Properties properties = System.getProperties();
        BUFFER_SIZE = PropertiesUtils.getInt(properties, "cn.wjybxx.base.io.buffer_size", 64 * 1024);
        POOL_SIZE = PropertiesUtils.getInt(properties, "cn.wjybxx.base.io.buffer_poolsize", 4);
        LOCAL_BUFFER_QUEUE = ThreadLocal.withInitial(() -> new ArrayDeque<>(POOL_SIZE));
    }

    @Override
    public byte[] rent() {
        // 使用栈式结构，更容易发现问题
        final byte[] buffer = LOCAL_BUFFER_QUEUE.get().pollLast();
        if (buffer != null) {
            return buffer;
        }
        return new byte[BUFFER_SIZE];
    }

    @Override
    public void returnOne(byte[] buffer) {
        Objects.requireNonNull(buffer, "buffer");
        final ArrayDeque<byte[]> queue = LOCAL_BUFFER_QUEUE.get();
        if (queue.size() < POOL_SIZE) {
            queue.addLast(buffer);
        }
    }

    @Override
    public void clear() {

    }
}