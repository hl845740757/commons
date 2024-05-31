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
using NUnit.Framework;
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
            var next = Random.Shared.Next();
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
            if (linkedHashSet.Add(key)) {
                keyList.Add(key);
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
        LinkedHashSet<string> dictionary = TestStringSet(10000);
        dictionary.AdjustCapacity(15000);
        dictionary.AdjustCapacity(10001);
        dictionary.AdjustCapacity(10000);
    }
}