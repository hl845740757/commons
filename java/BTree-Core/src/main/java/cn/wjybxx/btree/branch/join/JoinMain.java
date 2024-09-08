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
import cn.wjybxx.btree.TaskStatus;
import cn.wjybxx.btree.branch.*;

/**
 * Main策略，当第一个任务完成时就完成。
 * 类似{@link SimpleParallel}，但Join在得出结果前不重复运行已完成的子节点
 *
 * @author wjybxx
 * date - 2023/12/2
 */
public class JoinMain<T> implements JoinPolicy<T> {

    private static final JoinMain<?> INSTANCE = new JoinMain<>();

    @SuppressWarnings("unchecked")
    public static <T> JoinMain<T> getInstance() {
        return (JoinMain<T>) INSTANCE;
    }

    @Override
    public void resetForRestart() {

    }

    @Override
    public void beforeEnter(Join<T> join) {

    }

    @Override
    public void enter(Join<T> join) {
        if (join.getChildCount() == 0) {
            join.setFailed(TaskStatus.CHILDLESS);
        }
    }

    @Override
    public void onChildCompleted(Join<T> join, Task<T> child) {
        Task<T> mainTask = join.getFirstChild();
        if (child == mainTask) {
            join.setCompleted(child.getStatus(), true);
        }
    }

    @Override
    public void onEvent(Join<T> join, Object event) {
        Task<T> mainTask = join.getFirstChild();
        ParallelChildHelper<T> childHelper = Parallel.getChildHelper(mainTask);

        Task<T> inlinedChild = childHelper.getInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.onEvent(event);
        } else {
            mainTask.onEvent(event);
        }
    }
}