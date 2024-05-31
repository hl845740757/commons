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
    /// (稳定值与平台无关)
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long SystemTicks() => (long)(Stopwatch.GetTimestamp() * s_tickFrequency);

    /// <summary>
    /// 系统tick对应的毫秒时间戳
    /// 注意：不是Unix时间戳！
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long SystemTickMillis() => (long)(Stopwatch.GetTimestamp() * s_millis_tickFrequency);

    /// <summary>
    /// 'Frequency'存储的是在当前平台上，1秒对应多少个原始tick -- 依赖平台。
    /// </summary>
    private static readonly double s_tickFrequency = (double)DatetimeUtil.TicksPerSecond / Stopwatch.Frequency;
    private static readonly double s_millis_tickFrequency = (double)DatetimeUtil.TicksPerMillisecond / Stopwatch.Frequency;

    #region string

    /// <summary>
    /// 通过索引区间获取子字符串。
    /// C#的字符串接口和Java差异较大，这里提供一个适配方法。
    /// </summary>
    /// <param name="value"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Substring2(this string value, int start, int end) {
        return value.Substring(start, end - start + 1);
    }

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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToCase(this CaseMode caseMode, string value) {
        return caseMode switch
        {
            CaseMode.UpperCase => value.ToUpper(),
            CaseMode.LowerCase => value.ToLower(),
            _ => throw new AssertionError()
        };
    }

    /// <summary>
    /// 获取字符串的UTF-8编码结果
    /// (经常找不到地方...)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetUtf8Bytes(string value) {
        return Encoding.UTF8.GetBytes(value);
    }

    /// <summary>
    /// 将utf8字节数组转为字符串
    /// </summary>
    /// <param name="data">utf8字节</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetUtf8String(byte[] data) {
        return Encoding.UTF8.GetString(data);
    }

    #endregion
}