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
import cn.wjybxx.btree.branch.join.JoinSequence;

import javax.annotation.Nonnull;
import java.util.List;

/**
 * Join
 * 1.在得出结果之前不会重复执行已完成的任务。
 * 2.默认为子节点分配独立的取消令牌
 *
 * @author wjybxx
 * date - 2023/12/2
 */
public class Join<T> extends ParallelBranch<T> {

    /** 子节点的管理策略 */
    protected JoinPolicy<T> policy;
    /** 已进入完成状态的子节点 */
    protected transient int completedCount;
    /** 成功完成的子节点 */
    protected transient int succeededCount;

    @Override
    public void resetForRestart() {
        super.resetForRestart();
        completedCount = 0;
        succeededCount = 0;
        policy.resetForRestart();
    }

    @Override
    protected void beforeEnter() {
        super.beforeEnter();
        if (policy == null) {
            policy = JoinSequence.getInstance();
        }
        completedCount = 0;
        succeededCount = 0;
        // policy的数据重置
        policy.beforeEnter(this);
    }

    @Override
    protected void enter(int reentryId) {
        // 记录子类上下文 -- 由于beforeEnter可能改变子节点信息，因此在enter时处理
        initChildHelpers(isCancelTokenPerChild());
        policy.enter(this);
    }

    @Override
    protected void execute() {
        final List<Task<T>> children = this.children;
        if (children.isEmpty()) {
            return;
        }
        final int reentryId = getReentryId();
        for (int i = 0; i < children.size(); i++) {
            final Task<T> child = children.get(i);
            final ParallelChildHelper<T> childHelper = getChildHelper(child);
            final boolean started = child.isExited(childHelper.reentryId);
            if (started && child.isCompleted()) {
                continue; // 未重置的情况下可能是上一次的完成状态
            }
            Task<T> inlinedChild = childHelper.getInlinedChild();
            if (inlinedChild != null) {
                inlinedChild.template_executeInlined(childHelper, child);
            } else if (child.isRunning()) {
                child.template_execute(true);
            } else {
                setChildCancelToken(child, childHelper.cancelToken); // 运行前赋值取消令牌
                template_startChild(child, true);
            }
            if (checkCancel(reentryId)) {
                return;
            }
        }
        if (completedCount >= children.size()) { // child全部执行，但没得出结果
            throw new IllegalStateException();
        }
    }

    @Override
    protected void onChildRunning(Task<T> child, boolean starting) {
        ParallelChildHelper<T> helper = getChildHelper(child);
        helper.inlineChild(child);
    }

    @Override
    protected void onChildCompleted(Task<T> child) {
        ParallelChildHelper<T> helper = getChildHelper(child);
        helper.stopInline();
        unsetChildCancelToken(child); // 删除分配的token

        completedCount++;
        if (child.isSucceeded()) {
            succeededCount++;
        }
        policy.onChildCompleted(this, child);
    }

    @Override
    protected void onEventImpl(@Nonnull Object event) {
        policy.onEvent(this, event);
    }

    // region
    public boolean isAllChildCompleted() {
        return completedCount >= children.size();
    }

    public boolean isAllChildSucceeded() {
        return succeededCount >= children.size();
    }

    public int getCompletedCount() {
        return completedCount;
    }

    public int getSucceededCount() {
        return succeededCount;
    }
    // endregion

    public JoinPolicy<T> getPolicy() {
        return policy;
    }

    public void setPolicy(JoinPolicy<T> policy) {
        this.policy = policy;
    }

}