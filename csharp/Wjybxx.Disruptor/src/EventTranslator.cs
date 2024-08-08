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
/// 事件转换器，用于将数据添加到队列中的事件对象上
///
/// Q:为什么是接口？
/// A：因为需要支持转为其它接口类型的对象。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface EventTranslator<in T>
{
    /// <summary>
    /// 将数据传输到时间上
    /// </summary>
    /// <param name="eventObj">事件对象</param>
    /// <param name="sequence">事件对应的序号</param>
    void TranslateTo(T eventObj, long sequence);
}
}