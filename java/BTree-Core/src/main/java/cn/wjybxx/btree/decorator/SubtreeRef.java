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

/**
 * 子树引用
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class SubtreeRef<T> extends Decorator<T> {

    private String subtreeName;

    public SubtreeRef() {
    }

    public SubtreeRef(String subtreeName) {
        this.subtreeName = subtreeName;
    }

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        if (child == null) {
            Task<T> rootTask = getTaskEntry().getTreeLoader().loadRootTask(subtreeName);
            addChild(rootTask);
        }
    }

    @Override
    protected void execute() {
        Task<T> inlinedRunningChild = inlineHelper.getInlinedRunningChild();
        if (inlinedRunningChild != null) {
            template_runInlinedChild(inlinedRunningChild, inlineHelper, child);
        } else if (child.isRunning()) {
            child.template_execute();
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
        setCompleted(child.getStatus(), true);
    }

    public String getSubtreeName() {
        return subtreeName;
    }

    public void setSubtreeName(String subtreeName) {
        this.subtreeName = subtreeName;
    }
}