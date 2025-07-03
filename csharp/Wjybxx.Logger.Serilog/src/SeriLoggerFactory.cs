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
using System.Collections.Concurrent;
using Serilogger = Serilog.ILogger;

namespace Wjybxx.Commons.Logger
{
/// <summary>
/// Serilog似乎不能创建多个实例?
/// </summary>
public sealed class SeriLoggerFactory : ILoggerFactory
{
    private readonly Serilogger _logger;
    private readonly bool _appendName;
    /// <summary>
    /// 所有的Logger
    /// </summary>
    private readonly ConcurrentDictionary<string, SeriLogger> _loggerMap = new ConcurrentDictionary<string, SeriLogger>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger">serilog</param>
    /// <param name="appendName">是否在日志前面追加name</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SeriLoggerFactory(Serilogger logger, bool appendName = false) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _appendName = appendName;
    }

    /// <summary>
    /// 全局logger
    /// </summary>
    public Serilogger GlobalLogger => _logger;

    public void Dispose() {
        _loggerMap.Clear();
    }

    public ILogger GetLogger(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (_loggerMap.TryGetValue(name, out var logger)) {
            return logger;
        }
        logger = new SeriLogger(_logger, name, _appendName);
        logger = _loggerMap.GetOrAdd(name, logger);
        return logger;
    }
}
}