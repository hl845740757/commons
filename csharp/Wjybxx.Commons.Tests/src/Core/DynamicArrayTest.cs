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
using System.Linq;
using NUnit.Framework;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;

namespace Commons.Tests.Core;

public class DynamicArrayTest
{
    private static int capacity = 128;
    private static int repeat;

    private static IDynamicArray<Indexed> dynamicArray;
    private static Indexed[] valArray;
    private static Dictionary<int, Indexed> cacheMap = new(1000);

    private static Indexed ValueOf(int val) {
        if (!cacheMap.TryGetValue(val, out Indexed indexed)) {
            indexed = new Indexed(val);
            cacheMap[val] = indexed;
        }
        return indexed;
    }

    [SetUp]
    public void SetUp() {
        cacheMap.Clear();
        if (MathCommon.IsOdd(repeat++)) {
            capacity = 64;
            dynamicArray = new SmallDynamicArray<Indexed>(capacity / 3); // 测试扩容
        } else {
            capacity = 1000;
            dynamicArray = new IndexedDynamicArray<Indexed>(Helper.INST, capacity / 6); // 测试扩容
        }
        for (int i = 0; i < capacity; i++) {
            dynamicArray.Add(ValueOf(i));
        }
        valArray = new Indexed[capacity];
        for (int i = 0; i < capacity; i++) {
            valArray[i] = ValueOf(i);
        }
        ArrayUtil.Shuffle(valArray);
    }

    [Repeat(2)]
    [Test]
    public void testRemove() {
        for (int i = 0; i < valArray.Length; i++) {
            Indexed val = valArray[i];
            dynamicArray.Remove(val);

            Assert.IsFalse(dynamicArray.Contains(val), "remove failed");
            for (int j = i + 1; j < valArray.Length; j++) {
                Indexed jVal = valArray[j];
                Assert.IsTrue(dynamicArray.Contains(jVal), "val is absent" + jVal);
            }
        }
        Assert.AreEqual(0, dynamicArray.ElementCount);
    }

    [Repeat(2)]
    [Test]
    public void testRemoveWhenIterating() {
        dynamicArray.BeginItr();
        try {
            for (int i = 0; i < valArray.Length; i++) {
                Indexed val = valArray[i];
                dynamicArray.Remove(val);

                Assert.IsFalse(dynamicArray.Contains(val), "remove failed");
                for (int j = i + 1; j < valArray.Length; j++) {
                    Indexed jVal = valArray[j];
                    Assert.IsTrue(dynamicArray.Contains(jVal), "val is absent" + jVal);
                }
            }
            Assert.AreEqual(capacity, dynamicArray.Length);
        }
        finally {
            dynamicArray.EndItr();
        }
        Assert.AreEqual(0, dynamicArray.ElementCount);
    }

    [Repeat(2)]
    [Test]
    public void testInsert() {
        // 先删除一半，再insert回去
        List<Indexed> arrayList = dynamicArray.ToList();
        List<Indexed> removedList = new List<Indexed>(arrayList);
        CollectionUtil.Shuffle(removedList);
        removedList.RemoveRange(0, removedList.Count / 2);
        //
        foreach (Indexed val in removedList) {
            if (!arrayList.Remove(val)) {
                throw new AssertionError();
            }
            if (!dynamicArray.Remove(val)) {
                throw new AssertionError();
            }
        }
        dynamicArray.Compress(true);
        Assert.AreEqual(arrayList.Count, dynamicArray.ElementCount);
        // Assert.Equals(arrayList, dynamicArray.ToList());
        Assert.IsTrue(arrayList.SequenceEqual(dynamicArray.ToList()));

        // 插入
        foreach (Indexed val in removedList) {
            int index = Random.Shared.Next(arrayList.Count);
            arrayList.Insert(index, val);
            dynamicArray.Insert(index, val);
        }
        Assert.AreEqual(arrayList.Count, dynamicArray.ElementCount);
        Assert.IsTrue(arrayList.SequenceEqual(dynamicArray.ToList()));
    }

    [Repeat(2)]
    [Test]
    public void testMove() {
        cacheMap.Clear(); // 默认的缓存数据会导致异常--添加到了另一个队列

        int capacity = 16;
        IndexedDynamicArray<Indexed> dynamicArray = new IndexedDynamicArray<Indexed>(Helper.INST, capacity / 3);
        for (int i = 0; i < capacity; i++) {
            dynamicArray.Add(ValueOf(i));
        }
        // 随机移动一半元素
        List<Indexed> arrayList = dynamicArray.ToList();
        List<Indexed> moveList = new List<Indexed>(arrayList);
        CollectionUtil.Shuffle(moveList);
        moveList.RemoveRange(0, moveList.Count / 2);

        foreach (Indexed value in moveList) {
            int prevIndex = value.qIndex;
            int index = Random.Shared.Next(arrayList.Count);
            dynamicArray.MoveTo(value, index);

            if (prevIndex == index) {
                continue;
            }
            arrayList.Remove(value); // 先删除再插入
            arrayList.Insert(index, value);
        }

        Assert.AreEqual(arrayList.Count, dynamicArray.ElementCount);
        // Assert.Equals(arrayList, dynamicArray.ToList());
        Assert.IsTrue(arrayList.SequenceEqual(dynamicArray.ToList()));
    }

    #region MyRegion

    private class Helper : IIndexedElementHelper<Indexed>
    {
        internal static readonly Helper INST = new Helper();

        public int CollectionIndex(Object collection, Indexed element) {
            return element.qIndex;
        }

        public void CollectionIndex(Object collection, Indexed element, int index) {
            element.qIndex = index;
        }
    }

    private class Indexed : IEquatable<Indexed>
    {
        internal readonly int val;
        internal int qIndex = -1;

        public Indexed(int val) {
            this.val = val;
        }

        public bool Equals(Indexed? other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return val == other.val;
        }

        public override bool Equals(object? obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Indexed)obj);
        }

        public override int GetHashCode() {
            return val;
        }

        public static bool operator ==(Indexed? left, Indexed? right) {
            return Equals(left, right);
        }

        public static bool operator !=(Indexed? left, Indexed? right) {
            return !Equals(left, right);
        }

        public override string ToString() {
            return $"{nameof(val)}: {val}, {nameof(qIndex)}: {qIndex}";
        }
    }

    #endregion
}