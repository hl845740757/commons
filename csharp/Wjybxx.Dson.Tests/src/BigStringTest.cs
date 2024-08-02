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

using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using NUnit.Framework;
using Wjybxx.Commons.Time;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 测试解析和生成Json字符串的性能（Release|AnyCPU）
/// 我们仍然使用那个540K的文件，读取到内存中保存为String，然后进行解析和生成。
/// 取稳定值结果如下：
/// <code>
/// StopWatch[System.Json=14ms][Read=9ms,Write=5ms] // 系统库的read好快，可能用的unsafe处理字符串...
/// StopWatch[Newtonsoft.Json=64ms][Read=46ms,Write=17ms]
/// StopWatch[Wjybxx.Dson=35ms][Read=27ms,Write=8ms]
/// StopWatch[MongoDB.Bson=33ms][Read=23ms,Write=10ms]
/// </code>
/// ps：
/// 1. 本机设备信息：I7-9750H 2.6GHz  16G内存
/// 2. 如果是只是简单使用Json，强烈建议使用系统库 -- 不论是读写文件，还是字符串性能都极好。
/// 3. Dson现在还没有支持解码器，所以还不算最终数据。
/// </summary>
public class BigStringTest
{
    [Test]
    public void TestReadWriteString() {
        if (!File.Exists("D:\\Test.json")) {
            return;
        }
        string json = File.ReadAllText("D:\\Test.json");
        // 不涉及资源释放问题，连续运行
        TestSystemJson(json);
        TestNewtonsoftJson(json);
        TestDson(json);
        TestBson(json);
    }

    private void TestSystemJson(string json) {
        StopWatch stopWatch = StopWatch.CreateStarted("System.Json");

        object jsonObject = JsonSerializer.Deserialize<object>(json);
        stopWatch.LogStep("Read");

        JsonSerializer.Serialize(jsonObject,
            new JsonSerializerOptions
            {
                WriteIndented = true,
            });
        stopWatch.Stop("Write");
        Console.WriteLine(stopWatch.GetLog());
    }

    private void TestNewtonsoftJson(string json) {
        StopWatch stopWatch = StopWatch.CreateStarted("Newtonsoft.Json");

        object jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
        stopWatch.LogStep("Read");

        Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject);
        stopWatch.Stop("Write");
        Console.WriteLine(stopWatch.GetLog());
    }

    private void TestDson(string json) {
        StopWatch stopWatch = StopWatch.CreateStarted("Wjybxx.Dson");

        DsonValue dsonValue = Dsons.FromDson(json);
        stopWatch.LogStep("Read");

        DsonTextWriterSettings settings = new DsonTextWriterSettings.Builder
        {
            EnableText = false,
            MaxLengthOfUnquoteString = 16,
        }.Build();

        using DsonTextWriter writer = new DsonTextWriter(settings, new StringWriter());
        Dsons.WriteTopDsonValue(writer, dsonValue);
        stopWatch.Stop("Write");
        Console.WriteLine(stopWatch.GetLog());
    }

    private void TestBson(string json) {
        StopWatch stopWatch = StopWatch.CreateStarted("MongoDB.Bson");

        BsonDocument bsonDocument = BsonSerializer.Deserialize<BsonDocument>(new JsonReader(json));
        stopWatch.LogStep("Read");

        using JsonWriter jsonWriter = new JsonWriter(new StringWriter(), new JsonWriterSettings()
        {
            Indent = true
        });
        BsonSerializer.Serialize(jsonWriter, bsonDocument);
        stopWatch.Stop("Write");
        Console.WriteLine(stopWatch.GetLog());
    }
}