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
 * date - 2023/8/9
 */
@NotThreadSafe
public class StringBuilderPool implements ObjectPool<StringBuilder> {

    private final int poolSize;
    private final int initCapacity;
    private final List<StringBuilder> freeBuilders;

    public StringBuilderPool(int poolSize, int initCapacity) {
        if (poolSize < 0 || initCapacity < 0) {
            throw new IllegalArgumentException();
        }
        this.poolSize = poolSize;
        this.initCapacity = initCapacity;
        this.freeBuilders = new ArrayList<>(MathCommon.clamp(poolSize, 0, 10));
    }

    @Nonnull
    @Override
    public StringBuilder rent() {
        int size = freeBuilders.size();
        if (size > 0) {
            return freeBuilders.remove(size - 1);
        }
        return new StringBuilder(initCapacity);
    }

    @Override
    public void returnOne(StringBuilder builder) {
        Objects.requireNonNull(builder);
        if (freeBuilders.size() < poolSize) {
            builder.setLength(0);
            freeBuilders.add(builder);
        }
    }

    @Override
    public void clear() {
        freeBuilders.clear();
    }
}
