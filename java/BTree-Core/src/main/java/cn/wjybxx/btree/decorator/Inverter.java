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
import cn.wjybxx.btree.TaskStatus;

/**
 * 反转装饰器，它用于反转子节点的执行结果。
 * 如果被装饰的任务失败，它将返回成功；
 * 如果被装饰的任务成功，它将返回失败；
 * 如果被装饰的任务取消，它将返回取消。
 * <p>
 * 对于普通的条件节点，可以通过控制流标记直接取反{@link #setInvertedGuard(boolean)}，避免增加封装。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class Inverter<T> extends Decorator<T> {

    public Inverter() {
    }

    public Inverter(Task<T> child) {
        super(child);
    }

    @Override
    protected void enter(int reentryId) {
        if (isCheckingGuard()) {
            // 条件检测优化
            template_checkGuard(child);
            setCompleted(TaskStatus.invert(child.getStatus()), true);
        }
    }

    @Override
    protected void execute() {
        Task<T> inlinedRunningChild = inlineHelper.getInlinedRunningChild();
        if (inlinedRunningChild != null) {
            template_runInlinedChild(inlinedRunningChild, inlineHelper, child);
        } else if (child.isRunning()) {
            child.template_execute(true);
        } else {
            template_startChild(child, true);
        }
    }

    @Override
    protected void onChildRunning(Task<T> child) {
        inlineHelper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        inlineHelper.stopInline();
        setCompleted(TaskStatus.invert(child.getStatus()), true);
    }
}