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

namespace Wjybxx.Commons.Fx
{
/// <summary>
/// 组件重定向注解。
/// 用于子类告知框架应当使用它的哪个超类来解析<see cref="ComponentId"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class ComponentRedirect : Attribute
{
    /// <summary>
    /// 真实组件类型
    /// </summary>
    public readonly Type baseType;

    public ComponentRedirect(Type baseType) {
        this.baseType = baseType ?? throw new ArgumentNullException(nameof(baseType));
    }
}
}