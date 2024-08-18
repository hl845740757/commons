/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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
 * @author wjybxx
 * date - 2024/8/18
 */
public class StateMachineHandlers {

    private static final StateMachineHandler<?> DEFAULT = (stateMachineTask, curState, nextState) -> {};

    @SuppressWarnings("unchecked")
    public static <T> StateMachineHandler<T> defaultHandler() {
        return (StateMachineHandler<T>) DEFAULT;
    }

    @SuppressWarnings("unchecked")
    public static <T> StateMachineHandler<T> redoHandler() {
        return (StateMachineHandler<T>) RedoHandler.INST;
    }

    @SuppressWarnings("unchecked")
    public static <T> StateMachineHandler<T> undoHandler() {
        return (StateMachineHandler<T>) UndoHandler.INST;
    }

    public static class RedoHandler<T> implements StateMachineHandler<T> {

        private static final RedoHandler<?> INST = new RedoHandler<>();

        @Override
        public boolean onNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState) {
            if (stateMachineTask.redoChangeState()) {
                return true;
            }
            stateMachineTask.setCompleted(preState.getStatus(), true);
            return true;
        }

        @Override
        public void beforeChangeState(StateMachineTask<T> stateMachineTask, Task<T> curState, Task<T> nextState) {

        }
    }

    private static class UndoHandler<T> implements StateMachineHandler<T> {

        private static final UndoHandler<?> INST = new UndoHandler<>();

        @Override
        public boolean onNextStateAbsent(StateMachineTask<T> stateMachineTask, Task<T> preState) {
            if (stateMachineTask.undoChangeState()) {
                return true;
            }
            stateMachineTask.setCompleted(preState.getStatus(), true);
            return true;
        }

        @Override
        public void beforeChangeState(StateMachineTask<T> stateMachineTask, Task<T> curState, Task<T> nextState) {

        }
    }
}