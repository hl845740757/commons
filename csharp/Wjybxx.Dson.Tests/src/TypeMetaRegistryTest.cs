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
using Wjybxx.Dson.Codec;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 测试TypeMeta动态化是否正确
/// </summary>
public class TypeMetaRegistryTest
{
    private static DynamicTypeMetaRegistry registry;

    [SetUp]
    public void SetUp() {
        ITypeMetaRegistry basicRegistry = TypeMetaRegistries.FromMetas(
            TypeMeta.Of(typeof(Dictionary<,>), ObjectStyle.Indent, "Dictionary", "Dictionary`2"),
            TypeMeta.Of(typeof(List<>), ObjectStyle.Indent, "List", "List`1")
        );
        basicRegistry = TypeMetaRegistries.FromRegistries(DsonConverterUtils.GetDefaultTypeMetaRegistry(), basicRegistry);
        registry = new DynamicTypeMetaRegistry(basicRegistry);
    }

    [Test]
    public void TestGeneric() {
        Type type = typeof(List<string>);
        TypeMeta typeMeta = registry.OfType(type);
        Assert.NotNull(typeMeta);

        TypeMeta typeMeta2 = registry.OfName("List[s]");
        Assert.That(typeMeta2, Is.SameAs(typeMeta));

        // 它俩无法简单解析到同一个TypeMeta？
        // 确实无法，在类型支持别名的情况下，除非我们注册所有的组合情况 -- 实际上没有必要，指向不同的TypeMeta不影响正确性
        TypeMeta typeMeta3 = registry.OfName("List`1[s]");
        // Assert.That(typeMeta3, Is.SameAs(typeMeta));
        Assert.NotNull(typeMeta3);
        Assert.That(typeMeta3.type, Is.SameAs(type));

        // 最终会指向同一个TypeMeta -- 我们实现了动态合并
        typeMeta = registry.OfType(type);
        typeMeta2 = registry.OfName("List[s]");
        typeMeta3 = registry.OfName("List`1[s]");
        Assert.That(typeMeta2, Is.SameAs(typeMeta));
        Assert.That(typeMeta3, Is.SameAs(typeMeta));
    }

    [Test]
    public void TestArray() {
        Type type = typeof(string[]);
        TypeMeta typeMeta = registry.OfType(type);
        Assert.NotNull(typeMeta);

        TypeMeta typeMeta2 = registry.OfName("s[]");
        Assert.That(typeMeta2, Is.SameAs(typeMeta));
    }

    /** 先通过Type查找TypeMeta */
    [Test]
    public void TestGenericArray() {
        Type type = typeof(List<List<string>>[]);
        TypeMeta typeMeta = registry.OfType(type);
        Assert.NotNull(typeMeta);

        string clsName = "List[List[s]][]";
        Assert.That(typeMeta.MainClsName, Is.EqualTo(clsName));

        TypeMeta typeMeta2 = registry.OfName(clsName);
        Assert.That(typeMeta2, Is.SameAs(typeMeta));
    }

    /** 先通过clsName查找TypeMeta */
    [Test]
    public void TestGenericArray2() {
        string clsName = "List[List[i]][]";
        TypeMeta typeMeta = registry.OfName(clsName);
        Assert.NotNull(typeMeta);
        Assert.That(typeMeta.MainClsName, Is.EqualTo(clsName));

        Type type = typeof(List<List<int>>[]);
        TypeMeta typeMeta2 = registry.OfType(type);

        Assert.That(typeMeta2, Is.SameAs(typeMeta));
    }
}