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
using System.Reflection;

namespace Wjybxx.Commons.Apt;

/// <summary>
/// TypeName的属性，即类型引用约束
/// </summary>
[Flags]
public enum TypeNameAttributes
{
    /// <summary>
    /// 默认值
    /// </summary>
    None = 0,

    /// <summary>
    /// 可空引用类型 -- 对引用类型追加'?'
    /// 
    /// 注意： NRT并不是真正的类型，而是注解(属性)，在运行时无效；但使用注解来标记类型实在不方便，因此我们存储在TypeName上。
    /// </summary>
    NullableReferenceType = 0x0001,
    /// <summary>
    /// 非空引用类型 -- 用于泛型约束(notnull)
    /// 
    /// 注意：非空引用类型也不是真正的类型,，而是注解(属性)，在运行时无效。
    /// </summary>
    NotNullableReferenceType = 0x002,

    #region 泛型变量约束

    /// <summary>
    /// 引用类型约束
    /// <see cref="GenericParameterAttributes.ReferenceTypeConstraint"/>
    /// </summary>
    ReferenceTypeConstraint = 0x0004,
    /// <summary>
    /// 非空值类型约束
    /// <see cref="GenericParameterAttributes.NotNullableValueTypeConstraint"/>
    /// </summary>
    NotNullableValueTypeConstraint = 0x0008,
    /// <summary>
    /// 默认构造器约束
    /// <see cref="GenericParameterAttributes.DefaultConstructorConstraint"/>
    /// </summary>
    DefaultConstructorConstraint = 0x0010,

    #endregion
}