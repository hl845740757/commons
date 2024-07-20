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
 * 类数组(ArrayLike)对象池抽象
 * 类数组的定义：对象和数组一样固定长度(空间)，不可自动扩容，常见于数组的封装类。
 *
 * @param <T> 数组的类型
 * @author wjybxx
 * date - 2024/1/3
 */
public interface ArrayLikePool<T> extends ObjectPool<T> {

    /** 返回池中一个默认大小的数组 */
    @Override
    T acquire();

    /**
     * 1.返回的数组可能大于期望的数组长度
     * 2.默认情况下不清理
     *
     * @param minimumLength 期望的最小数组长度
     * @return 池化的字节数组
     */
    T acquire(int minimumLength);
}