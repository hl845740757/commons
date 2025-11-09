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
using Wjybxx.BTree.Leaf;
using Wjybxx.BTreeCodec;
using Wjybxx.Dson;
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

    private static string dsonString2 = """
                                        {@{clsName: "AlwaysSuccess[string]", localId: 1}
                                            child: @ptr 2
                                        }
                                        {@{clsName: "SimpleRandom[string]", localId: 2}
                                            p: 0.5
                                        }
                                        {@{clsName: "AlwaysFail[string]", localId: 3}
                                            child: @ptr 2
                                        }
                                        """;

    private static IDsonConverter converter;

    [SetUp]
    public void SetUp() {
        DsonConverterBuilder builder = new DsonConverterBuilder();
        // 反射查找所有的Codec
        List<Type> codecTypes = typeof(BtreeCodecLinker).Assembly.GetTypes()
            .Where(e => e.GetInterface("Wjybxx.Dson.Codec.IDsonCodec`1") != null)
            .ToList();
        foreach (Type codecType in codecTypes) {
            // 传递给AbstractCodec的才是EncoderType
            Type encoderType = codecType.BaseType!.GenericTypeArguments[0].GetGenericTypeDefinition();
            builder.AddGenericCodec(encoderType, codecType);

            TypeMeta typeMeta = TypeMeta.Of(encoderType, RemoveGenericInfo(encoderType.Name));
            builder.AddTypeMeta(typeMeta);
        }
        converter = builder.Build();
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
        SimpleRandom<string> simpleRandom = task.GetChild(0) as SimpleRandom<string>;
        Assert.NotNull(simpleRandom);
        Assert.AreEqual(0.5, simpleRandom.P);
    }

    [Test]
    public void ReadCollectionTest() {
        List<Task<string>> list = converter.ReadFromDsonCollectionString<Task<string>>(dsonString2);
        SimpleRandom<string> simpleRandom = list[1] as SimpleRandom<string>;
        Assert.NotNull(simpleRandom);
        Assert.AreEqual(0.5, simpleRandom.P);
        //
        Task<string> decorator1 = list[0];
        Task<string> decorator2 = list[2];
        Assert.AreSame(decorator1.GetChild(0), decorator2.GetChild(0));
    }

    [Test]
    public void SerializeTest() {
        List<Task<string>> list = converter.ReadFromDsonCollectionString<Task<string>>(dsonString2);
        string collectionString = converter.WriteAsDsonCollectionString(list, typeof(Task<string>));
        Console.WriteLine(collectionString);
    }
}