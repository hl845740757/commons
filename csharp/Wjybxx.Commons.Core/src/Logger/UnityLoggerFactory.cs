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

#if UNITY_EDITOR
using System;
using System.Collections.Concurrent;

namespace Wjybxx.Commons.Logger
{
public sealed class UnityLoggerFactory : ILoggerFactory
{
    /// <summary>
    /// 全局静态单例
    /// </summary>
    public static UnityLoggerFactory Inst { get; } = new UnityLoggerFactory();

    /// <summary>
    /// 所有的Logger
    /// </summary>
    private readonly ConcurrentDictionary<string, UnityLogger> _loggerMap = new ConcurrentDictionary<string, UnityLogger>();
    /// <summary>
    /// 启用的log等级
    /// </summary>
    private volatile Level _enabledLevel = Level.Info;

    private UnityLoggerFactory() {
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
        logger = new UnityLogger(this, name);
        logger = _loggerMap.GetOrAdd(name, logger);
        return logger;
    }

    internal bool IsEnabled(Level level) {
        return level >= _enabledLevel;
    }
}
}
#endif