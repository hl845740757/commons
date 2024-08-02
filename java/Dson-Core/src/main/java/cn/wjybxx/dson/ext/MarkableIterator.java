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

package cn.wjybxx.dson.ext;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/8/8
 */
public final class MarkableIterator<E> implements Iterator<E> {

    private Iterator<E> baseIterator;
    private boolean marking;

    private final ArrayList<E> buffer = new ArrayList<>(4);
    private int bufferIndex;
    /** buffer起始偏移，用于避免List频繁删除队首 */
    private int bufferOffset;

    public MarkableIterator(Iterator<E> baseIterator) {
        unsafeInit(baseIterator);
    }

    public boolean isMarking() {
        return marking;
    }

    public void mark() {
        if (marking) throw new IllegalStateException();
        marking = true;
    }

    public void rewind() {
        if (!marking) throw new IllegalStateException();
        bufferIndex = bufferOffset;
    }

    public void reset() {
        if (!marking) throw new IllegalStateException();
        marking = false;
        bufferIndex = bufferOffset;
    }

    @Override
    public boolean hasNext() {
        if (bufferIndex < buffer.size()) {
            return true;
        }
        return baseIterator.hasNext();
    }

    @Override
    public E next() {
        ArrayList<E> buffer = this.buffer;
        E value;
        if (bufferIndex < buffer.size()) {
            value = buffer.get(bufferIndex++);
            if (!marking) {
                buffer.set(bufferOffset++, null); // 使用双指针法避免频繁的拷贝
                if (bufferOffset == buffer.size() || bufferOffset >= 8) {
                    if (bufferOffset == buffer.size()) {
                        buffer.clear();
                    } else {
                        buffer.subList(0, bufferOffset).clear();
                    }
                    bufferIndex = 0;
                    bufferOffset = 0;
                }
            }
        } else {
            value = baseIterator.next();
            if (marking) { // 所有读取的值要保存下来
                buffer.add(value);
                bufferIndex++;
            }
        }
        return value;
    }

    // region pool

    /** 创建用于池化的实例 */
    public static <E> MarkableIterator<E> unsafeCreate() {
        return new MarkableIterator<>();
    }

    private MarkableIterator() {
    }

    public void unsafeInit(Iterator<E> baseIterator) {
        this.baseIterator = Objects.requireNonNull(baseIterator);
        this.bufferIndex = 0;
        this.marking = false;
    }

    public void unsafeDispose() {
        baseIterator = null;
        marking = false;
        buffer.clear();
        bufferIndex = 0;
        bufferOffset = 0;
    }
    // endregion
}