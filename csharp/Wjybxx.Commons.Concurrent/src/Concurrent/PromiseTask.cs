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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// </summary>
/// <typeparam name="T">结果类型</typeparam>
public class PromiseTask<T> : Promise<T>, IFutureTask<T>
{
    /**
     * queueId的掩码 -- 8bit，最大255。
     * 1.放在低8位，减少运算，queueId的计算频率高于其它部分。
     * 2.大于{@link TaskOption}的中的64阶段。
     */
    protected const int maskQueueId = 0xFF;
    /** 任务类型的掩码 -- 4bit，可省去大量的instanceof测试 */
    protected const int maskTaskType = 0x0F00;
    /** 调度类型的掩码 -- 4bit，最大16种 */
    protected const int maskScheduleType = 0xF000;
    /** 是否已经声明任务的归属权 */
    protected const int maskClaimed = 1 << 16;
    /** 分时任务是否已启动 */
    protected const int maskStarted = 1 << 17;
    /** 分时任务是否已停止 */
    protected const int maskStopped = 1 << 18;

    protected const int offsetQueueId = 0;
    protected const int offsetTaskType = 8;
    protected const int offsetScheduleType = 12;
    protected const int maxQueueId = 255;

    /** 用户的委托 */
    private Delegate _action;
    /** 任务上下文 */
    private TaskContext _context;
    /** 任务的调度选项 */
    protected readonly int options;
    /** 任务的控制标记 */
    private int ctl;

    public PromiseTask(IExecutor executor, Delegate action, in TaskContext context, int options = 0)
        : base(executor) {
        _action = action;
        _context = context;
        this.options = options;
    }

    /// <summary>
    /// 任务的调度选项
    /// </summary>
    public int Options => options;

    /// <summary>
    /// 任务关联的Future
    /// </summary>
    public IFuture<T> Future => this;


    public void Run() {
        try {
            object value = _action.DynamicInvoke();
            TrySetResult((T)value);
        }
        catch (Exception ex) {
            TrySetException(ex);
        }
    }
}