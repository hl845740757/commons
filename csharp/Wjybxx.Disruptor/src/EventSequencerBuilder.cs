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
using System.Threading;

namespace Wjybxx.Disruptor
{
public abstract class EventSequencerBuilder<T>
{
    private readonly Func<T> factory;
    private int spinIterations = 10;
    private WaitStrategy waitStrategy = TimeoutSleepingWaitStrategy.Inst;
    private SequenceBlocker? blocker;

    protected EventSequencerBuilder(Func<T> factory) {
        this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public abstract EventSequencer<T> Build();

    /// <summary>
    /// 事件对象工厂
    /// </summary>
    public Func<T> Factory => factory;

    /// <summary>
    /// 自旋参数。
    /// 1. 大于0表示自旋，否则表示Sleep 1毫秒。
    /// 2. 自旋<see cref="Thread.SpinWait(int)"/>,睡眠<see cref="Thread.Sleep(int)"/>.
    /// 
    /// ps: 由于C#的sleep最小单位是毫秒，而1毫秒时间已足够长，因此不需要配置睡眠时间。
    /// 由于C#的SpinWait提供了参数，那就入乡随俗提供支持。
    /// </summary>
    public int ProducerSpinIterations {
        get => spinIterations;
        set => spinIterations = value;
    }

    /// <summary>
    /// 消费者默认的等待策略
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public WaitStrategy WaitStrategy {
        get => waitStrategy;
        set => waitStrategy = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// 启用序号阻塞器。
    /// 1. 如果存在需要通过{@link Condition}等待生产者发布序号的消费者，则需要启用blocker。
    /// 2. 默认情况下不启用。
    /// </summary>
    public void EnableBlocker() {
        blocker = new SequenceBlocker();
    }

    public void DisableBlocker() {
        blocker = null;
    }

    /// <summary>
    /// 序列阻塞器
    /// </summary>
    public SequenceBlocker? Blocker => blocker;
}
}