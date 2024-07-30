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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 定时任务关联的Promise
/// </summary>
public interface IScheduledPromise : IPromise, IScheduledFuture
{
}

public interface IScheduledPromise<T> : IScheduledPromise, IPromise<T>, IScheduledFuture<T>
{
    /// <summary>
    /// 注入关联的任务.
    /// 1.Promise需要了解任务的状态以支持用户的查询;
    /// 2.由于存在双向依赖，因此需要延迟注入;
    /// </summary>
    /// <param name="task">关联的任务</param>
    void SetTask(IScheduledFutureTask<T> task);
}
}