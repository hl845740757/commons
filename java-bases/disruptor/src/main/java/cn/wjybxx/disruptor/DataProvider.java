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
package cn.wjybxx.disruptor;

/**
 * 数据提供者
 *
 * @param <T>
 * @author wjybxx
 * date - 2024/1/16
 */
public interface DataProvider<T> {

    /**
     * 根据指定序号获取data
     * 该接口可用于生产者和消费者获取数据，但对于非固定大小的数据结构而言，可能有较长的查询路径。
     */
    T get(long sequence);

    /** 该接口用于优化生产者查询数据 */
    T producerGet(long sequence);

    /** 该接口用于优化消费者查询数据 */
    T consumerGet(long sequence);

    /**
     * 该接口用于生产者填充数据
     * 1. 当拷贝既有数据成本较高时可替换既有对象
     * 2. set不提供特殊的内存语义，因此只应该生产者调用
     */
    void producerSet(long sequence, T data);

    /**
     * 该接口用于消费者覆盖数据(通常用于删除数据)
     * 1.当使用无界队列需要即使清理内存时使用。
     * 2.set不提供特殊的内存语义，因此只应该由末尾的消费者调用
     */
    void consumerSet(long sequence, T data);
}