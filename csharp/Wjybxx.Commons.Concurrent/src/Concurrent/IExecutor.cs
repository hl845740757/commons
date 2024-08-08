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
using System.Threading;
using System.Threading.Tasks;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 异步任务的执行器
///
/// ps：C#的<see cref="Task{TResult}"/>和<see cref="TaskScheduler"/>是套糟糕的抽象。
/// 它最大的问题是：把特定领域(UI)的解决方案定为基础方案，用户对上下文和线程的控制力太差 => 隐式上下文在并发编程中是糟糕的。
/// </summary>
public interface IExecutor
{
    /// <summary>
    /// 获取当前Executor绑定的同步上下文（视图）
    /// 
    /// 1.实现类应当返回同一个实例。
    /// 2.一般而言，外部不应该调用SyncContext中Post以外的方法
    /// 3.一个简单的工具类<see cref="ExecutorSynchronizationContext"/>
    /// </summary>
    SynchronizationContext AsSyncContext();

    /// <summary>
    /// 获取当前Executor的<see cref="TaskScheduler"/>视图。
    /// 
    /// ps: 该接口仅用于和系统库的Task协作，用于执行Task的延续任务，其它时候避免使用。
    /// </summary>
    /// <returns></returns>
    TaskScheduler AsScheduler();

    /// <summary>
    /// 调度一个受信任的Task类型，Executor不会再对其进行封装
    /// </summary>
    /// <param name="task">要调度的任务</param>
    void Execute(ITask task);

    #region 辅助方法

    /// C#的lambda是基于委托的，而Java的lambda是基于函数式接口的；
    /// 在java端，只要方法参数是函数式接口，用户就可以传递lambda；在C#端，只有方法参数是委托类型，才可以使用lambda；
    /// 支持lambda是必要的，因此我们需要提供一些辅助方法来简化使用；
    /// <summary>
    /// 在将来的某个时间执行给定的命令。
    /// 命令可以在新线程中执行，也可以在池线程中执行，或者在调用线程中执行，这由Executor实现决定。
    /// </summary>
    /// <param name="action">要执行的任务</param>
    /// <param name="options">任务的调度特征值，见<see cref="TaskOption"/></param>
    void Execute(Action action, int options = 0) {
        Execute(Executors.BoxAction(action, options));
    }

    /// <summary>
    /// 调度器在执行之前会检测Context中的取消信号，如果已收到取消信号则放弃执行。
    /// ps：用户接收context参数，可在执行中检测取消信号。
    /// </summary>
    /// <param name="action">要执行的任务</param>
    /// <param name="context">任务上下文</param>
    /// <param name="options">任务的调度特征值，见<see cref="TaskOption"/></param>
    void Execute(Action<IContext> action, IContext context, int options = 0) {
        Execute(Executors.BoxAction(action, context, options));
    }

    /// <summary>
    /// 调度器在执行之前会检测取消信号，如果已收到取消信号则放弃执行。
    /// ps:该接口用于兼容老系统的使用者。
    /// </summary>
    /// <param name="action">要执行的任务</param>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="options">任务的调度特征值，见<see cref="TaskOption"/></param>
    void Execute(Action action, CancellationToken cancelToken, int options = 0) {
        Execute(Executors.BoxAction(action, cancelToken, options));
    }

    #endregion
}
}