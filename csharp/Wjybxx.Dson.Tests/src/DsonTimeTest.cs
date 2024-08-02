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

using NUnit.Framework;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Tests;

public class DsonTimeTest
{
    private const string DsonDateTimeString = """
            [
              @dt 2023-06-17T18:37:00,
              {@dt date: 2023-06-17, time: 18:37:00},
              {@dt date: 2023-06-17, time: 18:37:00, offset: +8},
              {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, millis: 100},
              {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, nanos: 100_000_000}
            ]
            """;

    [Test]
    public void TestDateTime() {
        DsonArray<String> dsonArray = Dsons.FromDson(DsonDateTimeString).AsArray();
        Assert.That(dsonArray[1], Is.EqualTo(dsonArray[0]));

        ExtDateTime time3 = dsonArray[2].AsDateTime();
        ExtDateTime time4 = dsonArray[3].AsDateTime();
        ExtDateTime time5 = dsonArray[4].AsDateTime();
        // 偏移相同
        Assert.That(time4.Offset, Is.EqualTo(time3.Offset));
        Assert.That(time5.Offset, Is.EqualTo(time3.Offset));
        // 纳秒部分相同
        Assert.That(time5.Nanos, Is.EqualTo(time4.Nanos));

        // 测试编解码
        string dsonString2 = Dsons.ToDson(dsonArray);
        Console.WriteLine(dsonString2);
        DsonArray<string> dsonArray2 = Dsons.FromDson(dsonString2).AsArray();
        Assert.That(dsonArray2, Is.EqualTo(dsonArray));
    }

    private const string DsonTimestampString = """
            [
              @ts 1715659200,
              @ts 1715659200100ms,
              {@ts seconds: 1715659200, millis: 100},
              {@ts seconds: 1715659200, nanos: 100_000_000}
            ]
            """;

    [Test]
    public void TestTimestamp() {
        DsonArray<String> dsonArray = Dsons.FromDson(DsonTimestampString).AsArray();
        Timestamp time1 = dsonArray[0].AsTimestamp();
        Timestamp time2 = dsonArray[1].AsTimestamp();
        Timestamp time3 = dsonArray[2].AsTimestamp();
        Timestamp time4 = dsonArray[3].AsTimestamp();
        // 秒相同
        Assert.That(time1.Seconds, Is.EqualTo(time2.Seconds));
        Assert.That(time1.Seconds, Is.EqualTo(time3.Seconds));
        Assert.That(time1.Seconds, Is.EqualTo(time4.Seconds));
        // 纳秒相同
        Assert.That(time2.Nanos, Is.EqualTo(time3.Nanos));
        Assert.That(time2.Nanos, Is.EqualTo(time4.Nanos));

        // 测试编解码
        string dsonString2 = Dsons.ToDson(dsonArray);
        Console.WriteLine(dsonString2);
        DsonArray<string> dsonArray2 = Dsons.FromDson(dsonString2).AsArray();
        Assert.That(dsonArray2, Is.EqualTo(dsonArray));
    }
}