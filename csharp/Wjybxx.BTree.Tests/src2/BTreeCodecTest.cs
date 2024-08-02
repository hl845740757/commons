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

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wjybxx.BTreeCodec;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Text;

namespace Wjybxx.BTree.Codec;

/// <summary>
/// 测试DsonCodex
/// </summary>
public class BTreeCodecTest
{
    private static string dsonString = """

    {@{AlwaysSuccess[string]}
        child: {@{SimpleRandom[string]}
            p: 0.5
        }
    }
    """;

    private static DefaultDsonConverter converter;

    [SetUp]
    public void SetUp() {
        GenericCodecConfig genericCodecConfig = GenericCodecConfig.NewDefaultConfig();
        Dictionary<Type, Type> rawType2CodecTypeDic = BTreeCodecExporter.ExportCodecs();
        foreach (KeyValuePair<Type, Type> pair in rawType2CodecTypeDic) {
            genericCodecConfig.AddCodec(pair.Key, pair.Value);
        }

        List<TypeMeta> typeMetas = rawType2CodecTypeDic.Keys
            .Select(e => TypeMeta.Of(e, ObjectStyle.Indent, RemoveGenericInfo(e.Name)))
            .ToList();
        // 加入集合的类型数据
        typeMetas.AddRange(new List<TypeMeta>
        {
            // List
            TypeMeta.Of(typeof(List<>), ObjectStyle.Indent, "List", "List`1"),
            TypeMeta.Of(typeof(IList<>), ObjectStyle.Indent, "IList", "IList`1"),
            // 字典
            TypeMeta.Of(typeof(Dictionary<,>), ObjectStyle.Indent, "Dictionary", "Dictionary`2"),
            TypeMeta.Of(typeof(IDictionary<,>), ObjectStyle.Indent, "IDictionary", "IDictionary`2")
        });

        List<IDsonCodec> pojoCodecList = new List<IDsonCodec>();
        converter = DefaultDsonConverter.NewInstance(TypeMetaRegistries.FromMetas(typeMetas),
            pojoCodecList,
            genericCodecConfig,
            ConverterOptions.NewBuilder().Build());
    }

    private static string RemoveGenericInfo(string clsName) {
        int index = clsName.IndexOf('`');
        return index > 0 ? clsName.Substring(0, index) : clsName;
    }

    /// <summary>
    /// 准备代码稍微有点长
    /// </summary>
    [Test]
    public void DeserializeTest() {
        Task<string> task = converter.ReadFromDson<Task<string>>(dsonString);
        Console.WriteLine(task);
    }
}