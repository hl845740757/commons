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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Logger
{
/// <summary>
/// 通过扩展方法可避免虚方法调用
/// 
/// 注：Unity是根据Logger.LogXXX来匹配调用栈实现跳转的，所以方法签名有双份。
/// </summary>
public static class LoggerExtensions
{
    #region trace

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTraceEnabled(this ILogger logger) => logger.IsEnabled(Level.Trace);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Trace(this ILogger logger, Exception ex) => logger.Log(Level.Trace, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Trace(this ILogger logger, string format, params object?[] args) => logger.Log(Level.Trace, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Trace(this ILogger logger, Exception? ex, string format) => logger.Log(Level.Trace, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Trace(this ILogger logger, Exception? ex, string format, params object?[] args) => logger.Log(Level.Trace, ex, format, args);

#if UNITY_2021_3_OR_NEWER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTrace(this ILogger logger, Exception ex) => logger.Log(Level.Trace, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTrace(this ILogger logger, string format, params object?[] args) => logger.Log(Level.Trace, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTrace(this ILogger logger, Exception? ex, string format) => logger.Log(Level.Trace, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogTrace(this ILogger logger, Exception? ex, string format, params object?[] args) => logger.Log(Level.Trace, ex, format, args);
#endif

    #endregion

    #region debug

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsDebugEnabled(this ILogger logger) => logger.IsEnabled(Level.Debug);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(this ILogger logger, Exception ex) => logger.Log(Level.Debug, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(this ILogger logger, string format, params object?[] args) => logger.Log(Level.Debug, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(this ILogger logger, Exception? ex, string format) => logger.Log(Level.Debug, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Debug(this ILogger logger, Exception? ex, string format, params object?[] args) => logger.Log(Level.Debug, ex, format, args);

#if UNITY_2021_3_OR_NEWER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogDebug(this ILogger logger, Exception ex) => logger.Log(Level.Debug, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogDebug(this ILogger logger, string format, params object?[] args) => logger.Log(Level.Debug, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogDebug(this ILogger logger, Exception? ex, string format) => logger.Log(Level.Debug, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogDebug(this ILogger logger, Exception? ex, string format, params object?[] args) => logger.Log(Level.Debug, ex, format, args);
#endif

    #endregion

    #region info

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInfoEnabled(this ILogger logger) => logger.IsEnabled(Level.Info);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(this ILogger logger, Exception ex) => logger.Log(Level.Info, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(this ILogger logger, string format, params object?[] args) => logger.Log(Level.Info, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(this ILogger logger, Exception? ex, string format) => logger.Log(Level.Info, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Info(this ILogger logger, Exception? ex, string format, params object?[] args) => logger.Log(Level.Info, ex, format, args);

#if UNITY_2021_3_OR_NEWER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInfo(this ILogger logger, Exception ex) => logger.Log(Level.Info, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInfo(this ILogger logger, string format, params object?[] args) => logger.Log(Level.Info, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInfo(this ILogger logger, Exception? ex, string format) => logger.Log(Level.Info, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogInfo(this ILogger logger, Exception? ex, string format, params object?[] args) => logger.Log(Level.Info, ex, format, args);
#endif

    #endregion

    #region warn

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsWarnEnabled(this ILogger logger) => logger.IsEnabled(Level.Warn);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(this ILogger logger, Exception ex) => logger.Log(Level.Warn, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(this ILogger logger, string format, params object?[] args) => logger.Log(Level.Warn, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(this ILogger logger, Exception? ex, string format) => logger.Log(Level.Warn, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Warn(this ILogger logger, Exception? ex, string format, params object?[] args) => logger.Log(Level.Warn, ex, format, args);

#if UNITY_2021_3_OR_NEWER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogWarn(this ILogger logger, Exception ex) => logger.Log(Level.Warn, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogWarn(this ILogger logger, string format, params object?[] args) => logger.Log(Level.Warn, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogWarn(this ILogger logger, Exception? ex, string format) => logger.Log(Level.Warn, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogWarn(this ILogger logger, Exception? ex, string format, params object?[] args) => logger.Log(Level.Warn, ex, format, args);
#endif

    #endregion

    #region error

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsErrorEnabled(this ILogger logger) => logger.IsEnabled(Level.Error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(this ILogger logger, Exception ex) => logger.Log(Level.Error, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(this ILogger logger, string format, params object?[] args) => logger.Log(Level.Error, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(this ILogger logger, Exception? ex, string format) => logger.Log(Level.Error, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Error(this ILogger logger, Exception? ex, string format, params object?[] args) => logger.Log(Level.Error, ex, format, args);

#if UNITY_2021_3_OR_NEWER
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogError(this ILogger logger, Exception ex) => logger.Log(Level.Error, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogError(this ILogger logger, string format, params object?[] args) => logger.Log(Level.Error, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogError(this ILogger logger, Exception? ex, string format) => logger.Log(Level.Error, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LogError(this ILogger logger, Exception? ex, string format, params object?[] args) => logger.Log(Level.Error, ex, format, args);
#endif

    #endregion
}
}