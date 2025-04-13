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
using static Wjybxx.Commons.Collections.DynamicArrayHelper;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 默认的动态数组
/// </summary>
/// <typeparam name="E"></typeparam>
public class DynamicArray<E> : IDynamicArray<E> where E : class
{
    private const long WORD_MASK = -1;
    private const int ADDRESS_BITS_PER_WORD = 6;

    private E?[] elements;
    private long[] elementsMask;
    private readonly float nullFactor;

    private int len;
    private int elementCount;
    private int recursionDepth;

    public DynamicArray(int initCapacity)
        : this(initCapacity, 0.125f) { // 避免迭代时大量的null
    }

    public DynamicArray(int initCapacity, float nullFactor) {
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
            elementCount--;
        }
        if (e != null) {
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
        if (e == null) throw new ArgumentNullException(nameof(e));
        if (len == elements.Length) {
            EnsureCapacity(len + 1);
        }
        elementCount++;
        SetBit(len, true);
        elements[len++] = e;
    }

    public void Insert(int index, E e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        ArrayUtil.CheckIndex(index, len); // 还是要求index已存在更好
        EnsureNotIterating();
        if (len == elements.Length) {
            EnsureCapacity(len + 1);
        }
        if (index < len) {
            Array.Copy(elements, index, elements, index + 1, len - index);
            InsertBit(index);
        }
        elementCount++;
        SetBit(index, true);
        elements[index] = e;
        len++;
    }

    public bool Remove(E? e) {
        if (e == null) return false;
        int i = IndexOf(e);
        if (i >= 0) {
            Set(i, null);
            return true;
        }
        return false;
    }

    public bool RemoveRef(E? e) {
        if (e == null) return false;
        int i = IndexOfRef(e);
        if (i >= 0) {
            Set(i, null);
            return true;
        }
        return false;
    }

    public void Clear() {
        if (len == 0) {
            return;
        }
        for (int idx = 0, len = this.len; idx < len; idx++) {
            E e = elements[idx];
            if (e == null) {
                continue;
            }
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
        return Array.IndexOf(elements, e, 0, len);
    }

    public int LastIndexOf(E? e) {
        if (e == null) {
            return LastNullIndex();
        }
        return Array.LastIndexOf(elements, e, 0, len);
    }

    public int IndexOfRef(E? e) {
        if (e == null) {
            return FirstNullIndex();
        }
        return ArrayUtil.IndexOfRef(elements, e, 0, len);
    }

    public int LastIndexOfRef(E? e) {
        if (e == null) {
            return LastNullIndex();
        }
        return ArrayUtil.LastIndexOfRef(elements, e, 0, len);
    }

    private int FirstNullIndex() {
        if (elementCount == len) return -1;
        for (int idx = 0, wordCount = WordCount(len); idx < wordCount; idx++) {
            long word = elementsMask[idx];
            if (word == -1) continue;
            // 将末尾的1转为0，这样低位的第一个1就是第一个null元素位置
            return (idx * 64) + MathCommon.NumberOfTrailingZeros(~word);
        }
        throw new AssertionError();
    }

    private int LastNullIndex() {
        if (elementCount == len) return -1;
        int wordCount = WordCount(len);
        for (int idx = wordCount - 1; idx >= 0; idx--) {
            long word = elementsMask[idx];
            // 先将超出len这部分也转为1，再整体取反转0，这样高位的第一个1就是第一个null元素位置 -- -1左移64位居然还是-1，我还以为是0
            if (idx == wordCount - 1 && (len & 63) != 0) {
                word |= -1L << len;
            }
            if (word == -1) continue;
            return (idx * 64) + (63 - MathCommon.NumberOfLeadingZeros(~word));
        }
        throw new AssertionError();
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
    }

    public void EnsureCapacity(int minCapacity) {
        int oldCapacity = elements.Length;
        if (minCapacity <= oldCapacity) {
            return;
        }
        // 我们需要较快的成长速度
        int grow = oldCapacity >> 1;
        int newCapacity = MathCommon.Clamp((long)oldCapacity + grow, 16, int.MaxValue - 8);
        if (newCapacity < minCapacity) {
            newCapacity = minCapacity;
        }
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
    private void InsertBit(int bitIndex) {
        DynamicArrayHelper.InsertBit(elementsMask, len, bitIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureNotIterating() {
        if (recursionDepth != 0) {
            throw new IllegalStateException("Invalid between iterating.");
        }
    }

    private bool IsCompressionNeeded() {
        float nullFactor = this.nullFactor;
        if (nullFactor == 0) return true;
        if (nullFactor > 1) return false;

        int nullCount = len - ElementCount;
        if (nullFactor == 1) {
            return nullCount == len;
        }
        return nullCount >= 4 && nullCount >= len * nullFactor;
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
        for (int index = firstNullIndex + 1; index < lastNullIndex; index++) {
            E element = elements[index];
            if (element == null) {
                continue;
            }
            SetBit(index, false);
            SetBit(firstNullIndex, true);

            elements[index] = null; // help debug
            elements[firstNullIndex++] = element;
        }
        // 批量前移
        int copyStart = lastNullIndex + 1;
        if (copyStart < len) {
            Array.Copy(elements, copyStart, elements, firstNullIndex, (len - copyStart));
        }
        DynamicArrayHelper.SetBit(elementsMask, firstNullIndex, elementCount);
        DynamicArrayHelper.ClearBit(elementsMask, elementCount, len);
        ArrayUtil.Fill2(elements, elementCount, len, null);
        this.len = elementCount;
    }

    #endregion
}
}