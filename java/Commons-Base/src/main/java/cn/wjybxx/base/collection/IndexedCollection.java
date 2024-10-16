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

package cn.wjybxx.base.collection;

import java.util.Collection;

/**
 * 在元素身上存储了索引信息的集合。
 * 1.这类集合禁止重复添加元素，且使用引用相等判断重复
 * 2.更多用于非连续存储的集合。
 * <p>
 * Q：在元素身上存储索引的好处？
 * A：索引信息存储在元素上，可大幅提高查找效率。
 *
 * @author wjybxx
 * date - 2023/12/21
 */
public interface IndexedCollection<E> extends Collection<E> {

    boolean removeTyped(E node);

    boolean containsTyped(E node);

    /**
     * 清除队列中的所有元素，并不更新队列中节点的索引，通常用在最后清理释放内存的时候。
     * (请确保调用该方法后，不会再访问该集合)
     */
    void clearIgnoringIndexes();

}
