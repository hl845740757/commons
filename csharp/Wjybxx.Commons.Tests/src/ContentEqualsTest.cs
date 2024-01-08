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
using Wjybxx.Commons.Collections;

namespace Commons.Tests;

/// <summary>
/// 测试实现的Set和字典的相等性是否正确
/// </summary>
public class ContentEqualsTest
{
    [Repeat(10)]
    [Test]
    public void TestSet() {
        byte[] buffer = new byte[12];
        HashSet<string> hashSet = new HashSet<string>(20);
        while (hashSet.Count < 20) {
            Random.Shared.NextBytes(buffer);
            hashSet.Add(Convert.ToHexString(buffer));
        }
        string[] elements = hashSet.ToArray();
        CollectionUtil.Shuffle(elements);
        LinkedHashSet<string> linkedHashSet = new LinkedHashSet<string>();
        CollectionUtil.AddAll(linkedHashSet, elements);

        Assert.True(linkedHashSet.ContentEquals(hashSet));
    }

    [Repeat(10)]
    [Test]
    public void TestDictionary() {
        Dictionary<string, string> dictionary = new Dictionary<string, string>(20);
        byte[] buffer = new byte[12];
        while (dictionary.Count < 20) {
            Random.Shared.NextBytes(buffer);
            string key = Convert.ToHexString(buffer);

            Random.Shared.NextBytes(buffer);
            string next = Convert.ToHexString(buffer);
            dictionary.TryAdd(key, next);
        }

        KeyValuePair<string, string>[] pairs = dictionary.ToArray();
        CollectionUtil.Shuffle(pairs);

        LinkedDictionary<string, string> linkedDictionary = new LinkedDictionary<string, string>(20);
        foreach (var pair in pairs) {
            linkedDictionary[pair.Key] = pair.Value;
        }
        Assert.True(CollectionUtil.ContentEquals(linkedDictionary, dictionary));
    }
}