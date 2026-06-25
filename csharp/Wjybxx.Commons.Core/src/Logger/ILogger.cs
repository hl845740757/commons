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

namespace Wjybxx.Commons.Logger
{
/// <summary>
/// Logger统一接口
/// </summary>
public interface ILogger
{
    /// <summary>
    /// logger的名字
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 查询对应的Level是否启用
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    bool IsEnabled(Level level);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="format">文本模板</param>
    void Log(Level level, string format);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="format">文本模板</param>
    /// <param name="args">格式化参数</param>
    void Log(Level level, string format, params object?[] args);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="ex">异常信息</param>
    void Log(Level level, Exception ex);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="ex">异常信息</param>
    /// <param name="format">文本模板</param>
    void Log(Level level, Exception? ex, string format);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level">日志等级</param>
    /// <param name="ex">异常信息</param>
    /// <param name="format">文本模板</param>
    /// <param name="args">格式化参数</param>
    void Log(Level level, Exception? ex, string format, params object?[] args);
}
}