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
using System.Text;
using Wjybxx.Commons.Pool;

namespace Wjybxx.Commons.Logger
{
/// <summary>
/// 
/// </summary>
internal class ConsoleLogger : ILogger
{
    private readonly ConsoleLoggerFactory _factory;
    private readonly string _name;

    public ConsoleLogger(ConsoleLoggerFactory factory, string name) {
        this._factory = factory;
        this._name = name;
    }

    public string Name => _name;

    public bool IsEnabled(Level level) {
        return _factory.IsEnabled(level);
    }

    public void Log(Level level, Exception? ex) {
        if (!_factory.IsEnabled(level)) {
            return;
        }
        if (ex == null) {
            return;
        }
        Console.WriteLine($"[{FormatDateTime(DateTime.Now)}] [{level}] [{_name}]");
        Console.WriteLine(ex.ToString());
    }

    public void Log(Level level, string format, params object?[] args) {
        if (!_factory.IsEnabled(level)) {
            return;
        }
        Console.WriteLine($"[{FormatDateTime(DateTime.Now)}] [{level}] [{_name}] {format}", args);
    }

    public void Log(Level level, Exception? ex, string format) {
        if (!_factory.IsEnabled(level)) {
            return;
        }
        Console.WriteLine($"[{FormatDateTime(DateTime.Now)}] [{level}] [{_name}] {format}");
        if (ex != null) {
            Console.WriteLine(ex.ToString());
        }
    }

    public void Log(Level level, Exception? ex, string format, params object?[] args) {
        if (!_factory.IsEnabled(level)) {
            return;
        }
        Console.WriteLine($"[{FormatDateTime(DateTime.Now)}] [{level}] [{_name}] {format}", args);
        if (ex != null) {
            Console.WriteLine(ex.ToString());
        }
    }

    #region util

    private static readonly ConcurrentObjectPool<StringBuilder> stringBuilderPool = new ConcurrentObjectPool<StringBuilder>(
        () => new StringBuilder(64), sb => sb.Clear(), 32);

    private static string FormatDateTime(DateTime dateTime) {
        StringBuilder sb = stringBuilderPool.Acquire();
        try {
            sb.Append(dateTime.ToString("s"));
            sb.Replace('T', ' '); // 替换T为空格
            sb.Append('.');

            int millis = dateTime.Millisecond;
            if (millis < 10) {
                sb.Append("00");
            } else if (millis < 100) {
                sb.Append('0');
            }
            sb.Append(millis);
            return sb.ToString();
        }
        finally {
            stringBuilderPool.Release(sb);
        }
    }

    #endregion
}
}