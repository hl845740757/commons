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

import javax.annotation.Nullable;
import java.util.List;

/**
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class Sequence<T> extends SingleRunningChildBranch<T> {

    public Sequence() {
    }

    public Sequence(List<Task<T>> children) {
        super(children);
    }

    public Sequence(Task<T> first, @Nullable Task<T> second) {
        super(first, second);
    }

    @Override
    protected void enter(int reentryId) {
        if (isCheckingGuard()) {
            // 条件检测性能优化
            for (int i = 0; i < children.size(); i++) {
                Task<T> child = children.get(i);
                if (!template_checkGuard(child)) {
                    setCompleted(child.getStatus(), true);
                    return;
                }
            }
            setSuccess();
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
        if (child.isCancelled()) {
            setCancelled();
            return;
        }
        if (child.isFailed()) { // 失败码有传递的价值
            setCompleted(child.getStatus(), true);
        } else if (isAllChildCompleted()) {
            setSuccess();
        } else {
            template_execute(false);
        }
    }
}
