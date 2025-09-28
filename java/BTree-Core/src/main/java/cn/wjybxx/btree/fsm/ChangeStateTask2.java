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

import cn.wjybxx.btree.LeafTask;

import javax.annotation.Nonnull;

/**
 * 通过新状态的name发起状态切换，目标状态存在于配置中
 *
 * @author wjybxx
 * date - 2023/12/1
 */
public class ChangeStateTask2<T> extends LeafTask<T> {

    /** 下一个状态的name */
    private String stateName;
    /** 目标状态机的名字，以允许切换更顶层的状态机 */
    private String machineName;
    /** 延迟模式 */
    private byte delayMode;
    /** 延迟参数 */
    private int delayArg;

    public ChangeStateTask2() {
    }

    @Override
    protected void execute() {
        final int reentryId = getReentryId();
        final StateMachineTask<T> stateMachine = StateMachineTask.findStateMachine(this, machineName);
        if (delayMode == 0) {
            stateMachine.changeState(stateName, delayArg);
        } else {
            stateMachine.changeState(stateName, ChangeStateArgs.PLAIN.with(delayMode, delayArg));
        }
        if (!isExited(reentryId)) {
            setSuccess();
        }
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {

    }

    // region

    public String getStateName() {
        return stateName;
    }

    public void setStateName(String stateName) {
        this.stateName = stateName;
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