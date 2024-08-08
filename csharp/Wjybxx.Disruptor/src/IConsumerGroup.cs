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
/// 消费者组
/// 
/// 1. 组可以单线程的或多线程的，外部不关注；外部只关注其关联的屏障<see cref="ConsumerBarrier"/>。
/// 2. 屏障负责管理进度信息，消费者负责真正的消费。
/// 3. 该接口只是一个示例实现，Barrier并没有依赖该接口 -- 依赖是反转的。
/// </summary>
public interface IConsumerGroup
{
    /// <summary>
    /// 消费组关联的屏障
    ///
    /// 1. 消费者需要更新<see cref="SequenceBarrier.GroupSequence"/>以更新屏障的进度。
    /// 2. 消费者需要响应<see cref="ConsumerBarrier.Alert"/>信号，以及时响应停止。
    /// 3. 多线程消费者需要通过<see cref="ConsumerBarrier.MemberSequence"/>获取自身的sequence。
    /// </summary>
    ConsumerBarrier Barrier { get; }
}
}