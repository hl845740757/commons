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

using System;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 提供最小支持的AgentEvent
/// </summary>
public struct MiniAgentEvent : IAgentEvent
{
    private int type;
    private object? obj1;
    private object? obj2;
    private int options;

    /// <summary>
    /// 构造函数将type声明为可选值，会导致不被调用构造函数
    /// </summary>
    public static readonly Func<MiniAgentEvent> FACTORY = () => {
        MiniAgentEvent r = default;
        r.type = IAgentEvent.TYPE_INVALID;
        return r;
    };

    public int Type {
        get => type;
        set => type = value;
    }

    public int Options {
        get => options;
        set => options = value;
    }

    public object? Obj1 {
        get => obj1;
        set => obj1 = value;
    }

    public object? Obj2 {
        get => obj2;
        set => obj2 = value;
    }

    public void Clean() {
        type = IAgentEvent.TYPE_INVALID;
        options = 0;
        obj1 = null;
        obj2 = null;
    }

    public void CleanAll() {
        type = IAgentEvent.TYPE_INVALID;
        options = 0;
        obj1 = null;
        obj2 = null;
    }
}
}