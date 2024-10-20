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
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Wjybxx.Commons
{
public static class SerilogHelper
{
#if NET6_0_OR_GREATER
    /// <summary>
    /// 通过AppSetting中的配置初始化
    /// </summary>
    /// <param name="appSettingsPath"></param>
    /// <returns></returns>
    public static SeriLoggerFactory InitFromAppSettings(string appSettingsPath = "appsettings.json") {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Environment.CurrentDirectory)
            .AddJsonFile(appSettingsPath)
            .Build();

        ILogger logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        return new SeriLoggerFactory(logger);
    }
#endif
}
}