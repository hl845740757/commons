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
using System.Collections.Generic;
using Wjybxx.Commons.Attributes;

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 对象注射器
///
/// <h3>注解</h3>
/// 依赖注入相关的注解，存放在<see cref="Wjybxx.Commons.Inject.Attributes"/>命名空间。
/// 
/// <h3>Optional</h3>
/// optional用于指示当目标服务不存在时是否抛出异常，optional为true表示请求的服务不存在时返回null，为false表示抛出异常。
///
/// <h3>启动</h3>
/// 默认实现通过<see cref="InjectorExtensions"/>创建
/// </summary>
[ThreadSafe]
public interface IInjector
{
    /// <summary>
    /// 获取指定实例
    /// </summary>
    /// <param name="serviceType">服务类型</param>
    /// <param name="name">多注入时实例的名字，null表示不根据name查找</param>
    /// <param name="optional">是否是可选的</param>
    /// <exception cref="InjectionException">如果获取实例出现错误</exception>
    /// <returns></returns>
    object? GetInstance(Type serviceType, string? name, bool optional = false);

    /// <summary>
    /// 创建子注射器
    /// 子注射器可以访问当前注射器的实例，当前注射器不访问到子注射器的实例。
    /// </summary>
    /// <returns></returns>
    IInjector CreateChild(IEnumerable<IInjectModule> modules);
}
}