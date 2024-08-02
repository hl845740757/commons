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

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.Preconditions;

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import javax.annotation.concurrent.NotThreadSafe;
import java.util.ArrayList;
import java.util.Collection;
import java.util.Comparator;
import java.util.Objects;
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
@NotThreadSafe
public final class DelayedCompressList<E> {

    public static final int INDEX_NOT_FOUND = CollectionUtils.INDEX_NOT_FOUND;

    private final ArrayList<E> children;
    private int recursionDepth;

    /**
     * 记录删除的元素的范围，避免迭代所有
     * 注意：可以根据firstIndex == lastIndex 可以得出只删除了一个元素；但不能根据firstIndex和lastIndex判断出是否调用过clear。
     * ps：如果迭代期间删除元素较少，也可以记录下标
     */
    private transient int firstIndex = INDEX_NOT_FOUND;
    private transient int lastIndex = INDEX_NOT_FOUND;

    public DelayedCompressList() {
        this(4);
    }

    public DelayedCompressList(int initCapacity) {
        children = new ArrayList<>(initCapacity);
    }

    public DelayedCompressList(Collection<? extends E> src) {
        Preconditions.checkNullElements(src);
        children = new ArrayList<>(src);
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
            ArrayList<E> children = this.children;
            int removed = lastIndex - firstIndex + 1;
            if (removed == 1) {
                // 很少在迭代期间删除多个元素，因此我们测试是否删除了单个
                children.remove(firstIndex);
            } else if (children.size() - removed <= 8) {
                // subList与源集合相近，使用subList意义不大
                children.removeIf(Objects::isNull);
            } else {
                children.subList(firstIndex, lastIndex + 1).removeIf(Objects::isNull);
            }
            firstIndex = INDEX_NOT_FOUND;
            lastIndex = INDEX_NOT_FOUND;
        }
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
        children.add(e);
        return true;
    }

    /** 批量添加元素 */
    public boolean addAll(@Nonnull Collection<? extends E> c) {
        Preconditions.checkNullElements(c);
        return children.addAll(c);
    }

    /**
     * 获取指定位置的元素
     *
     * @return 如果指定位置的元素已删除，则返回null
     */
    @Nullable
    public E get(int index) {
        return children.get(index);
    }

    /**
     * 将给定元素赋值到给定位置
     *
     * @return 该位置的前一个值
     * @throws NullPointerException 如果e为null
     */
    public E set(int index, E e) {
        Objects.requireNonNull(e);
        return children.set(index, e);
    }

    /**
     * 删除给定位置的元素
     *
     * @return 如果指定位置存在元素，则返回对应的元素，否则返回Null
     */
    public E removeAt(int index) {
        ArrayList<E> children = this.children;
        if (children.size() == 0) {
            return null;
        }
        if (recursionDepth == 0) {
            return children.remove(index);
        }

        E removed = children.set(index, null);
        if (removed != null) {
            if (firstIndex == INDEX_NOT_FOUND || index < firstIndex) {
                firstIndex = index;
            }
            if (lastIndex == INDEX_NOT_FOUND || index > lastIndex) {
                lastIndex = index;
            }
        }
        return removed;
    }

    /**
     * @apiNote 在迭代期间清理元素不会更新size
     */
    public void clear() {
        ArrayList<E> children = this.children;
        if (children.size() == 0) {
            return;
        }
        if (recursionDepth == 0) {
            children.clear();
            return;
        }

        firstIndex = 0;
        lastIndex = children.size() - 1;
        children.replaceAll(e -> null); // 这个似乎更快
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
        //noinspection SuspiciousMethodCalls
        return children.indexOf(e);
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
        //noinspection SuspiciousMethodCalls
        return children.lastIndexOf(e);
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
        return CollectionUtils.indexOfRef(children, e);
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
        return CollectionUtils.lastIndexOfRef(children, e);
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
        for (int index = 0; index < size; index++) {
            final E e = get(index);
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
        for (int index = size - 1; index >= 0; index--) {
            final E e = get(index);
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
        children.sort(comparator);
    }

    /**
     * 获取list的当前大小
     * 注意：迭代期间删除的元素并不会导致size变化，因此该值是一个不准确的值。
     */
    public int size() {
        return children.size();
    }

    /**
     * 判断list是否为空
     * 注意：迭代期间删除的元素并不会导致size变化，因此该值是一个不准确的值。
     */
    public boolean isEmpty() {
        return children.isEmpty();
    }

    /**
     * 获取list的真实大小
     * 如果当前正在迭代，则可能产生遍历统计的情况，要注意开销问题。
     */
    public int realSize() {
        final ArrayList<E> children = this.children;
        if (recursionDepth == 0 || firstIndex == INDEX_NOT_FOUND) { // 没有删除元素
            return children.size();
        }
        int removed = lastIndex - firstIndex + 1;
        if (removed == 1) { // 删除了一个元素
            return children.size() - 1;
        }
        // 统计区间内非null元素
        for (int index = firstIndex, endIndex = lastIndex; index <= endIndex; index++) {
            if (children.get(index) != null) {
                removed--;
            }
        }
        return children.size() - removed;
    }

    /**
     * 查询List是否真的为空
     * 如果当前正在迭代，则可能产生遍历统计的情况，要注意开销问题。
     */
    public boolean isRealEmpty() {
        final ArrayList<E> children = this.children;
        final int size = children.size();
        if (size == 0) {
            return true;
        }
        for (int index = 0; index < size; index++) {
            if (children.get(index) != null) {
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
        final ArrayList<E> children = this.children;
        final int size = children.size();
        if (size == 0) {
            return;
        }
        beginItr();
        try {
            for (int index = 0; index < size; index++) {
                final E e = children.get(index);
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
        final ArrayList<E> children = this.children;
        final int size = children.size();
        if (size == 0) {
            return;
        }
        beginItr();
        try {
            for (int index = 0; index < size; index++) {
                final E e = children.get(index);
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