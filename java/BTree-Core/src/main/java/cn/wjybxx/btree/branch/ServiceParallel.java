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

import javax.annotation.Nonnull;
import java.util.List;

/**
 * 服务并发节点
 * 1.其中第一个任务为主要任务，其余任务为后台服务。
 * 2.每次所有任务都会执行一次，并保持长期运行。
 * 3.外部事件将派发给主要任务。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public class ServiceParallel<T> extends ParallelBranch<T> {

    public ServiceParallel() {
    }

    public ServiceParallel(List<Task<T>> children) {
        super(children);
    }

    @Override
    protected void enter(int reentryId) {
        initChildHelpers(false);
    }

    @Override
    protected void execute() {
        final List<Task<T>> children = this.children;
        for (int idx = 0; idx < children.size(); idx++) {
            Task<T> child = children.get(idx);
            ParallelChildHelper<T> childHelper = getChildHelper(child);
            Task<T> inlinedChild = childHelper.getInlinedChild();
            if (inlinedChild != null) {
                inlinedChild.template_executeInlined(childHelper, child);
            } else if (child.isRunning()) {
                child.template_execute(true);
            } else {
                template_startChild(child, true);
            }
        }

        Task<T> mainTask = children.get(0);
        if (mainTask.isCompleted()) {
            setCompleted(mainTask.getStatus(), true);
        }
    }

    @Override
    protected void onChildRunning(Task<T> child, boolean starting) {
        ParallelChildHelper<T> helper = getChildHelper(child);
        helper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        ParallelChildHelper<T> helper = getChildHelper(child);
        helper.stopInline();
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {
        Task<T> mainTask = children.get(0);
        ParallelChildHelper<T> childHelper = getChildHelper(mainTask);

        Task<T> inlinedChild = childHelper.getInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.onEvent(event);
        } else {
            mainTask.onEvent(event);
        }
    }
}