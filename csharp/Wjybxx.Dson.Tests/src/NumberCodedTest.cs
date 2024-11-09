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
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 测试数字压缩算法的正确性
/// </summary>
public class NumberCodedTest
{
    private const int COUNT = 100000;
    private static int repeat = 0;
    private static Random random = new Random();

    [SetUp]
    public void SetUp() {
        repeat++;
    }

    [Repeat(3)]
    [Test]
    public void testInt32() {
        WireType wireType = WireTypes.ForNumber(repeat % 3);
        Console.WriteLine("Begin: WireType: " + wireType);

        byte[] buffer = new byte[5 * COUNT];
        int[] valueArray = new int[COUNT];
        int totalSize = 0;
        using (IDsonOutput dsonOutput = DsonOutputs.NewInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                int v = random.Next();
                valueArray[i] = v;
                wireType.WriteInt32(dsonOutput, v);
            }
            dsonOutput.Flush();
            totalSize = dsonOutput.Position;
        }
        using (IDsonInput dsonInput = DsonInputs.NewInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                int v = valueArray[i];
                int v2 = wireType.ReadInt32(dsonInput);
                Assert.That(v2, Is.EqualTo(v));
            }
        }
        Console.WriteLine("End: WireType: " + wireType);
    }

    [Repeat(3)]
    [Test]
    public void testInt64() {
        WireType wireType = WireTypes.ForNumber(repeat % 3);
        Console.WriteLine("Begin: WireType: " + wireType);

        byte[] buffer = new byte[10 * COUNT];
        long[] valueArray = new long[COUNT];
        int totalSize = 0;
        using (IDsonOutput dsonOutput = DsonOutputs.NewInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                long v = random.NextInt64();
                valueArray[i] = v;
                wireType.WriteInt64(dsonOutput, v);
            }
            dsonOutput.Flush();
            totalSize = dsonOutput.Position;
        }
        using (IDsonInput dsonInput = DsonInputs.NewInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                long v = valueArray[i];
                long v2 = wireType.ReadInt64(dsonInput);
                Assert.That(v2, Is.EqualTo(v));
            }
        }
        Console.WriteLine("End: WireType: " + wireType);
    }

    [Repeat(2)]
    [Test]
    public void testFloat() {
        WireType wireType = (repeat & 1) == 1 ? WireType.Uint : WireType.Fixed;
        Console.WriteLine("Begin: WireType: " + wireType);

        byte[] buffer = new byte[5 * COUNT];
        float[] valueArray = new float[COUNT];
        int totalSize = 0;
        using (IDsonOutput dsonOutput = DsonOutputs.NewInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                float v = random.NextSingle();
                valueArray[i] = v;
                wireType.WriteFloat(dsonOutput, v);
            }
            dsonOutput.Flush();
            totalSize = dsonOutput.Position;
        }
        using (IDsonInput dsonInput = DsonInputs.NewInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                float v = valueArray[i];
                float v2 = wireType.ReadFloat(dsonInput);
                Assert.That(v2, Is.EqualTo(v));
            }
        }
        Console.WriteLine("End: WireType: " + wireType);
    }

    [Repeat(2)]
    [Test]
    public void testDouble() {
        WireType wireType = (repeat & 1) == 1 ? WireType.Uint : WireType.Fixed;
        Console.WriteLine("Begin: WireType: " + wireType);

        byte[] buffer = new byte[10 * COUNT];
        double[] valueArray = new double[COUNT];
        int totalSize = 0;
        using (IDsonOutput dsonOutput = DsonOutputs.NewInstance(buffer)) {
            for (int i = 0; i < COUNT; i++) {
                double v = i * random.NextDouble();
                valueArray[i] = v;
                wireType.WriteDouble(dsonOutput, v);
            }
            dsonOutput.Flush();
            totalSize = dsonOutput.Position;
        }
        using (IDsonInput dsonInput = DsonInputs.NewInstance(buffer, 0, totalSize)) {
            for (int i = 0; i < COUNT; i++) {
                double v = valueArray[i];
                double v2 = wireType.ReadDouble(dsonInput);
                Assert.That(v2, Is.EqualTo(v));
            }
        }
        Console.WriteLine("End: WireType: " + wireType);
    }
}