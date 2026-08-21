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

import java.util.List;

/**
 * 所有条件都成功，则成功
 *
 * @param <T> 黑板类型
 * @author wjybxx
 */
public class ConditionAnd<T> extends ConditionGroup<T> {

    public ConditionAnd() {
    }

    public ConditionAnd(List<ICondition<T>> children) {
        super(children);
    }

    @Override
    public int test(T blackboard) {
        for (int idx = 0; idx < children.size(); idx++) {
            int code = children.get(idx).test(blackboard);
            if (code != 0) return code;
        }
        return 0;
    }
}
