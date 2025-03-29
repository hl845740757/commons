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
using Wjybxx.BTree.Decorator;
using Wjybxx.BTree.Leaf;
using Wjybxx.Commons;

namespace BTree.Tests;

/// <summary>
/// 由于我们对条件测试进行了专项优化，需要测试器正确性
/// </summary>
public class ConditionTest
{
    /** 测试需要覆盖成功的子节点数量 [0, 10] */
    private const int childCount = 10;

    private static void initChildren(BranchTask<Blackboard> branch, int childCount, int successCount) {
        branch.RemoveAllChild();
        for (int i = 0; i < childCount; i++) {
            branch.AddChild(new Success<Blackboard>());
        }
        // 顺便测试inverter内联
        int failCount = childCount - successCount;
        for (int i = 0; i < failCount; i++) {
            switch (BtreeTestUtil.random.Next(3)) {
                case 0: {
                    branch.GetChild(i).Flags = TaskOptions.MASK_INVERTED_GUARD;
                    break;
                }
                case 1: {
                    branch.SetChild(i, new Inverter<Blackboard>(new Success<Blackboard>()));
                    break;
                }
                case 2: {
                    branch.GetChild(i).Guard = new Failure<Blackboard>();
                    break;
                }
                default: throw new AssertionError();
            }
        }
        branch.ShuffleChild(); // 打乱child
    }

    [Test]
    public void selectorTest() {
        Selector<Blackboard> branch = new Selector<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount; expcted++) {
            initChildren(branch, childCount, expcted);
            taskEntry.Test();

            if (expcted > 0) {
                Assert.IsTrue(taskEntry.IsSucceeded, "Task is unsuccessful, status " + taskEntry.Status);
            } else {
                Assert.IsTrue(taskEntry.IsFailed, "Task is unfailed, status " + taskEntry.Status);
            }
        }
    }

    [Test]
    public void sequenceTest() {
        Sequence<Blackboard> branch = new Sequence<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount; expcted++) {
            initChildren(branch, childCount, expcted);
            taskEntry.Test();

            if (expcted < childCount) {
                Assert.IsTrue(taskEntry.IsFailed, "Task is unfailed, status " + taskEntry.Status);
            } else {
                Assert.IsTrue(taskEntry.IsSucceeded, "Task is unsuccessful, status " + taskEntry.Status);
            }
        }
    }

    [Test]
    public void selectorNTest() {
        SelectorN<Blackboard> branch = new SelectorN<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount + 1; expcted++) { // 期望成功的数量，需要包含边界外
            branch.Required = expcted;
            for (int real = 0; real <= childCount; real++) { // 真正成功的数量
                initChildren(branch, childCount, real);
                taskEntry.Test();

                if (real >= expcted) {
                    Assert.IsTrue(taskEntry.IsSucceeded, "Task is unsuccessful, status " + taskEntry.Status);
                } else {
                    Assert.IsTrue(taskEntry.IsFailed, "Task is unfailed, status " + taskEntry.Status);
                }

                if (expcted >= childCount) { // 所有子节点完成
                    Assert.AreEqual(childCount, BtreeTestUtil.completedCount(branch));
                }
            }
        }
    }
}