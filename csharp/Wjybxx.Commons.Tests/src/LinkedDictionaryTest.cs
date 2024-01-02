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

using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;
using Wjybxx.Commons.Collections;

namespace Commons.Tests;

public class LinkedDictionaryTest
{
    [Test]
    [Repeat(5)]
    public void TestIntDic() {
        int expectedCount = 10000;
        HashSet<int> keySet = new HashSet<int>(expectedCount);
        List<int> keyList = new List<int>(expectedCount);
        LinkedDictionary<int, string> dictionary = new LinkedDictionary<int, string>(expectedCount / 3); // 顺便测试扩容

        // 在插入期间随机删除已存在的key；不宜太频繁，否则keyList的移动开销太大
        while (keySet.Count < expectedCount) {
            if (Random.Shared.Next(0, 10) == 1 && keyList.Count > expectedCount / 3) {
                int idx = Random.Shared.Next(0, keyList.Count);
                int key = keyList[idx];
                keyList.RemoveAt(idx);
                keySet.Remove(key);
                dictionary.Remove(key, out _);
                continue;
            }
            var next = Random.Shared.Next();
            if (keySet.Add(next)) {
                keyList.Add(next);
                dictionary[next] = next.ToString();
            }
        }
        Assert.That(keySet.Count, Is.EqualTo(keyList.Count));
        Assert.That(dictionary.Count, Is.EqualTo(keyList.Count));

        int index = 0;
        foreach (KeyValuePair<int, string> pair in dictionary) {
            int expectedKey = keyList[index++];
            int realKey = pair.Key;
            Assert.That(realKey, Is.EqualTo(expectedKey));
        }
    }

    [Test]
    [Repeat(5)]
    public void TestStringDic1() {
        TestStringDic(10000);
    }

    [Test]
    [Repeat(5)]
    public void TestStringDic2() {
        TestStringDic(100000);
    }

    private static LinkedDictionary<string, string> TestStringDic(int expectedCount) {
        LinkedDictionary<string, string> dictionary = new LinkedDictionary<string, string>(expectedCount / 3); // 顺便测试扩容
        dictionary.DefaultValue = "wjybxx";
        // 测试默认值
        {
            dictionary.TryGetValue("abc", out string value);
            Assert.That(value, Is.EqualTo(dictionary.DefaultValue));
        }

        byte[] buffer = new byte[12];
        List<string> keyList = new List<string>(expectedCount);
        while (dictionary.Count < expectedCount) {
            Random.Shared.NextBytes(buffer);
            string next = Convert.ToHexString(buffer);
            string key = Random.Shared.Next(0, 10) == 0 ? null : next; // 随机使用nullKey
            if (dictionary.TryAdd(key, next)) {
                keyList.Add(key);
            }
        }

        Assert.That(dictionary.Count, Is.EqualTo(keyList.Count));
        // 顺序迭代测试
        int index = 0;
        foreach (var realKey in dictionary.Keys) {
            var expectedKey = keyList[index++];
            if (expectedKey != realKey) {
                throw new InvalidOperationException($"expectedKey:{expectedKey} == realKey:{realKey}");
            }
        }
        // 逆序迭代测试
        index = keyList.Count - 1;
        var reversedEnumerator = dictionary.Keys.GetReversedEnumerator();
        while (reversedEnumerator.MoveNext()) {
            var expectedKey = keyList[index--];
            string realKey = reversedEnumerator.Current;
            if (expectedKey != realKey) {
                throw new InvalidOperationException($"expectedKey:{expectedKey} == realKey:{realKey}");
            }
        }
        return dictionary;
    }

    [Test]
    public void TestAdjustCapacity() {
        LinkedDictionary<string, string> dictionary = TestStringDic(10000);
        dictionary.AdjustCapacity(15000);
        dictionary.AdjustCapacity(10001);
        dictionary.AdjustCapacity(10000);
    }

#pragma warning disable SYSLIB0011
    /** 序列化测试 */
    [Test]
    public void SerialTest() {
        LinkedDictionary<string, string> dictionary = TestStringDic(1000);

        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream stream = new MemoryStream(new byte[64 * 1024]);
        formatter.Serialize(stream, dictionary);

        stream.Position = 0;
        LinkedDictionary<string, string> dictionary2 = (LinkedDictionary<string, string>)formatter.Deserialize(stream);
        foreach (KeyValuePair<string, string> pair in dictionary) {
            string value2 = dictionary2[pair.Key];
            Assert.That(value2, Is.EqualTo(pair.Value));
        }
        Assert.That(dictionary2.DefaultValue, Is.EqualTo(dictionary.DefaultValue));
    }
#pragma warning restore SYSLIB0011

    [Test]
    public void NullKeyTest() {
        LinkedDictionary<string, string> dictionary = new LinkedDictionary<string, string>(3);
        string value = "wjybxx";
        dictionary[null] = value;
        dictionary["key1"] = "key1";
        dictionary["key2"] = "key2";
        Assert.That(dictionary[null], Is.EqualTo(value));

        Assert.True(dictionary.NextKey(null, out string nextKey));
        Assert.That(nextKey, Is.EqualTo("key1"));

        Assert.True(dictionary.NextKey("key1", out nextKey));
        Assert.That(nextKey, Is.EqualTo("key2"));

        dictionary.Remove(null);
        Assert.That(dictionary.PeekFirstKey(), Is.EqualTo("key1"));
    }
}