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

using System;

#pragma warning disable CS1591
namespace Wjybxx.Commons.Time;

/// <summary>
/// 基础的计时器实现
/// </summary>
public class Timepiece : ITimepiece
{
    private long _time;
    private long _deltaTime;

    public Timepiece() {
    }

    public long Current => _time;
    public long DeltaTime => _deltaTime;
    
    public void Update(long deltaTime) {
        if (deltaTime <= 0) {
            this._deltaTime = 0;
        } else {
            this._deltaTime = deltaTime;
            this._time += deltaTime;
        }
    }

    public void SetCurrent(long time) {
        this._time = time;
    }

    public void SetDeltaTime(long deltaTime) {
        CheckDeltaTime(deltaTime);
        this._deltaTime = deltaTime;
    }

    public void Restart(long curTime, long deltaTime) {
        CheckDeltaTime(deltaTime);
        this._time = curTime;
        this._deltaTime = deltaTime;
    }

    private static void CheckDeltaTime(long deltaTime) {
        if (deltaTime < 0) {
            throw new ArgumentException("deltaTime must gte 0,  value " + deltaTime);
        }
    }
}