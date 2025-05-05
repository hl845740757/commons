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
using Microsoft.CodeAnalysis;
using Wjybxx.Commons.Poet;

namespace Wjybxx.Commons.Apt
{
/// <summary>
/// 1.每一个注解元素都是一个具体的实例。
/// 2.编译期数据和反射数据无法建立统一的抽象，用户应当在业务层使用之前解析为统一的数据结构。
///
/// 有两种方式：
/// 1.一是将注解的数据缓存在该实例上，内置了<see cref="ResolvedValues"/>字段；
/// 2.另一种方式是在外部建立字典，通过<see cref="CompilationData"/>和<see cref="ReflectionData"/>进行映射。
/// 第一种方式可能存在重复解析的问题，因为我们难以保证该实例的唯一性，但第一种方式的使用起来较为方便。
/// 如果性能很重要，请在外部维护数据缓存。
/// </summary>
public sealed class AptAttributeData
{
#nullable disable
    /// <summary>
    /// 编译期数据
    /// </summary>
    private readonly AttributeData? _compilationData;
    /// <summary>
    /// 反射数据
    /// </summary>
    private readonly Attribute? _reflectionData;
    /// <summary>
    /// 用户自行解析得到的数据（缓存数据）
    /// 如果为null表示尚未初始化（解析）
    /// </summary>
    private volatile IList<AptAttributeValue> _resolvedValues;

    public AptAttributeData(AttributeData? compilationData, Attribute? reflectionData) {
        if (compilationData == null && reflectionData == null) {
            throw new ArgumentException("both compilationData and reflectionData are null");
        }
        _compilationData = compilationData;
        _reflectionData = reflectionData;
    }

    public AttributeData CompilationData => _compilationData;

    public Attribute ReflectionData => _reflectionData;

    public IList<AptAttributeValue> ResolvedValues {
        get => _resolvedValues;
        set => _resolvedValues = Util.ToImmutableList(value);
    }
}
}