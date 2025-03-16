#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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

namespace Wjybxx.BTree.FSM
{
/// <summary>
/// Fsm中的状态配置，运行时不可以修改
///
/// 注意：切换状态前记得将<see cref="props"/>赋值到<see cref="task"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public sealed class FsmStateCfg<T> where T : class
{
#nullable disable
    /** 状态的名字 */
    private string name;
    /** 状态的task的guid */
    private string guid;
    /** 状态关联的属性(输入) */
    private object props;
    /** 状态的task缓存 */
    [NonSerialized] private Task<T> task;

    public string Name {
        get => name;
        set => name = value;
    }
    public string Guid {
        get => guid;
        set => guid = value;
    }
    public object Props {
        get => props;
        set => props = value;
    }
    public Task<T> Task {
        get => task;
        set => task = value;
    }
}
}