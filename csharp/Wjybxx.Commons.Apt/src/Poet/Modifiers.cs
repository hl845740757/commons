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

namespace Wjybxx.Commons.Poet;

/// <summary>
/// 修饰符
/// 反射接口中的 <see cref="TypeAttributes"/>
/// <see cref="FieldAttributes"/>
/// <see cref="MethodAttributes"/>
/// <see cref="PropertyAttributes"/>等枚举信息过多，不直观。
///
/// 注意：方法参数的ref/in/out不是单纯的修饰符，而是修改了字段的类型，因此需要使用<see cref="ByRefTypeName"/>。
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

    // 声明顺序 
    // public new static extern unsafe 

    /// <summary>
    /// 隐藏父类的方法(new关键字)
    /// </summary>
    Hide = 0x0100,
    /// <summary>
    /// 静态类/字段/方法/属性 static
    /// </summary>
    Static = 0X0200,
    /// <summary>                   
    /// 外部方法                        
    /// </summary>                  
    Extern = 0x400,
    /// <summary>                   
    /// 方法使用了指针                     
    /// </summary>                  
    Unsafe = 0x800,

    /// <summary>
    /// 只读
    /// </summary>
    Readonly = 0x1000,
    /// <summary>
    /// 常量
    /// </summary>
    Const = 0x2000,
    /// <summary>
    /// 分部类/方法(建议只用在类上)
    /// </summary>
    Partial = 0x4000,
    /// <summary>
    /// 异步方法（实际上是注解）
    /// </summary>
    Async = 0x8000,

    /// <summary>
    /// 操作符重载（方法名即符号）
    /// C#的操作符重载是生成了特殊的方法名来实现的，但我们使用Modifier更简单点，也更容易扩展
    /// </summary>
    Operator = 0x10000,
    /// <summary>
    /// volatile
    /// </summary>
    Volatile = 0x20000,
}