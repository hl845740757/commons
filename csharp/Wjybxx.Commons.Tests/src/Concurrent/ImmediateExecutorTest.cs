#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

using System.Threading;
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Concurrent;

public class ImmediateExecutorTest
{
    [Test]
    public void TestExecuteRunsSynchronously() {
        int caller = Thread.CurrentThread.ManagedThreadId;
        int observed = -1;
        bool ran = false;

        ImmediateExecutor.Inst.Execute(() => {
            ran = true;
            observed = Thread.CurrentThread.ManagedThreadId;
        });

        Assert.IsTrue(ran);
        Assert.AreEqual(caller, observed);
    }

    [Test]
    public void TestSingletonIdentity() {
        Assert.AreSame(ImmediateExecutor.Inst, ImmediateExecutor.Inst);
    }

    [Test]
    public void TestSyncContextAndScheduler() {
        Assert.IsNotNull(ImmediateExecutor.Inst.AsSyncContext());
        Assert.IsNotNull(ImmediateExecutor.Inst.AsScheduler());
    }
}
