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

import cn.wjybxx.base.annotation.Internal;

import java.util.ArrayList;
import java.util.Iterator;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/8/8
 */
public final class MarkableIterator<E> implements Iterator<E>, AutoCloseable {

    private Iterator<E> baseIterator;
    private boolean marking;

    private final List<E> buffer;
    /** buffer当前value的索引 */
    private int bufferIndex;
    /** buffer起始偏移 -- 使用双指针法避免删除队首导致的频繁拷贝 */
    private int bufferOffset;

    public MarkableIterator(Iterator<E> baseIterator) {
        this(baseIterator, null);
    }

    /**
     * @param baseIterator 外部迭代器
     * @param buffer       buffer，方便外部池化
     */
    public MarkableIterator(Iterator<E> baseIterator, List<E> buffer) {
        if (buffer == null) {
            buffer = new ArrayList<>();
        } else if (buffer.size() > 0) {
            throw new IllegalArgumentException("buffer is not empty");
        }
        this.baseIterator = Objects.requireNonNull(baseIterator);
        this.marking = false;
        this.buffer = buffer;
        this.bufferIndex = 0;
        this.bufferOffset = 0;
    }

    /**
     * 是否处于干净的状态
     * 1.返回true表示可替换外部迭代器{@link #setBaseIterator(Iterator)}
     * 2.{@link #close()}后一定为true，用于复用对象
     */
    @Internal
    public boolean isClean() {
        return buffer.isEmpty();
    }

    @Internal
    public void setBaseIterator(Iterator<E> baseIterator) {
        if (!isClean()) {
            throw new IllegalStateException();
        }
        this.baseIterator = Objects.requireNonNull(baseIterator);
    }

    /** 当前是否处于标记中 */
    public boolean isMarking() {
        return marking;
    }

    /** 标记位置 */
    public void mark() {
        if (marking) throw new IllegalStateException();
        marking = true;
    }

    /** 倒回到Mark位置，不清理Mark状态 */
    public void rewind() {
        if (!marking) throw new IllegalStateException();
        bufferIndex = bufferOffset;
    }

    /** 倒回到Mark位置，并清理mark状态 */
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
        List<E> buffer = this.buffer;
        E value;
        if (bufferIndex < buffer.size()) {
            value = buffer.get(bufferIndex++);
            if (marking) {
                return value;
            }
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
        } else {
            value = baseIterator.next();
            if (marking) { // 所有读取的值要保存下来
                buffer.add(value);
                bufferIndex++;
            }
        }
        return value;
    }

    @Override
    public void close() {
        this.baseIterator = null;
        this.marking = false;

        this.buffer.clear();
        this.bufferIndex = 0;
        this.bufferOffset = 0;
    }
}