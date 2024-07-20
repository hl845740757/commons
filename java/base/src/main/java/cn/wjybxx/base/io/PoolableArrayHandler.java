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
 * 类数组对象的处理器
 *
 * @author wjybxx
 * date - 2024/7/19
 */
public interface PoolableArrayHandler<T> {

    /** 获取实例的空间 */
    int getCapacity(T obj);

    /**
     * 创建对象
     *
     * @param pool     请求创建对象的池
     * @param capacity 容量参数
     */
    T create(ObjectPool<? super T> pool, int capacity);

    /** 重置对象数据 */
    void reset(T obj);

    /** 验证对象的有效性 */
    boolean validate(T obj);

    /**
     * 销毁对象
     * 1.对象未能归还到池中，或对象池清理时调用。
     * 2.可能是一个无效状态的对象
     * 3.这类对象通常与IO操作相关，可能有必须释放的资源
     */
    void destroy(T obj);

}