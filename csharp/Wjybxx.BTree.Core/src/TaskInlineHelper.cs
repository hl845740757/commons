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
using System.Runtime.CompilerServices;
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
/// 内联工具类
/// 1.只有不能被内联的节点，才需要该工具类。
/// 2.实现内联优化时，应当在<see cref="Task{T}.OnChildRunning"/>时开启内联和<see cref="Task{T}.OnChildCompleted"/>时停止内联。
/// 3.在<see cref="Task{T}.Exit"/>时也调用一次停止内联可避免内存泄漏(不必要的引用)。
/// 4.在<see cref="Task{T}.OnEventImpl"/>时应当尝试将事件转发给被内联的子节点，可使用工具方法<see cref="OnEvent"/>。
///
/// ps：<see cref="TaskEntry{T}"/>就是标准实现。
/// </summary>
/// <typeparam name="T"></typeparam>
public struct TaskInlineHelper<T> where T : class
{
    /** 无效重入id */
    private const int INVALID_REENTRY_ID = int.MinValue;
    /** 表示内联失败 */
    private const int FAILED_REENTRY_ID = INVALID_REENTRY_ID + 1;

#nullable disable
    [NonSerialized] private Task<T> inlinedChild;
    /** 被内联的子节点的重入id */
    [NonSerialized] private int inlinedReentryId;
#nullable enable

    /** 获取被内联运行的子节点 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        while (cur.IsInlinable) {
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

    /** 转发事件的工具方法 -- 编写代码时使用该方法，编写完毕后点重构内联(保留该方法) */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnEvent(object eventObj, Task<T>? source) {
        Task<T>? inlinedChild = GetInlinedChild();
        if (inlinedChild != null) {
            inlinedChild.OnEvent(eventObj);
        } else if (source != null) {
            source.OnEvent(eventObj);
        }
    }
}
}