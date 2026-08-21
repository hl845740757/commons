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
 * 额外的条件节点抽象
 * <p>
 * 注：该抽象的目的在于减少条件节点的开销 —— Task实现条件的成本较高，包含大量的状态维护。
 *
 * @param <T> 黑板类型
 * @author wjybxx
 */
@SerializeReference
public interface ICondition<T> {

    /**
     * 条件测试
     * <p>
     * 注意：成功码固定为0，失败码应当从4开始，即{@link TaskStatus#ERROR}
     *
     * @return 错误码
     */
    int test(T blackboard);

}
