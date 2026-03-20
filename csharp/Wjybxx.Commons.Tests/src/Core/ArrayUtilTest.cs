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

namespace Commons.Tests.Core;

public class ArrayUtilTest
{
    [Test]
    public void MoveToTest() {
        {
            string[] array = new[] { "A", "B", "C", "D", "E" };
            ArrayUtil.MoveTo(array, 3, 1);
            Assert.IsTrue(ArrayUtil.Equals(array, new[] { "A", "D", "B", "C", "E" }));
        }
        {
            string[] array = new[] { "A", "B", "C", "D", "E" };
            ArrayUtil.MoveTo(array, 1, 3);
            Assert.IsTrue(ArrayUtil.Equals(array, new[] { "A", "C", "D", "B", "E" }));
        }
    }
}