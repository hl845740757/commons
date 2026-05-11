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

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wjybxx.Commons.Collections;

namespace Commons.Tests.Core;

public class ImmutableDictionaryTest
{
    [Test]
    public void TestEmpty() {
        ImmutableDictionary<string, int> dic = ImmutableDictionary<string, int>.Empty;
        Assert.IsTrue(dic.IsEmpty);
        Assert.AreEqual(0, dic.Count);
        Assert.IsTrue(dic.IsReadOnly);
    }

    [Test]
    public void TestCreateRangePreservesInsertionOrder() {
        var pairs = new[] {
            new KeyValuePair<string, int>("c", 3),
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("b", 2),
            new KeyValuePair<string, int>("d", 4),
        };
        ImmutableDictionary<string, int> dic = ImmutableDictionary<string, int>.CreateRange(pairs);
        Assert.AreEqual(pairs.Length, dic.Count);

        List<string> keys = new();
        foreach (var pair in dic) {
            keys.Add(pair.Key);
        }
        for (int i = 0; i < pairs.Length; i++) {
            Assert.AreEqual(pairs[i].Key, keys[i]);
        }
    }

    [Test]
    public void TestTryGetValue() {
        ImmutableDictionary<string, int> dic = ImmutableDictionary<string, int>.CreateRange(new[] {
            new KeyValuePair<string, int>("alpha", 1),
            new KeyValuePair<string, int>("beta", 2),
        });

        Assert.IsTrue(dic.TryGetValue("alpha", out int v));
        Assert.AreEqual(1, v);
        Assert.IsFalse(dic.TryGetValue("missing", out _));
    }

    [Test]
    public void TestIndexer() {
        ImmutableDictionary<string, int> dic = ImmutableDictionary<string, int>.Create("k", 100);
        Assert.AreEqual(100, dic["k"]);
        Assert.Throws<KeyNotFoundException>(() => { var _ = dic["missing"]; });
    }

    [Test]
    public void TestContainsKey() {
        ImmutableDictionary<int, string> dic = ImmutableDictionary<int, string>.CreateRange(new[] {
            new KeyValuePair<int, string>(1, "a"),
            new KeyValuePair<int, string>(2, "b"),
        });
        Assert.IsTrue(dic.ContainsKey(1));
        Assert.IsFalse(dic.ContainsKey(99));
    }

    [Test]
    public void TestCreateRangeWithDuplicateKeyThrows() {
        Assert.Throws<ArgumentException>(() => {
            ImmutableDictionary<string, int>.CreateRange(new[] {
                new KeyValuePair<string, int>("k", 1),
                new KeyValuePair<string, int>("k", 2),
            });
        });
    }

    [Test]
    public void TestCreateRangeReturnsSameInstanceForImmutable() {
        ImmutableDictionary<string, int> a = ImmutableDictionary<string, int>.Create("x", 1);
        ImmutableDictionary<string, int> b = ImmutableDictionary<string, int>.CreateRange(a);
        Assert.AreSame(a, b);
    }

    [Test]
    public void TestKeysAndValuesOrder() {
        var pairs = new[] {
            new KeyValuePair<string, int>("z", 26),
            new KeyValuePair<string, int>("a", 1),
            new KeyValuePair<string, int>("m", 13),
        };
        ImmutableDictionary<string, int> dic = ImmutableDictionary<string, int>.CreateRange(pairs);

        List<string> keys = new(dic.Keys);
        List<int> vals = new(dic.Values);
        Assert.AreEqual(new[] { "z", "a", "m" }, keys);
        Assert.AreEqual(new[] { 26, 1, 13 }, vals);
    }
}
