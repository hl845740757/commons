/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
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
 * 常用的访问者
 *
 * @author wjybxx
 * date - 2024/9/5
 */
public class TaskVisitors {

    @SuppressWarnings("unchecked")
    public static <T> TaskVisitor<T> refreshActive() {
        return (TaskVisitor<T>) RefreshActiveVisitor.INST;
    }

    @SuppressWarnings("unchecked")
    public static <T> TaskVisitor<T> resetForRestart() {
        return (TaskVisitor<T>) ResetForRestartVisitor.INST;
    }

    private static class RefreshActiveVisitor<T> implements TaskVisitor<T> {
        private static final RefreshActiveVisitor<?> INST = new RefreshActiveVisitor<>();

        @Override
        public void visitChild(Task<? extends T> child, int index, Object param) {
            child.refreshActiveInHierarchy();
        }

        @Override
        public void visitHook(Task<? extends T> child, Object param) {
            child.refreshActiveInHierarchy();
        }
    }

    private static class ResetForRestartVisitor<T> implements TaskVisitor<T> {

        private static final ResetForRestartVisitor<?> INST = new ResetForRestartVisitor<>();

        @Override
        public void visitChild(Task<? extends T> child, int index, Object param) {
            child.resetForRestart();
        }

        @Override
        public void visitHook(Task<? extends T> child, Object param) {
            child.resetForRestart();
        }
    }
}