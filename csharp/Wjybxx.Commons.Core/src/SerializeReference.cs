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

namespace Wjybxx.Commons
{
/// <summary>
/// 表示需要序列化的字段、属性、类型需要序列化为引用（同Unity）
///
/// 注：
/// 1.当用于List/HashSet/Map字段时，表示其Values需要序列化为引用，而不是集合整体序列化为引用；
/// 因为当注解表示List整体序列化为引用时，我们将无法知晓List的元素是否应该序列化为引用。
///
/// 2.当用于类型时，表示该类型及其子类默认序列化为引用类型 —— 可能与Unity的兼容性不好，减少使用。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
public sealed class SerializeReference : Attribute
{
    public SerializeReference() {
    }
}
}