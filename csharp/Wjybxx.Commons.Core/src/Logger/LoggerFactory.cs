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

namespace Wjybxx.Commons.Logger
{
/// <summary>
/// 日志工厂管理器
/// (该类虽然命名为LoggerFactory，实际上是LoggerFactoryMgr，为保持使用习惯，我们保留这个设计)
/// </summary>
public static class LoggerFactory
{
    /** C#不推荐锁定class，容易导致死锁问题... */
    private static readonly object _lockObject = new object();

#if UNITY_2018_4_OR_NEWER
    private static volatile ILoggerFactory provider = UnityLoggerFactory.Inst;
#else
    private static volatile ILoggerFactory provider = ConsoleLoggerFactory.Inst;
#endif

    #region GetLogger

    /// <summary>
    /// 通过Type的类型名申请Logger
    /// (type是泛型类时，应当去除泛型信息，返回同一logger)
    /// </summary>
    /// <param name="type">申请logger的类型</param>
    /// <returns></returns>
    public static ILogger GetLogger(Type type) {
        string name = GetName(type);
        return provider.GetLogger(name);
    }

    /// <summary>
    /// 获取指定name的logger。
    /// 如果logger不存在，则创建新的Logger并记录下来，下次调用该接口将返回同一个实例。
    /// </summary>
    /// <param name="name">logger的名字</param>
    /// <returns></returns>
    public static ILogger GetLogger(string name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        return provider.GetLogger(name);
    }

    private static string GetName(Type type) {
        if (type.IsGenericType) {
            type = type.GetGenericTypeDefinition();
        }
        string fullName = type.ToString();
        if (!fullName.Contains('`') && !fullName.Contains('+')) {
            return fullName;
        }
        // 内部类或泛型类
        string[] clsNames = fullName.Split('+');
        StringBuilder sb = new StringBuilder(fullName.Length);
        foreach (string clsName in clsNames) {
            if (sb.Length > 0) {
                sb.Append('.'); // 内部类之间改为点
            }
            int genericIndex = clsName.LastIndexOf('`');
            if (genericIndex > 0) {
                sb.Append(clsName, 0, genericIndex); // 删除泛型信息
            } else {
                sb.Append(clsName);
            }
        }
        return sb.ToString();
    }

    #endregion

    #region mgr

    /// <summary>
    /// 设置日志工厂的提供器
    /// (虽然SPI看起来很好，但用户主动注入则更为灵活)
    /// </summary>
    public static void SetLoggerProvider(ILoggerFactory provider) {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        lock (_lockObject) {
            LoggerFactory.provider = provider;
        }
    }

    #endregion
}
}