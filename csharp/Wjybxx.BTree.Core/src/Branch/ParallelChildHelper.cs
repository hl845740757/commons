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

using System.Runtime.CompilerServices;

namespace Wjybxx.BTree.Branch
{
/// <summary>
/// Q：为什么不直接叫{@code ChildHelper}？
/// A: 通常只应该在有多个运行中的子节点(含hook)的情况下才需要使用该工具类。
/// </summary>
/// <typeparam name="T"></typeparam>
public class ParallelChildHelper<T> where T : class
{
    private TaskInlineHelper<T> _inlineHelper = new TaskInlineHelper<T>();
#nullable disable
    /** 子节点的重入id */
    public int reentryId;
    /** 子节点的取消令牌 -- 应当在运行前赋值 */
    public CancelToken cancelToken;
    /** 子节点控制数据 -- 自行规划 */
    public int ctl;
    /** 用户自定义数据 */
    public object userData;

    public virtual void Reset() {
        _inlineHelper.StopInline();
        reentryId = 0;
        ctl = 0;
        userData = null;
        if (cancelToken != null) {
            cancelToken.Reset();
        }
    }

    public ref TaskInlineHelper<T> Unwrap() => ref _inlineHelper;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<T> GetInlinedChild() {
        return _inlineHelper.GetInlinedChild();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void StopInline() {
        _inlineHelper.StopInline();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InlineChild(Task<T> runningChild) {
        _inlineHelper.InlineChild(runningChild);
    }
}
}