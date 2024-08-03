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

import cn.wjybxx.base.CollectionUtils;
import cn.wjybxx.base.annotation.VisibleForTesting;

import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.Collections;
import java.util.List;
import java.util.Objects;
import java.util.stream.Stream;

/**
 * 分支任务（可能有多个子节点）
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class BranchTask<T> extends Task<T> {

    protected List<Task<T>> children;

    public BranchTask() {
        this.children = new ArrayList<>(4);
    }

    public BranchTask(List<Task<T>> children) {
        this.children = Objects.requireNonNull(children);
    }

    public BranchTask(Task<T> first, @Nullable Task<T> second) {
        Objects.requireNonNull(first);
        this.children = new ArrayList<>(2);
        this.children.add(first);
        if (second != null) {
            this.children.add(second);
        }
    }

    // region

    /** 是否是第一个子节点 */
    public final boolean isFirstChild(Task<?> child) {
        if (this.children.isEmpty()) {
            return false;
        }
        return this.children.get(0) == child;
    }

    /** 是否是第最后一个子节点 */
    public final boolean isLastChild(Task<?> child) {
        if (children.isEmpty()) {
            return false;
        }
        return children.getLast() == child;
    }

    /** 获取第一个子节点 -- 主要为MainPolicy提供帮助 */
    public final Task<T> getFirstChild() {
        return children.getFirst();
    }

    /** 获取最后一个子节点 */
    public final Task<T> getLastChild() {
        return children.getLast();
    }

    public boolean isAllChildCompleted() {
        // 在判断是否全部完成这件事上，逆序遍历有优势
        for (int idx = children.size() - 1; idx >= 0; idx--) {
            Task<?> child = children.get(idx);
            if (child.isRunning()) {
                return false;
            }
        }
        return true;
    }

    /** 用于避免测试的子节点过于规律 */
    @VisibleForTesting
    public final void shuffleChild() {
        Collections.shuffle(children);
    }

    // endregion

    // region child

    @Override
    public final void removeAllChild() {
        children.forEach(Task::unsetControl);
        children.clear();
    }

    @Override
    public final int indexChild(Task<?> task) {
        return CollectionUtils.indexOfRef(children, task);
    }

    @Override
    public final Stream<Task<T>> childStream() {
        return children.stream();
    }

    @Override
    public final int getChildCount() {
        return children.size();
    }

    @Override
    public final Task<T> getChild(int index) {
        return children.get(index);
    }

    @Override
    protected final int addChildImpl(Task<T> task) {
        children.add(task);
        return children.size() - 1;
    }

    @Override
    protected final Task<T> setChildImpl(int index, Task<T> task) {
        return children.set(index, task);
    }

    @Override
    protected final Task<T> removeChildImpl(int index) {
        return children.remove(index);
    }

    // endregion

    public List<Task<T>> getChildren() {
        return children;
    }

    public void setChildren(List<Task<T>> children) {
        if (children == null) {
            this.children.clear();
        } else {
            this.children = children;
        }
    }
}
