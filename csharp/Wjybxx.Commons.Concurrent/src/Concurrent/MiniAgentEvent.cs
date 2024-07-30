﻿#region LICENSE

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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 提供最小支持的AgentEvent
/// </summary>
public class MiniAgentEvent : IAgentEvent
{
    private int type = IAgentEvent.TYPE_INVALID;
    private object? obj0;
    private int options;

    public int Type {
        get => type;
        set => type = value;
    }

    public int Options {
        get => options;
        set => options = value;
    }

    public object? Obj0 {
        get => obj0;
        set => obj0 = value;
    }

    public void Clean() {
        type = IAgentEvent.TYPE_INVALID;
        options = 0;
        obj0 = null;
    }

    public void CleanAll() {
        Clean();
    }
}
}