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

namespace Wjybxx.Dson.Tests;

public class GenericTypeTest
{
    /** 测试能否拿到IDictionary接口 */
    [Test]
    public void TestGetInterface() {
        // 参数为 Type.Name 格式，泛型需要使用反引号分割
        Type type = typeof(LinkedDictionary<,>).GetInterface("IDictionary`2");
        Assert.IsTrue(type != null);

        type = typeof(LinkedDictionary<string, int>).GetInterface("IDictionary`2");
        Assert.IsTrue(type != null);
    }

    [Test]
    public void TestGenericArray() {
        Type type = typeof(List<string>[]);
        Console.WriteLine("type: " + type);
        Console.WriteLine("IsArray: " + type.IsArray);
        Console.WriteLine("IsGeneric: " + type.IsGenericType);
    }

    [Test]
    public void TestValueType() {
        Console.WriteLine("---------TestValueType------------");
        Console.WriteLine(typeof(Vector3));

        object obj = default(Vector3);
        Console.WriteLine(obj.GetType());

        // 装箱以后，GetType返回的仍然是装箱之前的类型！
        Assert.That(obj.GetType(), Is.EqualTo(typeof(Vector3)));
    }

    private enum MyEnum
    {
        Unknown = 0,
        Male = 1,
        Female = 2,
    }

    [Test]
    public void TestEnum() {
        MyEnum val = MyEnum.Male;
        Assert.That(val.GetHashCode(), Is.EqualTo(1));
        Assert.That(val.ToString(), Is.EqualTo("Male"));
    }

    [Test]
    public void TestIsCollection() {
        Assert.IsTrue(DsonConverterUtils.IsCollection(typeof(List<>)));
        Assert.IsTrue(DsonConverterUtils.IsCollection(typeof(List<string>)));
    }
}