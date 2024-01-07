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
using Wjybxx.Commons;

namespace Commons.Tests;

public class DateTimeTest
{
    [Test]
    public void UnixTimeTest() {
        long epochMillis = DatetimeUtil.CurrentEpochMillis();
        {
            DateTime dateTime = DatetimeUtil.ToDateTime(epochMillis);
            long epochMillis2 = DatetimeUtil.ToEpochMillis(dateTime);
            Assert.That(epochMillis2, Is.EqualTo(epochMillis));
        }
        {
            int hour = 8;
            int minutes = 30;
            TimeOnly timeOnly = new TimeOnly(hour, minutes, 00);

            int secondOfDay = DatetimeUtil.ToSecondOfDay(in timeOnly);
            Assert.That(secondOfDay, Is.EqualTo(hour * 3600 + minutes * 60));

            long millisOfDay = DatetimeUtil.ToMillisOfDay(in timeOnly);
            Assert.That(millisOfDay, Is.EqualTo(secondOfDay * 1000));
        }
    }

    [Test]
    public void TestFormat() {
        DateOnly dateOnly = new DateOnly(2024, 1, 7);
        TimeOnly timeOnly = new TimeOnly(14, 35, 0);
        DateTime dateTime = dateOnly.ToDateTime(timeOnly);

        // 测试要测试下午，以避免PM和AM这些坑 -- 要测试24小时制
        string dateString = "2024-01-07";
        string timeString = "14:35:00";

        Assert.That(DatetimeUtil.FormatDate(dateOnly), Is.EqualTo(dateString));
        Assert.That(DatetimeUtil.FormatTime(timeOnly), Is.EqualTo(timeString));
        Assert.That(DatetimeUtil.FormatDateTime(dateTime), Is.EqualTo(dateString + "T" + timeString));
    }

    [Test]
    public void TestOffsetParse() {
        int hour = 8;
        int seconds = 8 * 3600;

        Assert.That(DatetimeUtil.ParseOffset("+8"), Is.EqualTo(seconds));
        Assert.That(DatetimeUtil.ParseOffset("+08"), Is.EqualTo(seconds));
        Assert.That(DatetimeUtil.ParseOffset("+8:00"), Is.EqualTo(seconds));
        Assert.That(DatetimeUtil.ParseOffset("+08:00"), Is.EqualTo(seconds));
        Assert.That(DatetimeUtil.ParseOffset("+08:00:00"), Is.EqualTo(seconds));
    }
}