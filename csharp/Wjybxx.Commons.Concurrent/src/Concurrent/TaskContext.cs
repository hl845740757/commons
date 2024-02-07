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

namespace Wjybxx.Commons.Concurrent;

/// <summary>
/// 异步任务的上下文
/// 
/// ps：该结构体用于避免过多的方法扩展。
/// </summary>
public readonly struct TaskContext
{
    /// <summary>
    /// 状态参数 -- 状态参数用于支持私有变量，不同任务的State通常不同。
    /// </summary>
    public readonly object? State;

    /// <summary>
    /// 取消令牌 -- 如果未指定，则默认赋值为<see cref="ICancelToken.NONE"/>
    /// </summary>
    public readonly ICancelToken CancelToken;
    /// <summary>
    /// 任务黑板 -- 黑板用于支持读写共享变量，不同任务可能指向同一个对象。 
    /// </summary>
    public readonly object? Blackboard;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="cancelToken">取消令牌</param>
    /// <param name="state">回调参数</param>
    /// <param name="blackboard">任务黑板</param>
    public TaskContext(object? state, ICancelToken? cancelToken = null, object? blackboard = null) {
        State = state;
        CancelToken = cancelToken ?? ICancelToken.NONE;
        Blackboard = blackboard;
    }
}