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

namespace Wjybxx.Commons.Inject.Attributes
{
/// <summary>
/// 用于指示目标方法是依赖注入后的钩子方法
///
/// 1.目标方法必须是实例方法，可以是私有方法，默认通过反射调用。
/// 2.注意！当对象之间存在循环依赖时，依赖的对象可能是尚未完整初始化的！
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class InjectOnCreateAttribute : Attribute
{
}
}