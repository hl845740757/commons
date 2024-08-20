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
using Wjybxx.BTree.FSM;
using Wjybxx.BTree.Leaf;

namespace BTree.Tests;

public class ActiveTest
{
    private static TaskEntry<Blackboard> newStateMachineTree() {
        StackStateMachineTask<Blackboard> stateMachineTask = new StackStateMachineTask<Blackboard>();
        stateMachineTask.Name = "RootStateMachine";
        stateMachineTask.Handler = StateMachineHandlers.DefaultHandler<Blackboard>();

        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry();
        taskEntry.RootTask = stateMachineTask;
        return taskEntry;
    }

    /// <summary>
    /// waitframe本应该在第5帧完成，但我们暂停了其心跳，在第9帧后启用心跳，第10帧就完成
    /// </summary>
    [Test]
    public void testWaitFrame() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        WaitFrame<Blackboard> nextState = new WaitFrame<Blackboard>(5);
        taskEntry.GetRootStateMachine().ChangeState(nextState);

        const int expectedFrames = 10;
        BtreeTestUtil.untilCompleted(taskEntry, frame => {
            if (frame == 0) {
                taskEntry.SetActive(false);
            }
            if (frame == expectedFrames - 1) {
                taskEntry.SetActive(true);
            }
        });
        Assert.AreEqual(10, nextState.RunFrames);
    }
    
    /** 测试active为false的情况下在第9帧取消 */
    [Test]
    public void testCancel() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        WaitFrame<Blackboard> nextState = new WaitFrame<Blackboard>(5);
        taskEntry.GetRootStateMachine().ChangeState(nextState);

        const int expectedFrames = 10;
        BtreeTestUtil.untilCompleted(taskEntry, frame => {
            if (frame == 0) {
                taskEntry.SetActive(false);
            }
            if (frame == expectedFrames - 1) {
                taskEntry.CancelToken.Cancel(1);
            }
        });
        Assert.AreEqual(10, nextState.RunFrames);
    }
}