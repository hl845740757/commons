#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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
/// <summary>
/// 用于配置状态机池的大小
/// 系统库现在的解法是可以在方法上指定关联的Builder，通过Builder控制是否可池化，但还是不能指定池大小。
/// <![CDATA[
/// [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
/// public async ValueTask<int> ProcessAsync() {}
/// ]]>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, Inherited = false)]
public sealed class TaskPoolSizeAttribute : Attribute
{
    /// <summary>
    /// 如果异步方法的返回值是编译时确定类型，则该值就是最终池大小；
    /// 如果异步方法的返回值是运行时泛型<see cref="ValueFuture{T}"/>，
    /// 则该属性表示int或object类型结果的池大小，而<see cref="poolSize2"/>则表示其它类型的池大小。
    /// </summary>
    public readonly int poolSize;
    public readonly int poolSize2;

    public TaskPoolSizeAttribute(int poolSize, int poolSize2 = -1) {
        this.poolSize = poolSize;
        this.poolSize2 = poolSize2 == -1 ? poolSize / 4 : poolSize2;
    }
}
}