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

namespace Wjybxx.Dson.Apt
{
/// <summary>
/// 我们将类型的信息都存储在该类上，这样可以更好的支持<code>DsonCodecLinkerBeanAttribute</code>。
/// </summary>
internal class AptClassProps
{
#nullable disable
    /// <summary>
    /// 字段名样式
    /// </summary>
    public int nameStyle;
    /// <summary>
    /// 获取单例的方法名（兼容属性）
    /// </summary>
    public string? singleton;

    /// <summary>
    /// 跳过的字段
    /// </summary>
    public readonly HashSet<string> skipFields = new();
    /// <summary>
    /// 为生成代码附加的注解(只支持无参注解)
    /// </summary>
    public readonly List<INamedTypeSymbol> additionalAnnotations = new();
    /// <summary>
    /// 第三方命名空间别名
    /// </summary>
    public readonly List<KeyValuePair<string, string>> namespaceAliases = new();

    public AptClassProps() {
    }

    /// <summary>
    /// 是否是单例类型
    /// </summary>
    public bool IsSingleton => !string.IsNullOrWhiteSpace(singleton);

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
        // 解析字段命名规则
        {
            if (AptUtils.GetAttributeValue(attributeData, "NameStyle", out TypedConstant attributeValue)) {
                props.nameStyle = (int)attributeValue.Value!;
                if (props.nameStyle < 0 || props.nameStyle > 2) { // DsonNameStyle
                    throw new System.ArgumentException($"Invalid NameStyle: {props.nameStyle}");
                }
            }
        }
        // 解析不自动读字段
        {
            if (AptUtils.GetAttributeValue(attributeData, "SkipFields", out TypedConstant attributeValue)) {
                foreach (TypedConstant typedConstant in attributeValue.Values) {
                    props.skipFields.Add(typedConstant.GetValueAsString());
                }
            }
        }
        // 解析命名空间别名
        {
            if (AptUtils.GetAttributeValue(attributeData, "NamespaceAliases", out TypedConstant attributeValue)) {
                foreach (TypedConstant typedConstant in attributeValue.Values) {
                    string element = typedConstant.GetValueAsString();
                    if (string.IsNullOrWhiteSpace(element)) continue;
                    string[] pair = element.Split('=');
                    string alias = pair[0].Trim();
                    string name = pair[1].Trim();
                    props.namespaceAliases.Add(new KeyValuePair<string, string>(name, alias));
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