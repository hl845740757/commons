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
/// <summary>
/// 序号阻塞器
///
/// 在Disruptor的设计中，消费者只会条件等待生产者的序号发布，而不会条件等待前置消费者的序号发布，生产者也不会条件等待gating消费者的序号发布。
/// 这有一定的缺陷，但我将沿用这个设定，有以下理由：
/// 1. 生产者和消费者会构成复杂的回路，使用条件锁将极大增加wait/notify的复杂度，死锁风险高。
/// 2. 彼此都使用条件锁通知，会产生大量的notify调用，这对吞吐量会造成影响。
///
/// 原始设计的缺陷是什么？
/// 1. 由于只支持消费者可条件等待生产者，其它情况下的依赖都是通过观察sequence的更新实现的。
/// 2. 当使用Blocking策略时，如果当前消费者速度较快，而前置消费者速度很慢，那么消费者会消耗大量的CPU等待序号可用。
/// 3. Disruptor不支持每一级消费者使用不同的等待策略，因此这个问题需要重写Blocking策略实现 -- 将等待前置消费者的部分修改为sleep模式。
/// </summary>
public sealed class SequenceBlocker
{
    /// <summary>
    /// c#似乎并没有实质的Monitor以外的机制，我们直接通过Monitor封装
    /// </summary>
    private readonly object lockObject = new object();

    public void Lock() {
        Monitor.Enter(lockObject);
    }

    public void Unlock() {
        Monitor.Exit(lockObject);
    }

    public void Await() {
        Monitor.Wait(lockObject);
    }

    public void Await(TimeSpan timeout) {
        Monitor.Wait(lockObject, timeout);
    }

    public void Await(int millisecondsTimeout) {
        Monitor.Wait(lockObject, millisecondsTimeout);
    }

    /// <summary>
    /// 唤醒所有等待的线程
    /// 注意：
    /// 1.该接口由该屏障的上游或第三方在停止时调用。
    /// 2.唤醒不意味着等待的序号已变为可用，在醒来后需要检查中断和终止信号。
    /// 3.由于消费者共用一个Blocker，因此还会受到其它消费者的牵连。
    /// </summary>
    public void SignalAll() {
        Monitor.PulseAll(lockObject);
    }
}
}