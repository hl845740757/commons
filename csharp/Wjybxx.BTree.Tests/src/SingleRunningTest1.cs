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

using System.Linq;
using NUnit.Framework;
using Wjybxx.BTree;
using Wjybxx.BTree.Branch;
using Wjybxx.BTree.Leaf;

namespace BTree.Tests;

public class SingleRunningTest1
{
    /** 测试需要覆盖成功的子节点数量 [0, 5] */
    private const int childCount = 5;

    [Test]
    public void selectorTest() {
        Selector<Blackboard> branch = new Selector<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
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
    public void sequenceTest() {
        Sequence<Blackboard> branch = new Sequence<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
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
    public void selectorNTest() {
        SelectorN<Blackboard> branch = new SelectorN<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        for (int expcted = 0; expcted <= childCount + 1; expcted++) { // 期望成功的数量，需要包含边界外
            branch.Required = expcted;
            for (int real = 0; real <= childCount; real++) { // 真正成功的数量
                BtreeTestUtil.initChildren(branch, childCount, real);
                BtreeTestUtil.untilCompleted(taskEntry);

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

    /** 由于可能未命中分支，因此需要循环多次 */
    [Repeat(5)]
    [Test]
    public void switchTest() {
        Switch<Blackboard> branch = new Switch<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);
        branch.AddChild(new WaitFrame<Blackboard>
        {
            Guard = new SimpleRandom<Blackboard>(0.3f)
        });
        branch.AddChild(new Success<Blackboard>
        {
            Guard = new SimpleRandom<Blackboard>(0.4f)
        });
        branch.AddChild(new Failure<Blackboard>
        {
            Guard = new SimpleRandom<Blackboard>(0.5f)
        });
        BtreeTestUtil.untilCompleted(taskEntry);

        if (branch.RunningIndex < 0) {
            Assert.IsTrue(taskEntry.IsFailed);
        } else {
            Task<Blackboard> runChild = branch.GetChild(branch.RunningIndex);
            Assert.AreEqual(taskEntry.Status, runChild.Status);
        }
    }

    [Test]
    public void foreachTest() {
        Foreach<Blackboard> branch = new Foreach<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);

        branch.AddChild(new WaitFrame<Blackboard>
        {
            Guard = new SimpleRandom<Blackboard>(0.3f)
        });
        branch.AddChild(new Success<Blackboard>
        {
            Guard = new SimpleRandom<Blackboard>(0.4f)
        });
        branch.AddChild(new Failure<Blackboard>
        {
            Guard = new SimpleRandom<Blackboard>(0.5f)
        });
        
        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.IsTrue(taskEntry.IsSucceeded);
    }

    [Test]
    public void activeSelectorTest() {
        ActiveSelector<Blackboard> branch = new ActiveSelector<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(branch);

        branch.AddChild(new WaitFrame<Blackboard>
        {
            Required = 10,
            Guard = new FailAtFrame<Blackboard>(1)
        });
        branch.AddChild(new WaitFrame<Blackboard>
        {
            Required = 10,
            Guard = new FailAtFrame<Blackboard>(2)
        });
        branch.AddChild(new WaitFrame<Blackboard>
        {
            Required = 10,
            Guard = new FailAtFrame<Blackboard>(100)
        });
        
        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.IsTrue(taskEntry.IsSucceeded);
        
        Assert.IsTrue(branch.GetChild(0).IsCancelled);
        Assert.IsTrue(branch.GetChild(1).IsCancelled);
        Assert.IsTrue(branch.GetChild(2).IsSucceeded);
    }
    
    private class FailAtFrame<T> : LeafTask<T> where T: class
    {
        private readonly int frame;

        public FailAtFrame(int frame) {
            this.frame = frame;
        }

        protected override void Execute() {
            if (taskEntry.CurFrame >= frame) {
                SetFailed(TaskStatus.ERROR);
            } else {
                SetSuccess();
            }
        }

        protected override void OnEventImpl(object eventObj) {
        }
    }
}