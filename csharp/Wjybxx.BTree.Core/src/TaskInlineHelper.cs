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
using System.Diagnostics;
using Wjybxx.BTree.Branch;

namespace Wjybxx.BTree
{
/// <summary>
/// 存储共享变量等
/// </summary>
public static class TaskInlineHelper
{
    /// <summary>
    /// 是否启用内联
    /// </summary>
    public static bool enableInline = true;
}

/// <summary>
/// 
/// </summary>
/// <typeparam name="T"></typeparam>
public class TaskInlineHelper<T> where T : class
{
    /** 无效重入id */
    private const int INVALID_REENTRY_ID = int.MinValue;
    /** 表示内联失败 */
    private const int FAILED_REENTRY_ID = INVALID_REENTRY_ID + 1;

#nullable disable
    [NonSerialized] private Task<T> inlinedChild;
    /** 被内联的子节点的重入id */
    [NonSerialized] private int inlinedReentryId = INVALID_REENTRY_ID;
#nullable enable

    /** 获取被内联运行的子节点 */
    public Task<T>? GetInlinedChild() {
        Task<T> r = inlinedChild;
        if (r == null) {
            return null;
        }
        if (r.ReentryId == inlinedReentryId) {
            return r;
        }
        this.inlinedChild = null;
        this.inlinedReentryId = INVALID_REENTRY_ID;
        return null;
    }

    /** 取消内联 */
    public void StopInline() {
        this.inlinedChild = null;
        this.inlinedReentryId = INVALID_REENTRY_ID;
    }

    /** 尝试内联运行中的子节点 */
    public void InlineChild(Task<T> runningChild) {
        if (!runningChild.IsRunning) {
            throw new ArgumentException("runningChild must running");
        }
        if (!TaskInlineHelper.enableInline) {
            this.inlinedChild = null;
            this.inlinedReentryId = INVALID_REENTRY_ID;
            return;
        }
        Task<T>? cur = runningChild;
        while (true) {
            if (!cur.IsInlinable) {
                break; // 不可内联
            }
            if (cur is SingleRunningChildBranch<T> branch) {
                if (branch.RunningChild == null || branch.RunningChild!.IsCompleted) {
                    break;
                }
                cur = branch.GetInlineHelper().GetInlinedChild();
                if (cur != null) { // 分支有成功内联数据
                    break;
                }
                if (branch.GetInlineHelper().inlinedReentryId == FAILED_REENTRY_ID) {
                    cur = branch.RunningChild; // 分支内联子节点失败
                    break;
                }
                cur = branch.RunningChild!;
                continue;
            }
            if (cur is Decorator<T> decorator) {
                if (decorator.Child == null || decorator.Child.IsCompleted) {
                    break;
                }
                cur = decorator.GetInlineHelper().GetInlinedChild();
                if (cur != null) {
                    break;
                }
                if (decorator.GetInlineHelper().inlinedReentryId == FAILED_REENTRY_ID) {
                    cur = decorator.Child;
                    break;
                }
                cur = decorator.Child;
                continue;
            }
            break;
        }
        Debug.Assert(cur.IsRunning);
        if (cur == runningChild) {
            // 无实际内联效果时置为null性能更好
            this.inlinedChild = null;
            this.inlinedReentryId = FAILED_REENTRY_ID;
        } else {
            this.inlinedChild = cur;
            this.inlinedReentryId = cur.ReentryId;
        }
    }
}
}