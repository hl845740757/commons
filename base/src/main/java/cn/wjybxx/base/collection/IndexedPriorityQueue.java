/*
 * Copyright 2023 wjybxx(845740757@qq.com)
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

package cn.wjybxx.base.collection;

import java.util.Queue;

/**
 * 参考自netty的实现
 * 由于{@link java.util.Collection}中的API是基于Object的，不利于查询性能，添加了一些限定类型的方法。
 *
 * @author wjybxx
 * date 2023/4/3
 */
public interface IndexedPriorityQueue<T extends IndexedElement> extends Queue<T>, IndexedCollection<T> {

    boolean removeTyped(T node);

    boolean containsTyped(T node);

    /**
     * 队列中节点元素的优先级发生变化时，将通过该方法通知队列调整
     *
     * @param node 发生优先级变更的节点
     */
    void priorityChanged(T node);

}