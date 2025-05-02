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

namespace Wjybxx.Commons.Inject.Attributes
{
/// <summary>
/// 该注解用于定义一个注入点。
///
/// 1.该注解可重复添加，以声明多个服务的依赖；但用于构造函数时不应该重复添加，避免导致奇怪的语义。、
/// 2.框架默认仅支持name为string，如果期望是枚举或其它类型，可以在注入后建立缓存 -- <see cref="InjectOnCreateAttribute"/>。
///
/// <h3>多实例注入</h3>
/// 在字段/属性/参数上可重复声明该属性，从而申请多个服务实例；
/// 以字段为例，如果字段是<see cref="IList{T}"/>类型，则会将所有存在的服务注入到List。
/// 如果字段是<see cref="IDictionary{K,V}"/>类型，则会将所有的存在的服务注入到字典 -- Key固定为服务的名字。
/// 如果字段不是List和字段，则按照声明信息依次查找服务，直到注入成功或抛出异常 -- 如果所有服务都是可选的，则最终为null。
/// <code>
/// [Inject("dson", true)]
/// [Inject("bson", true)]
/// [Inject("json")]
/// SerializeMgr serializeMgr;
/// </code>
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter
                | AttributeTargets.Constructor, AllowMultiple = true)]
public sealed class InjectAttribute : Attribute
{
    /// <summary>
    /// 服务的名字
    /// </summary>
    public readonly string? name;
    /// <summary>
    /// 服务是否可选，可选表示目标服务不存在时不抛出异常；
    /// 如果服务是可选的，且不存在，则不会添加到List和Dictionary中。
    /// </summary>
    public readonly bool optional;

    public InjectAttribute() {
        this.name = null;
        this.optional = false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="optional">是否可选</param>
    public InjectAttribute(bool optional) {
        this.name = null;
        this.optional = optional;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name">服务的名字</param>
    /// <param name="optional">是否可选</param>
    public InjectAttribute(string? name, bool optional = false) {
        this.name = name;
        this.optional = optional;
    }
}
}