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

    /// <summary>
    /// varfloat没有PB实现可以对照，因此以实际写入的字节数为基准
    /// </summary>
    private static int WrittenSizeOfVarFloat(float value) {
        byte[] buffer = new byte[CodedUtil.MAX_VAR_FLOAT32_LENGTH];
        return CodedUtil.WriteVarFloat(buffer, 0, value);
    }

    private static int WrittenSizeOfVarDouble(double value) {
        byte[] buffer = new byte[CodedUtil.MAX_VAR_FLOAT64_LENGTH];
        return CodedUtil.WriteVarDouble(buffer, 0, value);
    }

    [Test]
    public void ComputeVarFloat32() {
        float[] specialValues =
        {
            0f, 1f, -1f, 0.5f, 2f, -2f, 100f, 3.14f, float.MaxValue, float.MinValue,
            float.Epsilon, float.NaN, float.PositiveInfinity, float.NegativeInfinity
        };
        foreach (float value in specialValues) {
            int rawBits = BitConverter.SingleToInt32Bits(value);
            Assert.That(CodedUtil.ComputeRawVarFloat32Size(rawBits),
                Is.EqualTo(WrittenSizeOfVarFloat(value)), $"value: {value}");
        }
        // 随机bit模式 -- 覆盖非规格化数等边界
        for (int i = 0; i < 10000; i++) {
            int rawBits = Random.Shared.Next(int.MinValue, int.MaxValue);
            float value = BitConverter.Int32BitsToSingle(rawBits);
            Assert.That(CodedUtil.ComputeRawVarFloat32Size(rawBits),
                Is.EqualTo(WrittenSizeOfVarFloat(value)), $"rawBits: {rawBits}");
        }
        // 穷举每个终止边界：低shift位为0
        for (int shift = 0; shift < 32; shift++) {
            int rawBits = -1 << shift;
            float value = BitConverter.Int32BitsToSingle(rawBits);
            Assert.That(CodedUtil.ComputeRawVarFloat32Size(rawBits),
                Is.EqualTo(WrittenSizeOfVarFloat(value)), $"shift: {shift}");
        }
    }

    [Test]
    public void ComputeVarFloat64() {
        double[] specialValues =
        {
            0d, 1d, -1d, 0.5d, 2d, -2d, 100d, 3.14d, double.MaxValue, double.MinValue,
            double.Epsilon, double.NaN, double.PositiveInfinity, double.NegativeInfinity
        };
        foreach (double value in specialValues) {
            long rawBits = BitConverter.DoubleToInt64Bits(value);
            Assert.That(CodedUtil.ComputeRawVarFloat64Size(rawBits),
                Is.EqualTo(WrittenSizeOfVarDouble(value)), $"value: {value}");
        }
        for (int i = 0; i < 10000; i++) {
            long rawBits = Random.Shared.NextInt64(long.MinValue, long.MaxValue);
            double value = BitConverter.Int64BitsToDouble(rawBits);
            Assert.That(CodedUtil.ComputeRawVarFloat64Size(rawBits),
                Is.EqualTo(WrittenSizeOfVarDouble(value)), $"rawBits: {rawBits}");
        }
        for (int shift = 0; shift < 64; shift++) {
            long rawBits = -1L << shift;
            double value = BitConverter.Int64BitsToDouble(rawBits);
            Assert.That(CodedUtil.ComputeRawVarFloat64Size(rawBits),
                Is.EqualTo(WrittenSizeOfVarDouble(value)), $"shift: {shift}");
        }
    }
}