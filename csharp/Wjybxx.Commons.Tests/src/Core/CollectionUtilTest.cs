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
using NUnit.Framework;
using Wjybxx.Commons.Collections;

namespace Commons.Tests.Core;

public class CollectionUtilTest
{
    private sealed class RefBox
    {
        public int Value;
        public RefBox(int value) => Value = value;
    }

    [Test]
    public void TestIndexOfRef() {
        RefBox a = new(1);
        RefBox b = new(1); // 与 a 值相同但引用不同
        RefBox c = new(2);
        List<RefBox> list = new() { a, b, c };

        Assert.AreEqual(0, CollectionUtil.IndexOfRef(list, a));
        Assert.AreEqual(1, CollectionUtil.IndexOfRef(list, b));
        Assert.AreEqual(2, CollectionUtil.IndexOfRef(list, c));
        Assert.AreEqual(-1, CollectionUtil.IndexOfRef(list, new RefBox(1)));
    }

    [Test]
    public void TestLastIndexOfRef() {
        RefBox a = new(1);
        RefBox b = new(2);
        List<RefBox> list = new() { a, b, a };

        Assert.AreEqual(2, CollectionUtil.LastIndexOfRef(list, a));
        Assert.AreEqual(1, CollectionUtil.LastIndexOfRef(list, b));
    }

    [Test]
    public void TestRemoveRef() {
        RefBox a = new(1);
        RefBox b = new(1); // 同值不同引用
        List<RefBox> list = new() { a, b };

        Assert.IsTrue(CollectionUtil.RemoveRef(list, a));
        Assert.AreEqual(1, list.Count);
        Assert.AreSame(b, list[0]);
        Assert.IsFalse(CollectionUtil.RemoveRef(list, new RefBox(1)));
    }

    [Test]
    public void TestBinarySearchHit() {
        List<int> sorted = new() { 1, 3, 5, 7, 9, 11 };
        Assert.AreEqual(0, CollectionUtil.BinarySearch(sorted, 1, Comparer<int>.Default));
        Assert.AreEqual(3, CollectionUtil.BinarySearch(sorted, 7, Comparer<int>.Default));
        Assert.AreEqual(5, CollectionUtil.BinarySearch(sorted, 11, Comparer<int>.Default));
    }

    [Test]
    public void TestBinarySearchMiss() {
        List<int> sorted = new() { 1, 3, 5, 7 };
        // 缺失时返回 -(insertion point) - 1
        int r = CollectionUtil.BinarySearch(sorted, 4, Comparer<int>.Default);
        Assert.IsTrue(r < 0);
        int insertion = (r + 1) * -1;
        Assert.AreEqual(2, insertion);
    }

    [Test]
    public void TestBinarySearchWithFunc() {
        List<int> sorted = new() { 10, 20, 30, 40 };
        // 查找 30：mid 与 30 比较
        int r = CollectionUtil.BinarySearch(sorted, mid => mid.CompareTo(30));
        Assert.AreEqual(2, r);
    }

    [Test]
    public void TestPeekFirstAndLast() {
        List<int> list = new() { 10, 20, 30 };
        Assert.AreEqual(10, list.PeekFirst());
        Assert.AreEqual(30, list.PeekLast());

        Assert.IsTrue(list.TryPeekFirst(out int first));
        Assert.AreEqual(10, first);
        Assert.IsTrue(list.TryPeekLast(out int last));
        Assert.AreEqual(30, last);
    }

    [Test]
    public void TestPeekOnEmptyThrows() {
        List<int> list = new();
        Assert.Throws<InvalidOperationException>(() => list.PeekFirst());
        Assert.Throws<InvalidOperationException>(() => list.PeekLast());
        Assert.IsFalse(list.TryPeekFirst(out _));
        Assert.IsFalse(list.TryPeekLast(out _));
    }

    [Test]
    public void TestShufflePreservesElements() {
        List<int> list = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        CollectionUtil.Shuffle(list, new Random(20250513));
        Assert.AreEqual(10, list.Count);
        // 使用排序对照
        List<int> sorted = new(list);
        sorted.Sort();
        for (int i = 0; i < sorted.Count; i++) {
            Assert.AreEqual(i + 1, sorted[i]);
        }
    }

    [Test]
    public void TestConcatTwo() {
        List<int> a = new() { 1, 2 };
        List<int> b = new() { 3, 4 };
        List<int> r = CollectionUtil.Concat(a, b);
        Assert.AreEqual(new[] { 1, 2, 3, 4 }, r);

        Assert.AreEqual(new[] { 1, 2 }, CollectionUtil.Concat(a, null));
        Assert.AreEqual(new[] { 3, 4 }, CollectionUtil.Concat(null, b));
        Assert.AreEqual(0, CollectionUtil.Concat<int>(null, null).Count);
    }

    [Test]
    public void TestConcatMany() {
        List<int> r = CollectionUtil.Concat(new[] { 1 }, null, new[] { 2, 3 }, new[] { 4 });
        Assert.AreEqual(new[] { 1, 2, 3, 4 }, r);
    }

    [Test]
    public void TestToStack() {
        List<int> list = new() { 1, 2, 3 };
        Stack<int> stack = CollectionUtil.ToStack(list);
        Assert.AreEqual(1, stack.Pop());
        Assert.AreEqual(2, stack.Pop());
        Assert.AreEqual(3, stack.Pop());
    }

    [Test]
    public void TestAddRange2() {
        List<int> list = new() { 1, 2 };
        list.AddRange2(new[] { 3, 4, 5 });
        Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, list);
    }

    [Test]
    public void TestComputeIfAbsent() {
        Dictionary<string, int> dic = new() { ["a"] = 1 };
        int callCount = 0;
        int v = dic.ComputeIfAbsent("a", _ => {
            callCount++;
            return 99;
        });
        Assert.AreEqual(1, v);
        Assert.AreEqual(0, callCount);

        v = dic.ComputeIfAbsent("b", _ => {
            callCount++;
            return 42;
        });
        Assert.AreEqual(42, v);
        Assert.AreEqual(1, callCount);
        Assert.AreEqual(42, dic["b"]);
    }

    [Test]
    public void TestDataEqualsCollection() {
        HashSet<int> a = new() { 1, 2, 3 };
        HashSet<int> b = new() { 3, 1, 2 };
        HashSet<int> c = new() { 1, 2, 4 };

        Assert.IsTrue(CollectionUtil.DataEquals(a, b));
        Assert.IsFalse(CollectionUtil.DataEquals(a, c));
        Assert.IsTrue(CollectionUtil.DataEquals<int>(null, null));
        Assert.IsFalse(CollectionUtil.DataEquals(a, null));
    }

    [Test]
    public void TestDataEqualsDictionary() {
        Dictionary<string, int> a = new() { ["a"] = 1, ["b"] = 2 };
        Dictionary<string, int> b = new() { ["b"] = 2, ["a"] = 1 };
        Dictionary<string, int> c = new() { ["a"] = 1, ["b"] = 99 };

        Assert.IsTrue(CollectionUtil.DataEquals(a, b));
        Assert.IsFalse(CollectionUtil.DataEquals(a, c));
    }

    [Test]
    public void TestAddAllAndRemoveAll() {
        List<int> list = new();
        list.AddAll(new[] { 1, 2, 3, 4 });
        Assert.AreEqual(4, list.Count);

        int removed = list.RemoveAll(new[] { 2, 4, 99 });
        Assert.AreEqual(2, removed);
        Assert.AreEqual(new[] { 1, 3 }, list);
    }

    [Test]
    public void TestRetainAll() {
        List<int> list = new() { 1, 2, 3, 4, 5 };
        CollectionUtil.RetainAll(list, new HashSet<int> { 2, 4 });
        Assert.AreEqual(new[] { 2, 4 }, list);
    }

    [Test]
    public void TestPutAllOverwrite() {
        Dictionary<string, int> dic = new() { ["a"] = 1 };
        dic.PutAll(new[] {
            new KeyValuePair<string, int>("a", 99),
            new KeyValuePair<string, int>("b", 2),
        });
        Assert.AreEqual(99, dic["a"]);
        Assert.AreEqual(2, dic["b"]);
    }

    [Test]
    public void TestGetValueOrDefault() {
        Dictionary<string, int> dic = new() { ["a"] = 1 };
        Assert.AreEqual(1, CollectionUtil.GetValueOrDefault(dic, "a", 99));
        Assert.AreEqual(99, CollectionUtil.GetValueOrDefault(dic, "missing", 99));
    }

    [Test]
    public void TestIsNullOrEmpty() {
        Assert.IsTrue(CollectionUtil.IsNullOrEmpty<int>((ICollection<int>?)null));
        Assert.IsTrue(CollectionUtil.IsNullOrEmpty(new List<int>()));
        Assert.IsFalse(CollectionUtil.IsNullOrEmpty(new List<int> { 1 }));
    }

    [Test]
    public void TestCapacity() {
        Assert.AreEqual(4, CollectionUtil.Capacity(0));
        Assert.AreEqual(4, CollectionUtil.Capacity(2));
        // 4 / 0.75 = 5.33 -> ceil = 6
        Assert.AreEqual(6, CollectionUtil.Capacity(4));
    }
}
