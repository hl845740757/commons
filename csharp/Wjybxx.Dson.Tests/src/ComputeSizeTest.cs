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
        int value = 0;
        {
            int pbSize = CodedOutputStream.ComputeRawVarint32Size((uint)value);
            int mySize = CodedUtil.ComputeRawVarInt32Size(value);
            Assert.That(mySize, Is.EqualTo(pbSize));
        }
        for (int i = 0; i < 10000; i++) {
            value = Random.Shared.Next();
            int pbSize = CodedOutputStream.ComputeRawVarint32Size((uint)value);
            int mySize = CodedUtil.ComputeRawVarInt32Size(value);
            Assert.That(mySize, Is.EqualTo(pbSize));
        }
    }

    /// <summary>
    /// 我们修改了int64的编码，因此不再相同
    /// </summary>
    // [Test]
    public void ComputeVarInt64() {
        long value = 0;
        {
            int pbSize = CodedOutputStream.ComputeRawVarint64Size((ulong)value);
            int mySize = CodedUtil.ComputeRawVarInt64Size(value);
            Assert.That(mySize, Is.EqualTo(pbSize));
        }
        for (int i = 0; i < 10000; i++) {
            value = Random.Shared.NextInt64();
            int pbSize = CodedOutputStream.ComputeRawVarint64Size((ulong)value);
            int mySize = CodedUtil.ComputeRawVarInt64Size(value);
            Assert.That(mySize, Is.EqualTo(pbSize));
        }
    }

    /// <summary>
    /// 比较PB的VarInt64和修改后的VarInt64压缩率
    /// </summary>
    [Test]
    public void CompareVarInt64() {
        int pbTotalSize = 0;
        int dsonTotalSize = 0;
        for (int i = 0; i < 10000; i++) {
            long value = Random.Shared.NextInt64();
            pbTotalSize += CodedOutputStream.ComputeRawVarint64Size((ulong)value);
            dsonTotalSize += CodedUtil.ComputeRawVarInt64Size(value);
        }
        Console.WriteLine($"pbTotalSize: {pbTotalSize}, dsonTotalSize: {dsonTotalSize}");
    }
}