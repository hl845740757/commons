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

public class ImmutableListTest
{
    [Test]
    public void TestEmpty() {
        ImmutableList<int> list = ImmutableList<int>.Empty;
        Assert.IsTrue(list.IsEmpty);
        Assert.AreEqual(0, list.Count);
        Assert.IsTrue(list.IsReadOnly);
    }

    [Test]
    public void TestCreateRangePreservesOrder() {
        int[] src = { 3, 1, 4, 1, 5, 9, 2, 6 };
        ImmutableList<int> list = ImmutableList<int>.CreateRange(src);
        Assert.AreEqual(src.Length, list.Count);
        for (int i = 0; i < src.Length; i++) {
            Assert.AreEqual(src[i], list[i]);
        }
    }

    [Test]
    public void TestCreateRangeWithComparerSorts() {
        int[] src = { 3, 1, 4, 1, 5, 9, 2, 6 };
        ImmutableList<int> list = ImmutableList<int>.CreateRange(src, Comparer<int>.Default);
        int last = int.MinValue;
        foreach (int v in list) {
            Assert.IsTrue(v >= last);
            last = v;
        }
    }

    [Test]
    public void TestSingleCreate() {
        ImmutableList<string> list = ImmutableList<string>.Create("hello");
        Assert.AreEqual(1, list.Count);
        Assert.AreEqual("hello", list[0]);
    }

    [Test]
    public void TestIndexerSetThrows() {
        ImmutableList<int> list = ImmutableList<int>.CreateRange(new[] { 1, 2, 3 });
        Assert.Throws<NotSupportedException>(() => list[0] = 100);
    }

    [Test]
    public void TestPeekFirstLast() {
        ImmutableList<int> list = ImmutableList<int>.CreateRange(new[] { 7, 8, 9 });
        Assert.AreEqual(7, list.PeekFirst());
        Assert.AreEqual(9, list.PeekLast());
    }

    [Test]
    public void TestContains() {
        ImmutableList<string> list = ImmutableList<string>.CreateRange(new[] { "a", "b", "c" });
        Assert.IsTrue(list.Contains("b"));
        Assert.IsFalse(list.Contains("z"));
    }

    [Test]
    public void TestCreateRangeReturnsSameInstanceForImmutable() {
        ImmutableList<int> a = ImmutableList<int>.CreateRange(new[] { 1, 2, 3 });
        ImmutableList<int> b = ImmutableList<int>.CreateRange(a);
        Assert.AreSame(a, b);
    }

    [Test]
    public void TestEmptyForSameTypeIsCached() {
        Assert.AreSame(ImmutableList<int>.Empty, ImmutableList<int>.Empty);
    }
}
