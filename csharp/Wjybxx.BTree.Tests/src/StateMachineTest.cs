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
using Wjybxx.BTree.FSM;
using Wjybxx.BTree.Leaf;
using Wjybxx.Commons;

namespace BTree.Tests;

public class StateMachineTest
{
    private static int global_count = 0;
    private static bool delayChange = false;
    private const int queue_size = 5;

    [SetUp]
    public void setUp() {
        global_count = 0;
        delayChange = false;
    }

    private static TaskEntry<Blackboard> newStateMachineTree() {
        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry();
        taskEntry.RootTask = new StateMachineTask<Blackboard>();

        StateMachineTask<Blackboard> stateMachineTask = taskEntry.GetRootStateMachine();
        stateMachineTask.Name = ("RootStateMachine");
        stateMachineTask.NoneChildStatus = TaskStatus.SUCCESS;
        stateMachineTask.SetUndoQueueSize(queue_size);
        stateMachineTask.SetRedoQueueSize(queue_size);
        return taskEntry;
    }

    #region reentry

    /** 不延迟的情况下，三个任务都会进入被取消状态 */
    [Test]
    public void testCount() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        taskEntry.GetRootStateMachine().ChangeState(new StateA<Blackboard>());
        BtreeTestUtil.untilCompleted(taskEntry);
        taskEntry.GetRootStateMachine().Listener = ((stateMachineTask, curState, nextState) => { Assert.IsTrue(curState.IsCancelled); });
        Assert.AreEqual(3, global_count);
    }

    /** 延迟到当前状态退出后切换，三个任务都会进入成功完成状态 */
    [Test]
    public void testCountDelay() {
        delayChange = true;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        taskEntry.GetRootStateMachine().ChangeState(new StateA<Blackboard>());
        BtreeTestUtil.untilCompleted(taskEntry);
        taskEntry.GetRootStateMachine().Listener = ((stateMachineTask, curState, nextState) => { Assert.IsTrue(curState.IsSucceeded); });
        Assert.AreEqual(3, global_count);
    }

    /** 测试同一个状态重入 */
    [Test]
    public void testReentry() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateA<Blackboard> stateA = new StateA<Blackboard>();
        StateB<Blackboard> stateB = new StateB<Blackboard>();
        stateA.nextState = stateB;
        stateB.nextState = stateA;
        taskEntry.GetRootStateMachine().ChangeState(stateA);

        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(3, global_count);
    }

    private class StateA<T> : ActionTask<T> where T : class
    {
        internal Task<T>? nextState;

        protected override int ExecuteImpl() {
            if (global_count++ == 0) {
                if (nextState == null) {
                    nextState = new StateB<T>();
                }
                ChangeStateArgs args = delayChange ? ChangeStateArgs.PLAIN_WHEN_COMPLETED : ChangeStateArgs.PLAIN;
                StateMachineTask<T>.FindStateMachine(this).ChangeState(nextState, args);
            }
            return TaskStatus.SUCCESS;
        }

        protected override void OnEventImpl(object eventObj) {
        }
    }

    private class StateB<T> : ActionTask<T> where T : class
    {
        internal Task<T>? nextState;

        protected override int ExecuteImpl() {
            if (global_count++ == 1) {
                if (nextState == null) {
                    nextState = new StateA<T>();
                }
                ChangeStateArgs args = delayChange ? ChangeStateArgs.PLAIN_WHEN_COMPLETED : ChangeStateArgs.PLAIN;
                StateMachineTask<T>.FindStateMachine(this).ChangeState(nextState, args);
            }
            return TaskStatus.SUCCESS;
        }

        protected override void OnEventImpl(object eventObj) {
        }
    }

    #endregion

    #region redo/undo

    /** redo，计数从 0 加到 5 */
    [Test]
    public void testRedo() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> stateMachine = taskEntry.GetRootStateMachine();
        fillRedoQueue(stateMachine);

        stateMachine.StateMachineHandler = new RedoHandler<Blackboard>();
        stateMachine.RedoChangeState(); // 初始化

        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(queue_size, global_count);
    }


    /** undo，计数从 5 减到 0 */
    [Test]
    public void testUndo() {
        global_count = queue_size;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> stateMachine = taskEntry.GetRootStateMachine();
        fillUndoQueue(stateMachine);

        stateMachine.StateMachineHandler = new UndoHandler<Blackboard>();
        stateMachine.UndoChangeState(); // 初始化

        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(0, global_count);
    }

    /** redo再undo，计数从0加到5，再减回0 */
    [Test]
    public void testRedoUndo() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> stateMachine = taskEntry.GetRootStateMachine();
        fillRedoQueue(stateMachine);

        stateMachine.StateMachineHandler = new RedoUndoHandler<Blackboard>();
        stateMachine.RedoChangeState(); // 初始化

        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.AreEqual(0, global_count);
    }

    private static void fillRedoQueue<T>(StateMachineTask<T> stateMachine) where T : class {
        stateMachine.GetRedoQueue().AddLast(new RedoState<T>(0));
        stateMachine.GetRedoQueue().AddLast(new RedoState<T>(1));
        stateMachine.GetRedoQueue().AddLast(new RedoState<T>(2));
        stateMachine.GetRedoQueue().AddLast(new RedoState<T>(3));
        stateMachine.GetRedoQueue().AddLast(new RedoState<T>(4));
    }

    internal static void fillUndoQueue<T>(StateMachineTask<T> stateMachine) where T : class {
        stateMachine.GetUndoQueue().AddLast(new UndoState<T>(1)); // addLast容易写
        stateMachine.GetUndoQueue().AddLast(new UndoState<T>(2));
        stateMachine.GetUndoQueue().AddLast(new UndoState<T>(3));
        stateMachine.GetUndoQueue().AddLast(new UndoState<T>(4));
        stateMachine.GetUndoQueue().AddLast(new UndoState<T>(5));
    }


    private class RedoHandler<T> : StateMachineHandler<T> where T : class
    {
        public bool OnNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState) {
            return stateMachineTask.RedoChangeState();
        }
    }

    private class UndoHandler<T> : StateMachineHandler<T> where T : class
    {
        public bool OnNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState) {
            return stateMachineTask.UndoChangeState();
        }
    }

    private class RedoUndoHandler<T> : StateMachineHandler<T> where T : class
    {
        private bool redoFinished;

        public bool OnNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState) {
            if (!redoFinished) {
                if (stateMachineTask.RedoChangeState()) {
                    return true;
                }
                Assert.AreEqual(queue_size, global_count);
                fillUndoQueue(stateMachineTask);
                redoFinished = true;
            }
            return stateMachineTask.UndoChangeState();
        }
    }

    private class UndoState<T> : ActionTask<T> where T : class
    {
        readonly int expected;

        internal UndoState(int expected) {
            this.expected = expected;
        }

        protected override int ExecuteImpl() {
            if (BtreeTestUtil.random.Next(2) == 1) {
                return TaskStatus.RUNNING; // 随机等待
            }
            if (global_count == expected) {
                global_count--;
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.ERROR;
        }

        protected override void OnEventImpl(object eventObj) {
        }
    }

    private class RedoState<T> : ActionTask<T> where T : class
    {
        readonly int expected;

        internal RedoState(int expected) {
            this.expected = expected;
        }

        protected override int ExecuteImpl() {
            if (BtreeTestUtil.random.Next(2) == 1) {
                return TaskStatus.RUNNING; // 随机等待
            }
            if (global_count == expected) {
                global_count++;
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.ERROR;
        }

        protected override void OnEventImpl(object eventObj) {
        }
    }

    #endregion

    #region 传统状态机样式

    [Test]
    public void testDelayExecute() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        ClassicalState<Blackboard> nextState = new ClassicalState<Blackboard>();
        taskEntry.GetRootStateMachine().ChangeState(nextState);
        BtreeTestUtil.untilCompleted(taskEntry);

        Assert.IsTrue(nextState.IsSucceeded);
        Assert.IsTrue(taskEntry.IsSucceeded);
    }

    /** 传统状态机下的状态；期望enter和execute分开执行 */
    class ClassicalState<T> : LeafTask<T> where T : class
    {
        protected override void BeforeEnter() {
            base.BeforeEnter();
            IsSlowStart = true;
        }

        protected override void Execute() {
            if (RunFrames != 1) {
                throw new IllegalStateException();
            }
            SetSuccess();
        }

        protected override void OnEventImpl(object eventObj) {
        }
    }

    #endregion

    #region ChangeState

    /**
     * {@link ChangeStateTask}先更新为完成，然后再调用的{@link StateMachineTask#ChangeState(Task)}，
     * 因此完成应该处于成功状态
     */
    [Test]
    public void testChangeStateTask() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        ChangeStateTask<Blackboard> stateTask = new ChangeStateTask<Blackboard>(new Success<Blackboard>());
        taskEntry.GetRootStateMachine().ChangeState(stateTask);

        BtreeTestUtil.untilCompleted(taskEntry);
        Assert.IsTrue(stateTask.IsCancelled, "ChangeState task is cancelled? code: " + stateTask.Status);
    }

    [Test]
    public void testDelay_currentCompleted() {
        int runFrames = 10;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> rootStateMachine = taskEntry.GetRootStateMachine();
        rootStateMachine.Listener = ((stateMachineTask, curState, nextState) => {
            if (curState != null && nextState != null) {
                Assert.AreEqual(runFrames, curState.RunFrames);
            }
        });
        rootStateMachine.ChangeState(new WaitFrame<Blackboard>(runFrames));
        taskEntry.Update(0); // 启动任务树，使行为树处于运行状态

        rootStateMachine.ChangeState(new WaitFrame<Blackboard>(1), ChangeStateArgs.PLAIN_WHEN_COMPLETED);
        BtreeTestUtil.untilCompleted(taskEntry);
    }

    [Test]
    public void testDelay_nextFrame() {
        int runFrames = 10;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> rootStateMachine = taskEntry.GetRootStateMachine();
        rootStateMachine.Listener = ((stateMachineTask, curState, nextState) => {
            if (curState != null && nextState != null) {
                Assert.AreEqual(1, curState.RunFrames);
            }
        });
        rootStateMachine.ChangeState(new WaitFrame<Blackboard>(runFrames));
        taskEntry.Update(0); // 启动任务树，使行为树处于运行状态

        rootStateMachine.ChangeState(new WaitFrame<Blackboard>(1), ChangeStateArgs.PLAIN_NEXT_FRAME);
        BtreeTestUtil.untilCompleted(taskEntry);
    }

    [Test]
    public void testDelay_specialFrame() {
        int runFrames = 10;
        int spFrame = 5;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> rootStateMachine = taskEntry.GetRootStateMachine();
        rootStateMachine.Listener = ((stateMachineTask, curState, nextState) => {
            if (curState != null && nextState != null) {
                Assert.AreEqual(spFrame, curState.RunFrames);
            }
        });
        rootStateMachine.ChangeState(new WaitFrame<Blackboard>(runFrames));
        taskEntry.Update(0); // 启动任务树，使行为树处于运行状态

        rootStateMachine.ChangeState(new WaitFrame<Blackboard>(1),
            ChangeStateArgs.PLAIN_NEXT_FRAME.WithFrame(spFrame)); // 在给定帧切换
        BtreeTestUtil.untilCompleted(taskEntry);
    }

    # endregion
}