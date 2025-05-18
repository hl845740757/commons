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

using System;
using System.Collections.Generic;
using System.Threading;
using Wjybxx.Disruptor;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 
/// </summary>
public abstract class EventLoopBuilder<T> where T : IAgentEvent
{
    private IEventLoopGroup? _parent;
    private int index = -1;
    private RejectedExecutionHandler _rejectedExecutionHandler = RejectedExecutionHandlers.ABORT;
    private ThreadFactory? _threadFactory;

    private long consumerId;
    private IEventLoopAgent<T>? _agent;
    private readonly List<EventLoopModule> _moduleList = new List<EventLoopModule>();
    private int _batchSize = 1024;

    public EventLoopBuilder() {
    }

    public abstract IEventLoop Build();

    public IEventLoopGroup? Parent {
        get => _parent;
        set => _parent = value;
    }

    /// <summary>
    /// Parent为当前EventLoop分配的索引
    /// </summary>
    public int Index {
        get => index;
        set => index = value;
    }

    public RejectedExecutionHandler RejectedExecutionHandler {
        get => _rejectedExecutionHandler;
        set => _rejectedExecutionHandler = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// 线程工厂
    /// </summary>
    public ThreadFactory? ThreadFactory {
        get => _threadFactory;
        set => _threadFactory = value;
    }

    /// <summary>
    /// 事件循环的消费者id - 未指定将使用<see cref="Thread.ManagedThreadId"/>
    /// </summary>
    public long ConsumerId {
        get => consumerId;
        set => consumerId = value;
    }

    /// <summary>
    /// 事件循环的内部代理
    /// </summary>
    public IEventLoopAgent<T>? Agent {
        get => _agent;
        set => _agent = value;
    }

    /// <summary>
    /// 事件循环的模块
    /// </summary>
    public List<EventLoopModule> ModuleList => _moduleList;

    /// <summary>
    /// 添加模块
    /// </summary>
    /// <param name="module"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void AddModule(EventLoopModule module) {
        if (module == null) throw new ArgumentNullException(nameof(module));
        _moduleList.Add(module);
    }

    public void AddModules(List<EventLoopModule> modules) {
        if (modules == null) throw new ArgumentNullException(nameof(modules));
        _moduleList.AddRange(modules);
    }

    /// <summary>
    /// 最多连续处理多少个事件必须执行一次Update
    /// </summary>
    public int BatchSize {
        get => _batchSize;
        set => _batchSize = value;
    }
}

public class DisruptorEventLoopBuilder<T> : EventLoopBuilder<T> where T : IAgentEvent
{
#nullable disable
    private EventSequencer<T> eventSequencer;
    private WaitStrategy waitStrategy;
    private bool publishValueEventWithCopy;

    public DisruptorEventLoopBuilder() {
    }

    private void CheckBuild() {
        if (ThreadFactory == null) {
            ThreadFactory = new DefaultThreadFactory("DisruptorEventLoop");
        }
        if (eventSequencer == null) {
            throw new IllegalStateException("eventSequencer is null");
        }
    }

#if NET6_0_OR_GREATER
    public override DisruptorEventLoop<T> Build() {
#else
    public override IEventLoop Build() {
#endif
        CheckBuild();
        return new DisruptorEventLoop<T>(this);
    }

    /// <summary>
    /// 事件序列生成器
    /// 注意：应当避免使用无超时的等待策略，EventLoop需要处理定时任务，不能一直等待生产者。
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public EventSequencer<T>? EventSequencer {
        get => eventSequencer;
        set => eventSequencer = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// 等待策略
    /// 1.如果未显式指定，则使用<see cref="Sequencer.WaitStrategy"/>中的默认等待策略。
    /// 2.应当避免使用无超时的等待策略，EventLoop需要处理定时任务，不能一直等待生产者。
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public WaitStrategy WaitStrategy {
        get => waitStrategy;
        set => waitStrategy = value;
    }

    /// <summary>
    /// 当事件类型为值类型时，发布事件时是否采用copy的方式。
    /// 对于无界队列来说，采用copy的方式可以减少一次根据sequence查找data槽的开销，在生产者竞争较强的情况下可以提高性能。
    /// 对于有界队列来说，采用copy可以减少一小部分方法调用，影响可能不大。
    /// 用户需要权衡拷贝1次事件的开销和根据sequence查找data槽的开销。
    /// <see cref="EventSequencer.Publish(long, T)"/>
    /// </summary>
    public bool PublishValueEventWithCopy {
        get => publishValueEventWithCopy;
        set => publishValueEventWithCopy = value;
    }
}
}