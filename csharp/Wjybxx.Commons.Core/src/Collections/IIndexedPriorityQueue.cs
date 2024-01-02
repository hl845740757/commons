#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 在Item上存储了索引的优先级队列
/// (主要提高删除效率)
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IIndexedPriorityQueue<T> : IIndexedCollection<T>, IQueue<T> where T : class, IIndexedElement
{
    /// <summary>
    /// 队列中元素的优先级发生了变更，通知队列调整结构
    /// </summary>
    /// <param name="item"></param>
    void PriorityChanged(T item);
}