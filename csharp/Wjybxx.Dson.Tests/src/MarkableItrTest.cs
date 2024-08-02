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
using Wjybxx.Dson.Ext;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

public class MarkableItrTest
{
    static DsonObject<string> GenRandObject() {
        DsonObject<string> obj1 = new DsonObject<string>(64);
        obj1.Append("name", new DsonString("wjybxx"))
            .Append("age", new DsonInt32(Random.Shared.Next(28, 32)))
            .Append("url", new DsonString("http://www.wjybxx.cn"))
            .Append("time", new DsonInt64(DatetimeUtil.ToEpochMillis(DateTime.UtcNow) + Random.Shared.NextInt64(1, 1000)));

        DsonTextReader textReader = new DsonTextReader(DsonTextReaderSettings.Default, ReaderTest.DsonString);
        DsonArray<string> collection = Dsons.ReadCollection(textReader);
        obj1.Append("wrapped1", collection);
        obj1.Append("wrapped2", Dsons.FromDson(FormatTest.DsonString));

        // 测试基础数字
        for (int j = 0; j < 8; j++) {
            obj1.Put("iv" + j, new DsonInt32(Random.Shared.Next()));
            obj1.Put("lv" + j, new DsonInt64(Random.Shared.NextInt64()));
            obj1.Put("fv" + j, new DsonFloat(Random.Shared.NextSingle()));
            obj1.Put("dv" + j, new DsonDouble(Random.Shared.NextDouble()));
        }
        return obj1;
    }

    [Test]
    public void Test() {
        DsonObject<string> dsonObject = GenRandObject();
        MarkableIterator<DsonValue> iterator = new MarkableIterator<DsonValue>(dsonObject.Values.GetEnumerator());

        while (iterator.HasNext()) {
            if (Random.Shared.Next(2) == 1) {
                iterator.Mark();
                DsonValue markedNext = iterator.Next();

                int skip = Random.Shared.Next(1, 10);
                int c = 0;
                while (iterator.HasNext() && c < skip) {
                    iterator.Next();
                    c++;
                }
                iterator.Reset();

                DsonValue realNext = iterator.Next();
                Assert.That(realNext, Is.SameAs(markedNext));
            } else {
                iterator.Next();
            }
        }
    }
}