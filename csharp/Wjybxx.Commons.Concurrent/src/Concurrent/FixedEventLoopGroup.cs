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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wjybxx.Commons.Collections;

namespace Wjybxx.Commons.Concurrent
{
/// <summary>
/// 默认的固定Child的事件循环组
/// </summary>
public class FixedEventLoopGroup : AbstractEventLoopGroup, IFixedEventLoopGroup
{
    private readonly IPromise<int> terminationPromise = new Promise<int>();
    private readonly IEventLoop[] children;
    private readonly IList<IEventLoop> readonlyChildren;
    private readonly IEventLoopChooser chooser;
    private volatile int terminatedChildren;

    public FixedEventLoopGroup(EventLoopGroupBuilder builder) {
        int numChildren = builder.NumChildren;
        if (numChildren < 1) {
            throw new ArgumentException("childCount must greater than 0");
        }
        IEventLoopFactory eventLoopFactory = builder.EventLoopFactory ?? throw new ArgumentException("eventLoopFactory");
        EventLoopChooserFactory chooserFactory = builder.ChooserFactory ?? new EventLoopChooserFactory();

        children = new IEventLoop[numChildren];
        for (int i = 0; i < numChildren; i++) {
            IEventLoop eventLoop = eventLoopFactory.NewChild(this, i);
            if (eventLoop.Parent != this) throw new StateException("the parent of child is illegal");
            children[i] = eventLoop;
        }
        readonlyChildren = ImmutableList<IEventLoop>.CreateRange(children);
        chooser = chooserFactory.NewChooser(children);

        // 监听关闭信号
        foreach (IFuture future in children.Select(e => e.TerminationFuture)) {
            future.OnCompleted(OnChildTerminated);
        }
    }

    /** 子节点关闭回调 */
    private void OnChildTerminated(IFuture future) {
        if (Interlocked.Increment(ref terminatedChildren) == children.Length) {
            terminationPromise.TrySetResult(0);
        }
    }

    public int ChildCount => children.Length;


    public override IEventLoop Select() {
        return chooser.Select();
    }

    public IEventLoop Select(int key) {
        return chooser.Select(key);
    }

    public override void Shutdown() {
        foreach (IEventLoop eventLoop in children) {
            eventLoop.Shutdown();
        }
    }

    public override List<ITask> ShutdownNow() {
        List<ITask> tasks = new List<ITask>();
        foreach (IEventLoop eventLoop in children) {
            tasks.AddRange(eventLoop.ShutdownNow());
        }
        return tasks;
    }

    public override bool IsShuttingDown => children.All(e => e.IsShuttingDown);
    public override bool IsShutdown => children.All(e => e.IsShutdown);
    public override bool IsTerminated => terminationPromise.IsCompleted;
    public override IFuture TerminationFuture => terminationPromise.AsReadonly();

    public override IEnumerator<IEventLoop> GetEnumerator() {
        return readonlyChildren.GetEnumerator();
    }
}
}