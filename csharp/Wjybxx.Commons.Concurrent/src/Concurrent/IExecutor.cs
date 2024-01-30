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
using System.Threading.Tasks;

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 异步任务的执行器
///
/// ps：C#的<see cref="Task{TResult}"/>和<see cref="TaskScheduler"/>是套糟糕的抽象。
/// 它最大的问题是：把特定领域(UI)的解决方案定为基础方案，用户对上下文和线程的控制力太差 => 隐式上下文在并发编程中是糟糕的。
/// </summary>
public interface IExecutor
{
    /// <summary>
    /// 在将来的某个时间执行给定的命令。
    /// 命令可以在新线程中执行，也可以在池线程中执行，或者在调用线程中执行，这由Executor实现决定。
    /// </summary>
    /// <param name="action">要执行的任务</param>
    /// <param name="options">任务的调度特征值，见<see cref="TaskOption"/></param>
    void Execute(Action action, int options = 0);

    /// <summary>
    /// 在将来的某个时间执行给定的命令。
    /// 命令可以在新线程中执行，也可以在池线程中执行，或者在调用线程中执行，这由Executor实现决定。
    /// 调度器在执行之前会检测任务的取消信号，如果已收到取消信号则放弃执行。
    /// </summary>
    /// <param name="action">要执行的任务</param>
    /// <param name="context">任务关联的上下文</param>
    /// <param name="options">任务的调度特征值，见<see cref="TaskOption"/></param>
    void Execute(Action<IContext> action, IContext context, int options = 0);
}