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

import cn.wjybxx.btree.Task;
import cn.wjybxx.btree.TaskInlinable;
import cn.wjybxx.btree.TaskStatus;

/**
 * 重复运行子节点，直到该任务成功
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class UntilSuccess<T> extends LoopDecorator<T> {

    public UntilSuccess() {
    }

    public UntilSuccess(Task<T> child) {
        super(child);
    }

    @Override
    protected void onChildRunning(Task<T> child) {
        inlineHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        inlineHelper.stopInline();
        if (child.isCancelled()) {
            setCancelled();
            return;
        }
        if (child.isSucceeded()) {
            setSuccess();
        } else if (!hasNextLoop()) {
            setFailed(TaskStatus.LOOP_END);
        } else if (!isExecuting() || !isTailRecursion()) {
            template_execute();
        }
    }
}