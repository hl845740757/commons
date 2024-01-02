#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Attributes;

/// <summary>
/// 用于强调一个字段或属性需要序列化
///
/// 1.通常用于强调private字段
/// 2.并不真的建议用于属性 -- C#的自动属性经常搞坑。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SerializedAttribute : Attribute
{
    /// <summary>
    /// 序列化字段对应的名字
    ///
    /// Null处理：
    /// 如果是属性，则将属性名首字母小写看做字段名
    /// 如果是字段，则将字段首个下划线去除，然后首字母小写
    /// </summary>
    public readonly string? Name;

    public SerializedAttribute(string? name = null) {
        Name = name;
    }
}