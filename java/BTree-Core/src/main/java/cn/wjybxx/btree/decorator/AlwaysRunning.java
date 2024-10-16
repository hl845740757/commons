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
 * 在子节点完成之后仍返回运行。
 * 注意：在运行期间只运行一次子节点
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class AlwaysRunning<T> extends Decorator<T> {

    private transient boolean started;

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        started = false;
        if (child == null) {
            setBreakInline(true);
        }
    }

    @Override
    protected void execute() {
        Task<T> child = this.child;
        if (child == null) {
            return;
        }
        if (started && child.isCompleted()) {  // 勿轻易调整
            return;
        }
        Task<T> inlinedChild = inlineHelper.getInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.template_executeInlined(inlineHelper, child);
        } else if (child.isRunning()) {
            child.template_execute(true);
        } else {
            started = true;
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
        setBreakInline(true); // 阻断内联，避免频繁通知父节点

        // 不响应其它状态，但还是需要响应取消...
        if (child.isCancelled()) {
            setCancelled();
        }
    }

}