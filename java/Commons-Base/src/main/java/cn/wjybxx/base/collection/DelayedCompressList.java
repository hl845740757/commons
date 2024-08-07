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
import cn.wjybxx.base.annotation.Beta;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.*;
import java.util.function.Consumer;
import java.util.function.ObjIntConsumer;
import java.util.function.Predicate;

import static cn.wjybxx.base.collection.IndexedElementHelper.INDEX_NOT_FOUND;

/**
 * 迭代期间延迟压缩空间的List，在迭代期间删除元素只会清理元素，不会减少size，而插入元素会添加到List末尾并增加size
 * 1.不支持插入Null -- 理论上做的到，但会导致较高的复杂度，也很少有需要。
 * 2.未实现{@link Iterable}接口，因为不能按照正常方式迭代 -- 理论上迭代器实现{@link AutoCloseable}接口即可，但较危险。
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

    private Object[] elements;
    /** 用于管理元素的下标 */
    private final IndexedElementHelper<? super E> helper;
    /** 负载因子，当负载低于该值时才压缩空间 */
    private final float loadFactor;

    private int size;
    private int realSize;
    private int firstNullIndex = -1;
    private int recursionDepth;

    public DelayedCompressList() {
        this(8, 0.75f, null);
    }

    public DelayedCompressList(int initCapacity) {
        this(initCapacity, 0.75f, null);
    }

    public DelayedCompressList(int initCapacity, float loadFactor) {
        this(initCapacity, loadFactor, null);
    }

    public DelayedCompressList(int initCapacity, float loadFactor, IndexedElementHelper<? super E> helper) {
        if (loadFactor < 0 || loadFactor > 1) throw new IllegalArgumentException("loadFactor: " + loadFactor);
        this.elements = new Object[initCapacity];
        this.loadFactor = loadFactor;
        this.helper = helper;
    }

    /** 开始迭代 */
    public void beginItr() {
        recursionDepth++;
    }

    /** 迭代结束 -- 必须在finally块中调用，否则可能使List处于无效状态；特殊情况下可以反复调用该接口修复状态。 */
    public void endItr() {
        if (recursionDepth == 0) {
            throw new IllegalStateException("begin must be called before end.");
        }
        recursionDepth--;
        if (recursionDepth == 0 && isCompressionNeeded()) {
            removeNullElements();
        }
    }

    /** 主动压缩空间 */
    public void compress(boolean force) {
        ensureNotIterating();
        if (force || isCompressionNeeded()) {
            removeNullElements();
        }
    }

    /** 是否需要压缩空间 */
    private boolean isCompressionNeeded() {
        return (size - realSize) > 3 && realSize < size * loadFactor;
    }

    /** 获取当前负载 -- 主要用于debug */
    public float currentLoad() {
        if (realSize == 0) return 0;
        if (realSize == size) return 1;
        return realSize / (float) size;
    }

    /** 当前是否正在迭代 */
    public boolean isIterating() {
        return recursionDepth > 0;
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
        if (helper != null) {
            helper.collectionIndex(this, e, size);
        }
        elements[size++] = e;
        realSize++;
        return true;
    }

    /** 批量添加元素 */
    public boolean addAll(@Nonnull Collection<? extends E> c) {
        Object[] array = c.toArray();
        if (array.length == 0) {
            return false;
        }
        for (Object e : array) {
            Objects.requireNonNull(e, "collection contains null element");
        }
        ensureCapacity(size + array.length);
        System.arraycopy(array, 0, elements, size, array.length);
        if (helper != null) {
            batchUpdateIndex(elements, size, size + array.length, helper);
        }
        size += array.length;
        realSize += array.length;
        return true;
    }

    /** 插入元素（迭代期间禁止插入，不论index是否特殊） */
    public void insert(int index, E e) {
        Objects.requireNonNull(e);
        ensureNotIterating();
        // 理论上等同于size的时候是安全，但做特殊支持会让api变得不稳定
        if (index == size) {
            add(e);
        } else {
            Objects.checkIndex(index, size);
            if (elements[index] == null) {
                set(index, e);
                return;
            }
            if (size == elements.length) {
                ensureCapacity(size + 1);
            }
            if (helper != null) {
                helper.collectionIndex(this, e, index);
            }
            System.arraycopy(elements, index, elements, index + 1, size - index);
            if (helper != null) {
                batchUpdateIndex(elements, index + 1, size, helper);
            }
            if (firstNullIndex >= index) {
                firstNullIndex++;
            }

            elements[index] = e;
            realSize++;
            size++;
        }
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
     * @param e 如果为null，则表示删除
     * @return 该位置的前一个值
     * @throws NullPointerException 如果e为null
     */
    public E set(int index, E e) {
        Objects.checkIndex(index, size);
        @SuppressWarnings("unchecked") E ele = (E) elements[index];
        if (e == null) {
            if (ele == null) {
                return null;
            }
            // remove
            if (helper != null) {
                helper.collectionIndex(this, ele, INDEX_NOT_FOUND);
            }
            elements[index] = null;
            realSize--;

            // 更新null区间，无甚成本
            if (firstNullIndex > index || firstNullIndex == -1) {
                firstNullIndex = index;
            }
            if (recursionDepth == 0 && isCompressionNeeded()) {
                removeNullElements();
            }
            return ele;
        } else {
            if (ele != null) {
                // replace
                if (helper != null) {
                    helper.collectionIndex(this, ele, INDEX_NOT_FOUND);
                    helper.collectionIndex(this, e, index);
                }
                elements[index] = e;
                return ele;
            }
            // insert
            insertSet(index, e);
            return null;
        }
    }

    private void insertSet(int index, E e) {
        if (helper != null) {
            helper.collectionIndex(this, e, index);
        }
        elements[index] = e;
        realSize++;

        // 更新null区间，有成本
        if (index == firstNullIndex) {
            firstNullIndex = indexNextNullElement(index + 1);
        }
    }

    /**
     * 删除给定位置的元素
     *
     * @return 如果指定位置存在元素，则返回对应的元素，否则返回Null
     */
    public E removeAt(int index) {
        return set(index, null);
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
            set(i, null);
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
            set(i, null);
            return true;
        }
        return false;
    }

    /**
     * 清空List
     *
     * @apiNote 在迭代期间清理元素不会更新size
     */
    public void clear() {
        if (size == 0) {
            return;
        }
        // 立即清理元素，真实计数也更新为0
        if (helper != null) {
            batchUnsetIndex(elements, 0, size, helper);
        } else {
            Arrays.fill(elements, 0, size, null);
        }
        realSize = 0;
        if (recursionDepth == 0) {
            size = 0;
            firstNullIndex = -1;
        } else {
            firstNullIndex = 0;
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
        if (e != null && helper != null) {
            @SuppressWarnings("unchecked") E castE = (E) e;
            return helper.collectionIndex(this, castE);
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
        if (e != null && helper != null) {
            @SuppressWarnings("unchecked") E castE = (E) e;
            return helper.collectionIndex(this, castE);
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
        if (e != null && helper != null) {
            @SuppressWarnings("unchecked") E castE = (E) e;
            return helper.collectionIndex(this, castE);
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
        if (e != null && helper != null) {
            @SuppressWarnings("unchecked") E castE = (E) e;
            return helper.collectionIndex(this, castE);
        }
        return ArrayUtils.lastIndexOfRef(elements, e, 0, size);
    }

    /**
     * 自定义index查询；
     * 1.需要处理null
     * 2.查询过程不可以修改当前List的状态
     *
     * @param predicate 测试条件
     */
    public int indexCustom(Predicate<? super E> predicate) {
        Objects.requireNonNull(predicate);
        @SuppressWarnings("unchecked") E[] elements = (E[]) this.elements;
        return ArrayUtils.indexOfCustom(elements, predicate, 0, size);
    }

    /**
     * 自定义lastIndex查询；
     * 1.需要处理null
     * 2.查询过程不可以修改当前List的状态
     *
     * @param predicate 测试条件
     */
    public int lastIndexCustom(Predicate<? super E> predicate) {
        Objects.requireNonNull(predicate);
        @SuppressWarnings("unchecked") E[] elements = (E[]) this.elements;
        return ArrayUtils.lastIndexOfCustom(elements, predicate, 0, size);
    }

    /**
     * @throws IllegalStateException 如果当前正在迭代
     */
    public void sort(@Nonnull Comparator<? super E> comparator) {
        Objects.requireNonNull(comparator);
        ensureNotIterating();

        // 先压缩空间再排序
        if (realSize < size) {
            removeNullElements();
        }
        @SuppressWarnings("unchecked") E[] elements = (E[]) this.elements;
        Arrays.sort(elements, 0, size, comparator);
        if (helper != null) {
            batchUpdateIndex(elements, 0, size, helper);
        }
    }

    /**
     * 获取list的当前大小
     * 注意：迭代期间删除的元素并不会导致size变小，因此该值是一个不准确的值。
     */
    public int size() {
        return size;
    }

    /**
     * 判断list是否为空
     * 注意：迭代期间删除的元素并不会导致size变小，因此该值是一个不准确的值。
     */
    public boolean isEmpty() {
        return size == 0;
    }

    /**
     * 非空元素数量
     */
    public int elementCount() {
        return realSize;
    }

    /**
     * 空元素数量
     */
    public int nullCount() {
        return size - realSize;
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

    /** 转换为普通的List */
    public List<E> toList() {
        if (realSize == 0) {
            return new ArrayList<>();
        }
        @SuppressWarnings("unchecked") E[] elements = (E[]) this.elements;
        if (realSize == size) {
            return ArrayUtils.toList(elements, 0, size);
        }
        List<E> result = new ArrayList<>(realSize);
        for (int i = 0, end = size; i < end; i++) {
            E e = elements[i];
            if (e != null) {
                result.add(e);
            }
        }
        return result;
    }

    // region internal

    private void ensureNotIterating() {
        if (recursionDepth != 0) {
            throw new IllegalStateException("Invalid between iterating.");
        }
    }

    private void removeNullElements() {
        if (realSize == size) {
            return;
        }
        // 考虑clear
        if (realSize == 0) {
            this.size = 0;
            this.firstNullIndex = -1;
            return;
        }

        Object[] elements = this.elements;
        IndexedElementHelper<? super E> helper = this.helper;
        // 非null元素前移
        int firstNullIndex = this.firstNullIndex;
        int nextIndex = firstNullIndex + 1;
        int nullCount = size - realSize;
        for (int end = size; nextIndex < end && (nextIndex - firstNullIndex) < nullCount; nextIndex++) {
            Object element = elements[nextIndex];
            if (element == null) {
                continue;
            }
            if (helper != null) {
                @SuppressWarnings("unchecked") E castE = (E) element;
                helper.collectionIndex(this, castE, firstNullIndex);
            }
            elements[firstNullIndex++] = element;
        }
        if (nextIndex < size) {
            System.arraycopy(elements, nextIndex, elements, firstNullIndex, size - nextIndex);
        }
        // 清理后部分数据
        Arrays.fill(elements, realSize, size, null);
        this.size = realSize;
        this.firstNullIndex = -1;
    }

    private int indexNextNullElement(int start) {
        if (realSize == size) {
            return -1;
        }
        Object[] elements = this.elements;
        for (int index = start, end = size; index < end; index++) {
            if (elements[index] == null) return index;
        }
        throw new IllegalStateException();
    }

    private void batchUpdateIndex(Object[] elements, int start, int end, IndexedElementHelper<? super E> helper) {
        for (int index = start; index < end; index++) {
            @SuppressWarnings("unchecked") E castE = (E) elements[index];
            if (castE == null) {
                continue;
            }
            helper.collectionIndex(this, castE, index);
        }
    }

    private void batchUnsetIndex(Object[] elements, int start, int end, IndexedElementHelper<? super E> helper) {
        for (int index = start; index < end; index++) {
            @SuppressWarnings("unchecked") E castE = (E) elements[index];
            if (castE == null) {
                continue;
            }
            helper.collectionIndex(this, castE, INDEX_NOT_FOUND);
            elements[index] = null;
        }
    }

    private void ensureCapacity(int minCapacity) {
        int oldCapacity = elements.length;
        if (minCapacity <= oldCapacity) {
            return;
        }
        int grow = oldCapacity >> 1; // 位移运算符优先级较低
        int newCapacity = Math.clamp((long) oldCapacity + grow,
                4, Integer.MAX_VALUE - 8);
        if (newCapacity < minCapacity) {
            newCapacity = minCapacity;
        }
        elements = Arrays.copyOf(elements, newCapacity);
    }
    // endregion

}