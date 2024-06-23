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
/// 修饰符
/// 反射接口中的 <see cref="TypeAttributes"/>
/// <see cref="FieldAttributes"/>
/// <see cref="MethodAttributes"/>
/// <see cref="PropertyAttributes"/>等枚举信息过多，不直观。
/// </summary>
[Flags]
public enum Modifiers
{
    /// <summary>
    /// 无
    /// </summary>
    None = 0,

    /// <summary>
    /// private
    /// </summary>
    Private = 0x0001,
    /// <summary>
    /// public
    /// </summary>
    Public = 0x0002,
    /// <summary>
    /// internal
    /// </summary>
    Internal = 0x0004,
    /// <summary>
    /// protected
    /// </summary>
    Protected = 0x0008,

    /// <summary>
    /// 抽象类/方法
    /// </summary>
    Abstract = 0x0010,
    /// <summary>
    /// 虚方法
    /// </summary>
    Virtual = 0x0020,
    /// <summary>
    /// 重写方法
    /// </summary>
    Override = 0x0040,
    /// <summary>
    /// 最终类/方法 (sealed或非虚方法)
    /// （对于方法只能修饰override）
    /// （不能和abstract共同出现）
    /// </summary>
    Sealed = 0x0080,

    /// <summary>
    /// 静态类/字段/方法/属性 static
    /// </summary>
    Static = 0X0100,
    /// <summary>
    /// 隐藏父类的方法(new关键字)
    /// </summary>
    Hide = 0x0200,
    /// <summary>
    /// setter方法特殊修饰符（init关键字）
    /// </summary>
    Init = 0x0400,
    /// <summary>
    /// 分部类/方法(建议只用在类上)
    /// </summary>
    Partial = 0x800,
    
    /// <summary>
    /// 方法参数包含in修饰
    /// </summary>
    In = 0x1000,
    /// <summary>
    /// 方法参数包含out修饰
    /// </summary>
    Out = 0x2000,
    /// <summary>
    /// 方法参数包含ref修饰 -- 方法返回值也有ref...
    /// </summary>
    Ref = 0x4000,
}