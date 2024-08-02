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

using NUnit.Framework;
using Wjybxx.BTree;
using Wjybxx.BTree.Branch;
using Wjybxx.BTree.Branch.Join;
using Wjybxx.BTree.Leaf;
using Wjybxx.Commons.Ex;

namespace BTree.Tests;

public class JoinTest
{
    private static TaskEntry<Blackboard> newJoinTree(JoinPolicy<Blackboard> joinPolicy) {
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry();
        Join<Blackboard> join = new Join<Blackboard>();
        join.Policy = joinPolicy;
        taskEntry.RootTask = join;
        return taskEntry;
    }

    private static int globalCount = 0;
    private const int childCount = 5;

    [SetUp]
    public void setUp() {
        globalCount = 0;
    }

    // region
    private class Counter<T> : ActionTask<T> where T : class
    {
        protected override int ExecuteImpl() {
            // 不能过于简单成功，否则无法覆盖所有情况
            if (BtreeTestUtil.random.Next(2) == 1) {
                globalCount++;
                SetSuccess();
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.RUNNING;
        }

        protected override void OnEventImpl(object eventObj) {
        }
    }

    [Test]
    public void testWaitAll() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinWaitAll<Blackboard>.GetInstance());

        Task<Blackboard> rootTask = taskEntry.RootTask;
        for (int i = 0; i < childCount; i++) {
            rootTask.AddChild(new Counter<Blackboard>());
        }

        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.IsTrue(taskEntry.IsSucceeded);
        Assert.AreEqual(childCount, globalCount);
    }

    /** 测试join多次执行的正确性 */
    [Test]
    public void testWaitAllMultiLoop() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinWaitAll<Blackboard>.GetInstance());

        Task<Blackboard> rootTask = taskEntry.RootTask;
        for (int i = 0; i < childCount; i++) {
            rootTask.AddChild(new Counter<Blackboard>());
        }
        int loop = 3;
        for (int i = 0; i < loop; i++) {
            BtreeTestUtil.untilCompleted(taskEntry);
        }
        Assert.IsTrue(taskEntry.IsSucceeded);
        Assert.AreEqual(childCount * loop, globalCount);
    }

    [Test]
    public void testAnyOf() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinAnyOf<Blackboard>.GetInstance());
        Join<Blackboard> rootTask = (Join<Blackboard>)taskEntry.RootTask;
        // 需要测试子节点数量为0的情况
        for (int i = 0; i <= childCount; i++) {
            rootTask.RemoveAllChild();
            for (int j = 0; j < i; j++) {
                rootTask.AddChild(new Counter<Blackboard>());
            }
            if (i == 0) {
                Assert.Throws<InfiniteLoopException>(() => { BtreeTestUtil.untilCompleted(taskEntry); });
                taskEntry.Stop(); // 需要进入完成状态
                Assert.IsTrue(taskEntry.IsCancelled);
            } else {
                globalCount = 0;
                BtreeTestUtil.untilCompleted(taskEntry);
                // ... 这里有上次进入完成状态的子节点，直接遍历子节点进行统计不安全
                Assert.IsTrue(taskEntry.IsSucceeded);
                Assert.AreEqual(1, rootTask.CompletedCount);
                Assert.AreEqual(1, globalCount);
            }
        }
    }

    [Test]
    public void testMain() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinMain<Blackboard>.GetInstance());

        Task<Blackboard> rootTask = taskEntry.RootTask;
        for (int i = 0; i < childCount; i++) {
            rootTask.AddChild(new Counter<Blackboard>());
        }

        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.IsTrue(taskEntry.IsSucceeded);
        Assert.IsTrue(rootTask.GetChild(0).IsSucceeded);
    }

    [Test]
    public void testSelector() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinSelector<Blackboard>.GetInstance());
        Join<Blackboard> branch = (Join<Blackboard>)taskEntry.RootTask;
        for (int expcted = 0; expcted <= childCount; expcted++) {
            BtreeTestUtil.initChildren(branch, childCount, expcted);
            BtreeTestUtil.untilCompleted(taskEntry);

            if (expcted > 0) {
                Assert.IsTrue(taskEntry.IsSucceeded, "Task is unsuccessful, status " + taskEntry.Status);
            } else {
                Assert.IsTrue(taskEntry.IsFailed, "Task is unfailed, status " + taskEntry.Status);
            }
        }
    }

    [Test]
    public void testSequence() {
        TaskEntry<Blackboard> taskEntry = newJoinTree(JoinSequence<Blackboard>.GetInstance());
        Join<Blackboard> branch = (Join<Blackboard>)taskEntry.RootTask;

        for (int expcted = 0; expcted <= childCount; expcted++) {
            BtreeTestUtil.initChildren(branch, childCount, expcted);
            BtreeTestUtil.untilCompleted(taskEntry);

            if (expcted < childCount) {
                Assert.IsTrue(taskEntry.IsFailed, "Task is unfailed, status " + taskEntry.Status);
            } else {
                Assert.IsTrue(taskEntry.IsSucceeded, "Task is unsuccessful, status " + taskEntry.Status);
            }
        }
    }

    [Test]
    public void testSelectorN() {
        JoinSelectorN<Blackboard> policy = new JoinSelectorN<Blackboard>();
        TaskEntry<Blackboard> taskEntry = newJoinTree(policy);
        Join<Blackboard> branch = (Join<Blackboard>)taskEntry.RootTask;

        for (int expcted = 0; expcted <= childCount + 1; expcted++) { // 期望成功的数量，需要包含边界外
            policy.Required = expcted;
            for (int real = 0; real <= childCount; real++) { // 真正成功的数量
                BtreeTestUtil.initChildren(branch, childCount, real);
                BtreeTestUtil.untilCompleted(taskEntry);

                if (real >= expcted) {
                    Assert.IsTrue(taskEntry.IsSucceeded, "Task is unsuccessful, status " + taskEntry.Status);
                } else {
                    Assert.IsTrue(taskEntry.IsFailed, "Task is unfailed, status " + taskEntry.Status);
                }
                if (expcted >= childCount) { // 所有子节点完成
                    Assert.AreEqual(childCount, branch.CompletedCount);
                }
            }
        }
    }

    // endregion
}