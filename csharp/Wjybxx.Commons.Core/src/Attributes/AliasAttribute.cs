﻿#region LICENSE

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

#pragma warning disable CS1591

namespace Wjybxx.Commons.Attributes;

/// <summary>
/// 该属性用于为字段和方法等定义别名
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class AliasAttribute : Attribute
{
    /// <summary>
    /// 别名
    /// </summary>
    public string[] Values { get; }

    public AliasAttribute(string[] values) {
        Values = values;
    }
}