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

namespace Wjybxx.Commons.Time
{
/// <summary>
/// 计时器
/// </summary>
public interface ITimepiece : ITimeProvider
{
    /// <summary>
    /// 当前帧和前一帧之间的时间跨度，取决于Update
    /// </summary>
    int DeltaTime { get; }

    /// <summary>
    /// 运行帧数
    /// (每秒60帧可运行410天)
    /// </summary>
    int FrameCount { get; }

    /// <summary>
    /// 累加时间
    /// </summary>
    /// <param name="deltaTime">时间增量，如果该值小于0，则会被修正为0</param>
    void Update(int deltaTime);

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
    void SetDeltaTime(int deltaTime);

    /// <summary>
    /// 在不修改当前时间戳的情况下修改frameCount
    /// （慎用）
    /// </summary>
    /// <param name="frameCount"></param>
    void SetFrameCount(int frameCount);

    /// <summary>
    /// 重新启动计时器
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <param name="deltaTime">时间间隔</param>
    /// <param name="frameCount">当前帧号</param>
    void Restart(long currentTime, int deltaTime = 0, int frameCount = 0);

    /// <summary>
    /// 重新启动计时器
    /// </summary>
    void Restart();
}
}