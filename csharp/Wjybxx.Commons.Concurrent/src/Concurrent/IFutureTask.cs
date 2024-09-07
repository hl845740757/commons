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
/// FutureTask是Executor压入的可获取结果的任务类型。
/// 1. 该接口暴露给Executor的扩展类，不是用户使用的类。
/// 2. 需要获取结果的任务，我们将调度选项保存下来；普通任务的调度选项可能在execute。
/// 3. 该接口的实例通常是不应该被序列化的。
/// 4. 接口不再暴露Future，以允许Task在完成后清理Future。
/// </summary>
public interface IFutureTask : ITask
{
    /// <summary>
    /// 是否收到了取消信号
    /// 调度器会检查任务的取消信号，以避免不必要的执行。
    /// </summary>
    /// <returns></returns>
    bool IsCancelRequested();

    /// <summary>
    /// 取消执行
    /// 取消可能由调度器触发，因此需要暴露该接口给EventLoop。
    /// 该方法由EventLoop调用，不需要再回调通知EventLoop.
    /// 实现时，优先使用CancelToken中的取消码。
    /// </summary>
    /// <param name="code">默认取消码</param>
    void TrySetCancelled(int code = CancelCodes.REASON_SHUTDOWN);

    /// <summary>
    /// 清理任务
    /// </summary>
    void Clear();
}
}