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
using System.Threading.Tasks;
using NUnit.Framework;
using Wjybxx.Commons.Concurrent;

namespace Commons.Tests.Concurrent;

/// <summary>
/// 
/// </summary>
public class ExceptionDispatchTest
{
    private static readonly BetterCancellationException singletonEx = new BetterCancellationException(1);

    /// <summary>
    /// 测试单例异常在多个线程下抛出的堆栈
    /// </summary>
    [Test]
    public async Task TestSingletonException() {
        try {
            throw singletonEx;
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }
        try {
            await AsyncThrow(singletonEx);
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }
    }

    private async Task AsyncThrow(Exception ex) {
        await Task.Delay(1000);
        throw ex;
    }
}