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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Wjybxx.Commons.Attributes;
using Wjybxx.Commons.Poet;

#pragma warning disable CS1591

namespace Wjybxx.Commons.Apt;

public class AptUtils
{
    private static readonly ClassName clsName_SourceFileRef = ClassName.Get(typeof(SourceFileRefAttribute));

    /// <summary>
    /// 为生成代码的注解处理器创建一个通用注解
    /// </summary>
    public static AttributeSpec NewProcessorInfoAnnotation(Type type) {
        return AttributeSpec.NewBuilder(type)
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

    /// <summary>
    /// 拷贝方法信息，不包含代码块
    /// </summary>
    public static MethodSpec.Builder CopyMethod(MethodInfo methodInfo) {
        MethodSpec.Builder builder = MethodSpec.NewMethodBuilder(methodInfo.Name);
        builder.AddModifiers(MethodSpec.ParseModifiers(methodInfo));
        CopyTypeVariables(builder, methodInfo);
        CopyReturnType(builder, methodInfo);
        CopyParameters(builder, methodInfo);
        builder.varargs = IsVarArgsMethod(methodInfo);
        return builder;
    }

    /// <summary>
    /// 拷贝泛型参数
    /// </summary>
    public static void CopyTypeVariables(MethodSpec.Builder builder, MethodInfo methodInfo) {
        if (methodInfo.IsGenericMethodDefinition) {
            Type[] genericArguments = methodInfo.GetGenericArguments();
            foreach (Type genericArgument in genericArguments) {
                builder.AddTypeVariable(TypeVariableName.Get(genericArgument));
            }
        }
    }

    /// <summary>
    /// 拷贝返回值类型
    /// </summary>
    public static void CopyReturnType(MethodSpec.Builder builder, MethodInfo methodInfo) {
        builder.Returns(TypeName.Get(methodInfo.ReturnType));
    }

    /// <summary>
    /// 拷贝方法的所有参数
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="methodInfo"></param>
    public static void CopyParameters(MethodSpec.Builder builder, MethodInfo methodInfo) {
        CopyParameters(builder, methodInfo.GetParameters());
    }

    /// <summary>
    /// 拷贝这些方法参数
    /// </summary>
    public static void CopyParameters(MethodSpec.Builder builder, IEnumerable<ParameterInfo> parameters) {
        foreach (ParameterInfo parameter in parameters) {
            builder.AddParameter(ParameterSpec.Get(parameter));
        }
    }

    /// <summary>
    /// 是否是变长参数方法
    /// </summary>
    /// <param name="methodInfo"></param>
    /// <returns></returns>
    public static bool IsVarArgsMethod(MethodInfo methodInfo) {
        ParameterInfo[] parameterInfos = methodInfo.GetParameters();
        if (parameterInfos.Length > 0) {
            // 处理params修饰符
            return parameterInfos[parameterInfos.Length - 1].IsDefined(typeof(ParamArrayAttribute), true);
        }
        return false;
    }

    //----------------------------------------------------------------------------

    /**
     * 根据原类型，生成获得对应的辅助类的类名
     * 对于内部类，生成的类为：外部类名_内部类名
     *
     * @param suffix 后缀
     */
    public static string GetProxyClassName(Type typeElement, string? suffix = null) {
        if (suffix == null) suffix = "";
        if (typeElement.DeclaringType == null) {
            return typeElement.Name + suffix; // TopLevel
        } else {
            // 内部类，避免与其它的内部类冲突，不能使用简单名
            // Q: 为什么不使用$符合?
            // A: 因为生成的工具类都是外部类，不是内部类。
            List<string> simpleNames = new List<string>(3);
            simpleNames.Add(typeElement.Name);
            while ((typeElement = typeElement.DeclaringType) != null) {
                simpleNames.Add(typeElement.Name);
            }
            simpleNames.Reverse();
            return string.Join("_", simpleNames) + suffix;
        }
    }
}