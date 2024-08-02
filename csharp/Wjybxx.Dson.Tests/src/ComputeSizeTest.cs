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

using Google.Protobuf;
using NUnit.Framework;
using Wjybxx.Dson.Internal;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 测试自实现计算varint长度和protobuf的相等性
/// </summary>
public class ComputeSizeTest
{
    [Test]
    public void ComputeVarInt32() {
        uint value = 0;
        {
            int pbSize = CodedOutputStream.ComputeRawVarint32Size(value);
            int mySize = CodedUtil.ComputeRawVarInt32Size(value);
            Assert.That(mySize, Is.EqualTo(pbSize));
        }
        value = 1;
        for (int i = 0; i < 32; i++) {
            int pbSize = CodedOutputStream.ComputeRawVarint32Size(value);
            int mySize = CodedUtil.ComputeRawVarInt32Size(value);
            Assert.That(mySize, Is.EqualTo(pbSize));
            value *= 71;
        }
    }

    [Test]
    public void ComputeVarInt64() {
        ulong value = 0;
        {
            int pbSize = CodedOutputStream.ComputeRawVarint64Size(value);
            int mySize = CodedUtil.ComputeRawVarInt64Size(value);
            Assert.That(mySize, Is.EqualTo(pbSize));
        }
        value = 1;
        for (int i = 0; i < 64; i++) {
            int pbSize = CodedOutputStream.ComputeRawVarint64Size(value);
            int mySize = CodedUtil.ComputeRawVarInt64Size(value);
            Assert.That(mySize, Is.EqualTo(pbSize));
            value *= 71;
        }
    }
}