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

import cn.wjybxx.base.ObjectPath;
import cn.wjybxx.btree.LeafTask;
import cn.wjybxx.btree.Task;

import javax.annotation.Nonnull;

/**
 * 通用状态机切换任务
 *
 * @author wjybxx
 * date - 2023/12/1
 */
public class ChangeStateTask<T> extends LeafTask<T> {

    /** 下一个状态的guid或name -- 延迟加载 */
    private ObjectPath statePath;
    /** 目标状态的属性 */
    private Object stateProps;
    /** 下一个状态的对象缓存，通常延迟加载以避免循环引用 */
    private transient Task<T> nextState;

    /** 目标状态机的名字，以允许切换更顶层的状态机 */
    private String machineName;
    /** 延迟模式 */
    private byte delayMode;
    /** 延迟参数 */
    private int delayArg;

    public ChangeStateTask() {
    }

    public ChangeStateTask(Task<T> nextState) {
        this.nextState = nextState;
    }

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        if (nextState != null && nextState.getControl() == null) {
            nextState.resetForRestart();
        }
    }

    @Override
    protected void execute() {
        if (nextState == null) {
            if (statePath == null || statePath.isEmpty()) {
                throw new IllegalStateException("path is empty");
            }
            nextState = getTaskEntry().getTreeLoader().loadRootTask(statePath);
        }
        nextState.setSharedProps(stateProps);

        final int reentryId = getReentryId();
        final StateMachineTask<T> stateMachine = StateMachineTask.findStateMachine(this, machineName);
        if (delayMode == 0) {
            stateMachine.changeState(nextState, delayArg);
        } else {
            stateMachine.changeState(nextState, ChangeStateArgs.PLAIN.with(delayMode, delayArg));
        }
        if (!isExited(reentryId)) {
            setSuccess();
        }
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {

    }

    // region

    public ObjectPath getStatePath() {
        return statePath;
    }

    public void setStatePath(ObjectPath statePath) {
        this.statePath = statePath;
    }

    public Task<T> getNextState() {
        return nextState;
    }

    public void setNextState(Task<T> nextState) {
        this.nextState = nextState;
    }

    public Object getStateProps() {
        return stateProps;
    }

    public void setStateProps(Object stateProps) {
        this.stateProps = stateProps;
    }

    public String getMachineName() {
        return machineName;
    }

    public void setMachineName(String machineName) {
        this.machineName = machineName;
    }

    public byte getDelayMode() {
        return delayMode;
    }

    public void setDelayMode(byte delayMode) {
        this.delayMode = delayMode;
    }

    public int getDelayArg() {
        return delayArg;
    }

    public void setDelayArg(int delayArg) {
        this.delayArg = delayArg;
    }

    // endregion
}