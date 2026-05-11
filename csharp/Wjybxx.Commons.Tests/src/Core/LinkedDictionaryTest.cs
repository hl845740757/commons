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

public class LinkedDictionaryTest
{
    [Test]
    [Repeat(5)]
    public void TestIntDic() {
        int expectedCount = 10000;
        HashSet<int> keySet = new HashSet<int>(expectedCount);
        List<int> keyList = new List<int>(expectedCount);
        LinkedDictionary<int, string> dictionary = new LinkedDictionary<int, string>(expectedCount / 3); // 顺便测试扩容

        // 在插入期间随机删除已存在的key；不宜太频繁，否则keyList的移动开销太大
        while (keySet.Count < expectedCount) {
            if (Random.Shared.Next(0, 10) == 1 && keyList.Count > expectedCount / 3) {
                int idx = Random.Shared.Next(0, keyList.Count);
                int key = keyList[idx];
                keyList.RemoveAt(idx);
                keySet.Remove(key);
                dictionary.Remove(key, out _);
                continue;
            }
            var next = Random.Shared.Next();
            if (keySet.Add(next)) {
                keyList.Add(next);
                dictionary[next] = next.ToString();
            }
        }
        Assert.That(keySet.Count, Is.EqualTo(keyList.Count));
        Assert.That(dictionary.Count, Is.EqualTo(keyList.Count));

        int index = 0;
        foreach (KeyValuePair<int, string> pair in dictionary) {
            int expectedKey = keyList[index++];
            int realKey = pair.Key;
            Assert.That(realKey, Is.EqualTo(expectedKey));
        }
    }

    [Test]
    [Repeat(5)]
    public void TestStringDic1() {
        TestStringDic(10000);
    }

    [Test]
    [Repeat(5)]
    public void TestStringDic2() {
        TestStringDic(100000);
    }

    private static LinkedDictionary<string, string> TestStringDic(int expectedCount) {
        LinkedDictionary<string, string> dictionary = new LinkedDictionary<string, string>(expectedCount / 3); // 顺便测试扩容
        byte[] buffer = new byte[12];
        List<string> keyList = new List<string>(expectedCount);
        while (dictionary.Count < expectedCount) {
            Random.Shared.NextBytes(buffer);
            string next = Convert.ToHexString(buffer);
            string key = Random.Shared.Next(0, 10) == 0 ? null : next; // 随机使用nullKey
            // 还需要测试AddFirst
            if (dictionary.TryAdd(key, next)) {
                keyList.Add(key);
            }
            // 随机删除元素 30%概率
            if (dictionary.Count > expectedCount / 2) {
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
                    dictionary.Remove(remKey);
                }
            }
        }

        Assert.That(dictionary.Count, Is.EqualTo(keyList.Count));
        // 顺序迭代测试
        int index = 0;
        foreach (var realKey in dictionary.Keys) {
            var expectedKey = keyList[index++];
            if (expectedKey != realKey) {
                throw new InvalidOperationException($"expectedKey:{expectedKey} == realKey:{realKey}");
            }
        }
        // 逆序迭代测试
        index = keyList.Count - 1;
        var reversedEnumerator = dictionary.SequencedKeys().GetReversedEnumerator();
        while (reversedEnumerator.MoveNext()) {
            var expectedKey = keyList[index--];
            string realKey = reversedEnumerator.Current;
            if (expectedKey != realKey) {
                throw new InvalidOperationException($"expectedKey:{expectedKey} == realKey:{realKey}");
            }
        }
        return dictionary;
    }

    [Test]
    public void TestAdjustCapacity() {
        LinkedDictionary<string, string> dictionary = TestStringDic(10000);
        var rawArray = dictionary.ToArray();
        {
            dictionary.EnsureCapacity(15000);
            var copiedArray1 = dictionary.ToArray();
            ArrayUtil.Equals(rawArray, copiedArray1);
        }
        {
            dictionary.EnsureCapacity(10001);
            var copiedArray2 = dictionary.ToArray();
            ArrayUtil.Equals(rawArray, copiedArray2);
        }
        {
            dictionary.EnsureCapacity(10000);
            var copiedArray3 = dictionary.ToArray();
            ArrayUtil.Equals(rawArray, copiedArray3);
        }
    }

    [Test]
    public void NullKeyTest() {
        LinkedDictionary<string, string> dictionary = new LinkedDictionary<string, string>(3);
        string value = "wjybxx";
        dictionary[null] = value;
        dictionary["key1"] = "key1";
        dictionary["key2"] = "key2";
        Assert.That(dictionary[null], Is.EqualTo(value));

        Assert.True(dictionary.NextKey(null, out string nextKey));
        Assert.That(nextKey, Is.EqualTo("key1"));

        Assert.True(dictionary.NextKey("key1", out nextKey));
        Assert.That(nextKey, Is.EqualTo("key2"));

        dictionary.Remove(null);
        Assert.That(dictionary.PeekFirstKey(), Is.EqualTo("key1"));
    }
    
    
    [Test]
    public void TestMoveToFirst() {
        const int expectedCount = 10;
        List<int> keyList = new List<int>(expectedCount);
        LinkedDictionary<int, int> keySet = new LinkedDictionary<int, int>(expectedCount);
        // 连续的List更利于观察
        for (int i = 0; i < expectedCount; i++) {
            keyList.Add(i);
            keySet.Add(i, i);
        }
        for (int i = 0; i < expectedCount * 10; i++) {
            int idx1 = Random.Shared.Next(expectedCount);
            int key1 = keyList[idx1];
            
            keyList.RemoveAt(idx1);
            if (Random.Shared.NextBool()) {
                keyList.Insert(0, key1); // addFirst
                keySet.PutFirst(key1, key1);
            } else {
                keyList.Add(key1); // addLast
                keySet.PutLast(key1, key1);
            }
        }
        int index = 0;
        foreach (int realKey in keySet.Keys) {
            int expectedKey = keyList[index++];
            Assert.That(realKey, Is.EqualTo(expectedKey));
        }
    }
    
    [Test]
    public void TestMoveAfter() {
        const int expectedCount = 100;
        List<int> keyList = new List<int>(expectedCount);
        LinkedDictionary<int, int> keySet = new (expectedCount);
        while (keySet.Count < expectedCount) {
            int next = Random.Shared.Next();
            if (keySet.Put(next, next).IsInsert) {
                keyList.Add(next);
            }
        }
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
        foreach (int realKey in keySet.Keys) {
            int expectedKey = keyList[index++];
            Assert.That(realKey, Is.EqualTo(expectedKey));
        }
    }

    // ============= 补充：API 语义 / 复杂场景测试 =============

    /// <summary>
    /// PutResult 语义：Insert 返回 IsInsert=true & PrevValue=default；Update 返回 false & PrevValue=旧值
    /// </summary>
    [Test]
    public void TestPutResultSemantics() {
        LinkedDictionary<string, int> dic = new();
        PutResult<int> r1 = dic.Put("a", 1);
        Assert.IsTrue(r1.IsInsert);
        Assert.AreEqual(0, r1.PrevValue);

        PutResult<int> r2 = dic.Put("a", 99);
        Assert.IsFalse(r2.IsInsert);
        Assert.AreEqual(1, r2.PrevValue);
        Assert.AreEqual(99, dic["a"]);

        // PutFirst/PutLast 在 key 已存在时同样应记录旧值
        PutResult<int> r3 = dic.PutFirst("a", 100);
        Assert.IsFalse(r3.IsInsert);
        Assert.AreEqual(99, r3.PrevValue);
    }

    /// <summary>
    /// Add/AddFirst/AddLast 在 key 重复时抛 InvalidOperationException；TryAdd 返回 false 且不修改值
    /// </summary>
    [Test]
    public void TestAddDuplicateThrows() {
        LinkedDictionary<string, int> dic = new();
        dic.Add("a", 1);
        Assert.Throws<InvalidOperationException>(() => dic.Add("a", 2));
        Assert.Throws<InvalidOperationException>(() => dic.AddFirst("a", 3));
        Assert.Throws<InvalidOperationException>(() => dic.AddLast("a", 4));

        Assert.IsFalse(dic.TryAdd("a", 5));
        Assert.IsFalse(dic.TryAddFirst("a", 6));
        Assert.IsFalse(dic.TryAddLast("a", 7));
        Assert.AreEqual(1, dic["a"]); // 值未被修改
        Assert.AreEqual(1, dic.Count);
    }

    /// <summary>
    /// PutFirst / PutLast 对已存在的 key 既要更新值，又要把节点移动到首/尾
    /// </summary>
    [Test]
    public void TestPutFirstLastReorderExisting() {
        LinkedDictionary<string, int> dic = new();
        dic.Add("a", 1);
        dic.Add("b", 2);
        dic.Add("c", 3);

        // PutFirst("b", 20) → b 移到头部，值变为 20
        dic.PutFirst("b", 20);
        Assert.AreEqual(new[] { "b", "a", "c" }, dic.Keys.ToArray());
        Assert.AreEqual(20, dic["b"]);

        // PutLast("a", 10) → a 移到尾部，值变为 10
        dic.PutLast("a", 10);
        Assert.AreEqual(new[] { "b", "c", "a" }, dic.Keys.ToArray());
        Assert.AreEqual(10, dic["a"]);
    }

    /// <summary>
    /// AddBefore / AddAfter 的顺序及异常行为
    /// </summary>
    [Test]
    public void TestAddBeforeAfter() {
        LinkedDictionary<string, int> dic = new();
        dic.Add("a", 1);
        dic.Add("c", 3);

        dic.AddAfter("b", 2, "a"); // a, b, c
        Assert.AreEqual(new[] { "a", "b", "c" }, dic.Keys.ToArray());

        dic.AddBefore("z", 26, "c"); // a, b, z, c
        Assert.AreEqual(new[] { "a", "b", "z", "c" }, dic.Keys.ToArray());

        // 目标 key 不存在
        Assert.Throws<KeyNotFoundException>(() => dic.AddAfter("x", 0, "missing"));
        Assert.Throws<KeyNotFoundException>(() => dic.AddBefore("x", 0, "missing"));

        // key == targetKey
        Assert.Throws<ArgumentException>(() => dic.AddAfter("a", 0, "a"));
        Assert.Throws<ArgumentException>(() => dic.AddBefore("a", 0, "a"));

        // 重复 key
        Assert.Throws<InvalidOperationException>(() => dic.AddAfter("a", 0, "b"));
    }

    /// <summary>
    /// PutBefore / PutAfter 已存在的 key 应当移动并更新；并且应返回正确的 PutResult
    /// </summary>
    [Test]
    public void TestPutBeforeAfterMovesExisting() {
        LinkedDictionary<string, int> dic = new();
        dic.Add("a", 1);
        dic.Add("b", 2);
        dic.Add("c", 3);
        dic.Add("d", 4);

        // 把 d 移动到 a 之后并更新值
        PutResult<int> r1 = dic.PutAfter("d", 40, "a");
        Assert.IsFalse(r1.IsInsert);
        Assert.AreEqual(4, r1.PrevValue);
        Assert.AreEqual(new[] { "a", "d", "b", "c" }, dic.Keys.ToArray());
        Assert.AreEqual(40, dic["d"]);

        // 新增 e 在 b 之前
        PutResult<int> r2 = dic.PutBefore("e", 5, "b");
        Assert.IsTrue(r2.IsInsert);
        Assert.AreEqual(new[] { "a", "d", "e", "b", "c" }, dic.Keys.ToArray());

        Assert.Throws<KeyNotFoundException>(() => dic.PutAfter("x", 0, "missing"));
        Assert.Throws<ArgumentException>(() => dic.PutBefore("a", 0, "a"));
    }

    /// <summary>
    /// LRU 模式：GetAndMoveToFirst/Last 必须把已存在 key 移到端点；不存在 key 抛异常
    /// </summary>
    [Test]
    public void TestGetAndMoveToFirstLast() {
        LinkedDictionary<string, int> dic = new();
        dic.Add("a", 1);
        dic.Add("b", 2);
        dic.Add("c", 3);

        Assert.AreEqual(2, dic.GetAndMoveToFirst("b"));
        Assert.AreEqual(new[] { "b", "a", "c" }, dic.Keys.ToArray());

        Assert.AreEqual(1, dic.GetAndMoveToLast("a"));
        Assert.AreEqual(new[] { "b", "c", "a" }, dic.Keys.ToArray());

        Assert.Throws<KeyNotFoundException>(() => dic.GetAndMoveToFirst("missing"));
        Assert.Throws<KeyNotFoundException>(() => dic.GetAndMoveToLast("missing"));

        Assert.IsFalse(dic.TryGetAndMoveToFirst("missing", out int _));
        Assert.IsTrue(dic.TryGetAndMoveToLast("b", out int v));
        Assert.AreEqual(2, v);
        Assert.AreEqual(new[] { "c", "a", "b" }, dic.Keys.ToArray());
    }

    /// <summary>
    /// Reversed 视图与 Reversed().Reversed() 应等价于原序
    /// </summary>
    [Test]
    public void TestReversedView() {
        LinkedDictionary<int, int> dic = new();
        for (int i = 0; i < 5; i++) dic.Add(i, i * 10);

        ISequencedDictionary<int, int> rev = dic.Reversed();
        List<int> revKeys = new();
        foreach (var pair in rev) revKeys.Add(pair.Key);
        Assert.AreEqual(new[] { 4, 3, 2, 1, 0 }, revKeys);

        ISequencedDictionary<int, int> revRev = rev.Reversed();
        List<int> revRevKeys = new();
        foreach (var pair in revRev) revRevKeys.Add(pair.Key);
        Assert.AreEqual(new[] { 0, 1, 2, 3, 4 }, revRevKeys);
    }

    /// <summary>
    /// NextKey / PrevKey 完整链式遍历，并验证带 value 的重载
    /// </summary>
    [Test]
    public void TestNextPrevKeyChain() {
        LinkedDictionary<string, int> dic = new();
        dic.Add("a", 1);
        dic.Add("b", 2);
        dic.Add("c", 3);

        // 正向链
        Assert.IsTrue(dic.NextKey("a", out string n1, out int v1));
        Assert.AreEqual("b", n1);
        Assert.AreEqual(2, v1);
        Assert.IsTrue(dic.NextKey("b", out string n2, out int v2));
        Assert.AreEqual("c", n2);
        Assert.AreEqual(3, v2);
        Assert.IsFalse(dic.NextKey("c", out string _, out int _)); // 末尾

        // 反向链
        Assert.IsTrue(dic.PrevKey("c", out string p1, out int pv1));
        Assert.AreEqual("b", p1);
        Assert.AreEqual(2, pv1);
        Assert.IsTrue(dic.PrevKey("b", out string p2));
        Assert.AreEqual("a", p2);
        Assert.IsFalse(dic.PrevKey("a", out string _)); // 开头

        // 不存在的 key
        Assert.Throws<KeyNotFoundException>(() => dic.NextKey("missing", out string _));
        Assert.Throws<KeyNotFoundException>(() => dic.PrevKey("missing", out string _));
    }

    /// <summary>
    /// Peek/Remove on empty 抛 InvalidOperationException；Try* 返回 false
    /// </summary>
    [Test]
    public void TestPeekRemoveOnEmpty() {
        LinkedDictionary<int, int> dic = new();
        Assert.Throws<InvalidOperationException>(() => dic.PeekFirst());
        Assert.Throws<InvalidOperationException>(() => dic.PeekLast());
        Assert.Throws<InvalidOperationException>(() => dic.PeekFirstKey());
        Assert.Throws<InvalidOperationException>(() => dic.PeekLastKey());
        Assert.Throws<InvalidOperationException>(() => dic.RemoveFirst());
        Assert.Throws<InvalidOperationException>(() => dic.RemoveLast());

        Assert.IsFalse(dic.TryPeekFirst(out _));
        Assert.IsFalse(dic.TryPeekLast(out _));
        Assert.IsFalse(dic.TryRemoveFirst(out _));
        Assert.IsFalse(dic.TryRemoveLast(out _));
    }

    /// <summary>
    /// 迭代过程中修改集合应触发版本冲突异常
    /// </summary>
    [Test]
    public void TestEnumeratorVersionConflict() {
        LinkedDictionary<int, int> dic = new();
        for (int i = 0; i < 5; i++) dic.Add(i, i);

        var e1 = dic.GetEnumerator();
        e1.MoveNext();
        dic.Add(99, 99); // 修改字典
        Assert.Throws<InvalidOperationException>(() => e1.MoveNext());

        var e2 = dic.GetEnumerator();
        e2.MoveNext();
        dic[0] = 100; // 仅修改值不动结构 → 不影响版本
        Assert.DoesNotThrow(() => e2.MoveNext());

        var e3 = dic.GetEnumerator();
        e3.MoveNext();
        dic.Remove(99);
        Assert.Throws<InvalidOperationException>(() => e3.Reset());
    }

    /// <summary>
    /// IUnsafeIterator.Remove 在迭代中删除每隔一个元素，剩余元素相对顺序应保持
    /// </summary>
    [Test]
    public void TestUnsafeIteratorRemoveDuringIteration() {
        LinkedDictionary<int, int> dic = new();
        for (int i = 0; i < 10; i++) dic.Add(i, i);

        var it = dic.GetEnumerator();
        bool deleteFlag = false;
        while (it.MoveNext()) {
            if (deleteFlag) {
                it.Remove();
            }
            deleteFlag = !deleteFlag;
        }

        // 删除 1,3,5,7,9，剩 0,2,4,6,8
        Assert.AreEqual(new[] { 0, 2, 4, 6, 8 }, dic.Keys.ToArray());
    }

    /// <summary>
    /// 哈希冲突压力：所有 key 强制返回相同 hash，依旧能正确 Get/Remove，且插入顺序保持
    /// </summary>
    [Test]
    public void TestHashCollisionStress() {
        LinkedDictionary<string, int> dic = new(16, 0.5f, new ConstantHashComparer());
        const int n = 500;
        List<string> keys = new();
        for (int i = 0; i < n; i++) {
            string key = "key_" + i;
            keys.Add(key);
            dic.Add(key, i);
        }
        Assert.AreEqual(n, dic.Count);

        // 顺序保持
        Assert.AreEqual(keys.ToArray(), dic.Keys.ToArray());

        // 随机访问/删除依旧正确
        Random rng = new(20251205);
        for (int i = 0; i < 200; i++) {
            int idx = rng.Next(keys.Count);
            string k = keys[idx];
            Assert.IsTrue(dic.ContainsKey(k));
            Assert.AreEqual(int.Parse(k.Substring(4)), dic[k]);
        }

        // 删除一半，剩余顺序仍正确
        for (int i = 0; i < n; i += 2) {
            Assert.IsTrue(dic.Remove(keys[i]));
        }
        List<string> expected = new();
        for (int i = 1; i < n; i += 2) expected.Add(keys[i]);
        Assert.AreEqual(expected, dic.Keys.ToArray());
    }

    private sealed class ConstantHashComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y) => string.Equals(x, y);
        public int GetHashCode(string obj) => 0; // 全部冲突
    }

    /// <summary>
    /// 多次 rehash 后插入顺序保持不变
    /// </summary>
    [Test]
    public void TestRehashPreservesOrder() {
        LinkedDictionary<int, int> dic = new(2);
        const int n = 1000;
        for (int i = 0; i < n; i++) {
            dic.Add(i, i);
        }
        int idx = 0;
        foreach (var pair in dic) {
            Assert.AreEqual(idx, pair.Key);
            Assert.AreEqual(idx, pair.Value);
            idx++;
        }
        Assert.AreEqual(n, idx);
    }

    /// <summary>
    /// 从 IDictionary 构造保持插入顺序（依据源 dictionary 的迭代顺序）
    /// </summary>
    [Test]
    public void TestConstructFromIDictionary() {
        // 使用 LinkedDictionary 作为来源，确保来源本身有序
        LinkedDictionary<int, int> src = new();
        for (int i = 10; i >= 1; i--) src.Add(i, i * 100);

        LinkedDictionary<int, int> copy = new(src);
        Assert.AreEqual(src.Count, copy.Count);
        Assert.AreEqual(src.Keys.ToArray(), copy.Keys.ToArray());
        foreach (var pair in src) {
            Assert.AreEqual(pair.Value, copy[pair.Key]);
        }
    }

    /// <summary>
    /// Clear 后能正确重用，且不会残留状态
    /// </summary>
    [Test]
    public void TestClearAndReuse() {
        LinkedDictionary<int, int> dic = new();
        for (int i = 0; i < 50; i++) dic.Add(i, i);
        dic.Clear();
        Assert.AreEqual(0, dic.Count);
        Assert.IsTrue(dic.IsEmpty);
        Assert.IsFalse(dic.ContainsKey(0));
        Assert.IsFalse(dic.TryPeekFirst(out _));

        // 重用
        dic.Add(99, 99);
        Assert.AreEqual(1, dic.Count);
        Assert.AreEqual(99, dic.PeekFirstKey());
        Assert.AreEqual(99, dic.PeekLastKey());
    }

    /// <summary>
    /// 与 LinkedList+Dictionary 参考实现的随机 Oracle 对照测试，覆盖大量 API 组合
    /// </summary>
    [Test]
    [Repeat(3)]
    public void TestOracleAgainstReferenceImpl() {
        const int rounds = 5000;
        Random rng = new(20260511);

        LinkedDictionary<int, int> dic = new();
        // 参考实现：LinkedList 维护顺序，Dictionary 维护值
        LinkedList<int> orderRef = new();
        Dictionary<int, int> valueRef = new();

        for (int i = 0; i < rounds; i++) {
            int op = rng.Next(11);
            int key = rng.Next(100); // 小 key 域，制造大量重复
            int val = rng.Next();

            switch (op) {
                case 0: { // Put
                    var r = dic.Put(key, val);
                    if (valueRef.ContainsKey(key)) {
                        Assert.IsFalse(r.IsInsert);
                        Assert.AreEqual(valueRef[key], r.PrevValue);
                    } else {
                        Assert.IsTrue(r.IsInsert);
                        orderRef.AddLast(key);
                    }
                    valueRef[key] = val;
                    break;
                }
                case 1: { // PutFirst
                    var r = dic.PutFirst(key, val);
                    if (valueRef.ContainsKey(key)) {
                        Assert.IsFalse(r.IsInsert);
                        orderRef.Remove(key);
                    } else {
                        Assert.IsTrue(r.IsInsert);
                    }
                    orderRef.AddFirst(key);
                    valueRef[key] = val;
                    break;
                }
                case 2: { // PutLast
                    var r = dic.PutLast(key, val);
                    if (valueRef.ContainsKey(key)) {
                        Assert.IsFalse(r.IsInsert);
                        orderRef.Remove(key);
                    } else {
                        Assert.IsTrue(r.IsInsert);
                    }
                    orderRef.AddLast(key);
                    valueRef[key] = val;
                    break;
                }
                case 3: { // Remove
                    bool removed = dic.Remove(key);
                    if (valueRef.Remove(key)) {
                        Assert.IsTrue(removed);
                        orderRef.Remove(key);
                    } else {
                        Assert.IsFalse(removed);
                    }
                    break;
                }
                case 4: { // ContainsKey
                    Assert.AreEqual(valueRef.ContainsKey(key), dic.ContainsKey(key));
                    break;
                }
                case 5: { // GetAndMoveToFirst (LRU)
                    if (valueRef.ContainsKey(key)) {
                        int v = dic.GetAndMoveToFirst(key);
                        Assert.AreEqual(valueRef[key], v);
                        orderRef.Remove(key);
                        orderRef.AddFirst(key);
                    } else {
                        Assert.Throws<KeyNotFoundException>(() => dic.GetAndMoveToFirst(key));
                    }
                    break;
                }
                case 6: { // GetAndMoveToLast
                    if (valueRef.ContainsKey(key)) {
                        int v = dic.GetAndMoveToLast(key);
                        Assert.AreEqual(valueRef[key], v);
                        orderRef.Remove(key);
                        orderRef.AddLast(key);
                    } else {
                        Assert.Throws<KeyNotFoundException>(() => dic.GetAndMoveToLast(key));
                    }
                    break;
                }
                case 7: { // MoveToFirst
                    if (valueRef.ContainsKey(key)) {
                        dic.MoveToFirst(key);
                        orderRef.Remove(key);
                        orderRef.AddFirst(key);
                    }
                    break;
                }
                case 8: { // MoveToLast
                    if (valueRef.ContainsKey(key)) {
                        dic.MoveToLast(key);
                        orderRef.Remove(key);
                        orderRef.AddLast(key);
                    }
                    break;
                }
                case 9: { // RemoveFirst
                    if (orderRef.Count > 0) {
                        var pair = dic.RemoveFirst();
                        int expectedKey = orderRef.First!.Value;
                        Assert.AreEqual(expectedKey, pair.Key);
                        Assert.AreEqual(valueRef[expectedKey], pair.Value);
                        orderRef.RemoveFirst();
                        valueRef.Remove(expectedKey);
                    }
                    break;
                }
                case 10: { // RemoveLast
                    if (orderRef.Count > 0) {
                        var pair = dic.RemoveLast();
                        int expectedKey = orderRef.Last!.Value;
                        Assert.AreEqual(expectedKey, pair.Key);
                        Assert.AreEqual(valueRef[expectedKey], pair.Value);
                        orderRef.RemoveLast();
                        valueRef.Remove(expectedKey);
                    }
                    break;
                }
            }
        }

        // 终态比对
        Assert.AreEqual(orderRef.Count, dic.Count);
        Assert.AreEqual(orderRef.ToArray(), dic.Keys.ToArray());
        foreach (int k in orderRef) {
            Assert.AreEqual(valueRef[k], dic[k]);
        }
    }
}