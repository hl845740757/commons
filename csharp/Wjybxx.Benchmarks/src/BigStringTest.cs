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

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using Wjybxx.Commons;
using Wjybxx.Commons.Pool;
using Wjybxx.Commons.Time;
using Wjybxx.Dson;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Benchmarks;

/// <summary>
/// 测试解析和生成Json字符串的性能（Release|AnyCPU）
/// 我们仍然使用那个540K的文件，读取到内存中保存为String，然后进行解析和生成。
///
/// 这个基准测试并不精确：
/// 1.解码的内存分配这块，Bson有额外的装箱<see cref="BsonDocument"/>，Dson也有额外的装箱<see cref="DsonObject{TK}"/>。
/// 2.输入的json字符串，value基本都是字符串
/// 3.Bson和Dson都是支持二进制流的，解析标准的json不能发挥它们的优势
/// 
/// | Method              | Mean      | Error     | StdDev    | Gen0      | Gen1     | Gen2     | Allocated   |
/// |-------------------- |----------:|----------:|----------:|----------:|---------:|---------:|------------:|
/// | SystemJsonRead      |  3.437 ms | 0.0297 ms | 0.0264 ms |  128.9063 | 128.9063 | 128.9063 |  1580.46 KB |
/// | SystemJsonWrite     |  1.924 ms | 0.0176 ms | 0.0156 ms |   89.8438 |  89.8438 |  89.8438 |  1312.77 KB |
/// | NewtonsoftJsonRead  |  8.996 ms | 0.1685 ms | 0.1493 ms |  984.3750 | 328.1250 |        - |  6121.76 KB |
/// | NewtonsoftJsonWrite |  1.993 ms | 0.0397 ms | 0.0862 ms |  171.8750 | 117.1875 |  58.5938 |  1407.38 KB |
/// | DsonRead            | 13.846 ms | 0.0838 ms | 0.0699 ms |  578.1250 | 281.2500 |        - |  3626.47 KB |
/// | DsonReadBinary      |  4.121 ms | 0.0199 ms | 0.0186 ms |  585.9375 | 289.0625 |        - |  3624.05 KB |
/// | DsonWrite           |  3.804 ms | 0.0475 ms | 0.0397 ms |  160.1563 |  70.3125 |        - |   1005.2 KB |
/// | BsonRead            | 14.465 ms | 0.2883 ms | 0.4315 ms | 2078.1250 | 953.1250 | 109.3750 | 13167.95 KB |
/// | BsonReadBinary      |  7.874 ms | 0.0414 ms | 0.0367 ms |  937.5000 | 406.2500 |        - |   5800.4 KB |
/// | BsonWrite           |  5.808 ms | 0.0293 ms | 0.0274 ms |  398.4375 | 195.3125 |        - |   2476.6 KB |
/// 
/// 不过，测试结果也还是有一定的参考价值；至少可以确定Dson库的内存池化做得很好。
/// 系统库的Json是不是强得有点过分，怎么比Dson解析二进制流还快...
/// Mongo的Bson内存方面做得不好，尤其是解析字符串的时候
/// </summary>
[MemoryDiagnoser()]
[Orderer(SummaryOrderPolicy.Declared, MethodOrderPolicy.Declared)]
public class BigStringTest
{
#nullable disable
    private static readonly string json;
    private static readonly object _sysJsonObject;
    private static readonly object _newtonJsonObject;

    private static readonly DsonValue _dsonObject;
    private static readonly byte[] _dsonBinary;
    private static readonly BsonDocument _bsonDocument;
    private static readonly byte[] _bsonBinary;

    private static readonly JsonSerializerOptions _sysOptions = new JsonSerializerOptions()
    {
        WriteIndented = true
    };
    private static readonly DsonTextWriterSettings _dsonSettings = new DsonTextWriterSettings.Builder
    {
        EnableText = false,
        MaxLengthOfUnquoteString = 16,
    }.Build();
    private static readonly JsonWriterSettings _bsonSettings = new JsonWriterSettings()
    {
        Indent = true
    };
#nullable enable

    /// <summary>
    /// benchmark好像构建了独立的环境，似乎创建了另一个Type，
    /// 导致外部初始化json字段无效...
    /// </summary>
    static BigStringTest() {
        json = File.ReadAllText("D:\\Test.json");
        _sysJsonObject = JsonSerializer.Deserialize<object>(json);
        _newtonJsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
        {
            _dsonObject = Dsons.FromDson(json);

            using var dsonOutput = DsonOutputs.NewInstance(IArrayPool<byte>.Shared, 8192, 1024 * 1024);
            DsonBinaryWriter<string> binaryWriter = new DsonBinaryWriter<string>(DsonWriterSettings.Default, dsonOutput, false);
            Dsons.WriteObject(binaryWriter, _dsonObject.AsObject(), ObjectStyle.Flow);
            _dsonBinary = ArrayUtil.CopyOf(dsonOutput.Buffer, 0, dsonOutput.Position);
        }
        {
            _bsonDocument = BsonSerializer.Deserialize<BsonDocument>(new JsonReader(json));

            MemoryStream memoryStream = new MemoryStream();
            BsonBinaryWriter binaryWriter = new BsonBinaryWriter(new BsonStreamAdapter(memoryStream), BsonBinaryWriterSettings.Defaults);
            BsonSerializer.Serialize(binaryWriter, _bsonDocument);
            _bsonBinary = ArrayUtil.CopyOf(memoryStream.GetBuffer(), 0, (int)memoryStream.Position);
        }
    }

    [Benchmark()]
    public void SystemJsonRead() {
        JsonSerializer.Deserialize<object>(json);
    }

    [Benchmark()]
    public void SystemJsonWrite() {
        JsonSerializer.Serialize(_sysJsonObject, _sysOptions);
    }

    [Benchmark()]
    public void NewtonsoftJsonRead() {
        Newtonsoft.Json.JsonConvert.DeserializeObject(json);
    }

    [Benchmark()]
    public void NewtonsoftJsonWrite() {
        Newtonsoft.Json.JsonConvert.SerializeObject(_newtonJsonObject);
    }

    [Benchmark()]
    public void DsonRead() {
        Dsons.FromDson(json);
    }

    [Benchmark()]
    public void DsonReadBinary() {
        var binaryReader = new DsonBinaryReader<string>(DsonReaderSettings.Default, DsonInputs.NewInstance(_dsonBinary));
        Dsons.ReadTopDsonValue(binaryReader);
    }

    [Benchmark()]
    public void DsonWrite() {
        using DsonTextWriter writer = new DsonTextWriter(_dsonSettings, new StringWriter());
        Dsons.WriteTopDsonValue(writer, _dsonObject);
    }

    [Benchmark]
    public void BsonRead() {
        BsonSerializer.Deserialize<BsonDocument>(new JsonReader(json));
    }

    [Benchmark]
    public void BsonReadBinary() {
        BsonBinaryReader binaryReader = new BsonBinaryReader(new MemoryStream(_bsonBinary));
        BsonSerializer.Deserialize<BsonDocument>(binaryReader);
    }

    [Benchmark]
    public void BsonWrite() {
        using JsonWriter jsonWriter = new JsonWriter(new StringWriter(), _bsonSettings);
        BsonSerializer.Serialize(jsonWriter, _bsonDocument);
    }
}