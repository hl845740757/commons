#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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
using Serilog;

namespace Wjybxx.BTree
{
/// <summary>
/// 用于行为树记录日志
/// (暂时选择了Serilog，C#也没标准的接口)
/// </summary>
public static class TaskLogger
{
    private static readonly ILogger logger = Log.Logger;

    public static void Info(string format, params object[] args) {
        logger.Information(format, args);
    }

    public static void Info(Exception? ex, string format, params object[] args) {
        logger.Information(ex, format, args);
    }

    public static void Warning(string format, params object[] args) {
        logger.Warning(format, args);
    }

    public static void Warning(Exception? ex, string format, params object[] args) {
        logger.Warning(ex, format, args);
    }
}
}