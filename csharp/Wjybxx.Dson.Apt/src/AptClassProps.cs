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
using System.Linq;
using System.Reflection;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// 我们将类型的信息都存储在该类上，这样可以更好的支持<see cref="DsonCodecLinkerBeanAttribute"/>。
/// </summary>
internal class AptClassProps
{
    /// <summary>
    /// 属性配置
    /// </summary>
    public DsonSerializableAttribute attribute;
    /// <summary>
    /// 跳过的字段 -- HashSet加快查询
    /// </summary>
    public IGenericSet<string> skipFields = ImmutableLinkedHastSet<string>.Empty;
    /// <summary>
    /// 裁剪过的字段名，去掉了类名，只包含FieldName
    /// </summary>
    public IGenericSet<string> clippedSkipFields = ImmutableLinkedHastSet<string>.Empty;

    /// <summary>
    /// 编解码代理类
    /// </summary>
    public Type? codecProxyType;
    /// <summary>
    /// 代理类的TypeName
    /// </summary>
    public TypeName? codecProxyClassName;
    /// <summary>
    /// 编解码代理类的成员信息
    /// </summary>
    public List<MemberInfo> codecProxyEnclosedElements;

    public AptClassProps() {
    }

    public string? Singleton => attribute.Singleton;

    public bool IsSingleton() {
        return !string.IsNullOrWhiteSpace(attribute.Singleton);
    }

    public static AptClassProps Parse(DsonSerializableAttribute? attribute) {
        AptClassProps props = new AptClassProps();
        props.attribute = attribute ?? new DsonSerializableAttribute();
        if (props.attribute.SkipFields.Length > 0) {
            props.skipFields = new LinkedHashSet<string>(props.attribute.SkipFields);
            props.clippedSkipFields = props.skipFields.Select(e => {
                int index = e.LastIndexOf('.');
                return index < 0 ? e : e.Substring(0, index);
            }).ToImmutableLinkedHashSet();
        }
        return props;
    }
}
}