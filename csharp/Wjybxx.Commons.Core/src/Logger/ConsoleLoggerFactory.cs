﻿#region LICENSE

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

namespace Wjybxx.Commons.Logger
{
/// <summary>
/// 简单的打印到console的logger
/// (实际项目中不会使用，只是在开发期间避免依赖时使用)
/// </summary>
public sealed class ConsoleLoggerFactory : ILoggerFactory
{
    /// <summary>
    /// 静态全局单例
    /// </summary>
    public static ConsoleLoggerFactory Inst { get; } = new ConsoleLoggerFactory();

    /// <summary>
    /// 所有的Logger
    /// </summary>
    private readonly ConcurrentDictionary<string, ConsoleLogger> _loggerMap = new ConcurrentDictionary<string, ConsoleLogger>();
    /// <summary>
    /// 启用的log等级
    /// </summary>
    private volatile Level _enabledLevel = Level.Info;

    private ConsoleLoggerFactory() {
    }

    public void Dispose() {
        _loggerMap.Clear();
    }

    /// <summary>
    /// level可以在枚举外，0表示全部打印，[Error + 1]表示不打印
    /// </summary>
    public Level EnabledLevel {
        get => _enabledLevel;
        set => _enabledLevel = value;
    }

    public ILogger GetLogger(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        if (_loggerMap.TryGetValue(name, out var logger)) {
            return logger;
        }
        logger = new ConsoleLogger(this, name);
        logger = _loggerMap.GetOrAdd(name, logger);
        return logger;
    }

    internal bool IsEnabled(Level level) {
        return level >= _enabledLevel;
    }
}
}