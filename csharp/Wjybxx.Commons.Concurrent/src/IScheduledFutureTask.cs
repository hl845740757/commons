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
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 定时任务
///
/// 注：主要用于解决泛型问题。
/// </summary>
public interface IScheduledFutureTask : IFutureTask, IIndexedElement
{
#nullable disable

    #region 基础属性设置

    ISchedulerHelper Helper { get; set; }

    /// <summary>
    /// 任务的唯一id，不同的任务之间id不可重复
    /// </summary>
    long Id { get; set; }

    /// <summary>
    /// 调度类型
    /// </summary>
    int ScheduleType { get; set; }

    /// <summary>
    /// 下次触发时间
    /// </summary>
    long TriggerTime { get; set; }

    /// <summary>
    /// 触发周期
    /// </summary>
    long Period { get; set; }

    /// <summary>
    /// 截止时间
    /// </summary>
    long Deadline { get; set; }

    /// <summary>
    /// 剩余执行次数
    /// </summary>
    int Countdown { get; set; }

    /// <summary>
    /// 是否包含截止时间限制
    /// </summary>
    bool HasDeadline { get; set; }

    /// <summary>
    /// 是否包含次数限制
    /// </summary>
    bool HasCountdown { get; set; }

    /// <summary>
    /// 是否是周期性任务
    /// </summary>
    bool IsPeriodic { get; }

    /// <summary>
    /// 是否已完成首次触发(通常用于降低优先级)
    /// </summary>
    bool IsTriggered { get; }

    /// <summary>
    /// 取消令牌的监听句柄
    /// </summary>
    Registration CancelRegistration { get; set; }

    /// <summary>
    /// 关联的取消令牌
    /// </summary>
    /// <returns></returns>
    ICancelToken GetCancelToken();

    #endregion

    /// <summary>
    /// 外部确定性触发
    /// 该方法由EventLoop调用，不需要回调的方式重新压入队列，而是返回bool值告知EventLoop是否需要继续执行。
    /// 在该方法返回false后，EventLoop不可再持有Task的引用。
    /// </summary>
    /// <param name="tickTime">当前时间戳</param>
    /// <returns>是否还需要压入队列</returns>
    bool Trigger(long tickTime);

    /// <summary>
    /// 取消执行
    /// 可能是检测到取消信号，也可能是其它原因，EventLoop主动停止任务。
    /// </summary>
    void Cancel(int cancelCode);

    /// <summary>
    /// 归还到对象池（解决泛型问题）
    /// </summary>
    void Release();
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
        int r = lhs.TriggerTime.CompareTo(rhs.TriggerTime);
        if (r != 0) {
            return r;
        }
        // 未触发的放前面
        r = lhs.IsTriggered.CompareTo(rhs.IsTriggered);
        if (r != 0) {
            return r;
        }
        r = lhs.Id.CompareTo(rhs.Id);
        if (r == 0) {
            throw new InvalidOperationException($"lhs.id: {lhs.Id}, rhs.id: {rhs.Id}");
        }
        return r;
    }
}
}