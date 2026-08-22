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
            dynamicArray = new IndexedDynamicArray<Indexed>(Helper.Inst, capacity / 6); // 测试扩容
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
        IndexedDynamicArray<Indexed> dynamicArray = new IndexedDynamicArray<Indexed>(Helper.Inst, capacity / 3);
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

    // ============= 补充：API 语义 / 复杂场景测试 =============

    /// <summary>
    /// Add(null) / Insert(null) 抛 ArgumentNullException
    /// </summary>
    [Test]
    public void TestAddNullThrows() {
        DynamicArray<string> arr = new();
        Assert.Throws<ArgumentNullException>(() => arr.Add(null));
        Assert.Throws<ArgumentNullException>(() => arr.Insert(0, null));

        SmallDynamicArray<string> small = new();
        Assert.Throws<ArgumentNullException>(() => small.Add(null));
    }

    /// <summary>
    /// Set 返回旧值；Set(i, null) 即删除
    /// </summary>
    [Test]
    public void TestSetReturnsPrevAndDelete() {
        DynamicArray<string> arr = new(8, 1.0f); // nullFactor=1，不自动压缩
        arr.Add("a");
        arr.Add("b");
        arr.Add("c");

        string prev = arr.Set(1, "B");
        Assert.AreEqual("b", prev);
        Assert.AreEqual("B", arr[1]);
        Assert.AreEqual(3, arr.ElementCount);

        // Set null = 删除
        string prev2 = arr.Set(2, null);
        Assert.AreEqual("c", prev2);
        Assert.AreEqual(2, arr.ElementCount);
        Assert.IsTrue(arr.ContainsNull);
        Assert.AreEqual(1, arr.NullCount);
    }

    /// <summary>
    /// Length 与 ElementCount 的差异：迭代期间删除不减少 Length，结束后压缩
    /// </summary>
    [Test]
    public void TestLengthVsElementCount() {
        DynamicArray<string> arr = new();
        for (int i = 0; i < 5; i++) arr.Add("v" + i);

        arr.BeginItr();
        try {
            arr.Remove("v1");
            arr.Remove("v3");
            // 迭代期间 length 保持
            Assert.AreEqual(5, arr.Length);
            Assert.AreEqual(3, arr.ElementCount);
            Assert.IsTrue(arr.ContainsNull);
            Assert.AreEqual(2, arr.NullCount);
        }
        finally {
            arr.EndItr();
        }
        // 结束后 nullFactor=0 触发压缩
        Assert.AreEqual(3, arr.Length);
        Assert.AreEqual(3, arr.ElementCount);
        Assert.IsFalse(arr.ContainsNull);
        Assert.AreEqual(new List<string> { "v0", "v2", "v4" }, arr.ToList());
    }

    /// <summary>
    /// IndexOf / LastIndexOf 区分 equals 与 ref；查找 null 返回首/末 null 下标
    /// </summary>
    [Test]
    public void TestIndexOfMethods() {
        Indexed a = new(1);
        Indexed b = new(2);
        Indexed aDup = new(1); // equals 等价但引用不同
        DynamicArray<Indexed> arr = new(8, 1.0f);
        arr.Add(a);
        arr.Add(b);
        arr.Add(aDup);

        // equals 语义：第一个/最后一个值为 1 的位置
        Assert.AreEqual(0, arr.IndexOf(a));
        Assert.AreEqual(2, arr.LastIndexOf(a));
        Assert.AreEqual(0, arr.IndexOf(aDup));

        // ref 语义：仅匹配同引用
        Assert.AreEqual(0, arr.IndexOfRef(a));
        Assert.AreEqual(2, arr.IndexOfRef(aDup));
        Assert.AreEqual(0, arr.LastIndexOfRef(a));

        // 不存在
        Assert.AreEqual(-1, arr.IndexOf(new Indexed(99)));
        Assert.AreEqual(-1, arr.IndexOfRef(new Indexed(1)));

        // 查 null：返回首/末 null 下标
        arr.Set(1, null);
        Assert.AreEqual(1, arr.IndexOf(null));
        Assert.AreEqual(1, arr.LastIndexOf(null));
    }

    /// <summary>
    /// Contains / ContainsRef
    /// </summary>
    [Test]
    public void TestContainsAndContainsRef() {
        Indexed a = new(1);
        Indexed aDup = new(1);
        DynamicArray<Indexed> arr = new();
        arr.Add(a);

        Assert.IsTrue(arr.Contains(aDup)); // equals
        Assert.IsFalse(arr.ContainsRef(aDup)); // 引用不同
        Assert.IsTrue(arr.ContainsRef(a));
    }

    /// <summary>
    /// Sort 会强制压缩再排序
    /// </summary>
    [Test]
    public void TestSortCompressesAndSorts() {
        DynamicArray<Indexed> arr = new(8, 1.0f);
        arr.Add(new Indexed(3));
        arr.Add(new Indexed(1));
        arr.Add(new Indexed(4));
        arr.Add(new Indexed(1));
        arr.Add(new Indexed(5));
        arr.Set(2, null);
        arr.Set(4, null);

        Assert.IsTrue(arr.ContainsNull);
        arr.Sort(Comparer<Indexed>.Create((x, y) => x.val.CompareTo(y.val)));

        Assert.IsFalse(arr.ContainsNull);
        Assert.AreEqual(3, arr.Length);
        List<Indexed> sorted = arr.ToList();
        Assert.AreEqual(1, sorted[0].val);
        Assert.AreEqual(1, sorted[1].val);
        Assert.AreEqual(3, sorted[2].val);
    }

    /// <summary>
    /// EnsureCapacity 不改变 Length
    /// </summary>
    [Test]
    public void TestEnsureCapacityNoLengthChange() {
        DynamicArray<string> arr = new(4);
        for (int i = 0; i < 3; i++) arr.Add("v" + i);
        int prevLen = arr.Length;

        arr.EnsureCapacity(100);
        Assert.AreEqual(prevLen, arr.Length);
        Assert.AreEqual(3, arr.ElementCount);

        // 仍可继续 Add
        for (int i = 0; i < 50; i++) arr.Add("x" + i);
        Assert.AreEqual(53, arr.ElementCount);
    }

    /// <summary>
    /// Compress(force=true) 即使无 null 也无副作用；force=false + nullFactor > 阈值 不压缩
    /// </summary>
    [Test]
    public void TestCompressForceVsNot() {
        // nullFactor = 0.5 → 仅当 null 比例 ≥ 50% 才压缩
        DynamicArray<string> arr = new(8, 0.5f);
        for (int i = 0; i < 6; i++) arr.Add("v" + i);

        arr.BeginItr();
        try {
            arr.Set(0, null); // 1 个 null / 6 → 不压缩
        }
        finally {
            arr.EndItr();
        }
        Assert.AreEqual(6, arr.Length);
        Assert.IsTrue(arr.ContainsNull);

        arr.Compress(false); // 比例不足，不压缩
        Assert.AreEqual(6, arr.Length);

        arr.Compress(true); // 强制压缩
        Assert.AreEqual(5, arr.Length);
        Assert.IsFalse(arr.ContainsNull);
    }

    /// <summary>
    /// ForEach 跳过 null，不迭代迭代期间新增的元素
    /// </summary>
    [Test]
    public void TestForEachSkipsNullAndNewlyAdded() {
        DynamicArray<string> arr = new(8, 1.0f);
        for (int i = 0; i < 5; i++) arr.Add("v" + i);
        arr.Set(2, null);

        List<string> seen = new();
        arr.ForEach((e, idx) => {
            seen.Add(e);
            // 迭代期间新增的元素不应被本次 ForEach 看到
            arr.Add("X" + idx);
        });
        Assert.AreEqual(new List<string> { "v0", "v1", "v3", "v4" }, seen);
    }

    /// <summary>
    /// ToList 跳过 null
    /// </summary>
    [Test]
    public void TestToListSkipsNull() {
        DynamicArray<string> arr = new(8, 1.0f);
        arr.Add("a");
        arr.Add("b");
        arr.Add("c");
        arr.Set(1, null);

        Assert.AreEqual(new List<string> { "a", "c" }, arr.ToList());
    }

    /// <summary>
    /// 索引器 / Insert 越界检查
    /// </summary>
    [Test]
    public void TestIndexOutOfRange() {
        DynamicArray<string> arr = new();
        arr.Add("a");

        Assert.Throws<IndexOutOfRangeException>(() => { var _ = arr[5]; });
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = arr[-1]; });
        Assert.Throws<IndexOutOfRangeException>(() => arr.Set(5, "x"));
        Assert.Throws<IndexOutOfRangeException>(() => arr.Insert(5, "x"));
    }

    /// <summary>
    /// 迭代期间禁止 Insert / Sort / Compress
    /// </summary>
    [Test]
    public void TestMutationDuringIterationThrows() {
        DynamicArray<string> arr = new();
        for (int i = 0; i < 3; i++) arr.Add("v" + i);

        arr.BeginItr();
        try {
            Assert.Throws<InvalidOperationException>(() => arr.Insert(0, "x"));
            Assert.Throws<InvalidOperationException>(() => arr.Sort(Comparer<string>.Default));
            Assert.Throws<InvalidOperationException>(() => arr.Compress(true));
        }
        finally {
            arr.EndItr();
        }
    }

    /// <summary>
    /// EndItr 未配对 Begin 抛异常
    /// </summary>
    [Test]
    public void TestEndItrWithoutBeginThrows() {
        DynamicArray<string> arr = new();
        Assert.Throws<InvalidOperationException>(() => arr.EndItr());
    }

    /// <summary>
    /// 嵌套迭代：Begin/End 嵌套使用，仅最外层退出时才触发压缩
    /// </summary>
    [Test]
    public void TestNestedIteration() {
        DynamicArray<string> arr = new();
        for (int i = 0; i < 5; i++) arr.Add("v" + i);

        arr.BeginItr();
        try {
            arr.Remove("v1");
            arr.BeginItr();
            try {
                arr.Remove("v3");
                Assert.IsTrue(arr.IsIterating);
                Assert.AreEqual(5, arr.Length); // 内层未结束
            }
            finally {
                arr.EndItr();
            }
            // 内层 EndItr 后仍处于外层迭代
            Assert.IsTrue(arr.IsIterating);
            Assert.AreEqual(5, arr.Length);
        }
        finally {
            arr.EndItr();
        }
        Assert.IsFalse(arr.IsIterating);
        Assert.AreEqual(3, arr.Length); // 最外层结束 → 压缩
    }

    /// <summary>
    /// 迭代期间 Clear 不重置 Length（仅清除元素）
    /// </summary>
    [Test]
    public void TestClearDuringIterationDoesNotResetLength() {
        DynamicArray<string> arr = new();
        for (int i = 0; i < 5; i++) arr.Add("v" + i);

        arr.BeginItr();
        try {
            arr.Clear();
            Assert.AreEqual(5, arr.Length);
            Assert.AreEqual(0, arr.ElementCount);
        }
        finally {
            arr.EndItr();
        }
    }

    /// <summary>
    /// Clear 在非迭代期间会重置 Length
    /// </summary>
    [Test]
    public void TestClearOutsideIterationResetsLength() {
        DynamicArray<string> arr = new();
        for (int i = 0; i < 5; i++) arr.Add("v" + i);

        arr.Clear();
        Assert.AreEqual(0, arr.Length);
        Assert.AreEqual(0, arr.ElementCount);
    }

    /// <summary>
    /// IndexedDynamicArray：重复添加同一引用应抛异常
    /// </summary>
    [Test]
    public void TestIndexedDynamicArrayDuplicateAddThrows() {
        IndexedDynamicArray<Indexed> arr = new(Helper.Inst, 8);
        Indexed a = new(1);
        arr.Add(a);
        Assert.Throws<ArgumentException>(() => arr.Add(a));
    }

    /// <summary>
    /// IndexedDynamicArray：MoveTo 不在数组中的元素抛异常
    /// </summary>
    [Test]
    public void TestIndexedDynamicArrayMoveToMissingThrows() {
        IndexedDynamicArray<Indexed> arr = new(Helper.Inst, 8);
        for (int i = 0; i < 5; i++) arr.Add(new Indexed(i));

        Indexed orphan = new(99); // 未加入
        Assert.Throws<ArgumentException>(() => arr.MoveTo(orphan, 0));
    }

    /// <summary>
    /// IndexedDynamicArray：Add 后 helper 记录的 qIndex 与实际下标一致；Set/Remove 后变 -1
    /// </summary>
    [Test]
    public void TestIndexedDynamicArrayMaintainsQIndex() {
        IndexedDynamicArray<Indexed> arr = new(Helper.Inst, 8);
        Indexed[] items = new Indexed[5];
        for (int i = 0; i < 5; i++) {
            items[i] = new Indexed(i);
            arr.Add(items[i]);
            Assert.AreEqual(i, items[i].qIndex);
        }

        // 移除中间元素 → 该元素 qIndex 重置；后续元素的 qIndex 因压缩而前移
        arr.Remove(items[2]);
        Assert.AreEqual(-1, items[2].qIndex);
        Assert.AreEqual(0, items[0].qIndex);
        Assert.AreEqual(1, items[1].qIndex);
        Assert.AreEqual(2, items[3].qIndex);
        Assert.AreEqual(3, items[4].qIndex);
    }

    /// <summary>
    /// SmallDynamicArray 容量受 64 上限约束
    /// </summary>
    [Test]
    public void TestSmallDynamicArrayCapacityLimit() {
        SmallDynamicArray<string> arr = new(8);
        for (int i = 0; i < 64; i++) {
            arr.Add("v" + i);
        }
        Assert.AreEqual(64, arr.Length);
        Assert.Throws<InvalidOperationException>(() => arr.Add("overflow"));
    }

    /// <summary>
    /// SmallDynamicArray ElementCount 是基于 mask 位计数实时计算
    /// </summary>
    [Test]
    public void TestSmallDynamicArrayElementCount() {
        SmallDynamicArray<string> arr = new(8);
        for (int i = 0; i < 5; i++) arr.Add("v" + i);

        arr.BeginItr();
        try {
            arr.Set(1, null);
            arr.Set(3, null);
            Assert.AreEqual(5, arr.Length);
            Assert.AreEqual(3, arr.ElementCount);
            Assert.AreEqual(2, arr.NullCount);
            Assert.IsTrue(arr.ContainsNull);
        }
        finally {
            arr.EndItr();
        }
    }

    #region MyRegion

    private class Helper : IIndexedElementHelper<Indexed>
    {
        internal static readonly Helper Inst = new Helper();

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