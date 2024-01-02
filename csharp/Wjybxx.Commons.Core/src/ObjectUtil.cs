#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Wjybxx.Commons;

/// <summary>
/// 一些基础工具方法
/// </summary>
public static class ObjectUtil
{
    /// <summary>
    /// 如果参数为null，则抛出异常
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T RequireNonNull<T>(T obj, string? message = null) {
        if (obj == null) throw new ArgumentNullException(nameof(obj), message);
        return obj;
    }

    /// <summary>
    /// 如果参数为null，则转为给定的默认值
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T NullToDef<T>(T? obj, T? def) {
        return obj == null ? def : obj;
    }

    /// <summary>
    /// 获取系统的tick数
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long SystemTicks() => Stopwatch.GetTimestamp();

    #region string

    /// <summary>
    /// 获取字符串的长度，如果字符为null，则返回0
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Length(string? value) {
        return value?.Length ?? 0;
    }

    /// <summary>
    /// 如果字符串为null或空字符串，则转为默认字符串
    /// </summary>
    /// <param name="value">value</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string EmptyToDef(string value, string def) {
        return string.IsNullOrEmpty(value) ? def : value;
    }

    /// <summary>
    /// 如果字符串为全空白字符串，则转为默认字符串
    /// </summary>
    /// <param name="value">value</param>
    /// <param name="def">默认值</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string BlankToDef(string value, string def) {
        return string.IsNullOrWhiteSpace(value) ? def : value;
    }

    /// <summary>
    /// 首字母大写
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string FirstCharToUpperCase(string str) {
        int length = Length(str);
        if (length == 0) {
            return str;
        }
        char firstChar = str[0];
        if (char.IsLower(firstChar)) { // 可拦截非英文字符
            StringBuilder sb = new StringBuilder(str);
            sb[0] = char.ToUpper(firstChar);
            return sb.ToString();
        }
        return str;
    }

    /// <summary>
    /// 首字母小写
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string FirstCharToLowerCase(string str) {
        int length = Length(str);
        if (length == 0) {
            return str;
        }
        char firstChar = str[0];
        if (char.IsUpper(firstChar)) { // 可拦截非英文字符
            StringBuilder sb = new StringBuilder(str);
            sb[0] = char.ToLower(firstChar);
            return sb.ToString();
        }
        return str;
    }

    /// <summary>
    /// 字符串是否包含空白字符
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static bool ContainsWhitespace(string str) {
        int strLen = Length(str);
        if (strLen == 0) {
            return false;
        }
        for (int i = 0; i < strLen; i++) {
            if (char.IsWhiteSpace(str[i])) {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 反转大小写模式
    /// </summary>
    /// <param name="caseMode"></param>
    /// <returns></returns>
    public static CaseMode Invert(this CaseMode caseMode) {
        return caseMode switch
        {
            CaseMode.UpperCase => CaseMode.LowerCase,
            CaseMode.LowerCase => CaseMode.UpperCase,
            _ => throw new AssertionError()
        };
    }

    /// <summary>
    /// 将字符串转为给定模式
    /// </summary>
    /// <param name="caseMode">大小写模式</param>
    /// <param name="value">要转换的字符串</param>
    /// <returns></returns>
    public static string ToCase(this CaseMode caseMode, string value) {
        return caseMode switch
        {
            CaseMode.UpperCase => value.ToUpper(),
            CaseMode.LowerCase => value.ToLower(),
            _ => throw new AssertionError()
        };
    }

    #endregion
}