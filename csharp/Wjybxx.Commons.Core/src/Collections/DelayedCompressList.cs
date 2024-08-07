#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 延迟压缩空间的List（Array）
/// 1.该List主要用于事件监听器列表和对象列表等场景。
/// 2.使用<see cref="ForEach(System.Action{E})"/>可能有更好的迭代速度。
/// </summary>
[NotThreadSafe]
public sealed class DelayedCompressList<E> where E : class
{
    private const int INDEX_NOT_FOUND = -1;

    private E?[] elements;
    /** 用于管理元素的下标 */
    private readonly IIndexedElementHelper<E>? helper;
    /** 负载因子，当负载低于该值时才压缩空间 */
    private readonly float loadFactor;

    private int size;
    private int realSize;
    private int firstNullIndex = -1;
    private int recursionDepth;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="initCapacity">初始空间大小</param>
    /// <param name="loadFactor">负载因子</param>
    /// <param name="helper">管理元素索引的工具类</param>
    public DelayedCompressList(int initCapacity, float loadFactor = 0.75f, IIndexedElementHelper<E>? helper = null) {
        this.elements = new E[initCapacity];
        this.loadFactor = loadFactor;
        this.helper = helper;
    }

    /** 开始迭代 */
    public void BeginItr() {
        recursionDepth++;
    }

    /** 迭代结束 -- 必须在finally块中调用，否则可能使List处于无效状态；特殊情况下可以反复调用该接口修复状态。 */
    public void EndItr() {
        if (recursionDepth == 0) {
            throw new IllegalStateException("begin must be called before end.");
        }
        recursionDepth--;
        if (recursionDepth == 0 && IsCompressionNeeded()) {
            RemoveNullElements();
        }
    }

    /** 主动压缩空间 */
    public void Compress(bool force) {
        EnsureNotIterating();
        if (force || IsCompressionNeeded()) {
            RemoveNullElements();
        }
    }

    /** 是否需要压缩空间 */
    private bool IsCompressionNeeded() {
        return (size - realSize) > 3 && realSize < size * loadFactor;
    }

    /** 获取当前负载 -- 主要用于debug */
    public float CurrentLoad() {
        if (realSize == 0) return 0;
        if (realSize == size) return 1;
        return realSize / (float)size;
    }

    /** 当前是否正在迭代 */
    public bool IsIterating() {
        return recursionDepth > 0;
    }

    /// <summary>
    /// 添加一个元素到末尾
    /// </summary>
    /// <param name="e">要添加的元素，不可为null</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public bool Add(E e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        if (size == elements.Length) {
            EnsureCapacity(size + 1);
        }
        if (helper != null) {
            helper.CollectionIndex(this, e, size);
        }
        elements[size++] = e;
        realSize++;
        return true;
    }

    /** 批量添加元素 */
    public bool AddAll(IEnumerable<E> c) {
        E[] array = c.ToArray();
        if (array.Length == 0) {
            return false;
        }
        foreach (E e in array) {
            if (e == null) {
                throw new ArgumentException("collection contains null element");
            }
        }
        EnsureCapacity(size + array.Length);
        Array.Copy(array, 0, elements, size, array.Length);
        if (helper != null) {
            BatchUpdateIndex(elements, size, size + array.Length, helper);
        }
        size += array.Length;
        realSize += array.Length;
        return true;
    }

    public E? this[int index] {
        get {
            ArrayUtil.CheckIndex(index, size);
            return elements[index];
        }
        set => Set(index, value);
    }

    /// <summary>
    /// 获取指定索引位置的值
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public E? Get(int index) {
        ArrayUtil.CheckIndex(index, size);
        return elements[index];
    }

    /// <summary>
    /// 设置指定索引位置的值
    /// </summary>
    /// <param name="index"></param>
    /// <param name="e">如果赋值为null，等同于删除</param>
    /// <returns>index位置的旧值</returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public E? Set(int index, E? e) {
        ArrayUtil.CheckIndex(index, size);
        E ele = elements[index];
        if (e == null) {
            if (ele == null) {
                return null;
            }
            // remove
            if (helper != null) {
                helper.CollectionIndex(this, ele, INDEX_NOT_FOUND);
            }
            elements[index] = default;
            realSize--;

            // 更新null区间，无甚成本
            if (firstNullIndex > index || firstNullIndex == -1) {
                firstNullIndex = index;
            }
            if (recursionDepth == 0 && IsCompressionNeeded()) {
                RemoveNullElements();
            }
            return ele;
        } else {
            if (ele != null) {
                // replace
                if (helper != null) {
                    helper.CollectionIndex(this, ele, INDEX_NOT_FOUND);
                    helper.CollectionIndex(this, e, index);
                }
                elements[index] = e;
                return ele;
            }
            // insert
            InsertSet(index, e);
            return null;
        }
    }

    private void InsertSet(int index, E e) {
        if (helper != null) {
            helper.CollectionIndex(this, e, index);
        }
        elements[index] = e;
        realSize++;

        // 更新null区间，有成本
        if (index == firstNullIndex) {
            firstNullIndex = IndexNextNullElement(index + 1);
        }
    }

    /// <summary>
    /// 插入元素（迭代期间禁止插入，不论index是否特殊）
    /// </summary>
    /// <param name="index"></param>
    /// <param name="e"></param>
    public void Insert(int index, E e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        EnsureNotIterating();
        EnsureCapacity(size + 1);
        // 理论上等同于size的时候是安全，但做特殊支持会让api变得不稳定
        if (index == size) {
            Add(e);
        } else {
            ArrayUtil.CheckIndex(index, size);
            if (elements[index] == null) {
                InsertSet(index, e);
                return;
            }
            if (size == elements.Length) {
                EnsureCapacity(size + 1);
            }
            if (helper != null) {
                helper.CollectionIndex(this, e, index);
            }
            Array.Copy(elements, index, elements, index + 1, size - index);
            if (helper != null) {
                BatchUpdateIndex(elements, index + 1, size, helper);
            }
            if (firstNullIndex >= index) {
                firstNullIndex++;
            }

            elements[index] = e;
            size++;
            realSize++;
        }
    }

    /// <summary>
    /// 删除给定位置的元素
    /// </summary>
    /// <param name="index"></param>
    /// <returns>如果指定位置存在元素，则返回对应的元素，否则返回Null</returns>
    public E? RemoveAt(int index) {
        return Set(index, null);
    }

    /// <summary>
    /// 根据equals相等删除元素
    /// </summary>
    /// <param name="e"></param>
    /// <returns>如果元素在集合中则删除并返回true</returns>
    public bool Remove(E? e) {
        if (e == null) return false;
        int i = Index(e);
        if (i >= 0) {
            Set(i, null);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 根据引用相等删除元素
    /// </summary>
    /// <param name="e">如果元素在集合中则删除并返回true</param>
    /// <returns></returns>
    public bool RemoveRef(E? e) {
        if (e == null) return false;
        int i = IndexOfRef(e);
        if (i >= 0) {
            Set(i, null);
            return true;
        }
        return false;
    }

    /// <summary>
    /// 清空List。
    /// 注意：在迭代期间清理元素不会更新size
    /// </summary>
    public void Clear() {
        if (size == 0) {
            return;
        }
        // 立即清理元素，真实计数也更新为0
        if (helper != null) {
            BatchUnsetIndex(elements, 0, size, helper);
        } else {
            ArrayUtil.Fill2(elements, 0, size, null);
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
    public bool Contains(E? e) {
        return Index(e) >= 0;
    }

    /** 基于引用相等查询一个元素是否在List中 */
    public bool ContainsRef(E? e) {
        return IndexOfRef(e) >= 0;
    }

    /// <summary>
    /// 基于equals查找元素在List中的位置
    /// </summary>
    /// <param name="e">如果null，表示查询第一个删除的的元素位置</param>
    /// <returns>如果元素不在集合中，则返回-1</returns>
    public int Index(E? e) {
        if (e != null && helper != null) {
            return helper.CollectionIndex(this, e);
        }
        return ArrayUtil.IndexOf(elements, e, 0, size);
    }

    /// <summary>
    /// 基于equals逆向查找元素在List中的位置
    /// </summary>
    /// <param name="e">如果null，表示查询第一个删除的的元素位置</param>
    /// <returns>如果元素不在集合中，则返回-1</returns>
    public int LastIndex(E? e) {
        if (e != null && helper != null) {
            return helper.CollectionIndex(this, e);
        }
        return ArrayUtil.LastIndexOf(elements, e, 0, size);
    }

    /// <summary>
    /// 基于引用相等查找元素在List中的位置
    /// </summary>
    /// <param name="e">如果null，表示查询第一个删除的的元素位置</param>
    /// <returns>如果元素不在集合中，则返回-1</returns>
    public int IndexOfRef(E? e) {
        if (e != null && helper != null) {
            return helper.CollectionIndex(this, e);
        }
        return ArrayUtil.IndexOfRef(elements, e, 0, size);
    }

    /// <summary>
    /// 基于引用相等逆向查找元素在List中的位置
    /// </summary>
    /// <param name="e">如果null，表示查询第一个删除的的元素位置</param>
    /// <returns>如果元素不在集合中，则返回-1</returns>
    public int LastIndexOfRef(E? e) {
        if (e != null && helper != null) {
            return helper.CollectionIndex(this, e);
        }
        return ArrayUtil.LastIndexOfRef(elements, e, 0, size);
    }

    /// <summary>
    /// 自定义index查询
    /// 1.需要处理null
    /// 2.查询过程不可以修改当前List的状态
    /// </summary>
    /// <param name="predicate">测试条件</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public int IndexCustom(Predicate<E> predicate) {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return ArrayUtil.IndexOfCustom(elements, predicate, 0, size);
    }

    /// <summary>
    /// 自定义lastIndex查询
    /// 1.需要处理null
    /// 2.查询过程不可以修改当前List的状态
    /// </summary>
    /// <param name="predicate">测试条件</param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public int LastIndexCustom(Predicate<E> predicate) {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return ArrayUtil.LastIndexOfCustom(elements, predicate, 0, size);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="comparator"></param>
    /// <exception cref="ArgumentNullException">参数为null</exception>
    /// <exception cref="IllegalStateException">如果当前正在迭代</exception>
    public void Sort(IComparer<E> comparator) {
        if (comparator == null) throw new ArgumentNullException(nameof(comparator));
        EnsureNotIterating();

        // 先压缩空间再排序
        if (realSize < size) {
            RemoveNullElements();
        }
        Array.Sort(elements, 0, size, comparator);
        if (helper != null) {
            BatchUpdateIndex(elements, 0, size, helper);
        }
    }

    /// <summary>
    /// 获取list的当前大小
    /// 注意：迭代期间删除的元素并不会导致size变小，因此该值是一个不准确的值。
    /// </summary>
    public int Count => size;

    /// <summary>
    /// 判断list是否为空
    /// 注意：迭代期间删除的元素并不会导致size变小，因此该值是一个不准确的值。
    /// </summary>
    public bool IsEmpty => size == 0;

    /// <summary>
    /// 非空元素数量
    /// </summary>
    public int ElementCount => realSize;

    /// <summary>
    /// 空元素数量
    /// </summary>
    public int NullCount => (size - realSize);

    /// <summary>
    /// 迭代List内的元素，该快捷方式不会迭代迭代期间新增的元素
    /// 如果需要元素的下标，请使用<see cref="ForEach(System.Action{E, int})"/>
    /// </summary>
    /// <param name="action"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void ForEach(Action<E> action) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        int size = this.size;
        if (size == 0) {
            return;
        }

        E?[] elements = this.elements;
        BeginItr();
        try {
            for (int index = 0; index < size; index++) {
                E? e = elements[index];
                if (e != null) {
                    action(e);
                }
            }
        }
        finally {
            EndItr();
        }
    }

    /// <summary>
    /// 迭代List内的元素，该快捷方式不会迭代迭代期间新增的元素
    /// </summary>
    /// <param name="action">参数1为对应元素，参数2为下标 -- 返回index以方便快速删除</param>
    /// <exception cref="ArgumentNullException"></exception>
    public void ForEach(Action<E, int> action) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        int size = this.size;
        if (size == 0) {
            return;
        }

        E?[] elements = this.elements;
        BeginItr();
        try {
            for (int index = 0; index < size; index++) {
                E? e = elements[index];
                if (e != null) {
                    action(e, index);
                }
            }
        }
        finally {
            EndItr();
        }
    }

    /** 转换为普通的List */
    public List<E> ToList() {
        if (realSize == 0) {
            return new List<E>();
        }
        List<E> result = new List<E>(realSize);
        if (realSize == size) {
            result.AddRange(elements);
        } else {
            for (int i = 0, end = size; i < end; i++) {
                E e = elements[i];
                if (e != null) {
                    result.Add(e);
                }
            }
        }
        return result;
    }

    #region internal

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureNotIterating() {
        if (recursionDepth != 0) {
            throw new IllegalStateException("Invalid between iterating.");
        }
    }

    private void RemoveNullElements() {
        if (realSize == size) {
            return;
        }
        // 考虑clear
        if (realSize == 0) {
            this.size = 0;
            this.firstNullIndex = -1;
            return;
        }

        E?[] elements = this.elements;
        IIndexedElementHelper<E>? helper = this.helper;
        // 非null元素前移
        int firstNullIndex = this.firstNullIndex;
        int nextIndex = firstNullIndex + 1;
        int nullCount = size - realSize;
        for (int end = size; nextIndex < end && (nextIndex - firstNullIndex) < nullCount; nextIndex++) {
            E? element = elements[nextIndex];
            if (element == null) {
                continue;
            }
            if (helper != null) {
                helper.CollectionIndex(this, element, firstNullIndex);
            }
            elements[firstNullIndex++] = element;
        }
        if (nextIndex < size) {
            Array.Copy(elements, nextIndex, elements, firstNullIndex, size - nextIndex);
        }
        // 清理后部分数据
        ArrayUtil.Fill2(elements, realSize, size, null);
        this.size = realSize;
        this.firstNullIndex = -1;
    }

    private int IndexNextNullElement(int start) {
        if (realSize == size) {
            return -1;
        }
        E?[] elements = this.elements;
        for (int index = start, end = size; index < end; index++) {
            if (elements[index] == null) return index;
        }
        throw new IllegalStateException();
    }

    private void BatchUpdateIndex(E?[] elements, int start, int end, IIndexedElementHelper<E> helper) {
        for (int index = start; index < end; index++) {
            E? e = elements[index];
            if (e == null) {
                continue;
            }
            helper.CollectionIndex(this, e, index);
        }
    }

    private void BatchUnsetIndex(E?[] elements, int start, int end, IIndexedElementHelper<E> helper) {
        for (int index = start; index < end; index++) {
            E? e = elements[index];
            if (e == null) {
                continue;
            }
            helper.CollectionIndex(this, e, INDEX_NOT_FOUND);
            elements[index] = null;
        }
    }

    private void EnsureCapacity(int minCapacity) {
        int oldCapacity = elements.Length;
        if (minCapacity <= oldCapacity) {
            return;
        }
        int grow = oldCapacity >> 1; // 位移运算符优先级较低
        int newCapacity = MathCommon.Clamp((long)oldCapacity + grow,
            4, int.MaxValue - 8);
        if (newCapacity < minCapacity) {
            newCapacity = minCapacity;
        }
        elements = ArrayUtil.CopyOf(elements, 0, newCapacity);
    }

    #endregion
}
}