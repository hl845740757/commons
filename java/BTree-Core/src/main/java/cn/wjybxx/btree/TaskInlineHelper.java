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

import javax.annotation.Nullable;

/**
 * 内联工具类。
 * 1.只有不能被内联的节点，才需要该工具类。
 * 2.实现内联优化时，应当在{@link Task#onChildRunning(Task, boolean)}时开启内联和{@link Task#onChildCompleted(Task)}时停止内联。
 * 3.在{@link Task#exit()}时也调用一次停止内联可避免内存泄漏(不必要的引用)。
 * 4.在{@link Task#onEventImpl(Object)}时应当尝试将事件转发给被内联的子节点，可使用工具方法{@link #onEvent(Object, Task)}.
 * <p>
 * ps：{@link TaskEntry}就是标准实现。
 *
 * @author wjybxx
 * date - 2024/7/24
 */
public class TaskInlineHelper<T> {

    /** 是否启用内联 */
    public static boolean enableInline = true;
    /** 无效重入id */
    private static final int INVALID_REENTRY_ID = Integer.MIN_VALUE;

    /** 被内联运行的子节点 */
    private transient Task<T> inlinedChild = null;
    /** 被内联的子节点的重入id */
    private transient int inlinedReentryId = INVALID_REENTRY_ID;

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
        while (cur.isInlinable()) {
            if (cur instanceof SingleRunningChildBranch<T> branch) {
                cur = branch.getInlineHelper().getInlinedChild();
                if (cur != null) { // 分支有成功内联数据 -- 高概率
                    break;
                }
                cur = branch.getRunningChild(); // 尝试内联其child
                if (cur == null || cur.isCompleted()) {
                    cur = branch;
                    break;
                }
                continue;
            }
            if (cur instanceof Decorator<T> decorator) {
                cur = decorator.getInlineHelper().getInlinedChild();
                if (cur != null) { // 分支有成功内联数据 -- 高概率
                    break;
                }
                cur = decorator.getChild(); // 尝试内联其child
                if (cur == null || cur.isCompleted()) {
                    cur = decorator;
                    break;
                }
                continue;
            }
            break;
        }
        assert cur.isRunning();
        if (cur == runningChild) {
            // 无实际内联效果时置为null性能更好
            this.inlinedChild = null;
            this.inlinedReentryId = INVALID_REENTRY_ID;
        } else {
            this.inlinedChild = cur;
            this.inlinedReentryId = cur.getReentryId();
        }
    }

    /** 转发事件的工具方法 -- 编写代码时使用该方法，编写完毕后点重构内联(保留该方法) */
    public final void onEvent(Object event, @Nullable Task<T> source) {
        Task<T> inlinedChild = getInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.onEvent(event);
        } else if (source != null) {
            source.onEvent(event);
        }
    }
}