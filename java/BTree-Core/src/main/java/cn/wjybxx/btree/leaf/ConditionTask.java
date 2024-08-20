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
package cn.wjybxx.btree.leaf;

import cn.wjybxx.btree.LeafTask;
import cn.wjybxx.btree.TaskStatus;

import javax.annotation.Nonnull;

/**
 * 条件节点
 * 注意：并非条件节点必须继承该类。
 *
 * <h3>开销问题</h3>
 * Task类是比较大的，如果项目中有大量的条件，需要考虑开销问题。
 * 一种解决方案是：使用Task类做壳，作为条件测试的入口，内部使用自定义类型。
 * <pre>{@code
 * public class ConditionEntry<T> extends LeafTask<T> {
 *     private int type;
 *     private List<ICondition> children = new ArrayList<ICondition>(4);
 * }
 * }</pre>
 *
 * @author wjybxx
 * date - 2023/11/25
 */
public abstract class ConditionTask<T> extends LeafTask<T> {

    @Override
    protected final void execute() {
        int status = test();
        switch (status) {
            case TaskStatus.NEW,
                 TaskStatus.RUNNING,
                 TaskStatus.CANCELLED -> throw new IllegalStateException("Illegal condition status: " + status);
            case TaskStatus.SUCCESS -> setSuccess();
            default -> setFailed(status);
        }
    }

    /**
     * 检查条件 -- 同步返回
     *
     * @return 状态码
     */
    protected abstract int test();

    /** 条件节点正常情况下不会触发事件 */
    @Override
    public boolean canHandleEvent(@Nonnull Object event) {
        return false;
    }

    /** 条件节点正常情况下不会触发事件 */
    @Override
    protected void onEventImpl(@Nonnull Object event) {

    }

}
