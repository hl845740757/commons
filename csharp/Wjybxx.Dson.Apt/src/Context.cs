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
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Wjybxx.Commons.Poet;

namespace Wjybxx.Dson.Apt
{
#nullable disable

/// <summary>
/// 一个类型的处理上下文
/// </summary>
internal class Context
{
    /// <summary>
    /// 要处理的类型
    /// </summary>
    public readonly INamedTypeSymbol type;
    /// <summary>
    /// 配置类
    /// </summary>
    public readonly ISymbol linkerSymbol;

    #region Cache

    /// <summary>
    /// 所有的public/protected字段、方法、属性缓存
    /// (当前程序集可访问到的所有成员)
    /// </summary>
    public List<ISymbol> allMembers;
    /// <summary>
    /// 所有的实例字段缓存（包含私有字段，包含自动属性字段）
    /// </summary>
    public List<AptFieldInfo> allFields;

    /// <summary>
    /// 要处理的类的注解信息
    /// </summary>
    public AptClassProps aptClassProps;
    /// <summary>
    /// 为生成代码附加的注解
    /// </summary>
    public List<AttributeSpec> additionalAnnotations;
    /// <summary>
    /// 所有的字段注解信息缓存（包含自动属性字段）
    /// </summary>
    public readonly Dictionary<AptFieldInfo, AptFieldProps> fieldPropsMap = new();
    /// <summary>
    /// 需要序列化的字缓存
    /// </summary>
    public readonly List<AptFieldInfo> serialFields = new();

    #endregion

    #region CTX

    /// <summary>
    /// <code>AbstractDsonCodec{T}</code>
    /// c#是真实泛型，我们需要构造类型后再获取对应的需要overriding方法
    /// </summary>
    public INamedTypeSymbol superDeclaredType;
    public TypeSpec.Builder typeBuilder;
    public string outputNamespace;

    #endregion

    public Context(INamedTypeSymbol type, ISymbol? linkerSymbol) {
        this.type = type ?? throw new ArgumentNullException(nameof(type));
        this.linkerSymbol = linkerSymbol;
    }

    public AptFieldProps? FindFieldProps(string name) {
        foreach (var pair in fieldPropsMap) {
            if (pair.Key.Name == name) return pair.Value;
        }
        return null;
    }
}
}