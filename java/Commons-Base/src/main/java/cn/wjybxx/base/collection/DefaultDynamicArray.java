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

package cn.wjybxx.base.collection;

import cn.wjybxx.base.ArrayUtils;
import cn.wjybxx.base.MathCommon;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;
import java.util.function.ObjIntConsumer;

import static cn.wjybxx.base.collection.DynamicArrayHelper.wordCount;
import static cn.wjybxx.base.collection.DynamicArrayHelper.wordIndex;

/**
 * 默认的动态数组
 *
 * @author wjybxx
 * date - 2025/4/11
 */
public final class DefaultDynamicArray<E> implements DynamicArray<E> {

    private static final int MAX_CAPACITY = Integer.MAX_VALUE - 8;

    private Object[] elements;
    private long[] elementsMask;
    private final float nullFactor;

    private int len;
    private int elementCount;
    private int recursionDepth;

    public DefaultDynamicArray(int initCapacity) {
        this(initCapacity, 0);
    }

    public DefaultDynamicArray(int initCapacity, float nullFactor) {
        this.elements = new Object[initCapacity];
        this.elementsMask = new long[wordCount(initCapacity)];
        this.nullFactor = Math.max(0, nullFactor);
    }

    // region itr
    @Override
    public boolean isIterating() {
        return recursionDepth > 0;
    }

    @Override
    public void beginItr() {
        recursionDepth++;
    }

    @Override
    public void endItr() {
        if (recursionDepth == 0) {
            throw new IllegalStateException("begin must be called before end.");
        }
        recursionDepth--;
        if (recursionDepth == 0 && isCompressionNeeded()) {
            removeNullElements();
        }
    }
    // endregion

    // region update

    @SuppressWarnings("unchecked")
    @Nullable
    public E get(int index) {
        Objects.checkIndex(index, len);
        return (E) elements[index];
    }

    @SuppressWarnings("unchecked")
    @Override
    public E set(int index, @Nullable E e) {
        Objects.checkIndex(index, len);
        E prev = (E) elements[index];
        if (prev != null) {
            elementCount--;
        }
        if (e != null) {
            elementCount++;
        }
        setBit(index, e != null);
        elements[index] = e;
        if (e == null && recursionDepth == 0 && isCompressionNeeded()) {
            removeNullElements();
        }
        return prev;
    }

    @Override
    public void add(E e) {
        Objects.requireNonNull(e);
        if (len == elements.length) {
            ensureCapacity(len + 1);
        }
        elementCount++;
        setBit(len, true);
        elements[len++] = e;
    }

    @Override
    public void insert(int index, E e) {
        Objects.requireNonNull(e);
        Objects.checkIndex(index, len + 1);
        ensureNotIterating();
        if (len == elements.length) {
            ensureCapacity(len + 1);
        }
        if (index < len) {
            System.arraycopy(elements, index, elements, index + 1, len - index);
            insertBit(index);
        }
        elementCount++;
        setBit(index, true);
        elements[index] = e;
        len++;
    }

    @Override
    public boolean remove(E e) {
        if (e == null) return false;
        int i = indexOf(e);
        if (i >= 0) {
            set(i, null);
            return true;
        }
        return false;
    }

    @Override
    public boolean removeRef(E e) {
        if (e == null) return false;
        int i = indexOfRef(e);
        if (i >= 0) {
            set(i, null);
            return true;
        }
        return false;
    }

    @Override
    public void clear() {
        if (elementCount != 0) {
            Arrays.fill(elements, 0, len, null);
            Arrays.fill(elementsMask, 0, wordCount(len), 0);
        }
        elementCount = 0;
        if (recursionDepth == 0) {
            len = 0;
        }
    }
    // endregion

    // region indexOf

    @Override
    public boolean contains(E e) {
        return indexOf(e) >= 0;
    }

    @Override
    public boolean containsRef(E e) {
        return indexOfRef(e) >= 0;
    }

    @Override
    public int indexOf(@Nullable E e) {
        if (e == null) {
            return firstNullIndex();
        }
        return ArrayUtils.indexOf(elements, e, 0, len);
    }

    @Override
    public int lastIndexOf(@Nullable E e) {
        if (e == null) {
            return lastNullIndex();
        }
        return ArrayUtils.lastIndexOf(elements, e, len - 1, len);
    }

    @Override
    public int indexOfRef(@Nullable E e) {
        if (e == null) {
            return firstNullIndex();
        }
        return ArrayUtils.indexOfRef(elements, e, 0, len);
    }

    @Override
    public int lastIndexOfRef(@Nullable E e) {
        if (e == null) {
            return lastNullIndex();
        }
        return ArrayUtils.lastIndexOfRef(elements, e, len - 1, len);
    }

    private int firstNullIndex() {
        return DynamicArrayHelper.firstNullIndex(elementsMask, len, elementCount);
    }

    private int lastNullIndex() {
        return DynamicArrayHelper.lastNullIndex(elementsMask, len, elementCount);
    }

    // endregion

    // region len

    @Override
    public int length() {
        return len;
    }

    @Override
    public int elementCount() {
        return elementCount;
    }

    @Override
    public int nullCount() {
        return len - elementCount;
    }

    @Override
    public boolean containsNull() {
        return len > elementCount;
    }

    // endregion

    // region other

    @Override
    public void sort(@Nonnull Comparator<? super E> comparator) {
        Objects.requireNonNull(comparator);
        ensureNotIterating();
        // 先压缩空间再排序
        if (containsNull()) {
            removeNullElements();
        }
        @SuppressWarnings("unchecked") E[] elements = (E[]) this.elements;
        Arrays.sort(elements, 0, len, comparator);
    }

    @Override
    public void ensureCapacity(int minCapacity) {
        int oldCapacity = elements.length;
        if (minCapacity <= oldCapacity) {
            return;
        }
        if (minCapacity > MAX_CAPACITY) {
            throw new OutOfMemoryError("Required array length " + minCapacity + " is too large");
        }

        // 中度的成长速度
        int grow = Math.max(8, oldCapacity >> 1);
        int newCapacity = MathCommon.clamp((long) oldCapacity + grow, minCapacity, MAX_CAPACITY);
        elements = Arrays.copyOf(elements, newCapacity);
        if (wordCount(oldCapacity) < wordCount(newCapacity)) {
            elementsMask = Arrays.copyOf(elementsMask, wordCount(newCapacity));
        }
    }

    @Override
    public void compress(boolean force) {
        ensureNotIterating();
        if (force || isCompressionNeeded()) {
            removeNullElements();
        }
    }

    @Override
    public void forEach(ObjIntConsumer<? super E> action) {
        Objects.requireNonNull(action);
        final int size = this.len;
        if (size == 0) {
            return;
        }
        beginItr();
        try {
            Object[] elements = this.elements;
            for (int index = 0; index < size; index++) {
                @SuppressWarnings("unchecked") final E e = (E) elements[index];
                if (e != null) {
                    action.accept(e, index);
                }
            }
        } finally {
            endItr();
        }
    }

    @Override
    public List<E> toList() {
        final List<E> result = new ArrayList<>(elementCount);
        final Object[] elements = this.elements;
        for (int i = 0, end = len; i < end; i++) {
            @SuppressWarnings("unchecked") E e = (E) elements[i];
            if (e != null) {
                result.add(e);
            }
        }
        return result;
    }

    // endregion

    // region internal

    private void setBit(int index, boolean val) {
        // 左移和右移运算符会自动取余
        if (val) {
            elementsMask[wordIndex(index)] |= (1L << index);
        } else {
            elementsMask[wordIndex(index)] &= ~(1L << index);
        }
    }

    private boolean getBit(int index) {
        return (elementsMask[wordIndex(index)] & 1L << index) != 0;
    }

    private void insertBit(int bitIndex) {
        DynamicArrayHelper.insertBit(elementsMask, len, bitIndex);
    }

    private void ensureNotIterating() {
        if (recursionDepth != 0) {
            throw new IllegalStateException("Invalid between iterating.");
        }
    }

    private boolean isCompressionNeeded() {
        return elementCount < len && DynamicArrayHelper.isCompressionNeeded(nullFactor, len, elementCount);
    }

    private void removeNullElements() {
        assert recursionDepth == 0;
        int elementCount = this.elementCount;
        if (elementCount == len) {
            return;
        }
        if (elementCount == 0) {
            this.len = 0;
            return;
        }
        // 零散前移
        int firstNullIndex = firstNullIndex();
        final int lastNullIndex = lastNullIndex();
        final Object[] elements = this.elements;
        for (int index = firstNullIndex + 1; index < lastNullIndex; index++) {
            Object element = elements[index];
            if (element == null) {
                continue;
            }
//            setBit(index, false);
//            elements[index] = null; // help debug

            setBit(firstNullIndex, true);
            elements[firstNullIndex++] = element;
        }
        // 批量前移
        int copyStart = lastNullIndex + 1;
        if (copyStart < len) {
            System.arraycopy(elements, copyStart, elements, firstNullIndex, (len - copyStart));
        }
        DynamicArrayHelper.setBit(elementsMask, firstNullIndex, elementCount);
        DynamicArrayHelper.clearBit(elementsMask, elementCount, len);
        Arrays.fill(elements, elementCount, len, null);
        this.len = elementCount;
    }

    // endregion

}