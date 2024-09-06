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
package cn.wjybxx.btree.branch;

import cn.wjybxx.btree.Task;
import cn.wjybxx.btree.TaskStatus;

import java.util.List;

/**
 * 主动选择节点
 * 每次运行时都会重新测试节点的运行条件，选择一个新的可运行节点。
 * 如果新选择的运行节点与之前的运行节点不同，则取消之前的任务。
 * (ActiveSelector也是比较常用的节点，做内联支持是合适的)
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public class ActiveSelector<T> extends SingleRunningChildBranch<T> {

    public ActiveSelector() {
    }

    public ActiveSelector(List<Task<T>> children) {
        super(children);
    }

    @Override
    protected void execute() {
        Task<T> childToRun = null;
        int childIndex = -1;
        for (int idx = 0; idx < children.size(); idx++) {
            Task<T> child = children.get(idx);
            if (!template_checkGuard(child.getGuard())) {
                continue; // 不能调用SetGuardFailed，会中断当前运行中的child
            }
            childToRun = child;
            childIndex = idx;
            break;
        }
        if (childToRun == null) {
            stop(this.runningChild); // 不清理index，允许退出后查询最后一次运行的child
            setFailed(TaskStatus.ERROR);
            return;
        }

        Task<T> runningChild = this.runningChild;
        if (runningChild == childToRun) {
            Task<T> inlinedRunningChild = inlineHelper.getInlinedRunningChild();
            if (inlinedRunningChild != null) {
                template_runInlinedChild(inlinedRunningChild, inlineHelper, runningChild);
            } else if (runningChild.isRunning()) {
                runningChild.template_execute(true);
            } else {
                template_startChild(runningChild, false);
            }
        } else {
            if (runningChild != null) {
                runningChild.stop();
                inlineHelper.stopInline();
            }
            this.runningChild = childToRun;
            this.runningIndex = childIndex;
            template_startChild(childToRun, false);
        }
    }

    @Override
    protected void onChildRunning(Task<T> child) {
        inlineHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        runningChild = null;
        inlineHelper.stopInline();
        setCompleted(child.getStatus(), true);
    }
}