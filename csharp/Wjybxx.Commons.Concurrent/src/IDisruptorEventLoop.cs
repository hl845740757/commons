#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

using System;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 该接口用于外部获取事件的类型，测试事件类型的兼容性
/// </summary>
public interface IDisruptorEventLoop
{
    /// <summary>
    /// 事件的类型
    /// </summary>
    Type EventType { get; }
}

/// <summary>
/// 基于Disruptor架构的事件循环需要对外开放的接口
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDisruptorEventLoop<T> : IDisruptorEventLoop, IEventLoop where T : IAgentEvent
{
    /// <summary>
    /// 事件的类型
    /// </summary>
    Type IDisruptorEventLoop.EventType => typeof(T);

    /// <summary>
    /// 获取序号对应的事件 -- 适用Class类型事件
    /// </summary>
    /// <param name="sequence">申请到的序号</param>
    /// <returns></returns>
    T GetEvent(long sequence);

    /// <summary>
    /// 获取序号对应的事件引用 -- 适用结构体类型事件
    /// </summary>
    /// <param name="sequence"></param>
    /// <returns></returns>
    ref T GetEventRef(long sequence);

    /// <summary>
    /// 设置序号对应的事件 -- 适用结构体类型事件
    /// </summary>
    /// <param name="sequence"></param>
    /// <param name="eventObj"></param>
    void SetEvent(long sequence, T eventObj);

    /// <summary>
    ///
    /// </summary>
    /// <param name="size">申请的序号数</param>
    /// <returns>如果申请成功，则返回对应的sequence，否则返回-1</returns>
    long TryNextSequence(int size = 1);

    /// <summary>
    /// 发布事件
    ///
    /// 该接口为C#特殊支持，当事件为值类型且事件生成器为无界队列时，聚合数据写入和发布操作，可以提高生产者的效率。
    /// </summary>
    /// <param name="sequence"></param>
    /// <param name="evt"></param>
    void Publish(long sequence, in T evt);
    
    /// <summary>
    /// 申请事件序号
    /// 1.按照规范，在调用该方法后，必须在finally块中进行发布。
    /// 2.事件类型必须大于等于0，否则可能导致异常
    /// 3.返回值为null时必须检查
    /// <code>
    ///    long sequence = eventLoop.NextSequence();
    ///    try {
    ///         AgentEvent event = eventLoop.GetEvent(sequence);
    ///         // Do work.
    ///    } finally {
    ///          eventLoop.Publish(sequence)
    ///    }
    /// </code>
    /// </summary>
    /// <returns>如果申请成功，则返回对应的sequence，否则返回-1</returns>
    long NextSequence();

    /// <summary>
    /// 发布申请的序号
    /// </summary>
    /// <param name="sequence"></param>
    void Publish(long sequence);

    /// <summary>
    /// 批量申请事件序号
    /// 1.按照规范，在调用该方法后，必须在finally块中进行发布。
    /// 2.事件类型必须大于等于0，否则可能导致异常
    /// 3.返回值为null时必须检查
    /// <code>
    ///   int n = 10;
    ///   long hi = eventLoop.NextSequence(n);
    ///   try {
    ///      long lo = hi - (n - 1);
    ///      for (long sequence = lo; sequence &lt;= hi; sequence++) {
    ///          AgentEvent event = eventLoop.GetEvent(sequence);
    ///          // Do work.
    ///      }
    ///   } finally {
    ///      eventLoop.Publish(lo, hi);
    ///   }
    /// </code>
    /// </summary>
    /// <param name="size">申请的空间大小</param>
    /// <returns>如果申请成功，则返回申请空间的最大序号，否则返回-1</returns>
    long NextSequence(int size);

    /// <summary>
    /// 发布申请的序号
    /// </summary>
    /// <param name="lo">inclusive</param>
    /// <param name="hi">inclusive</param>
    void Publish(long lo, long hi);

    /// <summary>
    /// 订阅事件
    /// <see cref="IEventLoopModule"/>应当在启动时注册
    /// </summary>
    /// <param name="type">事件类型</param>
    /// <param name="handler">事件处理器</param>
    void Subscribe(int type, IAgentEventHandler<T> handler);
}
}