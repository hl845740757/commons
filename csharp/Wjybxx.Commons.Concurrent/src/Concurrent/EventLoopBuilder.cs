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

#pragma warning disable CS1591

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 
/// </summary>
public class EventLoopBuilder
{
    private IEventLoopGroup? _parent;
    private RejectedExecutionHandler _rejectedExecutionHandler = RejectedExecutionHandlers.ABORT;
    private ThreadFactory? _threadFactory;

    private IEventLoopAgent<IAgentEvent>? _agent = EmptyAgent<IAgentEvent>.INST;
    private IEventLoopModule? _mainModule;
    private int _waitTaskSpinTries = 10;
    private int _batchSize = 1024;

    private EventLoopBuilder() {
    }

    /// <summary>
    /// 创建一个Builder
    /// 
    /// ps:尽量使用静态方法创建，以避免依赖具体的类型
    /// </summary>
    /// <returns></returns>
    public static EventLoopBuilder NewBuilder() {
        return new EventLoopBuilder();
    }

    /// <summary>
    /// 创建一个Builder
    /// </summary>
    /// <param name="threadFactory">线程工厂</param>
    /// <returns></returns>
    public static EventLoopBuilder NewBuilder(ThreadFactory threadFactory) {
        EventLoopBuilder builder = new EventLoopBuilder();
        builder.ThreadFactory = threadFactory;
        return builder;
    }

    public virtual IEventLoop Build() {
        return new DefaultEventLoop(this);
    }

    public IEventLoopGroup? Parent {
        get => _parent;
        set => _parent = value;
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
    public IEventLoopAgent<IAgentEvent>? Agent {
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
    /// 等待任务时的自旋次数
    /// </summary>
    public int WaitTaskSpinTries {
        get => _waitTaskSpinTries;
        set => _waitTaskSpinTries = value;
    }

    /// <summary>
    /// 最多连续处理多少个事件必须执行一次Update
    /// </summary>
    public int BatchSize {
        get => _batchSize;
        set => _batchSize = value;
    }
}