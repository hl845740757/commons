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
import cn.wjybxx.btree.TaskStatus;
import cn.wjybxx.btree.leaf.Success;

import javax.annotation.Nullable;
import java.util.List;

/**
 * 多选Selector。
 * 如果{@link #required}小于等于0，则等同于{@link Success}
 * 如果{@link #required}等于1，则等同于{@link Selector}；
 * 如果{@link #required}等于{@code children.size}，则在所有child成功之后成功 -- 默认不会提前失败。
 * 如果{@link #required}大于{@code children.size}，则在所有child运行完成之后失败 -- 默认不会提前失败。
 *
 * @author wjybxx
 * date - 2023/11/26
 */
@TaskInlinable
public class SelectorN<T> extends SingleRunningChildBranch<T> {

    /** 需要达成的次数 */
    private int required = 1;
    /** 是否快速失败 */
    private boolean failFast;
    /** 当前计数 */
    private transient int count;

    public SelectorN() {
    }

    public SelectorN(List<Task<T>> children) {
        super(children);
    }

    public SelectorN(Task<T> first, @Nullable Task<T> second) {
        super(first, second);
    }

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        count = 0;
    }

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        count = 0;
    }

    @Override
    protected void enter(int reentryId) {
        super.enter(reentryId);
        if (required < 1) {
            setSuccess();
        } else if (getChildCount() == 0) {
            setFailed(TaskStatus.CHILDLESS);
        } else if (checkFailFast()) {
            setFailed(TaskStatus.INSUFFICIENT_CHILD);
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
        if (child.isSucceeded() && ++count >= required) {
            setSuccess();
        } else if (isAllChildCompleted() || checkFailFast()) {
            setFailed(TaskStatus.ERROR);
        } else if (!isExecuting() || !isTailRecursion()) {
            template_execute();
        }
    }

    private boolean checkFailFast() {
        return failFast && (children.size() - getCompletedCount() < required - count);
    }

    public int getRequired() {
        return required;
    }

    public void setRequired(int required) {
        this.required = required;
    }

    public boolean isFailFast() {
        return failFast;
    }

    public void setFailFast(boolean failFast) {
        this.failFast = failFast;
    }
}
