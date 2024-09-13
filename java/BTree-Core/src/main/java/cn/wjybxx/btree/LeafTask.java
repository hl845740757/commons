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

/**
 * 叶子任务（不能有子节点）
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class LeafTask<T> extends Task<T> {

    @Override
    protected final void onChildRunning(Task<T> child, boolean starting) {
        throw new AssertionError();
    }

    @Override
    protected final void onChildCompleted(Task<T> child) {
        throw new AssertionError();
    }

    // region child

    @Override
    public final int indexChild(Task<?> task) {
        return -1;
    }

    @Override
    public final void visitChildren(TaskVisitor<? super T> visitor, Object param) {

    }

    @Override
    public final int getChildCount() {
        return 0;
    }

    @Override
    public final Task<T> getChild(int index) {
        throw new IndexOutOfBoundsException("Leaf task can not have any children");
    }

    @Override
    protected final int addChildImpl(Task<T> task) {
        throw new IllegalStateException("Leaf task can not have any children");
    }

    @Override
    protected final Task<T> setChildImpl(int index, Task<T> task) {
        throw new IllegalStateException("Leaf task can not have any children");
    }

    @Override
    protected final Task<T> removeChildImpl(int index) {
        throw new IndexOutOfBoundsException("Leaf task can not have any children");
    }

    // endregion
}