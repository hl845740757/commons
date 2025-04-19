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
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Inject.Attributes
{
/// <summary>
/// 该注解用于配置一个注入的服务信息，该注解可重复添加。
/// 
/// 1.当字段/属性/参数声明该属性后，将不再自动查询无命名服务，即只从属性中查询 -- 否则会造成迷惑。
/// 2.框架默认仅支持name为string，如果期望是枚举或其它类型，可以在注入后建立缓存 -- <see cref="InjectOnCreateAttribute"/>。
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter,
    AllowMultiple = true)]
public sealed class InjectServiceAttribute : Attribute
{
    /// <summary>
    /// 服务的名字
    /// </summary>
    public readonly string? name;
    /// <summary>
    /// 服务是否可选，不存在时不抛出异常
    /// </summary>
    public readonly bool optional;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="optional">是否可选</param>
    public InjectServiceAttribute(bool optional) {
        this.name = null;
        this.optional = optional;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">服务的名字</param>
    /// <param name="optional">是否可选</param>
    public InjectServiceAttribute(string name, bool optional = false) {
        this.name = name;
        this.optional = optional;
    }
}
}