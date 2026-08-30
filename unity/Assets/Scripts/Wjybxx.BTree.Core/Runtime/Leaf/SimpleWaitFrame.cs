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

namespace Wjybxx.BTree.Leaf
{
/// <summary>
/// 简单等待一定帧数
///
/// 注：每Execute一次代表一次心跳，如果需要取用游戏的真实帧号，请自行实现。
/// </summary>
/// <typeparam name="T"></typeparam>
public class SimpleWaitFrame<T> : LeafTask<T> where T : class
{
    private int required = 1;
    [NonSerialized] private int count;

    public SimpleWaitFrame() {
    }

    public SimpleWaitFrame(int required) {
        this.required = required;
    }

    protected override void Enter(int reentryId) {
        count = 0;
        if (Required == 0) {
            SetSuccess();
        }
    }

    protected override void Execute() {
        if (++count >= required) {
            SetSuccess();
        }
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

    /// <summary>
    /// 执行帧数
    /// </summary>
    public int Count => count;
}
}