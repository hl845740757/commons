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
/// 可分时运行的任务 - 需要长时间运行才能得出结果的任务。
/// 1. 分时任务代表着所有需要自定义管理状态的任务。
/// 2. 该接口尚不稳定，避免用于非EventLoop架构。
/// 3. 用户可以通过ctx和外部通信
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="context">任务关联的山下文</param>
/// <param name="firstStep">是否是首次执行</param>
/// <param name="result">接收任务结果</param>
/// <returns>执行任务是否成功</returns>
public delegate bool TimeSharingTask<T>(IContext context, bool firstStep, out T result);
}