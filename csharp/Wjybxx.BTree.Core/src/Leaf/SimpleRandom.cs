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

namespace Wjybxx.BTree.Leaf
{
/// <summary>
/// 简单随机节点
/// 在正式的项目中，Random应当从实体上获取。
/// </summary>
/// <typeparam name="T"></typeparam>
public class SimpleRandom<T> : LeafTask<T> where T : class
{
    private float p;

    public SimpleRandom() {
    }

    public SimpleRandom(float p = 0.5f) {
        this.p = p;
    }

    protected override void Execute() {
        if (MathCommon.SharedRandom.NextDouble() <= p) {
            SetSuccess();
        } else {
            SetFailed(TaskStatus.ERROR);
        }
    }

    protected override void OnEventImpl(object _) {
    }

    /// <summary>
    /// 概率
    /// </summary>
    public float P {
        get => p;
        set => p = value;
    }
}
}