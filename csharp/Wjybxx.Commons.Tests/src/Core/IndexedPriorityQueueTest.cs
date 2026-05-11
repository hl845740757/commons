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

public class IndexedPriorityQueueTest
{
    private sealed class Node : IIndexedElement, IComparable<Node>
    {
        public int Priority;
        private int _index = IIndexedElement.IndexNotFound;

        public Node(int priority) {
            Priority = priority;
        }

        public int CollectionIndex(object collection) => _index;

        public void CollectionIndex(object collection, int index) => _index = index;

        public int CompareTo(Node? other) => Priority.CompareTo(other!.Priority);
    }

    private static IndexedPriorityQueue<Node> NewQueue() {
        return new IndexedPriorityQueue<Node>(Comparer<Node>.Default, 4);
    }

    [Test]
    public void TestEnqueueDequeueOrder() {
        IndexedPriorityQueue<Node> queue = NewQueue();
        Node[] nodes = { new(5), new(1), new(7), new(3), new(2), new(6), new(4) };
        foreach (Node n in nodes) {
            queue.Enqueue(n);
        }

        Assert.AreEqual(nodes.Length, queue.Count);
        Assert.IsFalse(queue.IsEmpty);

        int last = int.MinValue;
        while (queue.TryDequeue(out Node n)) {
            Assert.IsTrue(n.Priority >= last, $"out of order: {n.Priority} after {last}");
            last = n.Priority;
            // 出队后index应被重置
            Assert.AreEqual(IIndexedElement.IndexNotFound, n.CollectionIndex(queue));
        }
        Assert.IsTrue(queue.IsEmpty);
    }

    [Test]
    public void TestRemoveAny() {
        IndexedPriorityQueue<Node> queue = NewQueue();
        Node a = new(10);
        Node b = new(5);
        Node c = new(20);
        Node d = new(15);
        queue.Enqueue(a);
        queue.Enqueue(b);
        queue.Enqueue(c);
        queue.Enqueue(d);

        Assert.IsTrue(queue.Contains(c));
        Assert.IsTrue(queue.Remove(c));
        Assert.IsFalse(queue.Contains(c));
        Assert.AreEqual(IIndexedElement.IndexNotFound, c.CollectionIndex(queue));

        // 出队顺序应为 5,10,15
        Assert.AreEqual(5, queue.Dequeue().Priority);
        Assert.AreEqual(10, queue.Dequeue().Priority);
        Assert.AreEqual(15, queue.Dequeue().Priority);
        Assert.IsTrue(queue.IsEmpty);
    }

    [Test]
    public void TestPriorityChanged() {
        IndexedPriorityQueue<Node> queue = NewQueue();
        Node a = new(10);
        Node b = new(5);
        Node c = new(20);
        queue.Enqueue(a);
        queue.Enqueue(b);
        queue.Enqueue(c);

        // 把c从优先级最大变成最小
        c.Priority = 1;
        queue.PriorityChanged(c);
        Assert.AreEqual(1, queue.PeekHead().Priority);
        Assert.AreEqual(c, queue.PeekHead());

        // 把a从中间变成最大
        a.Priority = 100;
        queue.PriorityChanged(a);
        Assert.AreEqual(c, queue.Dequeue());
        Assert.AreEqual(b, queue.Dequeue());
        Assert.AreEqual(a, queue.Dequeue());
    }

    [Test]
    public void TestEnqueueAlreadyInQueueThrows() {
        IndexedPriorityQueue<Node> queue = NewQueue();
        Node a = new(1);
        queue.Enqueue(a);
        Assert.Throws<InvalidOperationException>(() => queue.Enqueue(a));
    }

    [Test]
    public void TestPeekAndTryPeek() {
        IndexedPriorityQueue<Node> queue = NewQueue();
        Assert.IsFalse(queue.TryPeekHead(out _));
        Assert.Throws<InvalidOperationException>(() => queue.PeekHead());

        queue.Enqueue(new Node(5));
        queue.Enqueue(new Node(1));
        Assert.IsTrue(queue.TryPeekHead(out Node head));
        Assert.AreEqual(1, head.Priority);
        Assert.AreEqual(2, queue.Count);
    }

    [Test]
    public void TestClearResetsIndexes() {
        IndexedPriorityQueue<Node> queue = NewQueue();
        Node a = new(1);
        Node b = new(2);
        queue.Enqueue(a);
        queue.Enqueue(b);
        queue.Clear();
        Assert.IsTrue(queue.IsEmpty);
        Assert.AreEqual(IIndexedElement.IndexNotFound, a.CollectionIndex(queue));
        Assert.AreEqual(IIndexedElement.IndexNotFound, b.CollectionIndex(queue));
    }

    /// <summary>
    /// 随机操作压力测试,与系统库 SortedSet 对比验证语义
    /// </summary>
    [Test]
    [Repeat(3)]
    public void TestRandomOps() {
        const int operations = 5000;
        Random rnd = new Random(20250511);
        IndexedPriorityQueue<Node> queue = NewQueue();
        List<Node> live = new();

        for (int i = 0; i < operations; i++) {
            int op = rnd.Next(10);
            if (op < 6 || live.Count == 0) {
                Node n = new Node(rnd.Next(1000));
                queue.Enqueue(n);
                live.Add(n);
            } else if (op < 8) {
                int idx = rnd.Next(live.Count);
                Node n = live[idx];
                live.RemoveAt(idx);
                Assert.IsTrue(queue.Remove(n));
            } else {
                Assert.IsTrue(queue.TryDequeue(out Node head));
                int min = int.MaxValue;
                Node target = null!;
                foreach (Node n in live) {
                    if (n.Priority < min) {
                        min = n.Priority;
                        target = n;
                    }
                }
                Assert.AreEqual(min, head.Priority);
                live.Remove(target);
            }
            Assert.AreEqual(live.Count, queue.Count);
        }
    }
}
