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

package cn.wjybxx.btree.fsm.handler;

import cn.wjybxx.btree.Task;
import cn.wjybxx.btree.fsm.StateMachineHandler;
import cn.wjybxx.btree.fsm.StateMachineTask;

/**
 * @author wjybxx
 * date - 2024/11/18
 */
public class DefaultStateMachineHandler<T> implements StateMachineHandler<T> {

    private static final DefaultStateMachineHandler<?> INST = new DefaultStateMachineHandler<>();

    @SuppressWarnings("unchecked")
    public static <T> DefaultStateMachineHandler<T> getInstance() {
        return (DefaultStateMachineHandler<T>) INST;
    }

    // region override
    @Override
    public final void resetForRestart(StateMachineTask<T> stateMachineTask) {
    }

    @Override
    public final void beforeEnter(StateMachineTask<T> stateMachineTask) {
    }

    @Override
    public final void beforeChangeState(StateMachineTask<T> stateMachineTask, Task<T> curState, Task<T> nextState) {
    }
    // endregion
}
