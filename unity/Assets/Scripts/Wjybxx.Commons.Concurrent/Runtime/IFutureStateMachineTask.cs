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

namespace Wjybxx.Commons.Concurrent
{
internal interface IFutureStateMachineTask<T> : IPromise<T>
{
    /// <summary>
    /// 返回用于驱动StateMachine的委托
    /// 
    /// ps：定义为属性以允许实现类进行一些优化，比如：缓存实例，代理。
    /// </summary>
    Action MoveToNext { get; }
}
}