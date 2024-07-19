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
 * 可此话对象处理器
 *
 * @author wjybxx
 * date - 2024/7/19
 */
public interface PoolableObjectHandler<T> {

    /**
     * 创建对象
     *
     * @param pool     请求创建对象的池
     * @param capacity 容量参数(主要为数组类对象池使用)，0表示未指定
     */
    T create(ObjectPool<? super T> pool, int capacity);

    /**
     * 测试对象是否可以归还到池中
     * 该方法在reset方法之前调用，成功则走reset然后尝试归还到池，否则直接销毁。
     */
    boolean test(T obj);

    /**
     * 重置对象数据
     */
    void reset(T obj);

    /**
     * 销毁对象
     * 1.对象未能归还到池中，或对象池清理时调用。
     * 2.可能需要处理池外对象
     */
    void destroy(T obj);


}