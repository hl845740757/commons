#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System.Diagnostics;
using NUnit.Framework;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 测试三种Reader/Writer实现之间的等效性
/// </summary>
public class DsonTextReaderTest
{
    // c#10还不支持 """，因此转为@格式
    internal const string DsonString = """
            @{clsName: FileHeader, intro: 预留设计，允许定义文件头}
            {@{MyStruct}
              name: wjybxx,
              age: 28,
              介绍: "这是一段中文而且非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常非常长",
              intro: "hello world",
              ptr1: @ptr 10001,
              ptr2: {@ptr coll: 16148b3b4e7b8923d398, localId: 10001},
              bin: @bin "35DF2E75E6A4BE9E6F4571C64CB6D08B0D6BC46C1754F6E9EB4A6E57E2FD53",
              bin2: @bin ""
            },
            {@{MyStruct}
              name: wjybxx,
              intro: "hello world",
              ptr1: @ptr 10001,
              ptr2: {@ptr coll: 16148b3b4e7b8923d398, localId: 10001},
              ptr: @ptr 0,
              intDecimal: @i 10001,
              intHex: @i 0xFF,
              intBinary: @i 0b10010001,
              intUnderscore: @i 100_000_000,
              intFlags: @i "0x01 | 0x02 | 0x10 | 0x20",
              longValue: @L 987654321,
              floatValue: @f 1.05,
              fx4Value: @fx -0.5,
              boolTrue: @b true,
              boolNumeric: @b 1,
              nullValue: @N null,
              timestampSimple: @ts 1715659200,
              timestampObject: {@ts seconds: 1715659200, millis: 100},
              dateTimeSimple: @dt 2023-06-17T18:37:00,
              dateTimeObject: {@dt date: 2023-06-17, time: 18:37:00, offset: +08:00, millis: 100},
              dq1: {@D4 x: 1, y: 1, z: 1, w: 0},
              dq2: {@D4 x: 1, y: 1, z: 1, w: 1},
              fv1: {@FX4 x: 1.2345, y: -1.2345, z: 0.0001, w: 0},
              fv2: {@FX4 x: 1.2345, y: -1.2345, z: 0.0001, w: 0}
            },
            [@{localId: 10001}
              @bin "FFFA",
              @bin ""
            ],
            [@{localId: 10001}]
            """;

    [SetUp]
    public void Setup() {
    }

    /// <summary>
    /// 程序生成的无法保证和手写的文本相同
    /// 但程序反复读写，以及不同方式之间的读写结果应当相同。
    /// </summary>
    [Test]
    public static void test_equivalenceOfAllReaders() {
        DsonArray<string> collection1;
        using (IDsonReader<string> reader = new DsonTextReader(DsonTextReaderSettings.Default, DsonString)) {
            collection1 = Dsons.ReadCollection(reader);
        }
        string dsonString1 = collection1.ToFlatDson();
        // Console.WriteLine(dsonString1);
        // Assert.That(dsonString1, Is.EqualTo(DsonString)); // 注意：可能受到平台换行符的影响

        // Binary
        {
            byte[] buffer = new byte[8192];
            IDsonOutput output = DsonOutputs.NewInstance(buffer);
            using (IDsonWriter<string> writer = new DsonBinaryWriter<string>(DsonTextWriterSettings.Default, output)) {
                Dsons.WriteCollection(writer, collection1);
            }
            IDsonInput input = DsonInputs.NewInstance(buffer, 0, output.Position);
            using (IDsonReader<string> reader = new DsonBinaryReader<string>(DsonTextReaderSettings.Default, input)) {
                DsonArray<string> collection2 = Dsons.ReadCollection(reader);
                Assert.That(collection2, Is.EqualTo(collection1));

                string dsonString2 = collection2.ToFlatDson();
                Assert.IsTrue(dsonString1 == dsonString2, "BinaryReader/BinaryWriter");
            }
        }

        // Object
        {
            DsonArray<string> outList = new DsonArray<string>();
            using (IDsonWriter<string> writer = new DsonCollectionWriter<string>(DsonTextWriterSettings.Default, outList)) {
                Dsons.WriteCollection(writer, collection1);
            }
            using (IDsonReader<string> reader = new DsonCollectionReader<string>(DsonTextReaderSettings.Default, outList)) {
                DsonArray<string> collection3 = Dsons.ReadCollection(reader);
                Assert.That(collection3, Is.EqualTo(collection1));

                string dsonString3 = collection3.ToFlatDson();
                Assert.IsTrue(dsonString1 == dsonString3, "ObjectReader/ObjectWriter");
            }
        }
        // Text
        {
            StringWriter stringWriter = new StringWriter();
            using (IDsonWriter<string> writer = new DsonTextWriter(DsonTextWriterSettings.Default, stringWriter)) {
                Dsons.WriteCollection(writer, collection1);
            }
            using (IDsonReader<string> reader = new DsonTextReader(DsonTextReaderSettings.Default, stringWriter.ToString())) {
                DsonArray<string> collection4 = Dsons.ReadCollection(reader);
                Assert.That(collection4, Is.EqualTo(collection1));

                string dsonString4 = collection4.ToFlatDson();
                Assert.IsTrue(dsonString1 == dsonString4, "TextReader/TextWriter");
            }
        }
    }

    [Test]
    public static void test_longStringCodec() {
        byte[] data = new byte[5200]; // 超过8192
        Random.Shared.NextBytes(data);
        string hexString = Convert.ToHexString(data);

        byte[] buffer = new byte[16 * 1024];
        IDsonOutput output = DsonOutputs.NewInstance(buffer);
        output.WriteString(hexString);


        IDsonInput input = DsonInputs.NewInstance(buffer, 0, output.Position);
        string string2 = input.ReadString();
        Debug.Assert(hexString == string2);
    }
}