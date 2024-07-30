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

namespace Wjybxx.Commons.Time
{
/// <summary>
/// 基础的计时器实现
/// </summary>
public class Timepiece : ITimepiece
{
    private long _time;
    private int _deltaTime;
    private int _frameCount;

    public Timepiece() {
    }

    public long Current => _time;
    public int DeltaTime => _deltaTime;

    public int FrameCount => _frameCount;

    public void Update(int deltaTime) {
        if (deltaTime <= 0) {
            this._deltaTime = 0;
        } else {
            this._deltaTime = deltaTime;
            this._time += deltaTime;
        }
        _frameCount++;
    }

    public void SetCurrent(long time) {
        this._time = time;
    }

    public void SetDeltaTime(int deltaTime) {
        CheckDeltaTime(deltaTime);
        this._deltaTime = deltaTime;
    }

    public void SetFrameCount(int frameCount) {
        CheckFrameCount(frameCount);
        this._frameCount = frameCount;
    }

    public void Restart() {
        this._time = 0;
        this._deltaTime = 0;
        this._frameCount = 0;
    }

    public void Restart(long currentTime, int deltaTime = 0, int frameCount = 0) {
        CheckDeltaTime(deltaTime);
        CheckFrameCount(frameCount);
        this._time = currentTime;
        this._deltaTime = deltaTime;
        this._frameCount = frameCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckFrameCount(int frameCount) {
        if (frameCount < 0) {
            throw new ArgumentException("frameCount must gte 0,  value " + frameCount);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CheckDeltaTime(int deltaTime) {
        if (deltaTime < 0) {
            throw new ArgumentException("deltaTime must gte 0,  value " + deltaTime);
        }
    }
}
}