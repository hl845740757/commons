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
using Serilog.Events;
using Wjybxx.Commons.Logger;

namespace Wjybxx.Commons
{
/// <summary>
/// Serilog适配器
/// </summary>
public sealed class SeriLoggerAdapter : ILogger
{
    private readonly string _name;
    private readonly Serilog.ILogger _logger;
    private readonly bool _appendName;

    public SeriLoggerAdapter(string name, Serilog.ILogger logger, bool appendName) {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appendName = appendName;
    }

    public string Name => _name;

    #region region core

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsEnabled(Level level) {
        return level switch
        {
            Level.Trace => _logger.IsEnabled(LogEventLevel.Verbose),
            Level.Debug => _logger.IsEnabled(LogEventLevel.Debug),
            Level.Info => _logger.IsEnabled(LogEventLevel.Information),
            Level.Warn => _logger.IsEnabled(LogEventLevel.Warning),
            Level.Error => _logger.IsEnabled(LogEventLevel.Error),
            _ => false
        };
    }

    public void Log(Level level, Exception ex) {
        switch (level) {
            case Level.Trace: {
                _logger.Verbose(ex, CheckFormat(level, null));
                break;
            }
            case Level.Debug: {
                _logger.Debug(ex, CheckFormat(level, null));
                break;
            }
            case Level.Info: {
                _logger.Information(ex, CheckFormat(level, null));
                break;
            }
            case Level.Warn: {
                _logger.Warning(ex, CheckFormat(level, null));
                break;
            }
            case Level.Error: {
                _logger.Error(ex, CheckFormat(level, null));
                break;
            }
        }
    }

    public void Log(Level level, string format, params object[] args) {
        switch (level) {
            case Level.Trace: {
                _logger.Verbose(format, args);
                break;
            }
            case Level.Debug: {
                _logger.Debug(format, args);
                break;
            }
            case Level.Info: {
                _logger.Information(format, args);
                break;
            }
            case Level.Warn: {
                _logger.Warning(format, args);
                break;
            }
            case Level.Error: {
                _logger.Error(format, args);
                break;
            }
        }
    }

    public void Log(Level level, Exception? ex, string format) {
        switch (level) {
            case Level.Trace: {
                _logger.Verbose(ex, format);
                break;
            }
            case Level.Debug: {
                _logger.Debug(ex, format);
                break;
            }
            case Level.Info: {
                _logger.Information(ex, format);
                break;
            }
            case Level.Warn: {
                _logger.Warning(ex, format);
                break;
            }
            case Level.Error: {
                _logger.Error(ex, format);
                break;
            }
        }
    }

    public void Log(Level level, Exception? ex, string format, params object[] args) {
        switch (level) {
            case Level.Trace: {
                _logger.Verbose(ex, format, args);
                break;
            }
            case Level.Debug: {
                _logger.Debug(ex, format, args);
                break;
            }
            case Level.Info: {
                _logger.Information(ex, format, args);
                break;
            }
            case Level.Warn: {
                _logger.Warning(ex, format, args);
                break;
            }
            case Level.Error: {
                _logger.Error(ex, format, args);
                break;
            }
        }
    }

    #endregion

    #region trace

    public bool IsTraceEnabled => _logger.IsEnabled(LogEventLevel.Verbose);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Trace(Exception ex) => _logger.Verbose(ex, CheckFormat(Level.Trace, null));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Trace(string format, params object[] args) => _logger.Verbose(CheckFormat(Level.Trace, format), args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Trace(Exception? ex, string format) => _logger.Verbose(ex, CheckFormat(Level.Trace, format));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Trace(Exception? ex, string format, params object[] args) => _logger.Verbose(ex, CheckFormat(Level.Trace, format), args);

    #endregion

    #region debug

    public bool IsDebugEnabled => _logger.IsEnabled(LogEventLevel.Debug);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug(Exception ex) => _logger.Debug(ex, CheckFormat(Level.Debug, null));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug(string format, params object[] args) => _logger.Debug(CheckFormat(Level.Debug, format), args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug(Exception? ex, string format) => _logger.Debug(ex, CheckFormat(Level.Debug, format));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Debug(Exception? ex, string format, params object[] args) => _logger.Debug(ex, CheckFormat(Level.Debug, format), args);

    #endregion

    #region info

    public bool IsInfoEnabled => _logger.IsEnabled(LogEventLevel.Information);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Info(Exception ex) => _logger.Information(ex, CheckFormat(Level.Info, null));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Info(string format, params object[] args) => _logger.Information(CheckFormat(Level.Info, format), args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Info(Exception? ex, string format) => _logger.Information(ex, CheckFormat(Level.Info, format));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Info(Exception? ex, string format, params object[] args) => _logger.Information(ex, CheckFormat(Level.Info, format), args);

    #endregion

    #region warn

    public bool IsWarnEnabled => _logger.IsEnabled(LogEventLevel.Warning);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warn(Exception ex) => _logger.Warning(ex, CheckFormat(Level.Warn, null));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warn(string format, params object[] args) => _logger.Warning(CheckFormat(Level.Warn, format), args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warn(Exception? ex, string format) => _logger.Warning(ex, CheckFormat(Level.Warn, format));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Warn(Exception? ex, string format, params object[] args) => _logger.Warning(ex, CheckFormat(Level.Warn, format), args);

    #endregion

    #region error

    public bool IsErrorEnabled => _logger.IsEnabled(LogEventLevel.Error);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error(Exception ex) => _logger.Error(ex, CheckFormat(Level.Error, null));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error(string format, params object[] args) => _logger.Error(CheckFormat(Level.Error, format), args);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error(Exception? ex, string format) => _logger.Error(ex, CheckFormat(Level.Error, format));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Error(Exception? ex, string format, params object[] args) => _logger.Error(ex, CheckFormat(Level.Error, format), args);

    #endregion

    #region util

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CheckFormat(Level level, string? format) {
        if (_appendName && IsEnabled(level)) {
            if (string.IsNullOrEmpty(format)) {
                return $"[{_name}]";
            }
            return $"[{_name}] {format}";
        }
        return string.IsNullOrEmpty(format) ? "" : format;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LogEventLevel ToSeriLevel(Level level) {
        return level switch
        {
            Level.Trace => LogEventLevel.Verbose,
            Level.Debug => LogEventLevel.Debug,
            Level.Info => LogEventLevel.Information,
            Level.Warn => LogEventLevel.Warning,
            Level.Error => LogEventLevel.Error,
            _ => LogEventLevel.Error
        };
    }

    #endregion
}
}