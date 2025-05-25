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

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Wjybxx.Commons.Apt;
using Wjybxx.Commons.Poet;

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// 我们将类型的信息都存储在该类上，这样可以更好的支持<code>DsonCodecLinkerBeanAttribute</code>。
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
    public readonly HashSet<string> skipFields = new();
    /// <summary>
    /// 裁剪过的字段名，去掉了类名，只包含FieldName
    /// </summary>
    public readonly HashSet<string> clippedSkipFields = new();
    /// <summary>
    /// 为生成代码附加的注解(只支持无参注解)
    /// </summary>
    public readonly List<INamedTypeSymbol> additionalAnnotations = new();

    /// <summary>
    /// 编解码代理类
    /// </summary>
    public INamedTypeSymbol? codecProxyType;
    /// <summary>
    /// 代理类的TypeName
    /// </summary>
    public TypeName? codecProxyClassName;

    internal AptClassProps() {
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
        return codecProxyType?.GetFirstMember(methodName) != null;
    }

    /// <summary>
    /// 解析注解
    /// </summary>
    /// <param name="attributeData"></param>
    /// <returns></returns>
    public static AptClassProps Parse(AttributeData? attributeData) {
        if (attributeData == null) {
            return new AptClassProps();
        }
        AptClassProps props = new AptClassProps();
        {
            if (AptUtils.GetAttributeValue(attributeData, "Singleton", out TypedConstant attributeValue)) {
                props.singleton = attributeValue.GetValueAsString();
            }
        }
        // 解析不自动编解码的字段
        {
            if (AptUtils.GetAttributeValue(attributeData, "SkipFields", out TypedConstant attributeValue)) {
                foreach (TypedConstant typedConstant in attributeValue.Values) {
                    string fieldName = typedConstant.GetValueAsString();
                    if (string.IsNullOrWhiteSpace(fieldName)) continue;
                    props.skipFields.Add(fieldName);

                    int spIndex = fieldName.LastIndexOf('.');
                    props.clippedSkipFields.Add(spIndex < 0 ? fieldName : fieldName.Substring(spIndex + 1));
                }
            }
        }
        // 解析附加注解
        {
            if (AptUtils.GetAttributeValue(attributeData, "Attributes", out TypedConstant attributeValue)) {
                foreach (TypedConstant typedConstant in attributeValue.Values) {
                    INamedTypeSymbol typeSymbol = typedConstant.Value as INamedTypeSymbol;
                    if (typeSymbol == null) continue;
                    props.additionalAnnotations.Add(typeSymbol);
                }
            }
        }
        return props;
    }
}
}