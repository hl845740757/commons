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
    /// 基于<code>DsonSerializableAttribute</code>生成Codec时的数据
    /// </summary>
    public AttributeData dsonSerilAttribute;
    /// <summary>
    ///  基于<code>DsonCodecLinkerGroupAttribute</code>生成Codec时的数据
    /// </summary>
    public AttributeData linkerGroupAttribute;
    /// <summary>
    /// 基于<code>DsonCodecLinkerBeanAttribute</code>生成codec时的数据
    /// </summary>
    public AttributeData linkerBeanAttribute;

    #region Cache

    /// <summary>
    /// 所有的字段和方法（和属性）缓存
    /// </summary>
    public List<ISymbol> allFieldsAndMethodWithInherit;
    /// <summary>
    /// 所有的实例字段缓存（包含自动属性字段）
    /// </summary>
    public List<IFieldSymbol> allFields;

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
    public readonly Dictionary<IFieldSymbol, AptFieldProps> fieldPropsMap = new(SymbolEqualityComparer.Default);
    /// <summary>
    /// 需要序列化的字缓存
    /// </summary>
    public readonly List<IFieldSymbol> serialFields = new();

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

    public Context(INamedTypeSymbol type) {
        this.type = type ?? throw new ArgumentNullException(nameof(type));
    }
}
}