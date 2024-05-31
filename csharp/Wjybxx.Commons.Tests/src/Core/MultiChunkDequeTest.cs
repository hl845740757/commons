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
using Wjybxx.Commons.Collections;

namespace Commons.Tests.Core;

public class MultiChunkDequeTest
{
    private const int NumberCount = 64;

    private static List<int> RandomNumbers() {
        // 去重，避免删除元素时导致的不稳定性
        ISet<int> numbers = new HashSet<int>(NumberCount);
        while (numbers.Count < NumberCount) {
            numbers.Add(Random.Shared.Next());
        }
        return new List<int>(numbers);
    }

    [Repeat(10)]
    [Test]
    public void DequeTest() {
        List<int> numbers = RandomNumbers();
        MultiChunkDeque<int> deque = new MultiChunkDeque<int>(4, 2);
        foreach (int number in numbers) {
            deque.AddLast(number);
        }
        // 随机删除X个元素，不为整倍数
        int delCount = (NumberCount / 2) - 1;
        for (int i = 0; i < delCount; i++) {
            int idx = Random.Shared.Next(numbers.Count);
            int value = numbers[idx];
            numbers.RemoveAt(idx);
            deque.Remove(value);
        }

        // 顺序迭代
        {
            int index = 0;
            IEnumerator<int> enumerator = deque.GetEnumerator();
            while (enumerator.MoveNext()) {
                int number = enumerator.Current;
                Assert.That(number, Is.EqualTo(numbers[index++]));
            }
        }
        // 逆序迭代
        {
            int index = numbers.Count - 1;
            IEnumerator<int> enumerator = deque.GetReversedEnumerator();
            while (enumerator.MoveNext()) {
                int number = enumerator.Current;
                Assert.That(number, Is.EqualTo(numbers[index--]));
            }
        }
        // ToArray的正确性
        int[] queueElements = deque.ToArray();
        Assert.True(queueElements.SequenceEqual(numbers));

        queueElements = deque.Reversed().ToArray();
        numbers.Reverse();
        Assert.True(queueElements.SequenceEqual(numbers));
    }
}