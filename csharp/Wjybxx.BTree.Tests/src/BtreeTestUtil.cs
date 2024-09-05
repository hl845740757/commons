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
using System.Linq;
using Wjybxx.BTree;
using Wjybxx.BTree.Decorator;
using Wjybxx.BTree.Leaf;
using Wjybxx.Commons.Ex;

namespace BTree.Tests;

internal class BtreeTestUtil
{
    internal static readonly Random random = new Random();

    public static TaskEntry<Blackboard> newTaskEntry() {
        return new TaskEntry<Blackboard>("Main", null, new Blackboard());
    }

    public static TaskEntry<Blackboard> newTaskEntry(Task<Blackboard> root) {
        return new TaskEntry<Blackboard>("Main", root, new Blackboard());
    }

    public static void untilCompleted<T>(TaskEntry<T> entry) where T : class {
        for (int idx = 0; idx < 200; idx++) { // 避免死循环
            entry.Update(idx);
            if (entry.IsCompleted) return;
        }
        throw new InfiniteLoopException();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="entry">任务入口</param>
    /// <param name="frameAction">帧回调，初始帧号0；在task执行后调用</param>
    /// <typeparam name="T"></typeparam>
    /// <exception cref="InfiniteLoopException"></exception>
    public static void untilCompleted<T>(TaskEntry<T> entry, Action<int> frameAction) where T : class {
        for (int idx = 0; idx < 200; idx++) { // 避免死循环
            entry.Update(idx);
            frameAction.Invoke(idx);
            if (entry.IsCompleted) return;
        }
        throw new InfiniteLoopException();
    }

    /** 需要注意！直接遍历子节点，可能统计到上次的执行结果 */
    public static int completedCount<T>(Task<T> ctrl) where T : class {
        int count = 0;
        for (int i = 0; i < ctrl.GetChildCount(); i++) {
            if (ctrl.GetChild(i).IsCompleted) count++;
        }
        return count;
    }

    public static int succeededCount<T>(Task<T> ctrl) where T : class {
        int count = 0;
        for (int i = 0; i < ctrl.GetChildCount(); i++) {
            if (ctrl.GetChild(i).IsSucceeded) count++;
        }
        return count;
    }

    public static int failedCount<T>(Task<T> ctrl) where T : class {
        int count = 0;
        for (int i = 0; i < ctrl.GetChildCount(); i++) {
            if (ctrl.GetChild(i).IsFailed) count++;
        }
        return count;
    }

    /**
     * @param childCount   子节点数量
     * @param successCount 期望成功的子节点数量
     */
    public static void initChildren(BranchTask<Blackboard> branch, int childCount, int successCount) {
        branch.RemoveAllChild();
        // 不能过于简单的成功或失败，否则可能无法覆盖所有情况
        for (int i = 0; i < successCount; i++) {
            branch.AddChild(new WaitFrame<Blackboard>(random.Next(0, 3)));
        }
        for (int i = successCount; i < childCount; i++) {
            branch.AddChild(new Inverter<Blackboard>(new WaitFrame<Blackboard>(random.Next(0, 3))));
        }
        branch.ShuffleChild(); // 打乱child
    }
}