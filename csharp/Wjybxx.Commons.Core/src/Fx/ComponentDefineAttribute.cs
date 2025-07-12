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
/// 1.可以通过额外的注解附加信息，需要定制解析器
/// 2.组件id解析重定向，请使用<see cref="ComponentRedirectAttribute"/>
///
/// 注：最新实现下，该注解是默认被继承的，但仅限于Class。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
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
    /// 高速缓存下标
    /// </summary>
    public int CacheIndex { get; set; } = -1;

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
}
}