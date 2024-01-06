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

import cn.wjybxx.base.MathCommon;
import cn.wjybxx.base.pool.ObjectPool;

import javax.annotation.Nonnull;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/3/31
 */
@NotThreadSafe
public class SimpleCharArrayPool implements ObjectPool<char[]> {

    private final int poolSize;
    private final int initCapacity;
    private final int maxCapacity;
    private final List<char[]> freeBuffers;

    /**
     * @param poolSize   池大小
     * @param initCapacity 数组大小
     */
    public SimpleCharArrayPool(int poolSize, int initCapacity) {
        this(poolSize, initCapacity, Integer.MAX_VALUE);
    }

    /**
     * @param poolSize   池大小
     * @param initCapacity 数组初始大小
     * @param maxCapacity  数组最大大小 -- 超过大小的字节数组不会放入池中
     */
    public SimpleCharArrayPool(int poolSize, int initCapacity, int maxCapacity) {
        if (poolSize < 0 || initCapacity < 0 || maxCapacity < 0) {
            throw new IllegalArgumentException();
        }
        this.poolSize = poolSize;
        this.initCapacity = initCapacity;
        this.maxCapacity = maxCapacity;
        this.freeBuffers = new ArrayList<>(MathCommon.clamp(poolSize, 0, 10));
    }

    @Nonnull
    @Override
    public char[] rent() {
        int size = freeBuffers.size();
        if (size > 0) {
            return freeBuffers.remove(size - 1);
        }
        return new char[initCapacity];
    }

    @Override
    public void returnOne(char[] buffer) {
        Objects.requireNonNull(buffer);
        if (freeBuffers.size() < poolSize && buffer.length <= maxCapacity) {
            freeBuffers.add(buffer);
        }
    }

    @Override
    public void clear() {
        freeBuffers.clear();
    }

}