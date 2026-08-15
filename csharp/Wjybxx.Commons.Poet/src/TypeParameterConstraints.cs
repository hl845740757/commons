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
using System.Reflection;

namespace Wjybxx.Commons.Poet
{
[Flags]
public enum TypeParameterConstraints
{
    /// <summary>
    /// 空
    /// </summary>
    None = 0,

    /// <summary>
    /// 可空引用类型 -- 对引用类型追加'?'
    /// 
    /// 注意：
    /// 1.NRT并不是真正的类型，而是注解(属性)，在运行时无效；但使用注解来标记类型实在不方便，因此我们存储在TypeName上。
    /// 2.netstandard2.x无法使用<code>NullableAttribute</code>类型，因此反射时无法解析。
    /// </summary>
    NullableReferenceType = 0x01,
    /// <summary>
    /// 非空引用类型 -- 对引用类型追加'notnull'
    /// 
    /// 注意：
    /// 1.非空引用类型也不是真正的类型,，而是注解(属性)，在运行时无效。
    /// 2.反射时无法获取到notnull属性
    /// </summary>
    NotNullableReferenceType = 0x02,

    /// <summary>
    /// 引用类型约束
    /// <see cref="GenericParameterAttributes.ReferenceTypeConstraint"/>
    /// </summary>
    ReferenceTypeConstraint = 0x04,
    /// <summary>
    /// 值类型约束
    /// <see cref="GenericParameterAttributes.NotNullableValueTypeConstraint"/>
    /// </summary>
    ValueTypeConstraint = 0x08,
    /// <summary>
    /// 默认构造器约束
    /// <see cref="GenericParameterAttributes.DefaultConstructorConstraint"/>
    /// </summary>
    DefaultConstructorConstraint = 0x10,

    /// <summary>
    /// 泛型变量包含in修饰符
    /// </summary>
    VarianceIn = 0x20,
    /// <summary>
    /// 泛型变量包含out修饰符
    /// </summary>
    VarianceOut = 0x40,

    /// <summary>
    /// 非托管类型约束(unmanaged)
    /// </summary>
    UnmanagedTypeConstraint = 0x0100,
    /// <summary>
    /// 默认类型约束/无类型约束(default)
    /// </summary>
    DefaultTypeConstraint = 0x0200,
}
}