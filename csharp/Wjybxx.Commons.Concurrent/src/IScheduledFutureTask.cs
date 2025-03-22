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
using System.Collections.Generic;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 定时任务
///
/// 部分属性定义在这里，以支持排序等
/// </summary>
public interface IScheduledFutureTask : IFutureTask, IIndexedElement
{
    #region internal

    // 以下接口应该仅用于Executor内部，不应该对用户开放

    /// <summary>
    /// 任务的唯一id，不同的任务之间id不可重复
    /// </summary>
    long Id { get; set; }

    /// <summary>
    /// 是否是周期性任务
    /// </summary>
    bool IsPeriodic { get; }

    /// <summary>
    /// 下次触发时间
    /// </summary>
    long NextTriggerTime { get; set; }

    /// <summary>
    /// 是否已完成首次触发(通常用于降低优先级)
    /// </summary>
    bool IsTriggered { get; }

    /// <summary>
    /// 任务的优先级
    /// </summary>
    int Priority { get; set; }

    /// <summary>
    /// 外部确定性触发
    /// 该方法由EventLoop调用，不需要回调的方式重新压入队列，而是返回bool值告知EventLoop是否需要继续执行
    /// </summary>
    /// <param name="tickTime">当前时间戳</param>
    /// <returns>是否还需要压入队列</returns>
    bool Trigger(long tickTime);

    /// <summary>
    /// 监听取消令牌中的取消信号
    /// 1. 该方法由EventLoop调用，通常在Task成功压入队列后调用。
    /// 2. 可检测是否已注册。
    /// </summary>
    void RegisterCancellation();

    #endregion
}

/// <summary>
/// 默认的比较器
/// </summary>
public sealed class ScheduledTaskComparator : IComparer<IScheduledFutureTask>
{
    public int Compare(IScheduledFutureTask? lhs, IScheduledFutureTask? rhs) {
        if (lhs == null) throw new ArgumentNullException(nameof(lhs));
        if (rhs == null) throw new ArgumentNullException(nameof(rhs));
        if (ReferenceEquals(lhs, rhs)) {
            return 0;
        }
        int r = lhs.NextTriggerTime.CompareTo(rhs.NextTriggerTime);
        if (r != 0) {
            return r;
        }
        // 未触发的放前面
        r = lhs.IsTriggered.CompareTo(rhs.IsTriggered);
        if (r != 0) {
            return r;
        }
        // 再按优先级排序
        r = lhs.Priority.CompareTo(rhs.Priority);
        if (r != 0) {
            return r;
        }
        // 再按id排序
        r = lhs.Id.CompareTo(rhs.Id);
        if (r == 0) {
            throw new InvalidOperationException($"lhs.id: {lhs.Id}, rhs.id: {rhs.Id}");
        }
        return r;
    }
}
}