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

using System.Numerics;
using NUnit.Framework;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Tests.Apt;

namespace Wjybxx.Dson.Tests;

public class ClassNameTest
{
    // 用于输出测试文本
    // [Test]
    public void TestClassType() {
        Print(typeof(Dictionary<int, Dictionary<BeanExample, string>>));

        Console.WriteLine();
        Print(typeof(Dictionary<int, Dictionary<BeanExample, string>>.KeyCollection));

        Console.WriteLine();
        Print(typeof(Dictionary<int, string[]>[]));

        Console.WriteLine();
        Print(typeof(DsonType[]));
    }

    private void Print(Type type) {
        Console.WriteLine("Type: " + type);
        Console.WriteLine("Name: " + type.Name);
        Console.WriteLine("Namespace: " + type.Namespace);
        Console.WriteLine("FullName: " + type.FullName);
    }

    // typeof(Dictionary<int, Dictionary<BeanExample, string>>)
    private const string GenericTypeName = """
    System.Collections.Generic.Dictionary`2
    [   
        System.Int32,
        System.Collections.Generic.Dictionary`2
        [
            Wjybxx.Dson.Tests.BeanExample,
            System.String
        ]
    ]
    """;

    // typeof(Dictionary<int, string[]>[])
    private const string GenericTypeName2 = """
    System.Collections.Generic.Dictionary`2
    [
        System.Int32,
        System.String[]
    ][]
    """;

    /** 测试缩写名称 */
    private static readonly string GenericTypeName3 = "List`1[s]";

    [Test]
    public void ParseTypeNameTest() {
        ParseTest(GenericTypeName);
        ParseTest(GenericTypeName2);
        ParseTest(GenericTypeName3);
    }

    private static void ParseTest(string dsonClassName) {
        ClassName className = ClassName.Parse(dsonClassName);
        string formatted = className.ToString();
        Console.WriteLine(formatted);

        ClassName cloned = ClassName.Parse(formatted); // 解析格式化导出的文本
        Assert.That(cloned, Is.EqualTo(className));
    }
}