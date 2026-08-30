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

namespace Wjybxx.Commons.Logger
{
/// <summary>
/// 日志工厂
/// 用户不直接使用该接口，而是通过<see cref="LoggerFactory"/>的静态方法获取Logger。
/// </summary>
public interface ILoggerFactory : IDisposable
{
    /// <summary>
    /// 获取指定name的Logger。
    /// 如果Logger不存在，则创建新的Logger并记录下来，下次调用该接口将返回同一个实例。
    /// </summary>
    /// <param name="name">logger的名字</param>
    /// <returns></returns>
    ILogger GetLogger(string name);
}
}