#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using System.Collections;
using System.Collections.Generic;

namespace Wjybxx.Commons.Collections
{
/// <summary>
/// 空的双端队列
/// </summary>
/// <typeparam name="T"></typeparam>
public class EmptyDequeue<T> : IDeque<T>
{
    public static EmptyDequeue<T> Instance { get; } = new EmptyDequeue<T>();

    public bool IsReadOnly => true;
    public int Count => 0;
    public bool IsEmpty => true;

    public void AdjustCapacity(int expectedCount) {
    }

    public T PeekFirst() {
        throw ThrowHelper.CollectionEmptyException();
    }

    public T PeekLast() {
        throw ThrowHelper.CollectionEmptyException();
    }

    public bool TryPeekFirst(out T item) {
        item = default;
        return false;
    }

    public bool TryPeekLast(out T item) {
        item = default;
        return false;
    }

    public void AddFirst(T item) {
        throw new InvalidOperationException("ImmutableEmptyQueue");
    }

    public void AddLast(T item) {
        throw new InvalidOperationException("ImmutableEmptyQueue");
    }

    public bool TryAddFirst(T item) {
        return false;
    }

    public bool TryAddLast(T item) {
        return false;
    }

    public bool Remove(T item) {
        return false;
    }

    public T RemoveFirst() {
        throw ThrowHelper.CollectionEmptyException();
    }

    public T RemoveLast() {
        throw ThrowHelper.CollectionEmptyException();
    }

    public bool TryRemoveFirst(out T item) {
        item = default;
        return false;
    }

    public bool TryRemoveLast(out T item) {
        item = default;
        return false;
    }

    public bool Contains(T item) {
        return false;
    }

    public void Clear() {
    }

    public IDeque<T> Reversed() {
        return this;
    }

    public void CopyTo(T[] array, int arrayIndex, bool reversed = false) {
    }

    public IEnumerator<T> GetEnumerator() {
        return Itr;
    }

    public IEnumerator<T> GetReversedEnumerator() {
        return Itr;
    }

    private static readonly Iterator Itr = new Iterator();

    private class Iterator : ISequentialEnumerator<T>
    {
        public bool HasNext() {
            return false;
        }

        public bool MoveNext() {
            return false;
        }

        public void Reset() {
        }

        object IEnumerator.Current => Current;

        public void Dispose() {
        }

        public T Current => default;
    }
}
}