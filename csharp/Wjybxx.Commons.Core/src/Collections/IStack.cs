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

namespace Wjybxx.Commons.Collections;

/// <summary>
/// 栈结构
/// 注意：Add方法在Stack这里语义是不清楚的，应当避免使用；将Add强制约定为栈顶不是很合适，多数情况下Add都是期望插到尾部。
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IStack<T> : IGenericCollection<T>
{
    /// <summary>
    /// 将一个元素入栈
    /// </summary>
    /// <param name="item"></param>
    void Push(T item);

    /// <summary>
    /// 尝试将元素入栈
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    bool TryPush(T item);

    /// <summary>
    /// 弹出栈顶元素
    /// </summary>
    /// <returns></returns>
    T Pop();

    /// <summary>
    /// 尝试弹出栈顶元素
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    bool TryPop(out T item);

    /// <summary>
    /// 查看栈顶元素
    /// (调整命名以避免接口之间冲突)
    /// </summary>
    /// <exception cref="InvalidOperationException">如果队列为空</exception>
    /// <returns></returns>
    T PeekTop();

    /// <summary>
    /// 查看栈顶元素
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    bool TryPeekTop(out T item);
}