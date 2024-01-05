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
import java.util.Arrays;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date 2023/3/31
 */
@NotThreadSafe
public class SimpleByteArrayPool implements ObjectPool<byte[]> {

    private final int poolSize;
    private final int bufferSize;
    private final boolean clear;
    private final List<byte[]> freeBuffers;

    public SimpleByteArrayPool(int poolSize, int bufferSize) {
        this(poolSize, bufferSize, false);
    }

    /**
     * @param poolSize   池大小
     * @param bufferSize 数组大小
     * @param clear      字节素组归入池中时是否clear
     */
    public SimpleByteArrayPool(int poolSize, int bufferSize, boolean clear) {
        if (poolSize < 0 || bufferSize < 0) {
            throw new IllegalArgumentException();
        }
        this.poolSize = poolSize;
        this.bufferSize = bufferSize;
        this.clear = clear;
        this.freeBuffers = new ArrayList<>(MathCommon.clamp(poolSize, 0, 10));
    }

    @Nonnull
    @Override
    public byte[] rent() {
        int size = freeBuffers.size();
        if (size > 0) {
            return freeBuffers.remove(size - 1);
        }
        return new byte[bufferSize];
    }

    @Override
    public void returnOne(byte[] buffer) {
        Objects.requireNonNull(buffer);
        if (freeBuffers.size() < poolSize) {
            if (clear) {
                Arrays.fill(buffer, (byte) 0);
            }
            freeBuffers.add(buffer);
        }
    }

    @Override
    public void clear() {
        freeBuffers.clear();
    }
}
