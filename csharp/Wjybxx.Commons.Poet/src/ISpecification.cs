#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Poet
{
/// <summary>
/// 规格接口
///
/// Q：为什么造该接口？
/// A：因为宏可能出现在文件的任意位置，我们需要保证用户的定义顺序，需要使用List来装所有元素。
/// (宏导致代码的复杂度急剧攀升，心里一万只草泥马奔过...我怎么就心血来潮写这工具)
/// </summary>
public interface ISpecification
{
    /// <summary>
    /// 规格名
    /// </summary>
    public string? Name { get; }

    /// <summary>
    /// 规格类型
    /// </summary>
    public SpecType SpecType { get; }
}

/// <summary>
/// 规格类型
/// </summary>
public enum SpecType : byte
{
    /// <summary>
    /// 类型
    /// </summary>
    Type,
    /// <summary>
    /// 字段(或事件)
    /// </summary>
    Field,
    /// <summary>
    /// 属性
    /// </summary>
    Property,
    /// <summary>
    /// 方法
    /// </summary>
    Method,
    /// <summary>
    /// 枚举值
    /// </summary>
    EnumValue,
    /// <summary>
    /// 方法参数
    /// </summary>
    Parameter,

    /// <summary>
    /// 属性（注解）
    /// </summary>
    Attribute,
    /// <summary>
    /// 命名空间
    /// </summary>
    Namespace,
    /// <summary>
    /// 宏
    /// </summary>
    Macro,
    /// <summary>
    /// 导入
    /// </summary>
    Import,
    /// <summary>
    /// 任意代码
    /// </summary>
    CodeBlock,
}
}