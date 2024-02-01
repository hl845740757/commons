﻿#region LICENSE

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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// <see cref="IExecutor"/>中调度的任务的抽象。
/// 1.该接口暴露给Executor的扩展类，不是用户使用的类 -- 用户面向Action等委托类型即可。
/// 2.
/// 
/// ps：可认为是Java的Runnable。
/// </summary>
public interface ITask
{
    /// <summary>
    /// 任务的调度选项
    /// </summary>
    int Options { get; set; }

    /// <summary>
    /// 任务的逻辑
    /// </summary>
    void Run();
}