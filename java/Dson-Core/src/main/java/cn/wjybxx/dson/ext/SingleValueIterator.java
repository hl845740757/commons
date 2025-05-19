/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

package cn.wjybxx.dson.ext;

import java.util.Iterator;
import java.util.NoSuchElementException;

/**
 * @author wjybxx
 * date - 2025/5/19
 */
public class SingleValueIterator<T> implements Iterator<T> {

    private final T value;
    private boolean hasNext = true;

    public SingleValueIterator(T value) {
        this.value = value;
    }

    @Override
    public boolean hasNext() {
        return hasNext;  // 仅在第一次调用时返回true
    }

    @Override
    public T next() {
        if (!hasNext) {
            throw new NoSuchElementException("No more elements");
        }
        hasNext = false;
        return value;
    }

    // 可选：不实现remove方法
    @Override
    public void remove() {
        throw new UnsupportedOperationException("remove not supported");
    }
}