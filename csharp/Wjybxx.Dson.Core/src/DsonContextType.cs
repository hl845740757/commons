#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson上下文类型
/// </summary>
public enum DsonContextType
{
    /** 顶层上下文（类数组结构） */
    TopLevel,
    /** 当前是一个普通对象结构 */
    Object,
    /** 当前是一个数组结构 */
    Array,
    /** 当前是一个Header结构 - 类似Object */
    Header,
}

/// <summary>
/// <see cref="DsonContextType"/>的工具类
/// </summary>
public static class DsonContextTypes
{
    /// <summary>
    /// 上下文的开始符号
    /// </summary>
    public static string? GetStartSymbol(this DsonContextType contextType) {
        return contextType switch
        {
            DsonContextType.TopLevel => null,
            DsonContextType.Object => "{",
            DsonContextType.Array => "[",
            DsonContextType.Header => "@{",
            _ => throw new ArgumentException(nameof(contextType))
        };
    }

    /// <summary>
    /// 上下文的结束符号
    /// </summary>
    public static string? GetEndSymbol(this DsonContextType contextType) {
        return contextType switch
        {
            DsonContextType.TopLevel => null,
            DsonContextType.Object => "}",
            DsonContextType.Array => "]",
            DsonContextType.Header => "}",
            _ => throw new ArgumentException(nameof(contextType))
        };
    }

    /// <summary>
    /// 上下文是否表示一个容器类型 - header属于普通容器类
    /// </summary>
    /// <param name="contextType">上下文类型</param>
    /// <returns></returns>
    public static bool IsContainer(this DsonContextType contextType) {
        return contextType == DsonContextType.Object || contextType == DsonContextType.Array;
    }

    /// <summary>
    /// 上下文是否表示一个数组或类似数组的类型
    /// </summary>
    /// <param name="contextType">上下文类型</param>
    /// <returns></returns>
    public static bool IsArrayLike(this DsonContextType contextType) {
        return contextType == DsonContextType.Array || contextType == DsonContextType.TopLevel;
    }

    /// <summary>
    /// 上下文是否表示一个Object或类似Object的类型(KV结构)
    /// </summary>
    /// <param name="contextType">上下文类型</param>
    /// <returns></returns>
    public static bool IsObjectLike(this DsonContextType contextType) {
        return contextType == DsonContextType.Object || contextType == DsonContextType.Header;
    }
}
}