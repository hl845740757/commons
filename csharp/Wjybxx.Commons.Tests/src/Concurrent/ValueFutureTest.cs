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

using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Wjybxx.Commons;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Concurrent;

public class ValueFutureTest
{
    [Test]
    public async Task TestRun() {
        ValueFuture future1 = asyncVoid();
        ValueFuture<int> future2 = asyncInt();
        await future1;
        await future2;
        
        // 重复await将抛出异常
        Assert.CatchAsync<IllegalStateException>(async () => await future1);
        Assert.CatchAsync<IllegalStateException>(async () => await future2);

        await asyncVoid();
        await asyncInt();

        await asyncVoid();
        await asyncInt();
    }

    private static async ValueFuture asyncVoid() {
        await GlobalEventLoop.Inst;
        Thread.Sleep(1);
    }

    private static async ValueFuture<int> asyncInt() {
        await GlobalEventLoop.Inst;
        return await ValueFutureTask.Call(GlobalEventLoop.Inst, () => 1);
    }
}