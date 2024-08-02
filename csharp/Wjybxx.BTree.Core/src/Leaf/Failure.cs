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

namespace Wjybxx.BTree.Leaf
{
/// <summary>
/// 固定返回失败的子节点
/// </summary>
/// <typeparam name="T"></typeparam>
public class Failure<T> : LeafTask<T> where T : class
{
    private int failureStatus;

    protected override void Execute() {
        SetFailed(TaskStatus.ToFailure(failureStatus));
    }

    protected override void OnEventImpl(object eventObj) {
    }

    /// <summary>
    /// 失败时使用的状态码
    /// </summary>
    public int FailureStatus {
        get => failureStatus;
        set => failureStatus = value;
    }
}
}