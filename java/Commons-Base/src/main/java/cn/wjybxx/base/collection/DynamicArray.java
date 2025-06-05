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

import javax.annotation.Nonnull;
import javax.annotation.Nullable;
import java.util.Comparator;
import java.util.List;
import java.util.function.ObjIntConsumer;

/**
 * 动态数组
 * (支持迭代期间删除元素和扩容)
 *
 * <h3>约定</h3>
 * 0.Set(index, null) 即表示删除元素
 * 1.Add和Insert一定会增加length；但Remove和Set方法不一定会减少length。
 * 2.在迭代期间删除元素，一定不会减少length；在迭代结束后，可能会压缩空间减少length -- 允许一定比例的null是该List的核心；
 * 3.在迭代期间添加元素，元素会被添加到List末尾并增加length；在迭代结束后，可能会压缩空间减少length。
 * 4.在非迭代期间，删除元素可能立即触发空间压缩。
 * 5.需要使用传统数组方式进行迭代，因此未实现{@link Iterable}接口。
 *
 * <h3>使用方式</h3>
 * <pre><code>
 *     list.beginItr();
 *     try {
 *         for(int i = 0, len = list.length();i < len; i++){
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
 *
 * @author wjybxx
 * date - 2025/4/11
 */
public interface DynamicArray<E> {

    /** 当前是否正在迭代 */
    boolean isIterating();

    /** 开始迭代 */
    void beginItr();

    /** 迭代结束 -- 特殊情况下可以反复调用该接口修复状态。 */
    void endItr();

    /**
     * 获取指定位置的元素
     *
     * @return 如果指定位置的元素已删除，则返回null
     */
    @Nullable
    E get(int index);

    /**
     * 将给定元素赋值到给定位置
     *
     * @param index 元素下标
     * @param e     如果为null表示删除
     * @return 该位置的前一个值
     */
    E set(int index, @Nullable E e);

    /**
     * 添加元素
     * 不论是否正在迭代，length一定会增加。
     *
     * @throws NullPointerException 如果e为null
     */
    void add(E e);

    /**
     * 插入元素
     *
     * @param index 要插入的位置，小于等于length
     * @param e     要插入的元素
     * @throws NullPointerException  如果e为null
     * @throws IllegalStateException 如果当前正在迭代
     */
    void insert(int index, E e);

    /**
     * 根据equals相等删除元素
     * 注意：不论是否正在迭代，length都可能不会变化。
     *
     * @param e null固定返回false
     * @return 如果元素在集合中则删除并返回true
     */
    boolean remove(E e);

    /**
     * 根据引用相等删除元素
     * 注意：不论是否正在迭代，length都可能不会变化。
     *
     * @param e null固定返回false
     * @return 如果元素在集合中则删除并返回true
     */
    boolean removeRef(E e);

    /**
     * 清空List
     * 注意：
     * 1.在迭代期间调用Clear是高风险行为，会清理自身迭代范围外的数据，可能影响其它迭代逻辑。
     * 2.在迭代期间清理元素不会更新length
     */
    void clear();

    /**
     * 基于equals查询一个元素是否在List中
     *
     * @param e 要查询的元素
     * @return 如果存在则返回true
     */
    boolean contains(E e);

    /**
     * 基于引用相等查询一个元素是否在List中
     *
     * @param e 要查询的元素
     * @return 如果存在则返回true
     */
    boolean containsRef(E e);

    /**
     * 根据equals查询下标
     *
     * @param e 要查询的元素
     * @return 如果存在则返回对应的下标，否则返回-1
     */
    int indexOf(E e);

    /**
     * 根据equals查询下标
     *
     * @param e 要查询的元素
     * @return 如果存在则返回对应的下标，否则返回-1
     */
    int lastIndexOf(E e);

    /**
     * 根据引用相等查询下标
     *
     * @param e 要查询的元素
     * @return 如果存在则返回对应的下标，否则返回-1
     */
    int indexOfRef(E e);

    /**
     * 根据引用相等查询下标
     *
     * @param e 要查询的元素
     * @return 如果存在则返回对应的下标，否则返回-1
     */
    int lastIndexOfRef(E e);

    /**
     * 数组的当前长度
     */
    int length();

    /**
     * 非空元素数量，实时值。
     * 注意：该方法可能有额外的开销
     */
    int elementCount();

    /**
     * 空元素数量，实时值。
     * 注意：该方法可能有额外的开销
     */
    int nullCount();

    /**
     * 判断数组是否包含Null元素，用于更快的判断是否为空。
     * 注意：该方法可能有额外的开销。
     */
    boolean containsNull();

    /**
     * 对数组元素进行排序
     * (该接口会强制压缩空间，再进行排序)
     *
     * @throws IllegalStateException 如果当前正在迭代
     */
    void sort(@Nonnull Comparator<? super E> comparator);

    /**
     * 确保空间足够（减少不必要的扩容）
     *
     * @param minCapacity 期望的最小空间
     */
    void ensureCapacity(int minCapacity);

    /**
     * 压缩数组空间
     *
     * @param ignoreFactor 是否忽略null比重
     * @throws IllegalStateException 如果当前正在迭代
     */
    void compress(boolean ignoreFactor);

    /**
     * 迭代数组内的元素，该快捷方式不会迭代迭代期间新增的元素
     *
     * @param action action接收元素和对应的下标
     */
    void forEach(ObjIntConsumer<? super E> action);

    /**
     * 将所有的非null元素转存到List
     */
    List<E> toList();
}