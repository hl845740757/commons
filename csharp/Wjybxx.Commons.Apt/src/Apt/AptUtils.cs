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

using System;
using System.Collections.Generic;
using System.Text;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Poet;

namespace Wjybxx.Commons.Apt
{
public class AptUtils
{
    private static readonly ClassName clsName_GeneratedAttribute = ClassName.Get(typeof(GeneratedAttribute));
    private static readonly ClassName clsName_SourceFileRef = ClassName.Get(typeof(SourceFileRefAttribute));

    /// <summary>
    /// 为生成代码的注解处理器创建一个通用注解
    /// </summary>
    public static AttributeSpec NewProcessorInfoAnnotation(Type type) {
        return AttributeSpec.NewBuilder(clsName_GeneratedAttribute)
            .Constructor(CodeBlock.Of("$S", type.ToString()))
            .Build();
    }

    /// <summary>
    /// 添加指向源代码文件的引用，方便查看文件依赖
    /// </summary>
    /// <param name="sourceFileTypeName"></param>
    /// <returns></returns>
    public static AttributeSpec NewSourceFileRefAnnotation(TypeName sourceFileTypeName) {
        return AttributeSpec.NewBuilder(clsName_SourceFileRef)
            .Constructor(CodeBlock.Of("typeof($T)", sourceFileTypeName))
            .Build();
    }

    /**
     * 将继承体系展开，不包含实现的接口。
     * （超类在后）
     */
    public static List<Type> FlatInherit(Type typeElement) {
        List<Type> result = new List<Type>(4);
        result.Add(typeElement);
        while ((typeElement = typeElement.BaseType) != null) {
            result.Add(typeElement);
        }
        return result;
    }

    /**
     * 将继承体系展开，并逆序返回，不包含实现的接口。
     * （超类在前）
     */
    public static List<Type> FlatInheritAndReverse(Type typeElement) {
        List<Type> result = FlatInherit(typeElement);
        result.Reverse();
        return result;
    }

    //----------------------------------------------------------------------------

    /**
     * 根据原类型，生成获得对应的辅助类的类名
     * 对于内部类，生成的类为：外部类名_内部类名
     *
     * @param suffix 后缀
     */
    public static string GetProxyClassName(Type type, string? suffix = null) {
        if (suffix == null) suffix = "";

        string proxyName;
        if (type.DeclaringType == null) {
            proxyName = type.Name + suffix; // TopLevel
        } else {
            // 内部类，避免与其它的内部类冲突，不能使用简单名
            // Q: 为什么不使用$符合?
            // A: 因为生成的工具类都是外部类，不是内部类。
            List<string> simpleNames = new List<string>(3);
            simpleNames.Add(type.Name);
            while ((type = type.DeclaringType) != null) {
                simpleNames.Add(type.Name);
            }
            simpleNames.Reverse();
            proxyName = string.Join("_", simpleNames) + suffix;
        }
        // C#泛型的simpleName会包含反引号...没有删除字符的快捷方法
        if (proxyName.Contains('`')) {
            StringBuilder builder = new StringBuilder(proxyName.Length);
            for (var i = 0; i < proxyName.Length; i++) {
                if (proxyName[i] != '`') {
                    builder.Append(proxyName[i]);
                }
            }
            proxyName = builder.ToString();
        }
        return proxyName;
    }
}
}