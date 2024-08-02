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

import java.util.List;

/**
 * 迭代所有的子节点最后返回成功
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class Foreach<T> extends SingleRunningChildBranch<T> {

    public Foreach() {
    }

    public Foreach(List<Task<T>> children) {
        super(children);
    }

    @Override
    protected void onChildRunning(Task<T> child) {
        inlineHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        runningChild = null;
        inlineHelper.stopInline();
        if (child.isCancelled()) {
            setCancelled();
            return;
        }
        if (isAllChildCompleted()) {
            setSuccess();
        } else if (!isExecuting() || !isTailRecursion()) {
            template_execute();
        }
    }
}