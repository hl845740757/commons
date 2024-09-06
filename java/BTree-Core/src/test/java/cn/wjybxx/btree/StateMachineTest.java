/*
 * Copyright 2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
package cn.wjybxx.btree;

import cn.wjybxx.base.mutable.MutableInt;
import cn.wjybxx.btree.fsm.*;
import cn.wjybxx.btree.leaf.ActionTask;
import cn.wjybxx.btree.leaf.Success;
import cn.wjybxx.btree.leaf.WaitFrame;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.BeforeEach;
import org.junit.jupiter.api.Test;

import javax.annotation.Nonnull;

/**
 * @author wjybxx
 * date - 2023/12/2
 */
public class StateMachineTest {

    private static int global_count = 0;
    private static boolean delayChange = false;
    private static final int queue_size = 5;

    @BeforeEach
    void setUp() {
        global_count = 0;
        delayChange = false;
    }

    private static TaskEntry<Blackboard> newStateMachineTree() {
        StackStateMachineTask<Blackboard> stateMachineTask = new StackStateMachineTask<>();
        stateMachineTask.setName("RootStateMachine");
        stateMachineTask.setUndoQueueCapacity(queue_size);
        stateMachineTask.setRedoQueueCapacity(queue_size);
        stateMachineTask.setHandler(StateMachineHandlers.defaultHandler());
//        stateMachineTask.setNoneChildStatus(TaskStatus.SUCCESS);

        TaskEntry<Blackboard> taskEntry = BtreeTestUtil.newTaskEntry();
        taskEntry.setRootTask(stateMachineTask);
        return taskEntry;
    }

    // region reentry

    /** 不延迟的情况下，三个任务都会进入被取消状态 */
    @Test
    void testCount() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        taskEntry.getRootStateMachine().setHandler((stateMachineTask, curState, nextState) -> {
            if (curState == null) return; // 尚未启动
            Assertions.assertTrue(curState.isCancelled());
        });

        taskEntry.getRootStateMachine().changeState(new StateA<>());
        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(3, global_count);
    }

    /** 延迟到当前状态退出后切换，三个任务都会进入成功完成状态 */
    @Test
    void testCountDelay() {
        delayChange = true;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        taskEntry.getRootStateMachine().setHandler((stateMachineTask, curState, nextState) -> {
            if (curState == null) return; // 尚未启动
            Assertions.assertTrue(curState.isSucceeded());
        });

        taskEntry.getRootStateMachine().changeState(new StateA<>());
        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(3, global_count);
    }

    /** 测试同一个状态重入 */
    @Test
    void testReentry() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateA<Blackboard> stateA = new StateA<>();
        StateB<Blackboard> stateB = new StateB<>();
        stateA.nextState = stateB;
        stateB.nextState = stateA;
        taskEntry.getRootStateMachine().changeState(stateA);

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(3, global_count);
    }

    private static class StateA<T> extends ActionTask<T> {

        Task<T> nextState;

        @Override
        protected int executeImpl() {
            if (global_count++ == 0) {
                if (nextState == null) {
                    nextState = new StateB<>();
                }
                ChangeStateArgs args = delayChange ? ChangeStateArgs.PLAIN_WHEN_COMPLETED : ChangeStateArgs.PLAIN;
                StateMachineTask.findStateMachine(this).changeState(nextState, args);
            }
            return TaskStatus.SUCCESS;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }

    private static class StateB<T> extends ActionTask<T> {

        Task<T> nextState;

        @Override
        protected int executeImpl() {
            if (global_count++ == 1) {
                if (nextState == null) {
                    nextState = new StateA<>();
                }
                ChangeStateArgs args = delayChange ? ChangeStateArgs.PLAIN_WHEN_COMPLETED : ChangeStateArgs.PLAIN;
                StateMachineTask.findStateMachine(this).changeState(nextState, args);
            }
            return TaskStatus.SUCCESS;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }
    // endregion

    // region redo/undo

    /** redo，计数从 0 加到 5 */
    @Test
    void testRedo() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StackStateMachineTask<Blackboard> stateMachine = (StackStateMachineTask<Blackboard>) taskEntry.getRootTask();
        fillRedoQueue(stateMachine);

        stateMachine.setHandler(StateMachineHandlers.redoHandler());
        stateMachine.redoChangeState(); // 初始化

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(queue_size, global_count);
    }


    /** undo，计数从 5 减到 0 */
    @Test
    void testUndo() {
        global_count = queue_size;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StackStateMachineTask<Blackboard> stateMachine = (StackStateMachineTask<Blackboard>) taskEntry.getRootTask();
        fillUndoQueue(stateMachine);

        stateMachine.setHandler(StateMachineHandlers.undoHandler());
        stateMachine.undoChangeState(); // 初始化

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(0, global_count);
    }

    /** redo再undo，计数从0加到5，再减回0 */
    @Test
    void testRedoUndo() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StackStateMachineTask<Blackboard> stateMachine = (StackStateMachineTask<Blackboard>) taskEntry.getRootTask();
        fillRedoQueue(stateMachine);

        MutableInt redoFinished = new MutableInt(0);
        stateMachine.setHandler(new StateMachineHandler<Blackboard>() {
            @Override
            public boolean onNextStateAbsent(StateMachineTask<Blackboard> stateMachineTask, Task<Blackboard> preState) {
                if (redoFinished.intValue() == 0) {
                    if (stateMachineTask.redoChangeState()) {
                        return true;
                    }
                    Assertions.assertEquals(queue_size, global_count);
                    fillUndoQueue(stateMachine);
                    redoFinished.setValue(1);
                }
                if (stateMachineTask.undoChangeState()) {
                    return true;
                }
                stateMachineTask.setSuccess();
                return true;
            }

            @Override
            public void beforeChangeState(StateMachineTask<Blackboard> stateMachineTask, Task<Blackboard> curState, Task<Blackboard> nextState) {

            }
        });
        stateMachine.redoChangeState(); // 初始化

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertEquals(0, global_count);
    }

    private static void fillRedoQueue(StackStateMachineTask<Blackboard> stateMachine) {
        stateMachine.addRedoState(new RedoState<>(4)); // redo是栈结构
        stateMachine.addRedoState(new RedoState<>(3));
        stateMachine.addRedoState(new RedoState<>(2));
        stateMachine.addRedoState(new RedoState<>(1));
        stateMachine.addRedoState(new RedoState<>(0));
    }

    private static void fillUndoQueue(StackStateMachineTask<Blackboard> stateMachine) {
        stateMachine.addUndoState(new UndoState<>(1)); // undo也是栈结构
        stateMachine.addUndoState(new UndoState<>(2));
        stateMachine.addUndoState(new UndoState<>(3));
        stateMachine.addUndoState(new UndoState<>(4));
        stateMachine.addUndoState(new UndoState<>(5));
    }

    private static class UndoState<T> extends ActionTask<T> {

        final int expected;

        private UndoState(int expected) {
            this.expected = expected;
        }

        @Override
        protected int executeImpl() {
            if (BtreeTestUtil.random.nextBoolean()) {
                return TaskStatus.RUNNING; // 随机等待
            }
            if (global_count == expected) {
                global_count--;
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.ERROR;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }

    private static class RedoState<T> extends ActionTask<T> {

        final int expected;

        private RedoState(int expected) {
            this.expected = expected;
        }

        @Override
        protected int executeImpl() {
            if (BtreeTestUtil.random.nextBoolean()) {
                return TaskStatus.RUNNING; // 随机等待
            }
            if (global_count == expected) {
                global_count++;
                return TaskStatus.SUCCESS;
            }
            return TaskStatus.ERROR;
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }
    // endregion

    // region 传统状态机样式

    @Test
    void testDelayExecute() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        ClassicalState<Blackboard> nextState = new ClassicalState<>();
        taskEntry.getRootStateMachine().changeState(nextState);
        BtreeTestUtil.untilCompleted(taskEntry);

        Assertions.assertTrue(nextState.isSucceeded());
        Assertions.assertTrue(taskEntry.isSucceeded());
    }

    /** 传统状态机下的状态；期望enter和execute分开执行 */
    private static class ClassicalState<T> extends LeafTask<T> {
        @Override
        protected void beforeEnter() {
            super.beforeEnter();
            setSlowStart(true);
        }

        @Override
        protected void execute() {
            if (getRunFrames() != 1) {
                throw new IllegalStateException();
            }
            setSuccess();
        }

        @Override
        protected void onEventImpl(@Nonnull Object event) {

        }
    }
    // endregion

    // region changeState

    /**
     * {@link ChangeStateTask}先更新为完成，然后再调用的{@link StateMachineTask#changeState(Task)}，
     * 因此完成应该处于成功状态
     */
    @Test
    void testChangeStateTask() {
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        ChangeStateTask<Blackboard> stateTask = new ChangeStateTask<>(new Success<>());
        taskEntry.getRootStateMachine().changeState(stateTask);

        BtreeTestUtil.untilCompleted(taskEntry);
        Assertions.assertTrue(stateTask.isCancelled(), "ChangeState task is cancelled? code: " + stateTask.getStatus());
    }

    @Test
    void testDelay_currentCompleted() {
        final int runFrames = 10;
        TaskEntry<Blackboard> taskEntry = newStateMachineTree();
        StateMachineTask<Blackboard> rootStateMachine = taskEntry.getRootStateMachine();
        rootStateMachine.setHandler((stateMachineTask, curState, nextState) -> {
            if (curState != null && nextState != null) {
                Assertions.assertEquals(runFrames, curState.getRunFrames());
            }
        });
        rootStateMachine.changeState(new WaitFrame<>(runFrames));
        taskEntry.update(0); // 启动任务树，使行为树处于运行状态

        rootStateMachine.changeState(new WaitFrame<>(1), ChangeStateArgs.PLAIN_WHEN_COMPLETED);
        BtreeTestUtil.untilCompleted(taskEntry);
    }

    // endregion

}