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

/**
 * 该接口用于避免集合的中元素直接实现{@link IndexedElement}，以暴露不必要的接口
 *
 * @author wjybxx
 * date - 2024/8/5
 */
public interface IndexedElementHelper<E> {

    /** 注意：未插入的节点的所以必须初始化为该值 */
    int INDEX_NOT_FOUNT = -1;

    /**
     * 获取对象在集合中的索引
     *
     * @param collection 考虑到一个元素可能在多个队列中，因此传入队列引用
     */
    int collectionIndex(Object collection, E element);

    /**
     * 设置其在集合中的索引
     *
     * @param index 如果是删除元素，则索引为-1
     */
    void collectionIndex(Object collection, E element, int index);

}
