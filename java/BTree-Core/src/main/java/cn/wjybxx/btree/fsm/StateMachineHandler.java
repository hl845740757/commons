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

/**
 * 状态机策略
 *
 * @author wjybxx
 * date - 2023/12/3
 */
@FunctionalInterface
public interface StateMachineHandler<T> {

    /**
     * handler可能也有需要重置的数据。
     *
     * @param stateMachineTask 状态机
     */
    default void resetForRestart(StateMachineTask<T> stateMachineTask) {

    }

    /**
     * handler可能也有需要初始化的数据。
     *
     * @param stateMachineTask 状态机
     */
    default void beforeEnter(StateMachineTask<T> stateMachineTask) {

    }

    /**
     * 当状态机没有下一个状态时调用该方法，以避免无可用状态
     * 注意：
     * 1.状态机启动时不会调用该方法
     * 2.如果该方法返回后仍无可用状态，将触发无状态逻辑
     *
     * @param stateMachineTask 状态机
     * @param preState         前一个状态，用于计算下一个状态
     * @return 用户是否执行了【状态切换】或【停止状态机】
     */
    default boolean onNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState) {
        stateMachineTask.setCompleted(preState.getStatus(), true);
        return true;
    }

    /**
     * 1.两个参数最多一个为null
     * 2.可以设置新状态的黑板和其它数据
     * 3.用户此时可为新状态分配上下文；同时清理前一个状态的上下文
     * 4.用户此时可拿到新状态{@link ChangeStateArgs}，后续则不可
     * 5.如果task需要感知redo和undo，则由用户将信息写入黑板
     *
     * @param stateMachineTask 状态机
     * @param curState         当前状态
     * @param nextState        下一个状态
     */
    void beforeChangeState(StateMachineTask<T> stateMachineTask, Task<T> curState, Task<T> nextState);
}