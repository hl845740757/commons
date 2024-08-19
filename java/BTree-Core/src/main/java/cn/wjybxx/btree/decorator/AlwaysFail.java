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
package cn.wjybxx.btree.decorator;

import cn.wjybxx.btree.Decorator;
import cn.wjybxx.btree.Task;
import cn.wjybxx.btree.TaskInlinable;
import cn.wjybxx.btree.TaskStatus;

/**
 * 在子节点完成之后固定返回失败
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class AlwaysFail<T> extends Decorator<T> {

    private int failureStatus;

    public AlwaysFail() {
    }

    public AlwaysFail(Task<T> child) {
        super(child);
    }

    @Override
    protected void execute() {
        if (child == null) {
            setFailed(TaskStatus.toFailure(failureStatus));
            return;
        }
        Task<T> inlinedRunningChild = inlineHelper.getInlinedRunningChild();
        if (inlinedRunningChild != null) {
            template_runInlinedChild(inlinedRunningChild, inlineHelper, child);
        } else if (child.isRunning()) {
            if (child.isActiveInHierarchy()) {
                child.template_execute();
            }
        } else {
            template_runChild(child);
        }
    }

    @Override
    protected void onChildRunning(Task<T> child) {
        inlineHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        inlineHelper.stopInline();
        setCompleted(TaskStatus.toFailure(child.getStatus()), true); // 错误码有传播的价值
    }

    public int getFailureStatus() {
        return failureStatus;
    }

    public void setFailureStatus(int failureStatus) {
        this.failureStatus = failureStatus;
    }
}