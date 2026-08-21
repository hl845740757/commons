/*
 * Copyright 2025 wjybxx(845740757@qq.com)
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
package cn.wjybxx.btree.condition;

import cn.wjybxx.base.SerializeReference;
import cn.wjybxx.btree.TaskStatus;

/**
 * 条件取反
 *
 * @param <T> 黑板类型
 * @author wjybxx
 */
public class ConditionNot<T> implements ICondition<T> {

    @SerializeReference
    private ICondition<T> child;

    public ConditionNot() {
    }

    public ConditionNot(ICondition<T> child) {
        this.child = child;
    }

    @Override
    public int test(T blackboard) {
        if (child == null) {
            throw new IllegalStateException("child is null");
        }
        return child.test(blackboard) == 0 ? TaskStatus.ERROR : 0;
    }

    public ICondition<T> getChild() {
        return child;
    }

    public void setChild(ICondition<T> child) {
        this.child = child;
    }
}
