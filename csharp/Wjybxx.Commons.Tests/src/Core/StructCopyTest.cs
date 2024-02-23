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

namespace Commons.Tests.Core;

/// <summary>
/// 测试结构体的防御性拷贝
/// </summary>
public class StructCopyTest
{
    private struct Value
    {
        internal int x;
        public Value(int x) {
            this.x = x;
        }
        public int X => x;
        public void Increment() {
            x++;
        }
    }

    private void IncrementByIn(in Value value) {
        value.Increment(); // IDEA 有拷贝提示...
    }

    private void IncrementByRef(ref Value value) {
        value.Increment();
    }

    [Test]
    public void TestStructCopy() {
        int initValue = 0;
        Value value = new Value(initValue);
        IncrementByIn(in value);
        Assert.That(value.X, Is.EqualTo(initValue));

        IncrementByRef(ref value);
        Assert.That(value.X, Is.EqualTo(initValue + 1));
    }
}