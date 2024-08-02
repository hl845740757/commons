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

namespace Wjybxx.Dson.Codec.Attributes
{
/// <summary>
/// 该注解用于标注一个字段是否需要被忽略
///
/// 1. 有<see cref="NonSerializedAttribute"/>注解的字段默认不被序列化
/// 2. private字段默认不序列化
/// 3. 属性默认都是不序列化的，通过该注解可将属性序列化
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
[Serializable]
public class DsonIgnoreAttribute : Attribute
{
    /// <summary>
    /// 是否忽略
    /// </summary>
    public bool Value { get; }

    public DsonIgnoreAttribute(bool value = true) {
        Value = value;
    }
}
}