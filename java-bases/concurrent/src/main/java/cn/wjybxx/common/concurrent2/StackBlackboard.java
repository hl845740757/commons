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

package cn.wjybxx.common.concurrent2;

/**
 * 栈式黑板，数据分为本地和共享两块
 *
 * @author wjybxx
 * date - 2023/11/16
 */
public interface StackBlackboard {

    /** 从当前节点开始，向上递归查询 */
    Object getValue(String key);

    /**
     * key如果存在于某个context，则更新对应的context；
     * 如果key不存在，则等价于{@link #localSetValue(String, Object)}
     */
    void setValue(String key, Object value);

    /** 从key对应的上下文删除 */
    Object removeValue(String key);

    /** 只在当前节点查询 */
    Object localGetValue(String key);

    /** 设置到本地 */
    void localSetValue(String key, Object value);

    /** 从当前上下文删除 */
    Object localRemoveValue(String key);

}
