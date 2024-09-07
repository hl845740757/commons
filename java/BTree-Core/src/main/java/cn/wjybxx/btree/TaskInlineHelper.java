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

package cn.wjybxx.btree;

import cn.wjybxx.btree.branch.SingleRunningChildBranch;

/**
 * @author wjybxx
 * date - 2024/7/24
 */
public class TaskInlineHelper<T> {

    /** 是否启用内联 */
    public static boolean enableInline = true;

    /** 无效重入id */
    private static final int INVALID_REENTRY_ID = Integer.MIN_VALUE;
    /** 表示内联失败 */
    private static final int FAILED_REENTRY_ID = INVALID_REENTRY_ID + 1;

    /** 被内联运行的子节点 */
    private transient Task<T> inlinedChild = null;
    /** 被内联的子节点的重入id */
    private transient int inlinedReentryId = INVALID_REENTRY_ID;

    /** 测试内联的有效性 */
    public boolean testInlined() {
        return inlinedChild != null && inlinedChild.getReentryId() == inlinedReentryId;
    }

    /** 获取被内联运行的子节点 */
    public final Task<T> getInlinedChild() {
        Task<T> r = inlinedChild;
        if (r == null) {
            return null;
        }
        if (r.getReentryId() == inlinedReentryId) {
            return r;
        }
        this.inlinedChild = null;
        this.inlinedReentryId = INVALID_REENTRY_ID;
        return null;
    }

    /** 取消内联 */
    public final void stopInline() {
        this.inlinedChild = null;
        this.inlinedReentryId = INVALID_REENTRY_ID;
    }

    /** 尝试内联运行中的子节点 */
    public final void inlineChild(Task<T> runningChild) {
        if (!runningChild.isRunning()) {
            throw new IllegalArgumentException("runningChild must running");
        }
        if (!enableInline) {
            this.inlinedChild = null;
            this.inlinedReentryId = INVALID_REENTRY_ID;
            return;
        }

        Task<T> cur = runningChild;
        // 只对确定逻辑的常见类型进行内联 -- 子节点完成必定触发控制节点完成的才可以内联
        while (true) {
            if (!cur.isInlinable()) {
                break; // 不可内联
            }
            if (cur instanceof SingleRunningChildBranch<T> branch) {
                if (branch.getRunningChild() == null || branch.getRunningChild().isCompleted()) {
                    break;
                }
                cur = branch.getInlineHelper().getInlinedChild();
                if (cur != null) { // 分支有成功内联数据
                    break;
                }
                if (branch.getInlineHelper().inlinedReentryId == FAILED_REENTRY_ID) {
                    cur = branch.getRunningChild(); // 分支内联子节点失败
                    break;
                }
                cur = branch.getRunningChild();
                continue;
            }
            if (cur instanceof Decorator<T> decorator) {
                if (decorator.getChild() == null || decorator.getChild().isCompleted()) {
                    break;
                }
                cur = decorator.getInlineHelper().getInlinedChild();
                if (cur != null) {
                    break;
                }
                if (decorator.getInlineHelper().inlinedReentryId == FAILED_REENTRY_ID) {
                    cur = decorator.getChild();
                    break;
                }
                cur = decorator.getChild();
                continue;
            }
            break;
        }
        assert cur.isRunning();
        if (cur == runningChild) {
            // 无实际内联效果时置为null性能更好
            this.inlinedChild = null;
            this.inlinedReentryId = FAILED_REENTRY_ID;
        } else {
            this.inlinedChild = cur;
            this.inlinedReentryId = cur.getReentryId();
        }
    }
}