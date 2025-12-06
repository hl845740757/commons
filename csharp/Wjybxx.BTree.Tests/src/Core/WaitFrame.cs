#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

using Wjybxx.BTree;

namespace BTree.Tests;

public class WaitFrame<T> : LeafTask<T> where T : class
{
    private int required = 1;
    private int _enterFrame;
    private int _exitFrame;

    public WaitFrame() {
    }

    public WaitFrame(int required) {
        this.required = required;
    }

    protected new TimingTaskEntry<T> taskEntry => (TimingTaskEntry<T>)TaskEntry;

    public int RunFrames {
        get {
            if (IsRunning) {
                return taskEntry.frameCount - _enterFrame;
            }
            return _exitFrame - _enterFrame;
        }
    }

    protected override void Enter(int reentryId) {
        _enterFrame = taskEntry.frameCount;
    }

    protected override void Execute() {
        int count = taskEntry.frameCount - _enterFrame;
        if (count >= required) {
            SetSuccess();
        }
    }

    protected override void Exit() {
        _exitFrame = taskEntry.frameCount;
    }

    protected override void OnEventImpl(object _) {
    }

    /// <summary>
    /// 需要等待的帧数
    /// </summary>
    public int Required {
        get => required;
        set => required = value;
    }
}