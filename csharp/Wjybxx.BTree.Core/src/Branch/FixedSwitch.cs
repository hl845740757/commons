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

using Wjybxx.Commons;

namespace Wjybxx.BTree.Branch
{
/// <summary>
/// 展开的switch
/// 在编辑器中，children根据坐标排序，容易变动；这里将其展开为字段，从而方便配置。
/// （这个类不是必须的，因为我们可以仅提供编辑器数据结构，在导出时转为Switch）
/// </summary>
/// <typeparam name="T"></typeparam>
[TaskInlinable]
public class FixedSwitch<T> : Switch<T> where T : class
{
    [SerializeReference] private Task<T>? branch1;
    [SerializeReference] private Task<T>? branch2;
    [SerializeReference] private Task<T>? branch3;
    [SerializeReference] private Task<T>? branch4;
    [SerializeReference] private Task<T>? branch5;

    public FixedSwitch() {
    }

    protected override void BeforeEnter() {
        base.BeforeEnter();
        if (children.Count != 0) return;
        if (branch1 != null) children.Add(branch1);
        if (branch2 != null) children.Add(branch2);
        if (branch3 != null) children.Add(branch3);
        if (branch4 != null) children.Add(branch4);
        if (branch5 != null) children.Add(branch5);
    }

    public Task<T>? Branch1 {
        get => branch1;
        set => branch1 = value;
    }

    public Task<T>? Branch2 {
        get => branch2;
        set => branch2 = value;
    }

    public Task<T>? Branch3 {
        get => branch3;
        set => branch3 = value;
    }

    public Task<T>? Branch4 {
        get => branch4;
        set => branch4 = value;
    }

    public Task<T>? Branch5 {
        get => branch5;
        set => branch5 = value;
    }
}
}