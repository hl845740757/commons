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
import cn.wjybxx.btree.TaskInlinable;
import cn.wjybxx.btree.TaskStatus;

/**
 * Switch-选择一个分支运行，直到其结束
 * <p>
 * Switch的基础实现通过逐个检测child的前置条件实现选择，在分支较多的情况下可能开销较大，
 * 在多数情况下，我们可能只是根据配置选择一个分支，可选择{@link SwitchHandler}实现。
 * <p>
 * Q：为什么Switch要支持内联？
 * A：Switch有一个重要的用途：决策树。在做出决策以后，中间层的节点就没有价值了，而保留它们会导致较大的运行时开销。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class Switch<T> extends SingleRunningChildBranch<T> {

    private SwitchHandler<? super T> handler;

    @Override
    protected void execute() {
        if (runningChild == null) {
            int index = selectChild();
            if (index < 0) {
                runningIndex = -1;
                runningChild = null;
                setFailed(TaskStatus.ERROR);
                return;
            }
            runningIndex = index;
            runningChild = children.get(index);
        }
        if (runningChild.isRunning()) {
            runningChild.template_execute(true);
        } else {
            template_runChildDirectly(runningChild);
        }
    }

    private int selectChild() {
        if (handler != null) {
            return handler.select(this);
        }
        for (int idx = 0; idx < children.size(); idx++) {
            Task<T> child = children.get(idx);
            if (!template_checkGuard(child.getGuard())) {
                child.setGuardFailed(null); // 不接收通知
                continue;
            }
            return idx;
        }
        return -1;
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

    public SwitchHandler<? super T> getHandler() {
        return handler;
    }

    public void setHandler(SwitchHandler<? super T> handler) {
        this.handler = handler;
    }
}