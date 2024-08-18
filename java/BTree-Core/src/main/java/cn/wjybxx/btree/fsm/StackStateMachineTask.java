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
package cn.wjybxx.btree.fsm;

import cn.wjybxx.btree.Task;

import java.util.ArrayDeque;

/**
 * 栈式状态机，在普通状态机的基础上支持了redo和undo
 *
 * @author wjybxx
 * date - 2023/12/1
 */
public class StackStateMachineTask<T> extends StateMachineTask<T> {

    private static final int QUEUE_CAPACITY = 5;

    // 需要支持编辑器指定
    private int undoQueueCapacity = QUEUE_CAPACITY;
    private int redoQueueCapacity = QUEUE_CAPACITY;
    // 为减少封装，java端直接使用ArrayDeque
    private final transient ArrayDeque<Task<T>> undoQueue = new ArrayDeque<>(QUEUE_CAPACITY);
    private final transient ArrayDeque<Task<T>> redoQueue = new ArrayDeque<>(QUEUE_CAPACITY);

    // region api

    /** 查看undo对应的state */
    public final Task<T> peekUndoState() {
        return undoQueue.peekLast();
    }

    /** 查看redo对应的state */
    public final Task<T> peekRedoState() {
        return redoQueue.peekFirst();
    }

    /** 设置undo队列大小 */
    public final void setUndoQueueCapacity(int capacity) {
        if (capacity < 0) throw new IllegalArgumentException("capacity: " + capacity);
        this.undoQueueCapacity = capacity;
        while (undoQueue.size() > capacity) {
            undoQueue.pollFirst();
        }
    }

    /** 设置redo队列大小 */
    public final void setRedoQueueCapacity(int capacity) {
        if (capacity < 0) throw new IllegalArgumentException("capacity: " + capacity);
        this.redoQueueCapacity = capacity;
        while (redoQueue.size() > capacity) {
            redoQueue.pollLast();
        }
    }

    /**
     * 向undo队列中添加一个状态
     *
     * @return 是否添加成功
     */
    public final boolean addUndoState(Task<T> curState) {
        if (undoQueueCapacity < 1) {
            return false;
        }
        undoQueue.addLast(curState);
        while (undoQueue.size() > undoQueueCapacity) {
            undoQueue.pollFirst();
        }
        return true;
    }

    /**
     * 向redo队列中添加一个状态
     *
     * @return 是否添加成功
     */
    public final boolean addRedoState(Task<T> curState) {
        if (redoQueueCapacity < 1) {
            return false;
        }
        redoQueue.addFirst(curState);
        while (redoQueue.size() > redoQueueCapacity) {
            redoQueue.pollLast();
        }
        return true;
    }

    /**
     * 撤销到前一个状态
     *
     * @return 如果有前一个状态则返回true
     */
    public final boolean undoChangeState(ChangeStateArgs changeStateArgs) {
        if (!changeStateArgs.isUndo()) {
            throw new IllegalArgumentException();
        }
        Task<T> prevState = undoQueue.peekLast(); // 真正切换以后再删除
        if (prevState == null) {
            return false;
        }
        changeState(prevState, ChangeStateArgs.UNDO);
        return true;
    }

    /**
     * 重新进入到下一个状态
     *
     * @return 如果有下一个状态则返回true
     */
    public final boolean redoChangeState(ChangeStateArgs changeStateArgs) {
        if (!changeStateArgs.isRedo()) {
            throw new IllegalArgumentException();
        }
        Task<T> nextState = redoQueue.peekFirst();  // 真正切换以后再删除
        if (nextState == null) {
            return false;
        }
        changeState(nextState, ChangeStateArgs.REDO);
        return true;
    }

    // endregion

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        undoQueue.clear();
        redoQueue.clear();
        // 不重写beforeEnter，是因为考虑保留用户的初始队列设置
    }

    @Override
    protected void exit() {
        undoQueue.clear();
        redoQueue.clear();
        super.exit();
    }

    @Override
    protected void beforeChangeState(Task<T> curState, Task<T> nextState) {
        if (nextState == null) {
            addUndoState(curState);
            return;
        }
        ChangeStateArgs changeStateArgs = (ChangeStateArgs) nextState.getControlData();
        switch (changeStateArgs.cmd) {
            case ChangeStateArgs.CMD_UNDO -> {
                undoQueue.pollLast();
                if (curState != null) {
                    addRedoState(curState);
                }
            }
            case ChangeStateArgs.CMD_REDO -> {
                redoQueue.pollFirst();
                if (curState != null) {
                    addUndoState(curState);
                }
            }
            default -> {
                // 进入新状态，需要清理redo队列
                redoQueue.clear();
                if (curState != null) {
                    addUndoState(curState);
                }
            }
        }
        super.beforeChangeState(curState, nextState);
    }

    // region 序列化

    public int getUndoQueueCapacity() {
        return undoQueueCapacity;
    }

    public int getRedoQueueCapacity() {
        return redoQueueCapacity;
    }

    // endregion

}
