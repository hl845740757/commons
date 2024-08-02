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
using NUnit.Framework.Internal;
using Wjybxx.BTree;
using Wjybxx.BTree.Leaf;

namespace BTree.Tests;

public class LeafTest
{
    [Test]
    public void waitFrameTest() {
        int expectedFrame = 10;
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(new WaitFrame<Blackboard>(expectedFrame));
        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(expectedFrame, taskEntry.CurFrame);
    }

    /** 测试ctl中记录的上一次执行结果的正确性 */
    [Test]
    public void testPrevStatus() {
        PrevStatusTask<Blackboard> root = new PrevStatusTask<Blackboard>();
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(root);

        int bound = (TaskStatus.MAX_PREV_STATUS + 1) * 2;
        for (int idx = 0; idx < bound; idx++) {
            int prevStatus = taskEntry.Status;
            BtreeTestUtil.untilCompleted(taskEntry);

            if (prevStatus >= TaskStatus.MAX_PREV_STATUS) {
                Assert.AreEqual(TaskStatus.MAX_PREV_STATUS, taskEntry.PrevStatus);
            } else {
                Assert.AreEqual(prevStatus, taskEntry.PrevStatus);
            }
        }
    }

    /** 测试启动前取消 */
    [Test]
    public void testStillborn() {
        WaitFrame<Blackboard> waitFrame = new WaitFrame<Blackboard>(10);
        waitFrame.CancelToken = new CancelToken(1); // 提前赋值的token不会被覆盖和删除
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry(waitFrame);
        BtreeTestUtil.untilCompleted(taskEntry);

        Assert.IsTrue(waitFrame.IsStillborn());
        Assert.AreEqual(0, waitFrame.PrevStatus);
    }

    private class PrevStatusTask<T> : ActionTask<T> where T : class
    {
        private int next = TaskStatus.SUCCESS;

        protected override int ExecuteImpl() {
            if (next == TaskStatus.GUARD_FAILED) {
                next++;
            }
            return next++;
        }

        protected override void OnEventImpl(object eventObj) {
        }
    }
}