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

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;
import java.util.function.ObjIntConsumer;

import static cn.wjybxx.base.collection.DynamicArrayHelper.wordCount;
import static cn.wjybxx.base.collection.DynamicArrayHelper.wordIndex;
import static cn.wjybxx.base.collection.IndexedElementHelper.INDEX_NOT_FOUND;

/**
 * 元素被索引的动态数组
 * 注意：会在元素上缓存在数组中的下标，因此{@code index}{@code remove}系列方法总是使用引用相等查询。
 *
 * @author wjybxx
 * date - 2025/4/11
 */
public final class IndexedDynamicArray<E> implements DynamicArray<E> {

    private final IndexedElementHelper<? super E> helper;
    private Object[] elements;
    private long[] elementsMask;
    private final float nullFactor;

    private int len;
    private int elementCount;
    private int recursionDepth;

    public IndexedDynamicArray(IndexedElementHelper<? super E> helper, int initCapacity) {
        this(helper, initCapacity, 0.125f); // 避免迭代时大量的null
    }

    public IndexedDynamicArray(IndexedElementHelper<? super E> helper, int initCapacity, float nullFactor) {
        this.helper = Objects.requireNonNull(helper, "helper");
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
            helper.collectionIndex(this, prev, INDEX_NOT_FOUND);
            elementCount--;
        }
        if (e != null) {
            helper.collectionIndex(this, e, index);
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
        if (helper.collectionIndex(this, e) != INDEX_NOT_FOUND) {
            throw new IllegalArgumentException("e.queueIndex(): %d (expected: %d) + e: %s"
                    .formatted(helper.collectionIndex(this, e), INDEX_NOT_FOUND, e));
        }

        if (len == elements.length) {
            ensureCapacity(len + 1);
        }
        helper.collectionIndex(this, e, len);
        elementCount++;
        setBit(len, true);
        elements[len++] = e;
    }

    @Override
    public void insert(int index, E e) {
        Objects.requireNonNull(e);
        if (helper.collectionIndex(this, e) != INDEX_NOT_FOUND) {
            throw new IllegalArgumentException("e.queueIndex(): %d (expected: %d) + e: %s"
                    .formatted(helper.collectionIndex(this, e), INDEX_NOT_FOUND, e));
        }

        Objects.checkIndex(index, len); // 还是要求index已存在更好
        ensureNotIterating();
        if (len == elements.length) {
            ensureCapacity(len + 1);
        }
        if (index < len) {
            System.arraycopy(elements, index, elements, index + 1, len - index);
            insertBit(index);
        }
        helper.collectionIndex(this, e, index);
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
        if (len == 0) {
            return;
        }
        for (int idx = 0, len = this.len; idx < len; idx++) {
            @SuppressWarnings("unchecked") E e = (E) elements[idx];
            if (e == null) {
                continue;
            }
            helper.collectionIndex(this, e, INDEX_NOT_FOUND);
            elements[idx] = null;
        }
        for (int idx = 0, wordCount = wordCount(len); idx < wordCount; idx++) {
            elementsMask[idx] = 0;
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
        return helper.collectionIndex(this, e);
    }

    @Override
    public int lastIndexOf(@Nullable E e) {
        if (e == null) {
            return lastNullIndex();
        }
        return helper.collectionIndex(this, e);
    }

    @Override
    public int indexOfRef(@Nullable E e) {
        if (e == null) {
            return firstNullIndex();
        }
        return helper.collectionIndex(this, e);
    }

    @Override
    public int lastIndexOfRef(@Nullable E e) {
        if (e == null) {
            return lastNullIndex();
        }
        return helper.collectionIndex(this, e);
    }

    private int firstNullIndex() {
        if (elementCount == len) return -1;
        for (int idx = 0, wordCount = wordCount(len); idx < wordCount; idx++) {
            long word = elementsMask[idx];
            if (word == -1) continue;
            // 将末尾的1转为0，这样低位的第一个1就是第一个null元素位置
            return (idx * 64) + Long.numberOfTrailingZeros(~word);
        }
        throw new AssertionError();
    }

    private int lastNullIndex() {
        if (elementCount == len) return -1;
        int wordCount = wordCount(len);
        for (int idx = wordCount - 1; idx >= 0; idx--) {
            long word = elementsMask[idx];
            // 先将超出len这部分也转为1，再整体取反转0，这样高位的第一个1就是第一个null元素位置 -- -1左移64位居然还是-1，我还以为是0
            if (idx == wordCount - 1 && (len & 63) != 0) {
                word |= -1L << len;
            }
            if (word == -1) continue;
            return (idx * 64) + (63 - Long.numberOfLeadingZeros(~word));
        }
        throw new AssertionError();
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
        return elementCount < len;
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
        batchUpdateIndex(elements, 0, len);
    }

    @Override
    public void ensureCapacity(int minCapacity) {
        int oldCapacity = elements.length;
        if (minCapacity <= oldCapacity) {
            return;
        }
        // 我们需要较快的成长速度
        int grow = oldCapacity >> 1;
        int newCapacity = Math.clamp((long) oldCapacity + grow, 16, Integer.MAX_VALUE - 8);
        if (newCapacity < minCapacity) {
            newCapacity = minCapacity;
        }
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

    private void insertBit(int bitIndex) {
        DynamicArrayHelper.insertBit(elementsMask, len, bitIndex);
    }

    private void ensureNotIterating() {
        if (recursionDepth != 0) {
            throw new IllegalStateException("Invalid between iterating.");
        }
    }

    private boolean isCompressionNeeded() {
        float nullFactor = this.nullFactor;
        if (nullFactor == 0) return true;
        if (nullFactor > 1) return false;

        int nullCount = len - elementCount();
        if (nullFactor == 1) {
            return nullCount == len;
        }
        return nullCount >= 4 && nullCount >= len * nullFactor;
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
        final IndexedElementHelper<? super E> helper = this.helper;
        for (int index = firstNullIndex + 1; index < lastNullIndex; index++) {
            Object element = elements[index];
            if (element == null) {
                continue;
            }
            @SuppressWarnings("unchecked") E castE = (E) element;
            helper.collectionIndex(this, castE, firstNullIndex);

            setBit(index, false);
            setBit(firstNullIndex, true);

            elements[index] = null; // help debug
            elements[firstNullIndex++] = element;
        }
        // 批量前移
        int copyStart = lastNullIndex + 1;
        if (copyStart < len) {
            System.arraycopy(elements, copyStart, elements, firstNullIndex, (len - copyStart));
        }
        batchUpdateIndex(elements, firstNullIndex, elementCount);
        DynamicArrayHelper.setBit(elementsMask, firstNullIndex, elementCount);
        DynamicArrayHelper.clearBit(elementsMask, elementCount, len);
        Arrays.fill(elements, elementCount, len, null);
        this.len = elementCount;
    }

    private void batchUpdateIndex(Object[] elements, int start, int end) {
        IndexedElementHelper<? super E> helper = this.helper;
        for (int index = start; index < end; index++) {
            @SuppressWarnings("unchecked") E castE = (E) elements[index];
            helper.collectionIndex(this, castE, index);
        }
    }

    // endregion

}