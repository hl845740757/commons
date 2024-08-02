#region LICENSE

// Copyright 2024 wjybxx(845740757@qq.com)
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

using System.Numerics;
using NUnit.Framework;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

public class CodecTest
{
    private static IDsonConverter converter;

    [SetUp]
    public void SetUp() {
        ITypeMetaRegistry typeMetaRegistry = TypeMetaRegistries.FromMetas(
            TypeMeta.Of(typeof(Vector3), ObjectStyle.Flow, "V3", "Vector3"),
            // List
            TypeMeta.Of(typeof(List<>), ObjectStyle.Indent, "List`1", "List"),
            TypeMeta.Of(typeof(IList<>), ObjectStyle.Indent, "IList`1", "IList"),
            // 字典
            TypeMeta.Of(typeof(Dictionary<,>), ObjectStyle.Indent, "Dictionary`2", "Dictionary"),
            TypeMeta.Of(typeof(IDictionary<,>), ObjectStyle.Indent, "IDictionary`2", "IDictionary")
        );
        IList<IDsonCodec> dsonCodecs = new IDsonCodec[]
        {
            new Vector3Codec()
        }.ToList();

        converter = DefaultDsonConverter.NewInstance(typeMetaRegistry, dsonCodecs,
            GenericCodecConfig.NewDefaultConfig(),
            ConverterOptions.NewBuilder().Build()
        );
    }

    // [Test]
    public void TestNullableInt() {
        // C#特殊处理了Nullable的GetType，返回的是值的类型 -- 和装箱是一样的。。。
        // 因此永远走不到NullableCodec
        int? val = 5;
        string dson = converter.WriteAsDson(val, typeof(int?)); //
        Console.WriteLine(dson);

        int? copied = converter.ReadFromDson<int?>(dson);
        Assert.IsTrue(copied == val);
    }

    [Test]
    public void TestVector3() {
        Vector3 vector3 = new Vector3(1, 1.5f, 1);
        string dson = converter.WriteAsDson(vector3, typeof(Vector3));
        Console.WriteLine(dson);

        Vector3 copied = converter.ReadFromDson<Vector3>(dson);
        Assert.IsTrue(copied == vector3);
    }

    [Test]
    public void TestListVector3() {
        Vector3 vector3 = new Vector3(1, 1.5f, 1);
        List<Vector3> list = new List<Vector3>
        {
            vector3
        };
        string listDson = converter.WriteAsDson(list, typeof(List<Vector3>));
        Console.WriteLine(listDson);

        List<Vector3> copied = converter.ReadFromDson<List<Vector3>>(listDson);
        Assert.IsTrue(copied.SequenceEqual(list));
    }

    [Test]
    public void TestDictionaryVector3() {
        IDictionary<int, Vector3> dictionary = new Dictionary<int, Vector3>();
        for (int i = 1; i <= 5; i++) {
            dictionary[i] = new Vector3(i - 0.5f, i, i + 0.5f);
        }

        string dson = converter.WriteAsDson(dictionary, typeof(object)); // 会写入类型信息
        Console.WriteLine(dson);

        IDictionary<int, Vector3> copied = converter.ReadFromDson<IDictionary<int, Vector3>>(dson, () => new Dictionary<int, Vector3>());
        Assert.IsTrue(CollectionUtil.ContentEquals(copied, dictionary));
    }
}