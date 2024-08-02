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
import cn.wjybxx.btree.TaskStatus;

/**
 * 每一帧都检查子节点的前置条件，如果前置条件失败，则取消child执行并返回失败。
 * 这是一个常用的节点类型，我们做内联优化，可以提高效率。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
public class AlwaysCheckGuard<T> extends Decorator<T> {

    public AlwaysCheckGuard() {
    }

    public AlwaysCheckGuard(Task<T> child) {
        super(child);
    }

    @Override
    protected void execute() {
        if (template_checkGuard(child.getGuard())) {
            Task<T> inlinedRunningChild = inlineHelper.getInlinedRunningChild();
            if (inlinedRunningChild != null) {
                template_runInlinedChild(inlinedRunningChild, inlineHelper, child);
            } else if (child.isRunning()) {
                child.template_execute();
            } else {
                template_runChildDirectly(child);
            }
        } else {
            child.stop();
            inlineHelper.stopInline(); // help gc
            setFailed(TaskStatus.ERROR);
        }
    }

    @Override
    protected void onChildRunning(Task<T> child) {
        inlineHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        inlineHelper.stopInline();
        setCompleted(child.getStatus(), true);
    }
}