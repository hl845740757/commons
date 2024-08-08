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
/// 生产者类型
/// 
/// 单生产者和多生产的主要差别在空间分配上(序号分配上)。
/// 在Disruptor下，其实都是多消费者模式，并没有针对单消费者的优化。
/// </summary>
public enum ProducerType
{
    /// <summary>
    /// 单生产者
    /// </summary>
    Single = 0,

    /// <summary>
    /// 多生产者
    /// </summary>
    Multi = 1,
}
}