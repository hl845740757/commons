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
using NUnit.Framework;
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;

namespace Commons.Tests.Core;

public class LinkedHashSetTest
{
    [Test]
    [Repeat(5)]
    public void TestIntSet() {
        int expectedCount = 10000;
        HashSet<int> keySet = new HashSet<int>(expectedCount);
        List<int> keyList = new List<int>(expectedCount);
        LinkedHashSet<int> linkedHashSet = new LinkedHashSet<int>(expectedCount / 3); // 顺便测试扩容

        // 在插入期间随机删除已存在的key；不宜太频繁，否则keyList的移动开销太大
        while (keySet.Count < expectedCount) {
            if (Random.Shared.Next(0, 10) == 1 && keyList.Count > expectedCount / 3) {
                int idx = Random.Shared.Next(0, keyList.Count);
                int key = keyList[idx];
                keyList.RemoveAt(idx);
                keySet.Remove(key);
                linkedHashSet.Remove(key);
                continue;
            }
            int next = Random.Shared.Next();
            if (keySet.Add(next)) {
                keyList.Add(next);
                linkedHashSet.Add(next);
            }
        }
        Assert.That(keySet.Count, Is.EqualTo(keyList.Count));
        Assert.That(linkedHashSet.Count, Is.EqualTo(keyList.Count));

        int index = 0;
        foreach (int realKey in linkedHashSet) {
            int expectedKey = keyList[index++];
            Assert.That(realKey, Is.EqualTo(expectedKey));
        }
    }

    [Test]
    [Repeat(5)]
    public void TestStringDic1() {
        TestStringSet(10000);
    }

    [Test]
    [Repeat(5)]
    public void TestStringDic2() {
        TestStringSet(100000);
    }

    private static LinkedHashSet<string> TestStringSet(int expectedCount) {
        LinkedHashSet<string> linkedHashSet = new LinkedHashSet<string>(expectedCount / 3); // 顺便测试扩容

        byte[] buffer = new byte[12];
        List<string> keyList = new List<string>(expectedCount);
        while (linkedHashSet.Count < expectedCount) {
            Random.Shared.NextBytes(buffer);
            string next = Convert.ToHexString(buffer);
            string key = Random.Shared.Next(0, 10) == 0 ? null : next; // 随机使用nullKey

            // 还需要测试AddFirst
            if (linkedHashSet.Add(key)) {
                keyList.Add(key);
            }
            // 随机删除元素 30%概率
            if (linkedHashSet.Count > expectedCount / 2) {
                int idx = -1;
                switch (Random.Shared.Next(10)) {
                    case 0: {
                        // 随机位置
                        idx = Random.Shared.Next(keyList.Count);
                        break;
                    }
                    case 1: {
                        // 删除首元素
                        idx = 0;
                        break;
                    }
                    case 2: {
                        // 删除尾元素
                        idx = keyList.Count - 1;
                        break;
                    }
                }
                if (idx >= 0) {
                    string remKey = keyList[idx];
                    keyList.RemoveAt(idx);
                    linkedHashSet.Remove(remKey);
                }
            }
        }

        Assert.That(linkedHashSet.Count, Is.EqualTo(keyList.Count));
        // 顺序迭代测试
        int index = 0;
        foreach (var realKey in linkedHashSet) {
            var expectedKey = keyList[index++];
            if (expectedKey != realKey) {
                throw new InvalidOperationException($"expectedKey:{expectedKey} == realKey:{realKey}");
            }
        }
        // 逆序迭代测试
        index = keyList.Count - 1;
        var reversedEnumerator = linkedHashSet.GetReversedEnumerator();
        while (reversedEnumerator.MoveNext()) {
            var expectedKey = keyList[index--];
            string realKey = reversedEnumerator.Current;
            if (expectedKey != realKey) {
                throw new InvalidOperationException($"expectedKey:{expectedKey} == realKey:{realKey}");
            }
        }
        return linkedHashSet;
    }

    [Test]
    public void TestAdjustCapacity() {
        LinkedHashSet<string> hashSet = TestStringSet(10000);
        string[] rawArray = hashSet.ToArray();
        {
            hashSet.EnsureCapacity(15000);
            string[] copiedArray1 = hashSet.ToArray();
            ArrayUtil.Equals(rawArray, copiedArray1);
        }
        {
            hashSet.EnsureCapacity(10001);
            string[] copiedArray2 = hashSet.ToArray();
            ArrayUtil.Equals(rawArray, copiedArray2);
        }
        {
            hashSet.EnsureCapacity(10000);
            string[] copiedArray3 = hashSet.ToArray();
            ArrayUtil.Equals(rawArray, copiedArray3);
        }
    }


    [Test]
    public void TestMoveToFirst() {
        const int expectedCount = 10;
        List<int> keyList = new List<int>(expectedCount);
        LinkedHashSet<int> keySet = new LinkedHashSet<int>(expectedCount);
        // 连续的List更利于观察
        for (int i = 0; i < expectedCount; i++) {
            keyList.Add(i);
            keySet.Add(i);
        }
        for (int i = 0; i < expectedCount * 10; i++) {
            int idx1 = Random.Shared.Next(expectedCount);
            int key1 = keyList[idx1];
            
            keyList.RemoveAt(idx1);
            if (Random.Shared.NextBool()) {
                keyList.Insert(0, key1); // addFirst
                keySet.AddFirst(key1);
            } else {
                keyList.Add(key1); // addLast
                keySet.AddLast(key1);
            }
        }
        int index = 0;
        foreach (int realKey in keySet) {
            int expectedKey = keyList[index++];
            Assert.That(realKey, Is.EqualTo(expectedKey));
        }
    }

    [Test]
    public void TestMoveAfter() {
        const int expectedCount = 10;
        List<int> keyList = new List<int>(expectedCount);
        LinkedHashSet<int> keySet = new LinkedHashSet<int>(expectedCount);
        // 连续的List更利于观察
        for (int i = 0; i < expectedCount; i++) {
            keyList.Add(i);
            keySet.Add(i);
        }
        // while (hashSet.Count < expectedCount) {
        //     int next = Random.Shared.Next();
        //     if (hashSet.Add(next)) {
        //         keyList.Add(next);
        //     }
        // }
        for (int i = 0; i < expectedCount * 10; i++) {
            int idx1 = Random.Shared.Next(expectedCount);
            int idx2 = Random.Shared.Next(expectedCount);
            if (idx1 == idx2) continue;

            int key1 = keyList[idx1];
            int key2 = keyList[idx2];
            keyList.RemoveAt(idx1);
            if (idx1 < idx2) {
                keyList.Insert(idx2, key1); // 插到key2后面 -- idx2前移了1位
                keySet.MoveToAfter(key1, key2);
            } else {
                keyList.Insert(idx2, key1); // 插到key2前面 -- idx2未移动
                keySet.MoveToBefore(key1, key2);
            }
        }

        int index = 0;
        foreach (int realKey in keySet) {
            int expectedKey = keyList[index++];
            Assert.That(realKey, Is.EqualTo(expectedKey));
        }
    }

    // ============= 补充：API 语义 / 复杂场景测试 =============

    /// <summary>
    /// Add 返回 true/false，重复添加不修改顺序
    /// </summary>
    [Test]
    public void TestAddReturnValue() {
        LinkedHashSet<string> set = new();
        Assert.IsTrue(set.Add("a"));
        Assert.IsTrue(set.Add("b"));
        Assert.IsFalse(set.Add("a")); // 重复
        Assert.AreEqual(new[] { "a", "b" }, set.ToArray());
        Assert.AreEqual(2, set.Count);
    }

    /// <summary>
    /// AddFirst / AddLast 对已存在 key 应当返回 false，但仍要重排到首/尾
    /// </summary>
    [Test]
    public void TestAddFirstLastReordersExisting() {
        LinkedHashSet<string> set = new();
        set.Add("a");
        set.Add("b");
        set.Add("c");

        Assert.IsFalse(set.AddFirst("b")); // 已存在 → false，但移到头部
        Assert.AreEqual(new[] { "b", "a", "c" }, set.ToArray());

        Assert.IsFalse(set.AddLast("a")); // 已存在 → false，但移到尾部
        Assert.AreEqual(new[] { "b", "c", "a" }, set.ToArray());

        // 新元素插入
        Assert.IsTrue(set.AddFirst("d"));
        Assert.AreEqual(new[] { "d", "b", "c", "a" }, set.ToArray());
        Assert.IsTrue(set.AddLast("e"));
        Assert.AreEqual(new[] { "d", "b", "c", "a", "e" }, set.ToArray());
    }

    /// <summary>
    /// AddFirstIfAbsent / AddLastIfAbsent 与 AddFirst/AddLast 的差异：已存在不重排
    /// </summary>
    [Test]
    public void TestAddIfAbsentDoesNotReorder() {
        LinkedHashSet<string> set = new();
        set.Add("a");
        set.Add("b");
        set.Add("c");

        Assert.IsFalse(set.AddFirstIfAbsent("b")); // 已存在 → false，不移动
        Assert.AreEqual(new[] { "a", "b", "c" }, set.ToArray());

        Assert.IsFalse(set.AddLastIfAbsent("a"));
        Assert.AreEqual(new[] { "a", "b", "c" }, set.ToArray());

        // 新元素插入到首/尾
        Assert.IsTrue(set.AddFirstIfAbsent("d"));
        Assert.AreEqual(new[] { "d", "a", "b", "c" }, set.ToArray());
        Assert.IsTrue(set.AddLastIfAbsent("e"));
        Assert.AreEqual(new[] { "d", "a", "b", "c", "e" }, set.ToArray());
    }

    /// <summary>
    /// AddBefore / AddAfter 顺序与异常
    /// </summary>
    [Test]
    public void TestAddBeforeAfter() {
        LinkedHashSet<string> set = new();
        set.Add("a");
        set.Add("c");

        set.AddAfter("b", "a"); // a, b, c
        Assert.AreEqual(new[] { "a", "b", "c" }, set.ToArray());

        set.AddBefore("z", "c"); // a, b, z, c
        Assert.AreEqual(new[] { "a", "b", "z", "c" }, set.ToArray());

        // 已存在 key 调用 AddAfter → 内部 TryInsert 返回 false，但随后会执行 MoveToAfter
        set.AddAfter("a", "c"); // a 移动到 c 之后 → b, z, c, a
        Assert.AreEqual(new[] { "b", "z", "c", "a" }, set.ToArray());

        Assert.Throws<KeyNotFoundException>(() => set.AddAfter("x", "missing"));
        Assert.Throws<KeyNotFoundException>(() => set.AddBefore("x", "missing"));
        Assert.Throws<ArgumentException>(() => set.AddAfter("a", "a"));
        Assert.Throws<ArgumentException>(() => set.AddBefore("a", "a"));
    }

    /// <summary>
    /// MoveToFirst / MoveToLast / MoveToBefore / MoveToAfter 行为及异常
    /// </summary>
    [Test]
    public void TestMoveOperations() {
        LinkedHashSet<int> set = new();
        for (int i = 0; i < 5; i++) set.Add(i);

        set.MoveToFirst(3);
        Assert.AreEqual(new[] { 3, 0, 1, 2, 4 }, set.ToArray());

        set.MoveToLast(0);
        Assert.AreEqual(new[] { 3, 1, 2, 4, 0 }, set.ToArray());

        set.MoveToAfter(1, 4); // 1 移到 4 之后
        Assert.AreEqual(new[] { 3, 2, 4, 1, 0 }, set.ToArray());

        set.MoveToBefore(0, 3); // 0 移到 3 之前
        Assert.AreEqual(new[] { 0, 3, 2, 4, 1 }, set.ToArray());

        Assert.Throws<KeyNotFoundException>(() => set.MoveToFirst(99));
        Assert.Throws<KeyNotFoundException>(() => set.MoveToLast(99));
        Assert.Throws<KeyNotFoundException>(() => set.MoveToAfter(99, 0));
        Assert.Throws<KeyNotFoundException>(() => set.MoveToAfter(0, 99));
        Assert.Throws<InvalidOperationException>(() => set.MoveToAfter(0, 0));
        Assert.Throws<InvalidOperationException>(() => set.MoveToBefore(0, 0));
    }

    /// <summary>
    /// NextKey / PrevKey 链式遍历 + 不存在 key 抛异常
    /// </summary>
    [Test]
    public void TestNextPrevKey() {
        LinkedHashSet<string> set = new();
        set.Add("a");
        set.Add("b");
        set.Add("c");

        Assert.IsTrue(set.NextKey("a", out string n1));
        Assert.AreEqual("b", n1);
        Assert.IsTrue(set.NextKey("b", out string n2));
        Assert.AreEqual("c", n2);
        Assert.IsFalse(set.NextKey("c", out string _)); // 末尾

        Assert.IsTrue(set.PrevKey("c", out string p1));
        Assert.AreEqual("b", p1);
        Assert.IsTrue(set.PrevKey("b", out string p2));
        Assert.AreEqual("a", p2);
        Assert.IsFalse(set.PrevKey("a", out string _)); // 开头

        Assert.Throws<KeyNotFoundException>(() => set.NextKey("missing", out string _));
        Assert.Throws<KeyNotFoundException>(() => set.PrevKey("missing", out string _));
    }

    /// <summary>
    /// PeekFirst / PeekLast / RemoveFirst / RemoveLast 在空集合上的行为
    /// </summary>
    [Test]
    public void TestPeekRemoveOnEmpty() {
        LinkedHashSet<int> set = new();
        Assert.Throws<InvalidOperationException>(() => set.PeekFirst());
        Assert.Throws<InvalidOperationException>(() => set.PeekLast());
        Assert.Throws<InvalidOperationException>(() => set.RemoveFirst());
        Assert.Throws<InvalidOperationException>(() => set.RemoveLast());

        Assert.IsFalse(set.TryPeekFirst(out _));
        Assert.IsFalse(set.TryPeekLast(out _));
        Assert.IsFalse(set.TryRemoveFirst(out _));
        Assert.IsFalse(set.TryRemoveLast(out _));
    }

    /// <summary>
    /// PeekFirst / PeekLast / RemoveFirst / RemoveLast 在有元素时的正确性
    /// </summary>
    [Test]
    public void TestPeekRemoveFirstLast() {
        LinkedHashSet<int> set = new();
        for (int i = 1; i <= 4; i++) set.Add(i);

        Assert.AreEqual(1, set.PeekFirst());
        Assert.AreEqual(4, set.PeekLast());
        Assert.IsTrue(set.TryPeekFirst(out int f));
        Assert.AreEqual(1, f);
        Assert.IsTrue(set.TryPeekLast(out int l));
        Assert.AreEqual(4, l);

        Assert.AreEqual(1, set.RemoveFirst());
        Assert.AreEqual(4, set.RemoveLast());
        Assert.AreEqual(new[] { 2, 3 }, set.ToArray());

        Assert.IsTrue(set.TryRemoveFirst(out int rf));
        Assert.AreEqual(2, rf);
        Assert.IsTrue(set.TryRemoveLast(out int rl));
        Assert.AreEqual(3, rl);
        Assert.AreEqual(0, set.Count);
    }

    /// <summary>
    /// Reversed 视图、双重 Reversed
    /// </summary>
    [Test]
    public void TestReversedView() {
        LinkedHashSet<int> set = new();
        for (int i = 0; i < 5; i++) set.Add(i);

        ISequencedSet<int> rev = set.Reversed();
        List<int> revKeys = new();
        foreach (int v in rev) revKeys.Add(v);
        Assert.AreEqual(new[] { 4, 3, 2, 1, 0 }, revKeys);

        ISequencedSet<int> revRev = rev.Reversed();
        List<int> revRevKeys = new();
        foreach (int v in revRev) revRevKeys.Add(v);
        Assert.AreEqual(new[] { 0, 1, 2, 3, 4 }, revRevKeys);
    }

    /// <summary>
    /// CopyTo 正向 / 反向
    /// </summary>
    [Test]
    public void TestCopyTo() {
        LinkedHashSet<int> set = new();
        for (int i = 0; i < 5; i++) set.Add(i);

        int[] forward = new int[5];
        set.CopyTo(forward, 0);
        Assert.AreEqual(new[] { 0, 1, 2, 3, 4 }, forward);

        int[] reversed = new int[5];
        set.CopyTo(reversed, 0, true);
        Assert.AreEqual(new[] { 4, 3, 2, 1, 0 }, reversed);

        // 偏移
        int[] withOffset = new int[7];
        set.CopyTo(withOffset, 2);
        Assert.AreEqual(new[] { 0, 0, 0, 1, 2, 3, 4 }, withOffset);

        // 容量不足
        int[] tooSmall = new int[3];
        Assert.Throws<ArgumentException>(() => set.CopyTo(tooSmall, 0));
    }

    /// <summary>
    /// 迭代过程中修改集合应触发版本冲突
    /// </summary>
    [Test]
    public void TestEnumeratorVersionConflict() {
        LinkedHashSet<int> set = new();
        for (int i = 0; i < 5; i++) set.Add(i);

        var e1 = set.GetEnumerator();
        e1.MoveNext();
        set.Add(99);
        Assert.Throws<InvalidOperationException>(() => e1.MoveNext());

        var e2 = set.GetEnumerator();
        e2.MoveNext();
        set.Remove(99);
        Assert.Throws<InvalidOperationException>(() => e2.Reset());
    }

    /// <summary>
    /// IUnsafeIterator.Remove 在迭代中删除每隔一个元素
    /// </summary>
    [Test]
    public void TestUnsafeIteratorRemoveDuringIteration() {
        LinkedHashSet<int> set = new();
        for (int i = 0; i < 10; i++) set.Add(i);

        var it = set.GetEnumerator();
        bool deleteFlag = false;
        while (it.MoveNext()) {
            if (deleteFlag) {
                it.Remove();
            }
            deleteFlag = !deleteFlag;
        }
        Assert.AreEqual(new[] { 0, 2, 4, 6, 8 }, set.ToArray());
    }

    /// <summary>
    /// nullKey 完整支持：add、contains、remove、移动、迭代
    /// </summary>
    [Test]
    public void TestNullKeyFullSupport() {
        LinkedHashSet<string> set = new();
        set.Add("a");
        Assert.IsTrue(set.Add(null));
        set.Add("b");
        Assert.IsFalse(set.Add(null)); // 重复

        Assert.IsTrue(set.Contains(null));
        Assert.AreEqual(new[] { "a", null, "b" }, set.ToArray());

        set.MoveToFirst(null);
        Assert.AreEqual(new[] { null, "a", "b" }, set.ToArray());

        set.MoveToLast(null);
        Assert.AreEqual(new[] { "a", "b", null }, set.ToArray());

        Assert.IsTrue(set.Remove(null));
        Assert.IsFalse(set.Contains(null));
        Assert.AreEqual(2, set.Count);
    }

    /// <summary>
    /// 哈希冲突压力：所有 key 强制返回相同 hash，依旧能正确 contains/remove，且插入顺序保持
    /// </summary>
    [Test]
    public void TestHashCollisionStress() {
        LinkedHashSet<string> set = new(16, 0.5f, new ConstantHashComparer());
        const int n = 500;
        List<string> keys = new();
        for (int i = 0; i < n; i++) {
            string key = "key_" + i;
            keys.Add(key);
            set.Add(key);
        }
        Assert.AreEqual(n, set.Count);
        Assert.AreEqual(keys.ToArray(), set.ToArray());

        Random rng = new(20251205);
        for (int i = 0; i < 200; i++) {
            int idx = rng.Next(keys.Count);
            Assert.IsTrue(set.Contains(keys[idx]));
        }

        for (int i = 0; i < n; i += 2) {
            Assert.IsTrue(set.Remove(keys[i]));
        }
        List<string> expected = new();
        for (int i = 1; i < n; i += 2) expected.Add(keys[i]);
        Assert.AreEqual(expected, set.ToArray());
    }

    private sealed class ConstantHashComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y) => string.Equals(x, y);
        public int GetHashCode(string obj) => 0;
    }

    /// <summary>
    /// 多次 rehash 后插入顺序保持不变
    /// </summary>
    [Test]
    public void TestRehashPreservesOrder() {
        LinkedHashSet<int> set = new(2);
        const int n = 1000;
        for (int i = 0; i < n; i++) {
            set.Add(i);
        }
        int idx = 0;
        foreach (int v in set) {
            Assert.AreEqual(idx, v);
            idx++;
        }
        Assert.AreEqual(n, idx);
    }

    /// <summary>
    /// 从 IEnumerable 构造保持源序
    /// </summary>
    [Test]
    public void TestConstructFromEnumerable() {
        int[] src = { 5, 3, 9, 1, 7 };
        LinkedHashSet<int> set = new(src);
        Assert.AreEqual(src, set.ToArray());

        // 含重复 → 仅保留首次出现
        int[] withDup = { 1, 2, 1, 3, 2, 4 };
        LinkedHashSet<int> set2 = new(withDup);
        Assert.AreEqual(new[] { 1, 2, 3, 4 }, set2.ToArray());
    }

    /// <summary>
    /// Clear 后能正确重用
    /// </summary>
    [Test]
    public void TestClearAndReuse() {
        LinkedHashSet<int> set = new();
        for (int i = 0; i < 50; i++) set.Add(i);
        set.Clear();
        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
        Assert.IsFalse(set.Contains(0));
        Assert.IsFalse(set.TryPeekFirst(out _));

        set.Add(99);
        Assert.AreEqual(1, set.Count);
        Assert.AreEqual(99, set.PeekFirst());
        Assert.AreEqual(99, set.PeekLast());
    }

    /// <summary>
    /// 与 LinkedList+HashSet 参考实现的随机 Oracle 对照测试
    /// </summary>
    [Test]
    [Repeat(3)]
    public void TestOracleAgainstReferenceImpl() {
        const int rounds = 5000;
        Random rng = new(20260511);

        LinkedHashSet<int> set = new();
        LinkedList<int> orderRef = new();
        HashSet<int> presenceRef = new();

        for (int i = 0; i < rounds; i++) {
            int op = rng.Next(11);
            int key = rng.Next(100); // 小 key 域，制造大量重复

            switch (op) {
                case 0: { // Add
                    bool added = set.Add(key);
                    if (presenceRef.Add(key)) {
                        Assert.IsTrue(added);
                        orderRef.AddLast(key);
                    } else {
                        Assert.IsFalse(added);
                    }
                    break;
                }
                case 1: { // AddFirst (重排或插入)
                    bool inserted = set.AddFirst(key);
                    if (presenceRef.Contains(key)) {
                        Assert.IsFalse(inserted);
                        orderRef.Remove(key);
                    } else {
                        Assert.IsTrue(inserted);
                        presenceRef.Add(key);
                    }
                    orderRef.AddFirst(key);
                    break;
                }
                case 2: { // AddLast
                    bool inserted = set.AddLast(key);
                    if (presenceRef.Contains(key)) {
                        Assert.IsFalse(inserted);
                        orderRef.Remove(key);
                    } else {
                        Assert.IsTrue(inserted);
                        presenceRef.Add(key);
                    }
                    orderRef.AddLast(key);
                    break;
                }
                case 3: { // AddFirstIfAbsent (仅插入不重排)
                    bool inserted = set.AddFirstIfAbsent(key);
                    if (presenceRef.Add(key)) {
                        Assert.IsTrue(inserted);
                        orderRef.AddFirst(key);
                    } else {
                        Assert.IsFalse(inserted);
                    }
                    break;
                }
                case 4: { // AddLastIfAbsent
                    bool inserted = set.AddLastIfAbsent(key);
                    if (presenceRef.Add(key)) {
                        Assert.IsTrue(inserted);
                        orderRef.AddLast(key);
                    } else {
                        Assert.IsFalse(inserted);
                    }
                    break;
                }
                case 5: { // Remove
                    bool removed = set.Remove(key);
                    if (presenceRef.Remove(key)) {
                        Assert.IsTrue(removed);
                        orderRef.Remove(key);
                    } else {
                        Assert.IsFalse(removed);
                    }
                    break;
                }
                case 6: { // Contains
                    Assert.AreEqual(presenceRef.Contains(key), set.Contains(key));
                    break;
                }
                case 7: { // MoveToFirst
                    if (presenceRef.Contains(key)) {
                        set.MoveToFirst(key);
                        orderRef.Remove(key);
                        orderRef.AddFirst(key);
                    }
                    break;
                }
                case 8: { // MoveToLast
                    if (presenceRef.Contains(key)) {
                        set.MoveToLast(key);
                        orderRef.Remove(key);
                        orderRef.AddLast(key);
                    }
                    break;
                }
                case 9: { // RemoveFirst
                    if (orderRef.Count > 0) {
                        int removed = set.RemoveFirst();
                        int expected = orderRef.First!.Value;
                        Assert.AreEqual(expected, removed);
                        orderRef.RemoveFirst();
                        presenceRef.Remove(expected);
                    }
                    break;
                }
                case 10: { // RemoveLast
                    if (orderRef.Count > 0) {
                        int removed = set.RemoveLast();
                        int expected = orderRef.Last!.Value;
                        Assert.AreEqual(expected, removed);
                        orderRef.RemoveLast();
                        presenceRef.Remove(expected);
                    }
                    break;
                }
            }
        }

        Assert.AreEqual(orderRef.Count, set.Count);
        Assert.AreEqual(orderRef.ToArray(), set.ToArray());
    }
}