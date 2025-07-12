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
using System.Reflection;
using Wjybxx.Commons.Poet;
using Wjybxx.Dson.Codec.Attributes;

namespace Wjybxx.Dson.Apt2
{
/// <summary>
/// 我们将类型的信息都存储在该类上，这样可以更好的支持<see cref="DsonCodecLinkerBeanAttribute"/>。
/// </summary>
internal class AptClassProps
{
#nullable disable
    /// <summary>
    /// 获取单例的方法名（兼容属性）
    /// </summary>
    public string? singleton = null;
    /// <summary>
    /// 跳过的字段 -- HashSet加快查询
    /// </summary>
    public HashSet<string> skipFields = new HashSet<string>();
    /// <summary>
    /// 裁剪过的字段名，去掉了类名，只包含FieldName
    /// </summary>
    public HashSet<string> clippedSkipFields = new HashSet<string>();
    /// <summary>
    /// 为生成代码附加的注解(只支持无参注解)
    /// </summary>
    public readonly List<Type> additionalAnnotations = new();

    /// <summary>
    /// 编解码代理类
    /// </summary>
    public Type? codecProxyType;
    /// <summary>
    /// 代理类的TypeName
    /// </summary>
    public TypeName? codecProxyClassName;
#nullable restore

    public AptClassProps() {
    }

    /// <summary>
    /// 是否是单例类型
    /// </summary>
    public bool IsSingleton => !string.IsNullOrWhiteSpace(singleton);

    /// <summary>
    /// 是否包含指定的钩子方法（或属性）
    /// </summary>
    /// <param name="methodName"></param>
    /// <returns></returns>
    public bool ContainsHookMethod(string methodName) {
        if (codecProxyType == null) return false;
        return codecProxyType.GetMember(methodName,
            BindingFlags.Public | BindingFlags.Static).Length > 0;
    }

    public static AptClassProps Parse(DsonSerializableAttribute? attribute) {
        if (attribute == null) {
            return new AptClassProps();
        }
        AptClassProps props = new AptClassProps();
        props.singleton = attribute.Singleton;
        // 解析不自动编解码的字段
        foreach (string fieldName in attribute.SkipFields) {
            if (string.IsNullOrWhiteSpace(fieldName)) continue;
            props.skipFields.Add(fieldName);

            int spIndex = fieldName.LastIndexOf('.');
            props.clippedSkipFields.Add(spIndex < 0 ? fieldName : fieldName.Substring(spIndex + 1));
        }
        // 解析附加注解
        if (attribute.Attributes.Length > 0) {
            props.additionalAnnotations.AddRange(attribute.Attributes);
        }
        return props;
    }
}
}