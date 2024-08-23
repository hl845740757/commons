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
using Wjybxx.Commons.Logger;
using ILogger = Wjybxx.Commons.Logger.ILogger;

namespace Wjybxx.Commons
{
/// <summary>
/// Serilog似乎不能创建多个实例?
/// </summary>
public sealed class SeriLoggerFactory : ILoggerFactory
{
    private readonly Serilog.ILogger _logger;
    /// <summary>
    /// 所有的Logger
    /// </summary>
    private readonly ConcurrentDictionary<string, SeriLoggerAdapter> _loggerMap = new ConcurrentDictionary<string, SeriLoggerAdapter>();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public SeriLoggerFactory(Serilog.ILogger logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 全局logger
    /// </summary>
    public Serilog.ILogger GlobalLogger => _logger;

    public void Dispose() {
        _loggerMap.Clear();
    }

    public ILogger GetLogger(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (_loggerMap.TryGetValue(name, out var logger)) {
            return logger;
        }
        logger = new SeriLoggerAdapter(name, _logger);
        logger = _loggerMap.GetOrAdd(name, logger);
        return logger;
    }
}
}