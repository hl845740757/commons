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

#if UNITY_2018_4_OR_NEWER
using System;
using UnityEngine;

namespace Wjybxx.Commons.Logger
{
internal class UnityLogger : ILogger
{
    private readonly UnityLoggerFactory _factory;
    private readonly string _name;

    public UnityLogger(UnityLoggerFactory factory, string name) {
        _factory = factory;
        _name = name;
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
        switch (level)
        {
            case Level.Warn:
                Debug.LogWarningFormat($"[{_name}]", Array.Empty<object>());
                break;
            case Level.Error:
                Debug.LogErrorFormat($"[{_name}]", Array.Empty<object>());
                break;
            default:
                Debug.LogFormat($"[{_name}]", Array.Empty<object>());
                break;
        }
        Debug.LogException(ex);
    }

    public void Log(Level level, string format, params object?[] args) {
        if (!_factory.IsEnabled(level)) {
            return;
        }
        switch (level) {
            case Level.Warn:
                Debug.LogWarningFormat($"[{_name}] {format}", args);
                break;
            case Level.Error:
                Debug.LogErrorFormat($"[{_name}] {format}", args);
                break;
            default:
                Debug.LogFormat($"[{_name}] {format}", args);
                break;
        }
    }

    public void Log(Level level, Exception? ex, string format) {
        if (!_factory.IsEnabled(level)) {
            return;
        }
        switch (level) {
            case Level.Warn:
                Debug.LogWarningFormat($"[{_name}] {format}", Array.Empty<object>());
                break;
            case Level.Error:
                Debug.LogErrorFormat($"[{_name}] {format}", Array.Empty<object>());
                break;
            default:
                Debug.LogFormat($"[{_name}] {format}", Array.Empty<object>());
                break;
        }
    }

    public void Log(Level level, Exception? ex, string format, params object?[] args) {
        if (!_factory.IsEnabled(level)) {
            return;
        }
        switch (level) {
            case Level.Warn:
                Debug.LogWarningFormat($"[{_name}] {format}", args);
                break;
            case Level.Error:
                Debug.LogErrorFormat($"[{_name}] {format}", args);
                break;
            default:
                Debug.LogFormat($"[{_name}] {format}", args);
                break;
        }
        if (ex != null) {
            Debug.LogException(ex);
        }
    }
}
}
#endif