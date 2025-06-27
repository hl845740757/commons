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
    /// (可直接await切换到事件循环线程)
    /// </summary>
    public IEventLoop EventLoop => _helper.EventLoop;

    /// <summary>
    /// 任务绑定的上下文
    /// </summary>
    public object Context => _ctx;

    /// <summary>
    /// 等待一段时间再次执行
    /// 
    /// 1.如果当前在其它线程，会在EventLoop线程醒来。
    /// 2.延迟时间小于0非法
    /// </summary>
    /// <param name="timeSpan"></param>
    /// <param name="cancelToken"></param>
    public ValueFuture Delay(TimeSpan timeSpan, ICancelToken? cancelToken = null) {
        return _helper.Delay(timeSpan, cancelToken);
    }
}
}