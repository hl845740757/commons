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

using System.Collections.Generic;
using System.Reflection;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Inject
{
/// <summary>
/// 注入点
/// </summary>
internal sealed class InjectionPoint
{
    /// <summary>
    /// 成员
    /// <see cref="FieldInfo"/><see cref="PropertyInfo"/>
    /// <see cref="ConstructorInfo"/><see cref="MethodInfo"/>
    /// </summary>
    public readonly MemberInfo memberInfo;
    /// <summary>
    /// 成员的依赖
    /// 构造函数和普通方法可能有多个依赖
    /// </summary>
    public readonly ImmutableList<Dependency> dependencies;

    public InjectionPoint(MemberInfo memberInfo, Dependency dependency) {
        this.memberInfo = memberInfo;
        this.dependencies = ImmutableList<Dependency>.Create(dependency);
        // 双向绑定
        dependency.injectionPoint = this;
    }

    public InjectionPoint(MemberInfo memberInfo, List<Dependency> dependencies) {
        this.memberInfo = memberInfo;
        this.dependencies = ImmutableList<Dependency>.CreateRange(dependencies);
        // 双向绑定
        foreach (Dependency dependency in this.dependencies) {
            dependency.injectionPoint = this;
        }
    }
}
}