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

namespace Wjybxx.Commons.Fx
{
/// <summary>
/// 默认的组件id定义注解
/// 1.该注解不会被继承，使用子类的Class查询得到的将是另一个组件id。
/// 2.可以通过额外的注解附加信息，需要定制解析器
/// 3.组件id解析重定向，请使用<see cref="ComponentRedirectAttribute"/>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
public class ComponentDefineAttribute : Attribute
{
    /// <summary>
    /// 组件的名字
    /// 默认使用<see cref="Type.Name"/>
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 组件类型 -- 默认为脚本类型，即所有方法都生效
    /// </summary>
    /// <returns></returns>
    public ComponentKind Kind { get; set; } = ComponentKind.Script;

    /// <summary>
    /// 是否是共享组件
    /// </summary>
    public bool Shared { get; set; } = false;

    /// <summary>
    ///  最大组件数
    /// </summary>
    public int MaxCount { get; set; } = 1;

    /// <summary>
    /// 用户自定义flags
    /// </summary>
    public int Flags { get; set; } = 0;

    /// <summary>
    /// 用户自定义分组键
    /// </summary>
    public int GroupKey { get; set; } = 0;

    /// <summary>
    /// 挂载路径
    /// </summary>
    public string? MountPath { get; set; }

    /// <summary>
    /// 自定义切面数据 -- 用于自定义解析
    /// </summary>
    public string? CustomData { get; set; }

    /// <summary>
    /// 组件的超类型
    ///
    /// 1.用于对齐缓存索引<see cref="ComponentId.index"/>，使得可以通过超类组件ID查询子类组件。
    /// 2.指向同一个超类型的组件，共享同一个Index。
    /// 3.如果实体支持同类型组件挂载多个，不可使用该特性，而是使用<see cref="ComponentRedirectAttribute"/>。
    /// </summary>
    public Type? BaseType { get; set; }
}
}