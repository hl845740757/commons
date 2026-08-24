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

using NUnit.Framework;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Concurrent;

public class AsyncStateMachinePoolTest
{
    [Test]
    public void Test() {
        int expected = 100;
        TaskPoolConfig.AddPoolConfig<int>(TaskPoolType.ValuePromise, expected);
        int poolSize = TaskPoolConfig.GetPoolSize<int>(TaskPoolType.ValuePromise);
        Assert.AreEqual(expected, poolSize);
        //
        AsyncMethod().Forget();
        AsyncMethod("").Forget();
        GenericAsyncMethod1("hello").Forget();
        GenericAsyncMethod2("world").Forget();
    }

    // 测试对象池是否生效
    [TaskPoolSize(100)]
    private static async ValueFuture AsyncMethod() {
        await GlobalEventLoop.Inst.ScheduleAction(() => { }, TimeSpan.FromMilliseconds(10));
    }

    // 测试重载
    [TaskPoolSize]
    private static async ValueFuture AsyncMethod(string input) {
        await GlobalEventLoop.Inst.ScheduleAction(() => { }, TimeSpan.FromMilliseconds(10));
    }

    // 测试泛型方法
    [TaskPoolSize(50)]
    private static async ValueFuture GenericAsyncMethod1<T>(T input) {
        await GlobalEventLoop.Inst.ScheduleAction(() => { }, TimeSpan.FromMilliseconds(10));
    }

    // 测试泛型方法
    [TaskPoolSize(50)]
    private static async ValueFuture<T> GenericAsyncMethod2<T>(T input) {
        await GlobalEventLoop.Inst.ScheduleAction(() => { }, TimeSpan.FromMilliseconds(10));
        return input;
    }
}