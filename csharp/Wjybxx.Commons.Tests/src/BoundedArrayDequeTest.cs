#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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

using NUnit.Framework;
using Wjybxx.Commons.Collections;

namespace Commons.Tests;

public class BoundedArrayDequeTest
{
    private const int QueueCapacity = 5;
    private const int NumberCount = QueueCapacity * 3;

    private static List<int> RandomNumbers() {
        List<int> numbers = new List<int>(NumberCount);
        for (int i = 0; i < NumberCount; i++) {
            numbers.Add(Random.Shared.Next());
        }
        return numbers;
    }

    [Test]
    public void DequeTest() {
        List<int> numbers = RandomNumbers();
        BoundedArrayDeque<int> deque = new BoundedArrayDeque<int>(QueueCapacity);
        for (int i = 0; i < QueueCapacity; i++) {
            deque.AddLast(numbers[i]);
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
            int index = QueueCapacity - 1;
            IEnumerator<int> enumerator = deque.GetReversedEnumerator();
            while (enumerator.MoveNext()) {
                int number = enumerator.Current;
                Assert.That(number, Is.EqualTo(numbers[index--]));
            }
        }
    }

    [Test]
    public void QueueTest() {
        List<int> numbers = RandomNumbers();
        BoundedArrayDeque<int> deque = new BoundedArrayDeque<int>(QueueCapacity);
        for (int i = 0; i < QueueCapacity; i++) {
            deque.Enqueue(numbers[i]);
        }
        // 一边删除一边插入
        for (int i = 0; i < NumberCount; i++) {
            int value = deque.Dequeue();
            Assert.That(value, Is.EqualTo(numbers[i]));
            // 
            if (i + QueueCapacity < NumberCount) {
                deque.Enqueue(numbers[i + QueueCapacity]);
            }
        }
    }

    /** 应该等于后5个 */
    [Test]
    public void QueueOverflowDiscardHeadTest() {
        List<int> numbers = RandomNumbers();
        BoundedArrayDeque<int> deque = new BoundedArrayDeque<int>(QueueCapacity, DequeOverflowBehavior.DiscardHead);
        foreach (int number in numbers) {
            deque.Enqueue(number);
        }
        for (int i = (NumberCount - QueueCapacity); i < NumberCount; i++) {
            int value = deque.Dequeue();
            Assert.That(value, Is.EqualTo(numbers[i]));
        }
    }

    /** 应该等于后5个的逆序 */
    [Test]
    public void QueueOverFlowDiscardTailTest() {
        List<int> numbers = RandomNumbers();
        BoundedArrayDeque<int> deque = new BoundedArrayDeque<int>(QueueCapacity, DequeOverflowBehavior.DiscardTail);
        for (int i = 0; i < QueueCapacity; i++) {
            deque.Enqueue(numbers[i]);
        }
        for (int i = QueueCapacity; i < NumberCount; i++) {
            deque.AddFirst(numbers[i]);
        }
        int[] queueReversedElements = deque.Reversed().ToArray();
        int[] numberElements = numbers.GetRange(NumberCount - QueueCapacity, QueueCapacity).ToArray();
        Assert.True(queueReversedElements.SequenceEqual(numberElements));
    }

    /// <summary>
    /// 普通的栈
    /// </summary>
    [Test]
    public void StackTest() {
        List<int> numbers = RandomNumbers();
        BoundedArrayDeque<int> deque = new BoundedArrayDeque<int>(QueueCapacity);
        for (int i = 0; i < QueueCapacity; i++) {
            deque.Push(numbers[i]);
        }
        List<int> stackOrder = numbers.GetRange(0, QueueCapacity);
        stackOrder.Reverse();
        for (int i = 0; i < QueueCapacity; i++) {
            int value = deque.Pop();
            Assert.That(value, Is.EqualTo(stackOrder[i]));
        }
    }
}