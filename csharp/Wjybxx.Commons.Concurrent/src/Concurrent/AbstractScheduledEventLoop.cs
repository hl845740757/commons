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

namespace Wjybxx.Commons.Concurrent;

public abstract class AbstractScheduledEventLoop : AbstractEventLoop
{
    protected AbstractScheduledEventLoop(IEventLoopGroup? parent) : base(parent) {
    }

    public override IFuture<TResult> Schedule<TResult>(ref ScheduledTaskBuilder<TResult> builder) {
        return base.Schedule(ref builder);
    }

    public override IFuture ScheduleAction(Action action, TimeSpan delay, ICancelToken? cancelToken = null) {
        return base.ScheduleAction(action, delay, cancelToken);
    }

    public override IFuture ScheduleAction(Action<IContext> action, TimeSpan delay, IContext context) {
        return base.ScheduleAction(action, delay, context);
    }

    public override IFuture<TResult> ScheduleFunc<TResult>(Func<TResult> action, TimeSpan delay, ICancelToken? cancelToken = null) {
        return base.ScheduleFunc(action, delay, cancelToken);
    }

    public override IFuture<TResult> ScheduleFunc<TResult>(Func<IContext, TResult> action, TimeSpan delay, IContext context) {
        return base.ScheduleFunc(action, delay, context);
    }

    public override IFuture ScheduleWithFixedDelay(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        return base.ScheduleWithFixedDelay(action, delay, period, cancelToken);
    }

    public override IFuture ScheduleAtFixedRate(Action action, TimeSpan delay, TimeSpan period, ICancelToken? cancelToken = null) {
        return base.ScheduleAtFixedRate(action, delay, period, cancelToken);
    }
}