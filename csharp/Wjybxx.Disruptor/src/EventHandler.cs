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
/// 事件处理器
///
/// ps:你可以实现自己的事件处理器和事件处理接口，这里的接口仅做参考。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface EventHandler<in T>
{
    /// <summary>
    /// 接收到一个事件
    /// 注意：如果消费者是多线程消费者，sequence可能不是有序（连续）的。
    /// </summary>
    /// <param name="eventObj"></param>
    /// <param name="sequence"></param>
    void OnEvent(T eventObj, long sequence);
}
}