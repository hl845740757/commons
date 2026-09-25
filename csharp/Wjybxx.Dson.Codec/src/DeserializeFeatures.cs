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

namespace Wjybxx.Dson.Codec
{
/// <summary>
/// 反序列化特征值(TODO)
///
/// 1.反序列化特征值主要用于处理数据异常的情况，因此大多仅为字段级别特征值。
/// 2.这里的特征值大多数未生效，只为了保持接口稳定预先添加了该枚举。
/// </summary>
[Flags]
public enum DeserializeFeatures
{
    /// <summary>
    /// 读取为DsonValue
    /// 注：字段应当声明为Object或DsonValue类型。
    /// </summary>
    ReadAsDsonValue = 0x04,

#pragma warning disable CA1069
    /// <summary>
    /// Enum解码时忽略大小写
    /// </summary>
    EnumIgnoreCase = 0x10 << 8,
    /// <summary>
    /// Enum解码时允许未定义枚举值(flags无需处理)
    /// </summary>
    EnumAllowUndefine = 0x20 << 8,
#pragma warning restore CA1069

    /// <summary>
    /// 集合元素的特征值
    /// </summary>
    MaskElementFeatures = EnumIgnoreCase | EnumAllowUndefine,
}
}