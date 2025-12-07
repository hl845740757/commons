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

package cn.wjybxx.base.collection;

import cn.wjybxx.base.ArrayUtils;
import cn.wjybxx.base.MathCommon;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.*;
import java.util.function.ObjIntConsumer;

/**
 * 小型动态数组，最大支持64个元素。
 *
 * <h3>null元素比重</h3>
 * 如果等于0，则总是压缩空间；如果等于1，则全为null才压缩空间；如果大于1，则表示不主动压缩空间；
 *
 * @author wjybxx
 * date - 2025/4/11
 */
public final class SmallDynamicArray<E> implements DynamicArray<E> {

    private static final int MAX_CAPACITY = 64;

    private Object[] elements;
    private long elementsMask;
    private final float nullFactor;

    private int len;
    private int recursionDepth;

    public SmallDynamicArray(int initCapacity) {
        this(initCapacity, 0);
    }

    /**
     * @param initCapacity 初始空间大小
     * @param nullFactor   null元素的比重
     */
    public SmallDynamicArray(int initCapacity, float nullFactor) {
        if (initCapacity > MAX_CAPACITY) {
            throw new IllegalArgumentException("initCapacity: " + initCapacity);
        }
        this.elements = initCapacity == 0 ? ArrayUtils.EMPTY_OBJECT_ARRAY : new Object[initCapacity];
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
    @Override
    public E get(int index) {
        Objects.checkIndex(index, len);
        return (E) elements[index];
    }

    @SuppressWarnings("unchecked")
    @Override
    public E set(int index, @Nullable E e) {
        Objects.checkIndex(index, len);
        E prev = (E) elements[index];
        setBit(index, e != null);
        elements[index] = e;
        // 尝试压缩空间
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
        if (elementsMask == 0) {
            return;
        }
        Arrays.fill(elements, 0, len, null);
        elementsMask = 0;
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
        return ArrayUtils.lastIndexOf(elements, e, 0, len);
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
        return ArrayUtils.lastIndexOfRef(elements, e, 0, len);
    }

    private int firstNullIndex() {
        if (len == 0) return -1;
        // 将末尾的1转为0，这样低位的第一个1就是第一个null元素位置
        return Long.numberOfTrailingZeros(~elementsMask);
    }

    private int lastNullIndex() {
        if (len == 0) return -1;
        // 先将超出len这部分也转为1，再整体取反转0，这样高位的第一个1就是第一个null元素位置 -- -1左移64位居然还是-1，我还以为是0
        long tempMask = len == 64
                ? (elementsMask)
                : (elementsMask | (-1L << len));
        return 63 - Long.numberOfLeadingZeros(~tempMask);
    }

    // endregion

    // region len

    @Override
    public int length() {
        return len;
    }

    @Override
    public int elementCount() {
        return MathCommon.bitCount(elementsMask);
    }

    @Override
    public int nullCount() {
        // 不能直接计算0的数量，0的数量可能超过len
        return len - MathCommon.bitCount(elementsMask);
    }

    @Override
    public boolean containsNull() {
        return len > MathCommon.bitCount(elementsMask);
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
        if (minCapacity > MAX_CAPACITY) {
            throw new IllegalStateException("overflow");
        }
        int oldCapacity = elements.length;
        if (minCapacity <= oldCapacity) {
            return;
        }
        int grow = oldCapacity < 16 ? 4 : 8;
        int newCapacity = MathCommon.clamp(oldCapacity + grow, minCapacity, MAX_CAPACITY);
        elements = Arrays.copyOf(elements, newCapacity);
    }

    @Override
    public void compress(boolean ignoreFactor) {
        ensureNotIterating();
        if (ignoreFactor || isCompressionNeeded()) {
            removeNullElements();
        }
    }

    @Override
    public void forEach(ObjIntConsumer<? super E> action) {
        Objects.requireNonNull(action);
        final int len = this.len;
        if (len == 0) {
            return;
        }
        beginItr();
        try {
            Object[] elements = this.elements;
            for (int index = 0; index < len; index++) {
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
        final List<E> result = new ArrayList<>(elementCount());
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
        if (val) {
            elementsMask |= (1L << index);
        } else {
            elementsMask &= ~(1L << index);
        }
    }

    private void insertBit(int index) {
        long high = (elementsMask << 1) & (-1L << (index + 1)); // [0, index] 全0，使index位为0
        long lower = (elementsMask) & ((1L << index) - 1); // [0, index -1] 全1
        elementsMask = high | lower;
    }

    private void ensureNotIterating() {
        if (recursionDepth != 0) {
            throw new IllegalStateException("Invalid between iterating.");
        }
    }

    private boolean isCompressionNeeded() {
        float nullFactor = this.nullFactor;
        if (nullFactor == 0f) return true;
        if (nullFactor > 1f) return false;

        int nullCount = len - elementCount();
        if (nullFactor == 1f) {
            return nullCount == len;
        }
        return nullCount > 0 && nullCount >= len * nullFactor;
    }

    private void removeNullElements() {
        assert recursionDepth == 0;
        int elementCount = elementCount();
        if (elementCount == len) {
            return;
        }
        if (elementCount == 0) {
            this.len = 0;
            this.elementsMask = 0;
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
            elements[index] = null; // help debug
            elements[firstNullIndex++] = element;
        }
        // 批量前移
        int copyStart = lastNullIndex + 1;
        if (copyStart < len) {
            System.arraycopy(elements, copyStart, elements, firstNullIndex, (len - copyStart));
        }
        Arrays.fill(elements, elementCount, len, null);
        this.len = elementCount;
        this.elementsMask = (1L << elementCount) - 1;
    }

    // endregion
}