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
using Serilogger = Serilog.ILogger;

namespace Wjybxx.Commons.Logger
{
/// <summary>
/// Serilog适配器
/// </summary>
internal sealed class SeriLogger : ILogger
{
    private readonly Serilogger _logger;
    private readonly string _name;
    private readonly bool _appendName;

    public SeriLogger(Serilogger logger, string name, bool appendName) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _name = name ?? throw new ArgumentNullException(nameof(name));
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
        if (!IsEnabled(level)) return;
        switch (level) {
            case Level.Trace: {
                _logger.Verbose(ex, CheckFormat(null));
                break;
            }
            case Level.Debug: {
                _logger.Debug(ex, CheckFormat(null));
                break;
            }
            case Level.Info: {
                _logger.Information(ex, CheckFormat(null));
                break;
            }
            case Level.Warn: {
                _logger.Warning(ex, CheckFormat(null));
                break;
            }
            case Level.Error: {
                _logger.Error(ex, CheckFormat(null));
                break;
            }
        }
    }

    public void Log(Level level, string format, params object[] args) {
        if (!IsEnabled(level)) return;
        switch (level) {
            case Level.Trace: {
                _logger.Verbose(CheckFormat(format), args);
                break;
            }
            case Level.Debug: {
                _logger.Debug(CheckFormat(format), args);
                break;
            }
            case Level.Info: {
                _logger.Information(CheckFormat(format), args);
                break;
            }
            case Level.Warn: {
                _logger.Warning(CheckFormat(format), args);
                break;
            }
            case Level.Error: {
                _logger.Error(CheckFormat(format), args);
                break;
            }
        }
    }

    public void Log(Level level, Exception? ex, string format) {
        if (!IsEnabled(level)) return;
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
        if (!IsEnabled(level)) return;
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

    #region util

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CheckFormat(string? format) {
        if (_appendName) {
            return string.IsNullOrEmpty(format) ? $"[{_name}]" : $"[{_name}] {format}";
        }
        return string.IsNullOrEmpty(format) ? "" : format;
    }

    #endregion
}
}