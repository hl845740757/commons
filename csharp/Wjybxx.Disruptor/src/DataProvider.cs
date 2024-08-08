#region LICENSE

// Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

namespace Wjybxx.Disruptor
{
/// <summary>
/// 数据提供者
/// </summary>
public interface DataProvider<T>
{
    /// <summary>
    /// 根据指定序号获取data
    /// 该接口可用于生产者和消费者获取数据，但对于非固定大小的数据结构而言，可能有较长的查询路径。
    /// </summary>
    /// <param name="sequence"></param>
    /// <returns></returns>
    T Get(long sequence);

    /// <summary>
    /// 该接口用于优化生产者查询数据
    /// </summary>
    /// <param name="sequence"></param>
    /// <returns></returns>
    T ProducerGet(long sequence);

    /// <summary>
    /// 该接口用于优化消费者查询数据
    /// </summary>
    /// <param name="sequence"></param>
    /// <returns></returns>
    T ConsumerGet(long sequence);

    /// <summary>
    /// 该接口用于生产者填充数据\
    /// 1. 当拷贝既有数据成本较高时可替换既有对象
    /// 2. set不提供特殊的内存语义，因此只应该生产者调用
    /// </summary>
    /// <param name="sequence"></param>
    /// <param name="data"></param>
    void ProducerSet(long sequence, T data);

    /// <summary>
    /// 该接口用于消费者覆盖数据(通常用于删除数据)
    /// 1.当使用无界队列需要即时清理内存时使用。
    /// 2.set不提供特殊的内存语义，因此只应该由末尾的消费者调用
    /// </summary>
    /// <param name="sequence"></param>
    /// <param name="data"></param>
    void ConsumerSet(long sequence, T data);

    /// <summary>
    /// 该接口用于优化生产者查询数据
    /// 1. 用于避免T为结构体类型时的拷贝，c#特殊支持；
    /// 2. 使用普通语义读写即可，可见性由<see cref="SequenceBarrier"/>保证；
    /// </summary>
    /// <param name="sequence"></param>
    /// <returns></returns>
    ref T ProducerGetRef(long sequence);

    /// <summary>
    /// 该接口用于优化消费者查询数据
    /// 1. 用于避免T为结构体类型时的拷贝，c#特殊支持；
    /// 2. 使用普通语义读写即可，可见性由<see cref="SequenceBarrier"/>保证；
    /// </summary>
    /// <param name="sequence"></param>
    /// <returns></returns>
    ref T ConsumerGetRef(long sequence);
}
}