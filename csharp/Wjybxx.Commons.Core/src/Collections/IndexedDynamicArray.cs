#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using static Wjybxx.Commons.Collections.IIndexedElement;
using static Wjybxx.Commons.Collections.DynamicArrayHelper;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 元素被索引的动态数组
/// 注意：会在元素上缓存在数组中的下标，因此{@code index}{@code remove}系列方法总是使用引用相等查询。
/// </summary>
/// <typeparam name="E"></typeparam>
public class IndexedDynamicArray<E> : IDynamicArray<E> where E : class
{
    private readonly IIndexedElementHelper<E> helper;
    private E?[] elements;
    private long[] elementsMask;
    private readonly float nullFactor;

    private int len;
    private int elementCount;
    private int recursionDepth;

    public IndexedDynamicArray(IIndexedElementHelper<E> helper, int initCapacity)
        : this(helper, initCapacity, 0.125f) { // 避免迭代时大量的null
    }

    public IndexedDynamicArray(IIndexedElementHelper<E> helper, int initCapacity, float nullFactor) {
        this.helper = helper ?? throw new ArgumentNullException(nameof(helper));
        this.elements = new E[initCapacity];
        this.elementsMask = new long[WordCount(initCapacity)];
        this.nullFactor = Math.Max(0, nullFactor);
    }

    #region itr

    public bool IsIterating => recursionDepth > 0;

    public void BeginItr() {
        recursionDepth++;
    }

    public void EndItr() {
        if (recursionDepth == 0) {
            throw new IllegalStateException("begin must be called before end.");
        }
        recursionDepth--;
        if (recursionDepth == 0 && IsCompressionNeeded()) {
            RemoveNullElements();
        }
    }

    #endregion

    #region update

    public E? this[int index] {
        get {
            ArrayUtil.CheckIndex(index, len);
            return elements[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => Set(index, value);
    }

    public E? Set(int index, E? e) {
        ArrayUtil.CheckIndex(index, len);
        E prev = elements[index];
        if (prev != null) {
            helper.CollectionIndex(this, prev, IndexNotFound);
            elementCount--;
        }
        if (e != null) {
            helper.CollectionIndex(this, e, index);
            elementCount++;
        }
        SetBit(index, e != null);
        elements[index] = e;
        if (e == null && recursionDepth == 0 && IsCompressionNeeded()) {
            RemoveNullElements();
        }
        return prev;
    }

    public void Add(E e) {
        RequireNonNullAndNotContains(e);
        if (len == elements.Length) {
            EnsureCapacity(len + 1);
        }
        helper.CollectionIndex(this, e, len);
        elementCount++;
        SetBit(len, true);
        elements[len++] = e;
    }

    public void Insert(int index, E e) {
        RequireNonNullAndNotContains(e);
        ArrayUtil.CheckIndex(index, len + 1);
        EnsureNotIterating();
        if (len == elements.Length) {
            EnsureCapacity(len + 1);
        }
        if (index < len) {
            Array.Copy(elements, index, elements, index + 1, len - index);
            InsertBit(index);
        }
        helper.CollectionIndex(this, e, index);
        elementCount++;
        SetBit(index, true);
        elements[index] = e;
        len++;
    }

    /// <summary>
    /// 将元素移动到指定下标，可避免产生remove事件
    /// </summary>
    public void MoveTo(E e, int newIndex) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        ArrayUtil.CheckIndex(newIndex, len);
        EnsureNotIterating();

        int preIndex = helper.CollectionIndex(this, e);
        if (preIndex == IndexNotFound) throw new ArgumentException();
        if (preIndex == newIndex) {
            return;
        }
        // 先将元素挪动到数组尾部，避免外部感知为删除事件，也避免移动期间index重复
        E[] elements = this.elements;
        helper.CollectionIndex(this, e, len); // 假移动，以避免扩容

        if (preIndex < newIndex) {
            // preIndex后面的元素前移
            for (int idx = preIndex; idx < newIndex; idx++) {
                E next = elements[idx + 1];
                elements[idx] = next;
                if (next != null) {
                    helper.CollectionIndex(this, next, idx);
                    SetBit(idx, true);
                } else {
                    SetBit(idx, false);
                }
            }
        } else {
            // preIndex前面的元素后移
            for (int idx = preIndex; idx > newIndex; idx--) {
                E prev = elements[idx - 1];
                elements[idx] = prev;
                if (prev != null) {
                    helper.CollectionIndex(this, prev, idx);
                    SetBit(idx, true);
                } else {
                    SetBit(idx, false);
                }
            }
        }
        elements[newIndex] = e;
        helper.CollectionIndex(this, e, newIndex);
        SetBit(newIndex, true);
    }

    public bool Remove(E? e) {
        if (e == null) return false;
        int i = helper.CollectionIndex(this, e);
        if (i >= 0) {
            Set(i, null);
            return true;
        }
        return false;
    }

    public bool RemoveRef(E? e) {
        if (e == null) return false;
        int i = helper.CollectionIndex(this, e);
        if (i >= 0) {
            Set(i, null);
            return true;
        }
        return false;
    }

    public void Clear() {
        if (elementCount == 0) {
            return;
        }
        for (int idx = 0, len = this.len; idx < len; idx++) {
            E e = elements[idx];
            if (e == null) {
                continue;
            }
            helper.CollectionIndex(this, e, IndexNotFound);
            elements[idx] = null;
        }
        for (int idx = 0, wordCount = WordCount(len); idx < wordCount; idx++) {
            elementsMask[idx] = 0;
        }
        elementCount = 0;
        if (recursionDepth == 0) {
            len = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RequireNonNullAndNotContains(E e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        if (helper.CollectionIndex(this, e) != IndexNotFound) {
            throw new ArgumentException($"e.queueIndex(): {helper.CollectionIndex(this, e)} " +
                                        $"(expected: {IndexNotFound}) + e: %s");
        }
    }

    #endregion

    #region indexOf

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(E e) {
        return IndexOf(e) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsRef(E e) {
        return IndexOfRef(e) >= 0;
    }

    public int IndexOf(E? e) {
        if (e == null) {
            return FirstNullIndex();
        }
        return helper.CollectionIndex(this, e);
    }

    public int LastIndexOf(E? e) {
        if (e == null) {
            return LastNullIndex();
        }
        return helper.CollectionIndex(this, e);
    }

    public int IndexOfRef(E? e) {
        if (e == null) {
            return FirstNullIndex();
        }
        return helper.CollectionIndex(this, e);
    }

    public int LastIndexOfRef(E? e) {
        if (e == null) {
            return LastNullIndex();
        }
        return helper.CollectionIndex(this, e);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int FirstNullIndex() {
        return DynamicArrayHelper.FirstNullIndex(elementsMask, len, elementCount);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int LastNullIndex() {
        return DynamicArrayHelper.LastNullIndex(elementsMask, len, elementCount);
    }

    #endregion

    #region Len

    public int Length => len;

    public int ElementCount => elementCount;

    public int NullCount => len - elementCount;

    public bool ContainsNull => elementCount < len;

    #endregion

    #region other

    public void Sort(IComparer<E> comparator) {
        if (comparator == null) throw new ArgumentNullException(nameof(comparator));
        EnsureNotIterating();
        // 先压缩空间再排序
        if (ContainsNull) {
            RemoveNullElements();
        }
        Array.Sort(elements, 0, len, comparator);
        BatchUpdateIndex(elements, 0, len);
    }

    public void EnsureCapacity(int minCapacity) {
        int oldCapacity = elements.Length;
        if (minCapacity <= oldCapacity) {
            return;
        }
        if (minCapacity > MAX_CAPACITY) {
            throw new OutOfMemoryException("Required array length " + minCapacity + " is too large");
        }

        // 较快的成长速度
        int grow = Math.Max(16, oldCapacity >> 1);
        int newCapacity = MathCommon.Clamp((long)oldCapacity + grow, minCapacity, MAX_CAPACITY);
        elements = ArrayUtil.CopyOf(elements, 0, newCapacity);
        if (WordCount(oldCapacity) < WordCount(newCapacity)) {
            elementsMask = ArrayUtil.CopyOf(elementsMask, 0, WordCount(newCapacity));
        }
    }

    public void Compress(bool force) {
        EnsureNotIterating();
        if (force || IsCompressionNeeded()) {
            RemoveNullElements();
        }
    }

    public void ForEach(Action<E, int> action) {
        if (action == null) throw new ArgumentNullException(nameof(action));
        int len = this.len;
        if (len == 0) {
            return;
        }
        BeginItr();
        try {
            E[] elements = this.elements;
            for (int index = 0; index < len; index++) {
                E e = elements[index];
                if (e != null) {
                    action(e, index);
                }
            }
        }
        finally {
            EndItr();
        }
    }

    public List<E> ToList() {
        E[] elements = this.elements;
        List<E> result = new List<E>(ElementCount);
        for (int i = 0, end = len; i < end; i++) {
            E e = elements[i];
            if (e != null) {
                result.Add(e);
            }
        }
        return result;
    }

    #endregion

    #region internal

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBit(int index, bool val) {
        // 左移和右移运算符会自动取余
        if (val) {
            elementsMask[WordIndex(index)] |= (1L << index);
        } else {
            elementsMask[WordIndex(index)] &= ~(1L << index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool GetBit(int index) {
        return (elementsMask[WordIndex(index)] & 1L << index) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InsertBit(int bitIndex) {
        DynamicArrayHelper.InsertBit(elementsMask, len, bitIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureNotIterating() {
        if (recursionDepth != 0) {
            throw new IllegalStateException("Invalid between iterating.");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsCompressionNeeded() {
        return elementCount < len && DynamicArrayHelper.IsCompressionNeeded(nullFactor, len, elementCount);
    }

    private void RemoveNullElements() {
        Debug.Assert(recursionDepth == 0);
        int elementCount = this.elementCount;
        if (elementCount == len) {
            return;
        }
        if (elementCount == 0) {
            this.len = 0;
            return;
        }
        // 零散前移
        int firstNullIndex = FirstNullIndex();
        int lastNullIndex = LastNullIndex();
        E[] elements = this.elements;
        IIndexedElementHelper<E> helper = this.helper;
        for (int index = firstNullIndex + 1; index < lastNullIndex; index++) {
            E element = elements[index];
            if (element == null) {
                continue;
            }
            helper.CollectionIndex(this, element, firstNullIndex);

            // SetBit(index, false);
            // elements[index] = null; // help debug

            SetBit(firstNullIndex, true);
            elements[firstNullIndex++] = element;
        }
        // 批量前移
        int copyStart = lastNullIndex + 1;
        if (copyStart < len) {
            Array.Copy(elements, copyStart, elements, firstNullIndex, (len - copyStart));
        }
        BatchUpdateIndex(elements, firstNullIndex, elementCount);
        DynamicArrayHelper.SetBit(elementsMask, firstNullIndex, elementCount);
        DynamicArrayHelper.ClearBit(elementsMask, elementCount, len);
        ArrayUtil.Fill2(elements, elementCount, len, null);
        this.len = elementCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void BatchUpdateIndex(E[] elements, int start, int end) {
        IIndexedElementHelper<E> helper = this.helper;
        for (int index = start; index < end; index++) {
            E castE = elements[index];
            helper.CollectionIndex(this, castE, index);
        }
    }

    #endregion
}
}