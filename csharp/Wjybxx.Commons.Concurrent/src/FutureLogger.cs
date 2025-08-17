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
using Wjybxx.Commons.Logger;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 用于记录Future异步执行过程中的异常，用于排查错误
/// </summary>
public sealed class FutureLogger
{
    private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(FutureLogger));

    /// <summary>
    /// Future异常日志处理器
    /// </summary>
    private static volatile ILogHandler? _handler;
    /// <summary>
    /// 默认日志等级
    /// </summary>
    private static volatile Level logLevel = Level.Warn;

    public static Level GetLogLevel() => logLevel;

    public static void SetLogLevel(Level value) => logLevel = value;

    public static ILogHandler? GetHandler() => _handler;

    public static void SetHandler(ILogHandler? handler) {
        _handler = handler;
    }

    /// <summary>
    /// 记录Future框架出现的异常
    /// </summary>
    /// <param name="ex">异常</param>
    /// <param name="message">信息</param>
    public static void LogCause(Exception ex, string? message = null) {
        if (ex == null) throw new ArgumentNullException(nameof(ex));
        message = message ?? "Task caught exception";
        try {
            if (_handler != null) {
                _handler.LogCause(ex, message);
                return;
            }
            logger.Log(logLevel, ex, message);
        }
        catch (Exception) {
            // 该接口不能出现异常，这里的异常只能被丢弃
        }
    }

    /// <summary>
    /// Future日志处理器
    /// 注意：该handler只应该输出日志。
    /// </summary>
    public interface ILogHandler
    {
        /// <summary>
        /// 一定不能抛出异常！！！！
        /// </summary>
        /// <param name="ex">底层运算产生的异常</param>
        /// <param name="message">额外消息</param>
        void LogCause(Exception ex, string message);
    }
}
}