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
 * 只执行一次。
 * 1.适用那些不论成功与否只执行一次的行为。
 * 2.在调用{@link #resetForRestart()}后可再次运行。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class OnlyOnce<T> extends Decorator<T> {

    public OnlyOnce() {
    }

    public OnlyOnce(Task<T> child) {
        super(child);
    }

    @Override
    protected void execute() {
        if (child.isCompleted()) {
            setCompleted(child.getStatus(), true);
            return;
        }
        Task<T> inlinedChild = inlineHelper.getInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.template_executeInlined(inlineHelper, child);
        } else if (child.isRunning()) {
            child.template_execute(true);
        } else {
            template_startChild(child, true);
        }
    }

    @Override
    protected void onChildRunning(Task<T> child, boolean starting) {
        inlineHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        inlineHelper.stopInline();
        setCompleted(child.getStatus(), true);
    }

}