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

import cn.wjybxx.btree.TaskStatus;
import cn.wjybxx.btree.branch.SelectorN;

import java.util.List;

/**
 * 对应{@link SelectorN}，但无快速失败逻辑
 *
 * @param <T> 黑板类型
 * @author wjybxx
 */
public class ConditionCount<T> extends ConditionGroup<T> {

    private int required; // 还可以支持成功或失败再计数

    public ConditionCount() {
    }

    public ConditionCount(List<ICondition<T>> children) {
        super(children);
    }

    @Override
    public int test(T blackboard) {
        if (required <= 0) return 0;
        if (children.size() < required) {
            return TaskStatus.INSUFF_CHILD;
        }
        int count = 0;
        int errorCode = TaskStatus.ERROR; // 可能没有失败子节点
        for (ICondition<T> child : children) {
            int code = child.test(blackboard);
            if (code == 0) {
                if (++count == required) {
                    return 0;
                }
            } else {
                if (errorCode == TaskStatus.ERROR) {
                    errorCode = code;
                }
            }
        }
        return errorCode;
    }

    public int getRequired() {
        return required;
    }

    public void setRequired(int required) {
        this.required = required;
    }
}
