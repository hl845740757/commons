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

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 小型动态数组，最大支持64个元素。
///
/// <h3>null元素比重</h3>
/// 如果等于0，则总是压缩空间；如果等于1，则全为null才压缩空间；如果大于1，则表示不主动压缩空间；
/// </summary>
/// <typeparam name="E"></typeparam>
public class SmallDynamicArray<E> : IDynamicArray<E> where E : class
{
    private const int MAX_CAPACITY = 64;
    private E?[] elements;
    private long elementsMask;
    private readonly float nullFactor;

    private int len;
    private int recursionDepth;

    public SmallDynamicArray() : this(0) {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="initCapacity">初始空间大小</param>
    /// <param name="nullFactor">null元素的比重</param>
    public SmallDynamicArray(int initCapacity, float nullFactor = 0) {
        if (initCapacity < 0 || initCapacity > MAX_CAPACITY) {
            throw new ArgumentOutOfRangeException(nameof(initCapacity));
        }
        this.elements = initCapacity == 0 ? Array.Empty<E>() : new E[initCapacity];
        this.nullFactor = Math.Max(0, nullFactor);
    }

    #region itr

    public bool IsIterating => recursionDepth > 0;

    public void BeginItr() {
        recursionDepth++;
    }

    public void EndItr() {
        if (recursionDepth == 0) {
            throw new InvalidOperationException("begin must be called before end.");
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
        E? prev = elements[index];
        SetBit(index, e != null);
        elements[index] = e;
        // 尝试压缩空间
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
        SetBit(len, true);
        elements[len++] = e;
    }

    public void Insert(int index, E e) {
        if (e == null) throw new ArgumentNullException(nameof(e));
        ArrayUtil.CheckIndex(index, len + 1);
        EnsureNotIterating();
        if (len == elements.Length) {
            EnsureCapacity(len + 1);
        }
        if (index < len) {
            Array.Copy(elements, index, elements, index + 1, len - index);
            InsertBit(index);
        }
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
        if (elementsMask != 0) {
            Array.Clear(elements, 0, len);
        }
        elementsMask = 0;
        if (recursionDepth == 0) {
            len = 0;
        }
    }

    #endregion

    #region indexOf

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(E? e) {
        return IndexOf(e) >= 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ContainsRef(E? e) {
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
        return Array.LastIndexOf(elements, e, len - 1, len);
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
        return ArrayUtil.LastIndexOfRef(elements, e, len - 1, len);
    }

    private int FirstNullIndex() {
        if (len == 0 || len == ElementCount) return -1;
        // 将末尾的1转为0，这样低位的第一个1就是第一个null元素位置
        return MathCommon.NumberOfTrailingZeros(~elementsMask);
    }

    private int LastNullIndex() {
        if (len == 0 || len == ElementCount) return -1;
        // 先将超出len这部分也转为1，再整体取反转0，这样高位的第一个1就是第一个null元素位置 -- -1左移64位居然还是-1，我还以为是0
        long tempMask = len == 64
            ? (elementsMask)
            : (elementsMask | (-1L << len));
        return 63 - MathCommon.NumberOfLeadingZeros(~tempMask);
    }

    #endregion

    #region len

    public int Length => len;
    public int ElementCount => MathCommon.BitCount(elementsMask);
    public int NullCount => len - MathCommon.BitCount(elementsMask);
    public bool ContainsNull => len > MathCommon.BitCount(elementsMask);

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

    public void Compress(bool ignoreFactor) {
        EnsureNotIterating();
        if (ignoreFactor || IsCompressionNeeded()) {
            RemoveNullElements();
        }
    }

    public void EnsureCapacity(int minCapacity) {
        if (minCapacity > MAX_CAPACITY) {
            throw new InvalidOperationException("overflow");
        }
        int oldCapacity = elements.Length;
        if (minCapacity <= oldCapacity) {
            return;
        }
        int grow = oldCapacity < 16 ? 4 : 8;
        int newCapacity = MathCommon.Clamp(oldCapacity + grow, minCapacity, MAX_CAPACITY);
        elements = ArrayUtil.CopyOf(elements, 0, newCapacity);
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

    public ReadOnlySpan<E?> AsSpan() => new ReadOnlySpan<E?>(elements, 0, len);

    #endregion

    #region internal

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBit(int index, bool val) {
        if (val) {
            elementsMask |= (1L << index);
        } else {
            elementsMask &= ~(1L << index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InsertBit(int index) {
        long high = (elementsMask << 1) & (-1L << (index + 1)); // [0, index] 全0，使index位为0
        long lower = (elementsMask) & ((1L << index) - 1); // [0, index -1] 全1
        elementsMask = high | lower;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureNotIterating() {
        if (recursionDepth != 0) {
            throw new InvalidOperationException("Invalid between iterating.");
        }
    }

    private bool IsCompressionNeeded() {
        float nullFactor = this.nullFactor;
        if (nullFactor == 0f) return true;
        if (nullFactor > 1f) return false;

        int nullCount = len - ElementCount;
        if (nullFactor == 1f) {
            return nullCount == len;
        }
        return nullCount > 0 && nullCount >= len * nullFactor;
    }

    private void RemoveNullElements() {
        Debug.Assert(recursionDepth == 0);
        int elementCount = ElementCount;
        if (elementCount == len) {
            return;
        }
        if (elementCount == 0) {
            this.len = 0;
            this.elementsMask = 0;
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
            elements[index] = null; // help debug
            elements[firstNullIndex++] = element;
        }
        // 批量前移
        int copyStart = lastNullIndex + 1;
        if (copyStart < len) {
            Array.Copy(elements, copyStart, elements, firstNullIndex, (len - copyStart));
        }
        ArrayUtil.Fill2(elements, elementCount, len, null);
        this.len = elementCount;
        this.elementsMask = (1L << elementCount) - 1;
    }

    #endregion
}
}