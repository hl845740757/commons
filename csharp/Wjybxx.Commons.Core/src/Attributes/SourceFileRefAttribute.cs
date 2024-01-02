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

#pragma warning disable CS1591
namespace Wjybxx.Commons.Attributes;

/// <summary>
/// 用于标注关联的原始类文件
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct |
                AttributeTargets.Enum | AttributeTargets.Interface)]
public class SourceFileRefAttribute : Attribute
{
    /// <summary>
    /// 原始Class的类型
    /// </summary>
    public readonly Type SourceType;

    public SourceFileRefAttribute(Type sourceType) {
        this.SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
    }
}