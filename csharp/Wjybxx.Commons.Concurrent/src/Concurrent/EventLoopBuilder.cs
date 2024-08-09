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

    private IEventLoopAgent<T>? _agent = EmptyAgent<T>.Inst;
    private IEventLoopModule? _mainModule;
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
    /// 事件循环的内部代理
    /// </summary>
    public IEventLoopAgent<T>? Agent {
        get => _agent;
        set => _agent = value;
    }

    /// <summary>
    /// 事件循环的主模块
    /// </summary>
    public IEventLoopModule? MainModule {
        get => _mainModule;
        set => _mainModule = value;
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
    private EventSequencer<T> eventSequencer;
    private WaitStrategy? waitStrategy;
    private bool cleanEventAfterConsumed = true;
    private bool cleanBufferOnExit = true;

    private void CheckBuild() {
        if (ThreadFactory == null) {
            ThreadFactory = new DefaultThreadFactory("DisruptorEventLoop");
        }
        if (eventSequencer == null) {
            throw new IllegalStateException("eventSequencer is null");
        }
    }

#if UNITY_EDITOR
    public override IEventLoop Build() {
        CheckBuild();
        return new DisruptorEventLoop<T>(this);
    }
#else
    public override DisruptorEventLoop<T> Build() {
        CheckBuild();
        return new DisruptorEventLoop<T>(this);
    }
#endif

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
    public WaitStrategy? WaitStrategy {
        get => waitStrategy;
        set => waitStrategy = value;
    }

    /// <summary>
    /// 在消费事件后是否调用<see cref="IAgentEvent.Clean()"/>方法清理引用数据 
    /// </summary>
    public bool CleanEventAfterConsumed {
        get => cleanEventAfterConsumed;
        set => cleanEventAfterConsumed = value;
    }

    /// <summary>
    /// EventLoop在退出的时候是否清理buffer
    /// 1. 默认清理
    /// 2. 如果该值为true，意味着当前消费者是消费者的末端，或仅有该EventLoop消费者。
    /// </summary>
    public bool CleanBufferOnExit {
        get => cleanBufferOnExit;
        set => cleanBufferOnExit = value;
    }
}
}