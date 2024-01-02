#region LICENSE
// Copyright 2023 wjybxx(845740757@qq.com)
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
using Wjybxx.Commons;

namespace Commons.Tests;

public class DateTimeTest
{

    [Test]
    public void UnixTimeTest() {
        long epochMillis = DatetimeUtil.CurrentEpochMillis();
        Console.WriteLine(DatetimeUtil.ToLocalDateTime(epochMillis, TimeSpan.FromHours(8)).ToString("s"));
        Console.WriteLine(TimeOnly.FromTimeSpan(DatetimeUtil.ZoneOffsetSystem).ToString("HH:mm:ss"));
    }
}