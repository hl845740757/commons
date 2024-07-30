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
/// 用于驱动<see cref="ValueFuture{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IFutureStateMachineDriver<T> : IStateMachineDriver<T>
{
    /// <summary>
    /// 用于用户等待状态机完成
    /// (不校验重入，因为只会被生成的状态机代码调用一次)
    /// </summary>
    IFuture VoidFuture { get; }

    /// <summary>
    /// 用于用户等待状态机完成，只能在Future上进行await操作，而不能进行更多操作
    /// (不校验重入，因为只会被生成的状态机代码调用一次)
    /// </summary>
    IFuture<T> Future { get; }
}
}