#region LICENSE

// Copyright 2025 wjybxx(845740757@qq.com)
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

namespace Commons.Tests.Core;

public class ArrayDictionaryTest
{
    [Test]
    [Repeat(5)]
    public void TestStringDic1() {
        TestStringDic(10);
    }

    public void TestStringDic(int expectedCount) {
        ArrayDictionary<string, string> dictionary = new ArrayDictionary<string, string>(expectedCount / 3); // 顺便测试扩容

        byte[] buffer = new byte[12];
        List<string> keyList = new List<string>(expectedCount);
        while (dictionary.Count < expectedCount) {
            Random.Shared.NextBytes(buffer);
            string next = Convert.ToHexString(buffer);
            string key = Random.Shared.Next(0, 10) == 0 ? null : next; // 随机使用nullKey
            // 还需要测试AddFirst
            if (dictionary.TryAdd(key, next)) {
                keyList.Add(key);
            }
            // 随机删除元素 30%概率
            if (dictionary.Count > expectedCount / 2) {
                int idx = -1;
                switch (Random.Shared.Next(10)) {
                    case 0: {
                        // 随机位置
                        idx = Random.Shared.Next(keyList.Count);
                        break;
                    }
                    case 1: {
                        // 删除首元素
                        idx = 0;
                        break;
                    }
                    case 2: {
                        // 删除尾元素
                        idx = keyList.Count - 1;
                        break;
                    }
                }
                if (idx >= 0) {
                    string remKey = keyList[idx];
                    keyList.RemoveAt(idx);
                    dictionary.Remove(remKey);
                }
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
        for (; index >= 0; index--) {
            var expectedKey = keyList[index];
            string realKey = dictionary.GetKey(index);
            if (expectedKey != realKey) {
                throw new InvalidOperationException($"expectedKey:{expectedKey} == realKey:{realKey}");
            }
        }
    }
}