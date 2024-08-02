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
package cn.wjybxx.btree.branch.join;

import cn.wjybxx.btree.Task;
import cn.wjybxx.btree.branch.Join;
import cn.wjybxx.btree.branch.JoinPolicy;

/**
 * 默认的AnyOf，不特殊处理取消
 * 相当于并发编程中的anyOf
 *
 * @author wjybxx
 * date - 2023/12/2
 */
public class JoinAnyOf<T> implements JoinPolicy<T> {

    private static final JoinAnyOf<?> INSTANCE = new JoinAnyOf<>();

    @SuppressWarnings("unchecked")
    public static <T> JoinAnyOf<T> getInstance() {
        return (JoinAnyOf<T>) INSTANCE;
    }

    @Override
    public void resetForRestart() {

    }

    @Override
    public void beforeEnter(Join<T> join) {

    }

    @Override
    public void enter(Join<T> join) {
        // 不能成功，失败也不能
        if (join.getChildCount() == 0) {
            Task.logger.info("JonAnyOf: children is empty");
        }
    }

    @Override
    public void onChildCompleted(Join<T> join, Task<T> child) {
        join.setCompleted(child.getStatus(), true);
    }

    @Override
    public void onEvent(Join<T> join, Object event) {

    }
}