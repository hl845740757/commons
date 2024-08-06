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
using System.Runtime.CompilerServices;

namespace Wjybxx.Commons.Logger
{
/// <summary>
/// Logger统一接口
/// </summary>
public interface ILogger
{
    /// <summary>
    /// logger的名字
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 查询对应的Level是否启用
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    bool IsEnabled(Level level);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="ex">异常信息</param>
    void Log(Level level, Exception ex);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="format">文本模板</param>
    /// <param name="args">格式化参数</param>
    void Log(Level level, string format, params object[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="ex">异常信息</param>
    /// <param name="format">文本模板</param>
    void Log(Level level, Exception? ex, string format);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="ex">异常信息</param>
    /// <param name="format">文本模板</param>
    /// <param name="args">格式化参数</param>
    void Log(Level level, Exception? ex, string format, params object[] args);

    #region trace

    bool IsTraceEnabled {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsEnabled(Level.Trace);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Trace(Exception ex) => Log(Level.Trace, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Trace(string format, params object[] args) => Log(Level.Trace, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Trace(Exception? ex, string format) => Log(Level.Trace, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Trace(Exception? ex, string format, params object[] args) => Log(Level.Trace, ex, format, args);

    #endregion

    #region debug

    bool IsDebugEnabled {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsEnabled(Level.Debug);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Debug(Exception ex) => Log(Level.Debug, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Debug(string format, params object[] args) => Log(Level.Debug, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Debug(Exception? ex, string format) => Log(Level.Debug, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Debug(Exception? ex, string format, params object[] args) => Log(Level.Debug, ex, format, args);

    #endregion

    #region info

    bool IsInfoEnabled {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsEnabled(Level.Info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Info(Exception ex) => Log(Level.Info, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Info(string format, params object[] args) => Log(Level.Info, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Info(Exception? ex, string format) => Log(Level.Info, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Info(Exception? ex, string format, params object[] args) => Log(Level.Info, ex, format, args);

    #endregion

    #region warn

    bool IsWarnEnabled {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsEnabled(Level.Warn);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Warn(Exception ex) => Log(Level.Warn, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Warn(string format, params object[] args) => Log(Level.Warn, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Warn(Exception? ex, string format) => Log(Level.Warn, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Warn(Exception? ex, string format, params object[] args) => Log(Level.Warn, ex, format, args);

    #endregion

    #region error

    bool IsErrorEnabled {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => IsEnabled(Level.Error);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Error(Exception ex) => Log(Level.Error, ex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Error(string format, params object[] args) => Log(Level.Error, format, args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Error(Exception? ex, string format) => Log(Level.Error, ex, format);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Error(Exception? ex, string format, params object[] args) => Log(Level.Error, ex, format, args);

    #endregion
}
}