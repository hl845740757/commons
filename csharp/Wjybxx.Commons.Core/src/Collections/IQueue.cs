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

using System;
using System.Collections.Generic;

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 队列
/// 注意：队列的所有操作都是隐含顺序的，包括Add方法，因此Reverse是可以反转的。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IQueue<T> : IGenericCollection<T>
{
    /// <summary>
    /// 将元素压入队列
    /// </summary>
    /// <param name="item"></param>
    void Enqueue(T item);

    /// <summary>
    /// 尝试将元素压入队列
    /// </summary>
    /// <param name="item"></param>
    /// <returns>压入成功则返回true</returns>
    bool TryEnqueue(T item);

    /// <summary>
    /// 弹出队首元素
    /// </summary>
    /// <returns></returns>
    T Dequeue();

    /// <summary>
    /// 尝试弹出队首元素
    /// </summary>
    /// <param name="item"></param>
    /// <returns>队列不为空则返回true</returns>
    bool TryDequeue(out T item);

    /// <summary>
    /// 查看队首元素
    /// (调整命名以避免接口之间冲突)
    /// </summary>
    /// <exception cref="InvalidOperationException">如果队列为空</exception>
    /// <returns></returns>
    T PeekHead();

    /// <summary>
    /// 查看队首元素
    /// </summary>
    /// <param name="item"></param>
    /// <returns>队列不为空则返回true</returns>
    bool TryPeekHead(out T item);

    #region 接口适配

    void ICollection<T>.Add(T item) {
        Enqueue(item);
    }

    #endregion
}