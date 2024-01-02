#region LICENSE

// Copyright 2023 wjybxx(845740757@qq.com)
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

namespace Wjybxx.Commons.Time;

/// <summary>
/// 时间提供者
/// </summary>
public interface ITimeProvider
{
    /// <summary>
    /// 获取当前时间
    /// </summary>
    /// <returns></returns>
    long Current { get; }
}

/// <summary>
/// 该接口表示实现类是基于缓存时间戳的，需要外部定时去更新
/// 线程安全性取决于实现类
/// </summary>
public interface ICachedTimeProvider : ITimeProvider
{
    /// <summary>
    /// 设置当前时间戳
    /// </summary>
    /// <param name="time"></param>
    void SetCurrent(long time);
}