#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 该上下文其实就是协程上下文的雏形，但公共并发库不会执行复杂的协程命令，
/// 因为公共模块缺少约定，一切考虑通用的话成本很高。
/// </summary>
public readonly struct AsyncTaskContext
{
#nullable disable
    private readonly ISchedulerHelper _helper;
    private readonly object _ctx;

    internal AsyncTaskContext(ISchedulerHelper helper, object ctx) {
        _helper = helper;
        _ctx = ctx;
    }

    /// <summary>
    /// 任务关联的事件循环
    ///
    /// 1.可通过await切换到事件循环线程，<code>await EventLoop</code>
    /// 2.可通过<see cref="IEventLoop.ScheduleAction"/>实现跨线程Sleep
    /// </summary>
    public IEventLoop EventLoop => _helper.EventLoop;

    /// <summary>
    /// 任务绑定的上下文
    /// </summary>
    public object Context => _ctx;

    /// <summary>
    /// 让出CPU，下一次事件循环的时候返回的Future进行完成状态
    /// </summary>
    /// <returns></returns>
    /// <exception cref="GuardedOperationException">如果当前不在事件循环线程</exception>
    public ValueFuture Yield() => _helper.Sleep(TimeSpan.Zero, null);

    /// <summary>
    /// 等待一段时间再次执行
    /// </summary>
    /// <param name="timeSpan"></param>
    /// <param name="cancelToken"></param>
    /// <exception cref="GuardedOperationException">如果当前不在事件循环线程</exception>
    public ValueFuture Sleep(TimeSpan timeSpan, ICancelToken? cancelToken = null) {
        return _helper.Sleep(timeSpan, cancelToken);
    }
}
}