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
import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.Preconditions;
import cn.wjybxx.base.annotation.Beta;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.*;
import java.util.function.Consumer;
import java.util.function.ObjIntConsumer;
import java.util.function.Predicate;

/**
 * 迭代期间延迟压缩空间的List，在迭代期间删除元素只会清理元素，不会减少size，而插入元素会添加到List末尾并增加size
 * 1.不支持插入Null -- 理论上做的到，但会导致较高的复杂度，也很少有需要。
 * 2.未实现{@link Iterable}接口，因为不能按照正常方式迭代
 * <h3>使用方式</h3>
 * <pre><code>
 *     list.beginItr();
 *     try {
 *         for(int i = 0, size = list.size();i < size; i++){
 *              E e = list.get(i);
 *              if (e == null) {
 *                  continue;
 *              }
 *              doSomething(e);
 *         }
 *     } finally {
 *         list.endItr();
 *     }
 * </code></pre>
 * PS：
 * 1.该List主要用于事件监听器列表和对象列表等场景。
 * 2.使用{@link #forEach(Consumer)}可能有更好的迭代速度。
 *
 * @author wjybxx
 * date 2023/4/6
 */
@Beta("改为数组后待测试")
@NotThreadSafe
public final class DelayedCompressList<E> {

    private static final int INDEX_NOT_FOUND = CollectionUtils.INDEX_NOT_FOUND;

    private Object[] elements;
    private int size;
    private int recursionDepth;

    /**
     * 记录删除的元素的范围，避免迭代所有
     * 注意：可以根据firstIndex == lastIndex 可以得出只删除了一个元素；但不能根据firstIndex和lastIndex判断出是否调用过clear。
     * ps：如果迭代期间删除元素较少，也可以记录下标
     */
    private transient int firstIndex = INDEX_NOT_FOUND;
    private transient int lastIndex = INDEX_NOT_FOUND;
    private transient int clearSize = 0;

    public DelayedCompressList() {
        this(4);
    }

    public DelayedCompressList(int initCapacity) {
        elements = new Object[initCapacity];
    }

    public DelayedCompressList(Collection<? extends E> src) {
        Object[] array = src.toArray();
        Preconditions.checkNullElements(array);
        // 确保为Object[]，且不产生内存共享
        if (array.length != 0 && src.getClass() != ArrayList.class) {
            array = Arrays.copyOf(array, array.length, Object[].class);
        }
        elements = array;
        size = elements.length;
    }

    /** 开始迭代 */
    public void beginItr() {
        recursionDepth++;
    }

    /** 迭代结束 -- 必须在finally块中调用，否则可能使List处于无效状态 */
    public void endItr() {
        if (recursionDepth == 0) {
            throw new IllegalStateException("begin must be called before end.");
        }
        recursionDepth--;
        if (recursionDepth == 0 && firstIndex != INDEX_NOT_FOUND) {
            if (firstIndex == lastIndex) {
                fastRemoveAt(firstIndex);
            } else if (clearSize == size) {
                Arrays.fill(elements, 0, size, null);
            } else {
                fastRemoveRange(firstIndex, lastIndex);
            }
            firstIndex = INDEX_NOT_FOUND;
            lastIndex = INDEX_NOT_FOUND;
            clearSize = 0;
        }
    }

    private void fastRemoveAt(int index) {
        int newSize = size - 1;
        if (index < newSize) {
            System.arraycopy(elements, index + 1, elements, index, size - firstIndex);
        }
        size = newSize;
        elements[newSize] = null;
    }

    /**
     * @param startIndex 被删除的第一个元素
     * @param endIndex   被删除的最后一个元素
     */
    private void fastRemoveRange(int startIndex, int endIndex) {
        // (startIndex, endIndex) 区间非null元素前移
        Object[] elements = this.elements;
        for (int index = startIndex; index < endIndex; index++) {
            Object e = elements[index];
            if (e != null) {
                elements[startIndex++] = e;
            }
        }
        // endIndex后续元素前移
        int copyLength = size - (endIndex + 1);
        if (copyLength > 0) {
            System.arraycopy(elements, (endIndex + 1), elements, startIndex, copyLength);
        }
        int newSize = startIndex + copyLength;
        Arrays.fill(elements, newSize, size, null);
        size = newSize;
    }

    /** 当前是否正在迭代 */
    public boolean isIterating() {
        return recursionDepth > 0;
    }

    /** 是否处于延迟压缩状态；是否在迭代期间删除了元素 */
    public boolean isDelayed() {
        return firstIndex != INDEX_NOT_FOUND;
    }

    /**
     * @return 如果添加元素成功则返回true
     * @throws NullPointerException 如果e为null
     */
    public boolean add(E e) {
        Objects.requireNonNull(e);
        if (size == elements.length) {
            ensureCapacity(size + 1);
        }
        elements[size++] = e; // element可能
        return true;
    }

    /** 批量添加元素 */
    public boolean addAll(@Nonnull Collection<? extends E> c) {
        Object[] others = c.toArray();
        for (Object e : others) {
            Objects.requireNonNull(e, "collection contains null element");
        }

        ensureCapacity(size + others.length);
        System.arraycopy(others, 0, elements, size, others.length);
        size += others.length;
        return true;
    }

    private void ensureCapacity(int minCapacity) {
        int oldCapacity = elements.length;
        if (minCapacity <= oldCapacity) {
            return;
        }
        int newCapacity = Math.clamp((long) oldCapacity + oldCapacity >> 1,
                4, Integer.MAX_VALUE - 8);
        if (newCapacity < minCapacity) {
            newCapacity = minCapacity;
        }
        elements = Arrays.copyOf(elements, newCapacity);
    }

    /**
     * 获取指定位置的元素
     *
     * @return 如果指定位置的元素已删除，则返回null
     */
    @SuppressWarnings("unchecked")
    @Nullable
    public E get(int index) {
        Objects.checkIndex(index, size);
        return (E) elements[index];
    }

    /**
     * 将给定元素赋值到给定位置
     *
     * @return 该位置的前一个值
     * @throws NullPointerException 如果e为null
     */
    public E set(int index, E e) {
        Objects.requireNonNull(e);
        Objects.checkIndex(index, size);
        @SuppressWarnings("unchecked") E ele = (E) elements[index];
        elements[index] = e;
        return ele;
    }

    /**
     * 删除给定位置的元素
     *
     * @return 如果指定位置存在元素，则返回对应的元素，否则返回Null
     */
    public E removeAt(int index) {
        Objects.checkIndex(index, size);
        Object[] elements = this.elements;

        @SuppressWarnings("unchecked") E ele = (E) elements[index];
        if (ele == null) {
            return null;
        }
        if (recursionDepth == 0) {
            // 立即删除
            fastRemoveAt(index);
        } else {
            // 延迟删除
            elements[index] = null;
            if (firstIndex == INDEX_NOT_FOUND || index < firstIndex) {
                firstIndex = index;
            }
            if (lastIndex == INDEX_NOT_FOUND || index > lastIndex) {
                lastIndex = index;
            }
        }
        return ele;
    }

    /**
     * @apiNote 在迭代期间清理元素不会更新size
     */
    public void clear() {
        if (size == 0) {
            return;
        }
        Object[] elements = this.elements;
        if (recursionDepth == 0) {
            // 立即clear
            Arrays.fill(elements, 0, size, null);
            size = 0;
        } else {
            // 延迟clear
            Arrays.fill(elements, 0, size, null);
            firstIndex = 0;
            lastIndex = size - 1;
            clearSize = size;
        }
    }

    /** 基于equals查询一个元素是否在List中 */
    public boolean contains(Object e) {
        return index(e) >= 0;
    }

    /** 基于引用相等查询一个元素是否在List中 */
    public boolean containsRef(Object e) {
        return indexOfRef(e) >= 0;
    }

    /**
     * 基于equals查找元素在List中的位置
     *
     * @param e 如果null，表示查询第一个删除的的元素位置
     * @return 如果元素不在集合中，则返回-1
     */
    public int index(@Nullable Object e) {
        if (e == null) {
            return firstIndex;
        }
        return ArrayUtils.indexOf(elements, e, 0, size);
    }

    /**
     * 基于equals逆向查找元素在List中的位置
     *
     * @param e 如果null，表示查询最后一个删除的的元素位置
     * @return 如果元素不在集合中，则返回-1
     */
    public int lastIndex(@Nullable Object e) {
        if (e == null) {
            return lastIndex;
        }
        return ArrayUtils.lastIndexOf(elements, e, 0, size);
    }

    /**
     * 基于引用相等查找元素在List中的位置
     *
     * @param e 如果null，表示查询第一个删除的的元素位置
     * @return 如果元素不在集合中，则返回-1
     */
    public int indexOfRef(@Nullable Object e) {
        if (e == null) {
            return firstIndex;
        }
        return ArrayUtils.indexOfRef(elements, e, 0, size);
    }

    /**
     * 基于引用相等逆向查找元素在List中的位置
     *
     * @param e 如果null，表示查询最后一个删除的的元素位置
     * @return 如果元素不在集合中，则返回-1
     */
    public int lastIndexOfRef(@Nullable Object e) {
        if (e == null) {
            return lastIndex;
        }
        return ArrayUtils.lastIndexOfRef(elements, e, 0, size);
    }

    /**
     * 自定义index查询；自定义查询时不支持查找null
     *
     * @param predicate 查询过程不可以修改当前List的状态
     */
    public int indexCustom(Predicate<? super E> predicate) {
        Objects.requireNonNull(predicate);
        int size = size();
        if (size == 0) {
            return INDEX_NOT_FOUND;
        }
        Object[] elements = this.elements;
        for (int index = 0; index < size; index++) {
            @SuppressWarnings("unchecked") final E e = (E) elements[index];
            if (e != null && predicate.test(e)) {
                return index;
            }
        }
        return INDEX_NOT_FOUND;
    }

    /**
     * 自定义lastIndex查询；自定义查询时不支持查找null
     *
     * @param predicate 查询过程不可以修改当前List的状态
     */
    public int lastIndexCustom(Predicate<? super E> predicate) {
        Objects.requireNonNull(predicate);
        int size = size();
        if (size == 0) {
            return INDEX_NOT_FOUND;
        }
        Object[] elements = this.elements;
        for (int index = size - 1; index >= 0; index--) {
            @SuppressWarnings("unchecked") final E e = (E) elements[index];
            if (e != null && predicate.test(e)) {
                return index;
            }
        }
        return INDEX_NOT_FOUND;
    }

    /**
     * 根据equals相等删除元素
     *
     * @return 如果元素在集合中则删除并返回true
     */
    public boolean remove(Object e) {
        if (e == null) return false;
        int i = index(e);
        if (i >= 0) {
            removeAt(i);
            return true;
        }
        return false;
    }

    /**
     * 根据引用相等删除元素
     *
     * @return 如果元素在集合中则删除并返回true
     */
    public boolean removeRef(Object e) {
        if (e == null) return false;
        int i = indexOfRef(e);
        if (i >= 0) {
            removeAt(i);
            return true;
        }
        return false;
    }

    /**
     * @throws IllegalStateException 如果当前正在迭代
     */
    public void sort(@Nonnull Comparator<? super E> comparator) {
        Objects.requireNonNull(comparator);
        ensureNotIterating();
        @SuppressWarnings("unchecked") E[] elements = (E[]) this.elements;
        Arrays.sort(elements, 0, size, comparator);
    }

    /**
     * 获取list的当前大小
     * 注意：迭代期间删除的元素并不会导致size变化，因此该值是一个不准确的值。
     */
    public int size() {
        return size;
    }

    /**
     * 判断list是否为空
     * 注意：迭代期间删除的元素并不会导致size变化，因此该值是一个不准确的值。
     */
    public boolean isEmpty() {
        return size == 0;
    }

    /**
     * 获取list的真实大小
     * 如果当前正在迭代，则可能产生遍历统计的情况，要注意开销问题。
     */
    public int realSize() {
        if (recursionDepth == 0
                || firstIndex == INDEX_NOT_FOUND) { // 没有删除元素
            return size;
        }
        Object[] elements = this.elements;
        int nullCount = 0;
        for (int index = firstIndex, endIndex = lastIndex; index <= endIndex; index++) {
            if (elements[index] == null) {
                nullCount++;
            }
        }
        return size - nullCount;
    }

    /**
     * 查询List是否真的为空
     * 如果当前正在迭代，则可能产生遍历统计的情况，要注意开销问题。
     */
    public boolean isRealEmpty() {
        if (size == 0) {
            return true;
        }
        Object[] elements = this.elements;
        for (int index = firstIndex, endIndex = lastIndex; index <= endIndex; index++) {
            if (elements[index] != null) {
                return false;
            }
        }
        return true;
    }

    /**
     * 迭代List内的元素，该快捷方式不会迭代迭代期间新增的元素
     * 如果需要元素的下标，请使用{@link #forEach(ObjIntConsumer)}
     */
    public void forEach(Consumer<? super E> action) {
        Objects.requireNonNull(action);
        final int size = this.size;
        if (size == 0) {
            return;
        }

        Object[] elements = this.elements;
        beginItr();
        try {
            for (int index = 0; index < size; index++) {
                @SuppressWarnings("unchecked") final E e = (E) elements[index];
                if (e != null) {
                    action.accept(e);
                }
            }
        } finally {
            endItr();
        }
    }

    /**
     * 迭代List内的元素，该快捷方式不会迭代迭代期间新增的元素
     *
     * @param action 参数1为对应元素，参数2为下标 -- 返回index以方便快速删除
     */
    public void forEach(ObjIntConsumer<? super E> action) {
        Objects.requireNonNull(action);
        final int size = this.size;
        if (size == 0) {
            return;
        }

        Object[] elements = this.elements;
        beginItr();
        try {
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

    //

    private void ensureNotIterating() {
        if (recursionDepth > 0) {
            throw new IllegalStateException("Invalid between iterating.");
        }
    }

}