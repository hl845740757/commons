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

public class TimingTaskEntry<T> : TaskEntry<T> where T : class
{
    public float time;
    public float deltaTime;
    public int frameCount;

    public TimingTaskEntry() {
    }

    public TimingTaskEntry(string? name, Task<T>? rootTask, T? blackboard)
        : base(name, rootTask, blackboard) {
    }
}