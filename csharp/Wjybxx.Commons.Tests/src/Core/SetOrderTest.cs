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
/// 结论：只插入的情况下HashSet保持了插入序（已存在的元素不后移），<see cref="ImmutableHashSet"/>不能保持原来的数据。
/// </summary>
public class SetOrderTest
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
}