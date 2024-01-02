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
/// 计时器
/// </summary>
public interface ITimepiece : ITimeProvider
{
    /// <summary>
    /// 当前帧和前一帧之间的时间跨度
    /// </summary>
    long DeltaTime { get; }

    /// <summary>
    /// 累加时间
    /// </summary>
    /// <param name="deltaTime">时间增量，如果该值小于0，则会被修正为0</param>
    void Update(long deltaTime);

    /// <summary>
    /// 设置当前时间戳
    /// </summary>
    /// <param name="time"></param>
    void SetCurrent(long time);

    /// <summary>
    /// 在不修改当前时间戳的情况下修改deltaTime
    /// （仅仅用在补偿的时候，慎用）
    /// </summary>
    /// <param name="deltaTime">时间间隔</param>
    void SetDeltaTime(long deltaTime);

    /// <summary>
    /// 重新启动计时器
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <param name="deltaTime">时间间隔</param>
    void Restart(long currentTime, long deltaTime);

    /// <summary>
    /// 重新启动计时 - 累积时间和deltaTime都清零。
    /// </summary>
    void Restart() {
        Restart(0, 0);
    }

    /// <summary>
    /// 重新启动计时器 - 累积时间设定为给定值，deltaTime设定为0。
    /// </summary>
    /// <param name="currentTime"></param>
    void Restart(long currentTime) {
        Restart(currentTime, 0);
    }
}