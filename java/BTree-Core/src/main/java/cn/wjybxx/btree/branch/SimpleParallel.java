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
 * 简单并发节点。
 * 1.其中第一个任务为主要任务，其余任务为次要任务;
 * 2.一旦主要任务完成，则节点进入完成状态；次要任务可能被运行多次。
 * 3.外部事件将派发给主要任务。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public class SimpleParallel<T> extends Parallel<T> {

    public SimpleParallel() {
    }

    public SimpleParallel(List<Task<T>> children) {
        super(children);
    }

    @Override
    protected void enter(int reentryId) {
        initChildHelpers(false);
    }

    @Override
    protected void execute() {
        final List<Task<T>> children = this.children;
        final int reentryId = getReentryId();
        for (int idx = 0; idx < children.size(); idx++) {
            Task<T> child = children.get(idx);
            ParallelChildHelper<T> childHelper = getChildHelper(child);
            Task<T> inlinedRunningChild = childHelper.getInlinedRunningChild();
            if (inlinedRunningChild != null) {
                template_runInlinedChild(inlinedRunningChild, childHelper, child);
            } else if (child.isRunning()) {
                if (child.isActiveInHierarchy()) {
                    child.template_execute();
                }
            } else {
                template_runChild(child);
            }
            if (checkCancel(reentryId)) { // 得出结果或取消
                return;
            }
        }
    }

    @Override
    protected void onChildRunning(Task<T> child) {
        ParallelChildHelper<T> childHelper = getChildHelper(child);
        childHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        ParallelChildHelper<T> childHelper = getChildHelper(child);
        childHelper.stopInline();

        if (child == children.get(0)) {
            setCompleted(child.getStatus(), true);
        }
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {
        Task<T> mainTask = children.get(0);
        ParallelChildHelper<T> childHelper = getChildHelper(mainTask);

        Task<T> inlinedRunningChild = childHelper.getInlinedRunningChild();
        if (inlinedRunningChild != null) {
            inlinedRunningChild.onEvent(event);
        } else {
            mainTask.onEvent(event);
        }
    }
}