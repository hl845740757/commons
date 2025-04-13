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
using Wjybxx.Commons;
using Wjybxx.Commons.Collections;

namespace Commons.Tests.Core;

public class ListenerListTest
{
    private const int capacity = 64;
    private static ListenerArray<string> list;
    private static string[] valArray;

    [SetUp]
    public void SetUp() {
        list = new RegularListenerArray<string>(capacity / 3); // 测试扩容
        for (int i = 0; i < capacity; i++) {
            list.Add(i.ToString());
        }
        valArray = new string[capacity];
        for (int i = 0; i < capacity; i++) {
            valArray[i] = i.ToString();
        }
        ArrayUtil.Shuffle(valArray);
    }

    [Test]
   public void testRemove() {
        for (int i = 0; i < valArray.Length; i++) {
            string val = valArray[i];
            list.Remove(val);

            Assert.IsFalse(list.Contains(val), "remove failed");
            for (int j = i + 1; j < valArray.Length; j++) {
                string jVal = valArray[j];
                Assert.IsTrue(list.Contains(jVal), "val is absent" + jVal);
            }
        }
        Assert.AreEqual(0, list.ElementCount);
    }
    
    [Test]
    public void testRemoveWhenIterating() {
        list.BeginItr();
        try {
            for (int i = 0; i < valArray.Length; i++) {
                string val = valArray[i];
                list.Remove(val);

                Assert.IsFalse(list.Contains(val), "remove failed");
                for (int j = i + 1; j < valArray.Length; j++) {
                    string jVal = valArray[j];
                    Assert.IsTrue(list.Contains(jVal), "val is absent" + jVal);
                }
            }
            Assert.AreEqual(capacity, list.Length);
        } finally {
            list.EndItr();
        }
        Assert.AreEqual(0, list.ElementCount);
    }

}