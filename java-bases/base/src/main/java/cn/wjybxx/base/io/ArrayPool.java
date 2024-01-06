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

package cn.wjybxx.base.io;

import cn.wjybxx.base.pool.ObjectPool;

/**
 * 数组池
 * Java不是真泛型，因此泛型不是数组元素的类型，而是数组的类型。
 *
 * @param <T> 数组的类型
 * @author wjybxx
 * date - 2024/1/3
 */
public interface ArrayPool<T> extends ObjectPool<T> {

    /**
     * 注意：返回的字节数组可能大于期望的数组长度
     *
     * @param minimumLength 期望的最小数组长度
     * @return 池化的字节数组
     */
    T rent(int minimumLength);

    /** 归还数组到池，默认情况下不清理数据 */
    @Override
    default void returnOne(T array) {
        returnOne(array, false);
    }

    /**
     * 归还数组到池
     *
     * @param array 租借的对象
     * @param clear 是否清理数组
     */
    void returnOne(T array, boolean clear);
}