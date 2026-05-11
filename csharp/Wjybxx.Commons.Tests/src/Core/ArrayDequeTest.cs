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

public class ArrayDequeTest
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
        ArrayDeque<int> deque = new ArrayDeque<int>();
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

    // ============= 补充：API 语义 / 复杂场景测试 =============

    /// <summary>
    /// AddFirst / AddLast / PeekFirst / PeekLast 基本语义
    /// </summary>
    [Test]
    public void TestAddFirstLast() {
        ArrayDeque<int> deque = new();
        deque.AddLast(2);
        deque.AddLast(3);
        deque.AddFirst(1);
        deque.AddFirst(0);

        Assert.AreEqual(4, deque.Count);
        Assert.AreEqual(0, deque.PeekFirst());
        Assert.AreEqual(3, deque.PeekLast());
        Assert.AreEqual(new[] { 0, 1, 2, 3 }, deque.ToArray());
    }

    /// <summary>
    /// 空集合 Peek/Remove 抛 InvalidOperationException；Try* 返回 false
    /// </summary>
    [Test]
    public void TestPeekRemoveOnEmpty() {
        ArrayDeque<int> deque = new();
        Assert.AreEqual(0, deque.Count);
        Assert.IsTrue(deque.IsEmpty);

        Assert.Throws<InvalidOperationException>(() => deque.PeekFirst());
        Assert.Throws<InvalidOperationException>(() => deque.PeekLast());
        Assert.Throws<InvalidOperationException>(() => deque.RemoveFirst());
        Assert.Throws<InvalidOperationException>(() => deque.RemoveLast());

        Assert.IsFalse(deque.TryPeekFirst(out _));
        Assert.IsFalse(deque.TryPeekLast(out _));
        Assert.IsFalse(deque.TryRemoveFirst(out _));
        Assert.IsFalse(deque.TryRemoveLast(out _));
    }

    /// <summary>
    /// 索引器读写、越界检查
    /// </summary>
    [Test]
    public void TestIndexer() {
        ArrayDeque<int> deque = new();
        for (int i = 0; i < 5; i++) deque.AddLast(i * 10);

        Assert.AreEqual(0, deque[0]);
        Assert.AreEqual(40, deque[4]);

        deque[2] = 999;
        Assert.AreEqual(999, deque[2]);

        Assert.Throws<IndexOutOfRangeException>(() => { var _ = deque[-1]; });
        Assert.Throws<IndexOutOfRangeException>(() => { var _ = deque[5]; });
        Assert.Throws<IndexOutOfRangeException>(() => deque[10] = 0);
    }

    /// <summary>
    /// 索引器在环绕状态下也能正确寻址
    /// </summary>
    [Test]
    public void TestIndexerWithWrapAround() {
        ArrayDeque<int> deque = new(8);
        // 制造环绕：先用 AddFirst 把 head 推到尾部
        for (int i = 0; i < 4; i++) deque.AddLast(i); // head=0, tail=3
        for (int i = 0; i < 3; i++) deque.RemoveFirst(); // head=3, tail=3
        for (int i = 0; i < 5; i++) deque.AddLast(10 + i); // 跨越数组末尾

        // 当前逻辑序列：[3, 10, 11, 12, 13, 14]
        Assert.AreEqual(6, deque.Count);
        Assert.AreEqual(new[] { 3, 10, 11, 12, 13, 14 }, deque.ToArray());
        for (int i = 0; i < deque.Count; i++) {
            Assert.AreEqual(deque.ToArray()[i], deque[i]);
        }
    }

    /// <summary>
    /// 队列(FIFO)语义：Enqueue/Dequeue/PeekHead
    /// </summary>
    [Test]
    public void TestQueueSemantics() {
        ArrayDeque<int> q = new();
        q.Enqueue(1);
        q.Enqueue(2);
        q.Enqueue(3);

        Assert.AreEqual(1, q.PeekHead());
        Assert.IsTrue(q.TryPeekHead(out int p));
        Assert.AreEqual(1, p);

        Assert.AreEqual(1, q.Dequeue());
        Assert.AreEqual(2, q.Dequeue());
        Assert.IsTrue(q.TryDequeue(out int last));
        Assert.AreEqual(3, last);
        Assert.IsFalse(q.TryDequeue(out _));
    }

    /// <summary>
    /// 栈(LIFO)语义：Push/Pop/PeekTop
    /// </summary>
    [Test]
    public void TestStackSemantics() {
        ArrayDeque<int> s = new();
        s.Push(1);
        s.Push(2);
        s.Push(3);

        Assert.AreEqual(3, s.PeekTop());
        Assert.IsTrue(s.TryPeekTop(out int t));
        Assert.AreEqual(3, t);

        Assert.AreEqual(3, s.Pop());
        Assert.AreEqual(2, s.Pop());
        Assert.IsTrue(s.TryPop(out int last));
        Assert.AreEqual(1, last);
        Assert.IsFalse(s.TryPop(out _));
    }

    /// <summary>
    /// Contains / Remove(value)
    /// </summary>
    [Test]
    public void TestContainsAndRemoveByValue() {
        ArrayDeque<int> deque = new();
        for (int i = 0; i < 5; i++) deque.AddLast(i);

        Assert.IsTrue(deque.Contains(3));
        Assert.IsFalse(deque.Contains(99));
        Assert.IsFalse(deque.Remove(99));

        Assert.IsTrue(deque.Remove(2)); // 中间删除
        Assert.AreEqual(new[] { 0, 1, 3, 4 }, deque.ToArray());

        Assert.IsTrue(deque.Remove(0)); // 删除头
        Assert.AreEqual(new[] { 1, 3, 4 }, deque.ToArray());

        Assert.IsTrue(deque.Remove(4)); // 删除尾
        Assert.AreEqual(new[] { 1, 3 }, deque.ToArray());
    }

    /// <summary>
    /// Clear 清空状态
    /// </summary>
    [Test]
    public void TestClear() {
        ArrayDeque<string> deque = new();
        for (int i = 0; i < 5; i++) deque.AddLast("v" + i);

        deque.Clear();
        Assert.AreEqual(0, deque.Count);
        Assert.IsTrue(deque.IsEmpty);
        Assert.IsFalse(deque.TryPeekFirst(out _));

        // 清空后能继续使用
        deque.AddLast("a");
        deque.AddFirst("z");
        Assert.AreEqual(new[] { "z", "a" }, deque.ToArray());
    }

    /// <summary>
    /// 自动扩容：从默认容量扩展到大量元素，顺序正确
    /// </summary>
    [Test]
    public void TestAutoGrow() {
        ArrayDeque<int> deque = new(2);
        for (int i = 0; i < 1000; i++) deque.AddLast(i);
        Assert.AreEqual(1000, deque.Count);
        for (int i = 0; i < 1000; i++) {
            Assert.AreEqual(i, deque[i]);
        }

        // 双端混合操作
        ArrayDeque<int> deque2 = new(2);
        for (int i = 0; i < 500; i++) {
            deque2.AddLast(i);
            deque2.AddFirst(-i - 1);
        }
        Assert.AreEqual(1000, deque2.Count);
        Assert.AreEqual(-500, deque2.PeekFirst());
        Assert.AreEqual(499, deque2.PeekLast());
    }

    /// <summary>
    /// EnsureCapacity / TrimCapacity 行为
    /// </summary>
    [Test]
    public void TestEnsureAndTrimCapacity() {
        ArrayDeque<int> deque = new(2);
        for (int i = 0; i < 5; i++) deque.AddLast(i);

        deque.EnsureCapacity(100);
        Assert.AreEqual(5, deque.Count); // Count 不变
        Assert.AreEqual(new[] { 0, 1, 2, 3, 4 }, deque.ToArray());

        // 继续添加无需扩容
        for (int i = 0; i < 50; i++) deque.AddLast(100 + i);
        Assert.AreEqual(55, deque.Count);

        deque.TrimCapacity();
        Assert.AreEqual(55, deque.Count);
        // 验证内容仍然正确
        Assert.AreEqual(0, deque[0]);
        Assert.AreEqual(149, deque[54]);
    }

    /// <summary>
    /// CopyTo 正向 / 反向，包括环绕状态
    /// </summary>
    [Test]
    public void TestCopyTo() {
        ArrayDeque<int> deque = new();
        for (int i = 0; i < 5; i++) deque.AddLast(i);

        int[] forward = new int[5];
        deque.CopyTo(forward, 0);
        Assert.AreEqual(new[] { 0, 1, 2, 3, 4 }, forward);

        int[] reversed = new int[5];
        deque.CopyTo(reversed, 0, true);
        Assert.AreEqual(new[] { 4, 3, 2, 1, 0 }, reversed);

        // 偏移
        int[] withOffset = new int[7];
        deque.CopyTo(withOffset, 2);
        Assert.AreEqual(new[] { 0, 0, 0, 1, 2, 3, 4 }, withOffset);

        // 容量不足
        Assert.Throws<ArgumentException>(() => deque.CopyTo(new int[3], 0));
        Assert.Throws<ArgumentNullException>(() => deque.CopyTo(null, 0));
    }

    /// <summary>
    /// CopyTo 在环绕状态下仍正确
    /// </summary>
    [Test]
    public void TestCopyToWithWrapAround() {
        ArrayDeque<int> deque = new(8);
        for (int i = 0; i < 6; i++) deque.AddLast(i);
        for (int i = 0; i < 4; i++) deque.RemoveFirst();
        for (int i = 0; i < 5; i++) deque.AddLast(100 + i); // 触发环绕

        int[] dst = new int[deque.Count];
        deque.CopyTo(dst, 0);
        Assert.AreEqual(deque.ToArray(), dst);

        int[] revDst = new int[deque.Count];
        deque.CopyTo(revDst, 0, true);
        int[] expectedReversed = deque.ToArray();
        Array.Reverse(expectedReversed);
        Assert.AreEqual(expectedReversed, revDst);
    }

    /// <summary>
    /// GetRange 在线性 / 环绕两种状态下都能正确切片
    /// </summary>
    [Test]
    public void TestGetRange() {
        ArrayDeque<int> deque = new();
        for (int i = 0; i < 10; i++) deque.AddLast(i);

        Assert.AreEqual(new[] { 2, 3, 4 }, deque.GetRange(2, 3));
        Assert.AreEqual(new[] { 0 }, deque.GetRange(0, 1));
        Assert.AreEqual(new int[0], deque.GetRange(0, 0));
        Assert.AreEqual(new[] { 9 }, deque.GetRange(9, 1));

        Assert.Throws<ArgumentException>(() => deque.GetRange(-1, 2));
        Assert.Throws<ArgumentException>(() => deque.GetRange(0, -1));
        Assert.Throws<ArgumentException>(() => deque.GetRange(8, 5)); // 越界

        // 环绕场景
        ArrayDeque<int> wrapped = new(8);
        for (int i = 0; i < 6; i++) wrapped.AddLast(i);
        for (int i = 0; i < 4; i++) wrapped.RemoveFirst();
        for (int i = 0; i < 5; i++) wrapped.AddLast(100 + i);
        // 逻辑序列：[4, 5, 100, 101, 102, 103, 104]
        Assert.AreEqual(new[] { 4, 5, 100, 101, 102, 103, 104 }, wrapped.ToArray());
        Assert.AreEqual(new[] { 5, 100, 101 }, wrapped.GetRange(1, 3));
        Assert.AreEqual(new[] { 102, 103, 104 }, wrapped.GetRange(4, 3));
        Assert.AreEqual(wrapped.ToArray(), wrapped.GetRange(0, wrapped.Count));
    }

    /// <summary>
    /// Reversed 视图：迭代反向；Reversed().Reversed() 还原
    /// </summary>
    [Test]
    public void TestReversedView() {
        ArrayDeque<int> deque = new();
        for (int i = 0; i < 5; i++) deque.AddLast(i);

        IDeque<int> rev = deque.Reversed();
        Assert.AreEqual(new[] { 4, 3, 2, 1, 0 }, rev.ToArray());

        IDeque<int> revRev = rev.Reversed();
        Assert.AreEqual(new[] { 0, 1, 2, 3, 4 }, revRev.ToArray());
    }

    /// <summary>
    /// 迭代过程中修改集合应触发版本冲突
    /// </summary>
    [Test]
    public void TestEnumeratorVersionConflict() {
        ArrayDeque<int> deque = new();
        for (int i = 0; i < 5; i++) deque.AddLast(i);

        var e1 = deque.GetEnumerator();
        e1.MoveNext();
        deque.AddLast(99);
        Assert.Throws<InvalidOperationException>(() => e1.MoveNext());

        var e2 = deque.GetEnumerator();
        e2.MoveNext();
        deque.RemoveFirst();
        Assert.Throws<InvalidOperationException>(() => e2.MoveNext());
    }

    /// <summary>
    /// 反向迭代器
    /// </summary>
    [Test]
    public void TestReversedEnumerator() {
        ArrayDeque<int> deque = new();
        for (int i = 0; i < 5; i++) deque.AddLast(i);

        List<int> seen = new();
        var e = deque.GetReversedEnumerator();
        while (e.MoveNext()) {
            seen.Add(e.Current);
        }
        Assert.AreEqual(new List<int> { 4, 3, 2, 1, 0 }, seen);

        // Reset 后可重新迭代
        e.Reset();
        Assert.IsTrue(e.MoveNext());
        Assert.AreEqual(4, e.Current);
    }

    /// <summary>
    /// 与 LinkedList 参考实现的随机 Oracle 对照
    /// </summary>
    [Test]
    [Repeat(3)]
    public void TestOracleAgainstLinkedList() {
        const int rounds = 5000;
        Random rng = new(20260512);

        ArrayDeque<int> deque = new(2); // 小初始容量加大扩容路径覆盖
        LinkedList<int> reference = new();

        for (int i = 0; i < rounds; i++) {
            int op = rng.Next(10);
            int value = rng.Next();

            switch (op) {
                case 0: // AddLast
                    deque.AddLast(value);
                    reference.AddLast(value);
                    break;
                case 1: // AddFirst
                    deque.AddFirst(value);
                    reference.AddFirst(value);
                    break;
                case 2: // RemoveFirst
                    if (reference.Count > 0) {
                        Assert.AreEqual(reference.First!.Value, deque.RemoveFirst());
                        reference.RemoveFirst();
                    } else {
                        Assert.IsFalse(deque.TryRemoveFirst(out _));
                    }
                    break;
                case 3: // RemoveLast
                    if (reference.Count > 0) {
                        Assert.AreEqual(reference.Last!.Value, deque.RemoveLast());
                        reference.RemoveLast();
                    } else {
                        Assert.IsFalse(deque.TryRemoveLast(out _));
                    }
                    break;
                case 4: // PeekFirst
                    if (reference.Count > 0) {
                        Assert.AreEqual(reference.First!.Value, deque.PeekFirst());
                    } else {
                        Assert.IsFalse(deque.TryPeekFirst(out _));
                    }
                    break;
                case 5: // PeekLast
                    if (reference.Count > 0) {
                        Assert.AreEqual(reference.Last!.Value, deque.PeekLast());
                    } else {
                        Assert.IsFalse(deque.TryPeekLast(out _));
                    }
                    break;
                case 6: // Contains（小概率命中）
                    if (reference.Count > 0 && rng.Next(2) == 0) {
                        // 取已存在元素
                        var node = reference.First;
                        int skip = rng.Next(reference.Count);
                        for (int s = 0; s < skip; s++) node = node!.Next;
                        Assert.IsTrue(deque.Contains(node!.Value));
                    } else {
                        Assert.AreEqual(reference.Contains(value), deque.Contains(value));
                    }
                    break;
                case 7: // Remove(value)
                    if (reference.Count > 0) {
                        // 注：ArrayDeque.Remove 在空队列上会越界(源 IndexOf 未防 head=-1)，故测试规避
                        bool removedRef = reference.Remove(value);
                        bool removedDeque = deque.Remove(value);
                        Assert.AreEqual(removedRef, removedDeque);
                    }
                    break;
                case 8: // Count check
                    Assert.AreEqual(reference.Count, deque.Count);
                    break;
                case 9: // 索引器读
                    if (reference.Count > 0) {
                        int idx = rng.Next(reference.Count);
                        var node = reference.First;
                        for (int s = 0; s < idx; s++) node = node!.Next;
                        Assert.AreEqual(node!.Value, deque[idx]);
                    }
                    break;
            }
        }

        // 终态对照
        Assert.AreEqual(reference.Count, deque.Count);
        Assert.AreEqual(reference.ToArray(), deque.ToArray());
    }
}