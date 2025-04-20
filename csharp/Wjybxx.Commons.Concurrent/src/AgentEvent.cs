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

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 最基础的<see cref="IAgentEvent"/>实现
/// </summary>
public struct AgentEvent : IAgentEvent
{
    private int type;
    private int options;
    public long longVal1;
    public long longVal2;
    public object? obj1;
    public object? obj2;

    /// <summary>
    /// 构造函数将type声明为可选值，会导致不被调用构造函数
    /// </summary>
    public static readonly Func<AgentEvent> FACTORY = () => {
        AgentEvent r = default;
        r.type = IAgentEvent.TYPE_INVALID;
        return r;
    };

    public AgentEvent(int type) : this() {
        this.type = type;
    }

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

    public long LongVal1 {
        get => longVal1;
        set => longVal1 = value;
    }
    public long LongVal2 {
        get => longVal2;
        set => longVal2 = value;
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
        longVal1 = 0;
        longVal2 = 0;
    }

    public override string ToString() {
        return $"{nameof(type)}: {type}," +
               $" {nameof(options)}: {options}," +
               $" {nameof(obj1)}: {obj1}," +
               $" {nameof(obj2)}: {obj2}," +
               $" {nameof(longVal1)}: {longVal1}," +
               $" {nameof(longVal2)}: {longVal2}";
    }
}
}