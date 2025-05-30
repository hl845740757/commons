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

using System.Numerics;
using NUnit.Framework;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Tests.Apt;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests.Codec
{
public class MapAsObjectCodecTest
{
    private static IDsonConverter converter;

    [SetUp]
    public void SetUp() {
        IList<TypeMeta> typeMetas = new List<TypeMeta>()
        {
            TypeMeta.Of(typeof(Vector3), ObjectStyle.Flow, "V3", "Vector3"),
        };
        DsonCodecConfig codecConfig = new DsonCodecConfig()
            .AddCodec(new Vector3Codec());

        converter = new DsonConverterBuilder()
            .AddTypeMetas(typeMetas)
            .AddCodecConfig(codecConfig)
            .SetOptions(new ConverterOptions.Builder()
            {
                MapEncodePolicy = MapEncodePolicy.Document
            }.Build())
            .Build();
    }

    [Test]
    public void Test() {
        Dictionary<int, Vector3> myDic = new();
        for (int i = 1; i <= 5; i++) {
            myDic[i] = new Vector3(i - 0.5f, i, i + 0.5f);
        }

        string dson = converter.WriteAsDson(myDic, typeof(object)); // 会写入类型信息
        Console.WriteLine(dson);
        
        Dictionary<int, Vector3> copied = converter.ReadFromDson<Dictionary<int, Vector3>>(dson);
        Assert.IsTrue(CollectionUtil.ContentEquals(copied, myDic));
    }
}
}