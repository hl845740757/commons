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
using System.Collections.Immutable;
using NUnit.Framework;

namespace Commons.Tests.Core;

/// <summary>
/// 测试<see cref="HashSet{T}"/>和ImmutableSet是否保持插入序（只插入的情况下）
///
/// 1. <see cref="HashSet{T}"/>保持插入序。
/// 2. <see cref="ImmutableHashSet{T}"/>不能保持插入序 -- 不能保持原始Set序。
/// 3. <see cref="Dictionary{TKey,TValue}"/>保持插入序。
/// 4. <see cref="ImmutableDictionary{TKey,TValue}"/>不能保持插入序 -- 不能保持原始字典序。
/// </summary>
public class CollectionOrderTest
{
    [Test]
    public void TestHashSet() {
        int expectedCount = 10000;
        HashSet<int> keySet = new HashSet<int>(expectedCount / 6); // 要测试扩容
        List<int> keyList = new List<int>(expectedCount / 6);

        while (keySet.Count < expectedCount) {
            var next = Random.Shared.Next();
            if (keySet.Add(next)) {
                keyList.Add(next);
            }
        }
        Assert.That(keySet.Count, Is.EqualTo(keyList.Count));

        int index = 0;
        foreach (int realKey in keySet) {
            int expectedKey = keyList[index++];
            Assert.That(realKey, Is.EqualTo(expectedKey));
        }
    }

    /** 不成立 */
    // [Test]
    public void TestImmutableSet() {
        int expectedCount = 10000;
        HashSet<int> keySet = new HashSet<int>(expectedCount / 6); // 要测试扩容
        while (keySet.Count < expectedCount) {
            var next = Random.Shared.Next();
            keySet.Add(next);
        }

        HashSet<int>.Enumerator rawItr = keySet.GetEnumerator();
        ImmutableHashSet<int>.Enumerator immutableItr = keySet.ToImmutableHashSet().GetEnumerator();
        while (rawItr.MoveNext()) {
            immutableItr.MoveNext();
            Assert.That(rawItr.Current, Is.EqualTo(immutableItr.Current));
        }
    }

    [Test]
    public void TestDictionary() {
        int expectedCount = 10000;
        List<int> keyList = new List<int>(expectedCount);
        Dictionary<int, int> dictionary = new Dictionary<int, int>(expectedCount / 6); // 顺便测试扩容
        while (dictionary.Count < expectedCount) {
            var next = Random.Shared.Next();
            if (dictionary.TryAdd(next, next)) {
                keyList.Add(next);
            }
        }
        Assert.That(dictionary.Count, Is.EqualTo(keyList.Count));

        int index = 0;
        foreach (KeyValuePair<int, int> pair in dictionary) {
            int expectedKey = keyList[index++];
            int realKey = pair.Key;
            Assert.That(realKey, Is.EqualTo(expectedKey));
        }
    }

    // [Test]
    public void TestImmutableDic() {
        int expectedCount = 10000;
        Dictionary<int, int> keySet = new Dictionary<int, int>(expectedCount / 6); // 要测试扩容
        while (keySet.Count < expectedCount) {
            var next = Random.Shared.Next();
            keySet.TryAdd(next, next);
        }

        var rawItr = keySet.GetEnumerator();
        var immutableItr = keySet.ToImmutableDictionary().GetEnumerator();
        while (rawItr.MoveNext()) {
            immutableItr.MoveNext();
            Assert.That(rawItr.Current, Is.EqualTo(immutableItr.Current));
        }
    }
}