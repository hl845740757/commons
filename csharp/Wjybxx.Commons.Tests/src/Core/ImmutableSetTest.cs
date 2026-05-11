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

using System.Collections.Generic;
using NUnit.Framework;
using Wjybxx.Commons.Collections;

namespace Commons.Tests.Core;

public class ImmutableSetTest
{
    [Test]
    public void TestEmpty() {
        ImmutableSet<int> set = ImmutableSet<int>.Empty;
        Assert.IsTrue(set.IsEmpty);
        Assert.AreEqual(0, set.Count);
    }

    [Test]
    public void TestCreateRangePreservesInsertionOrder() {
        // 故意打乱常规hash顺序
        int[] src = { 17, 4, 99, 1, 33, 8 };
        ImmutableSet<int> set = ImmutableSet<int>.CreateRange(src);
        Assert.AreEqual(src.Length, set.Count);

        List<int> seen = new();
        foreach (int v in set) {
            seen.Add(v);
        }
        Assert.AreEqual(src.Length, seen.Count);
        for (int i = 0; i < src.Length; i++) {
            Assert.AreEqual(src[i], seen[i]);
        }
    }

    [Test]
    public void TestContains() {
        ImmutableSet<string> set = ImmutableSet<string>.CreateRange(new[] { "alpha", "beta", "gamma" });
        Assert.IsTrue(set.Contains("alpha"));
        Assert.IsTrue(set.Contains("gamma"));
        Assert.IsFalse(set.Contains("delta"));
    }

    [Test]
    public void TestPeekFirstLast() {
        ImmutableSet<int> set = ImmutableSet<int>.CreateRange(new[] { 5, 2, 8, 1 });
        Assert.AreEqual(5, set.PeekFirst());
        Assert.AreEqual(1, set.PeekLast());
    }

    [Test]
    public void TestDuplicateInputThrows() {
        // CreateRange 不接受重复元素，会抛 ArgumentException
        Assert.Throws<System.ArgumentException>(() => {
            ImmutableSet<int>.CreateRange(new[] { 1, 2, 1, 3, 2 });
        });

        // 输入预先去重后可正常构造
        ImmutableSet<int> deduped = ImmutableSet<int>.CreateRange(new HashSet<int> { 1, 2, 3 });
        Assert.AreEqual(3, deduped.Count);
    }

    [Test]
    public void TestEmptyIsSingleton() {
        Assert.AreSame(ImmutableSet<int>.Empty, ImmutableSet<int>.Empty);
    }
}
