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

using System.Text;
using NUnit.Framework;
using Wjybxx.Commons;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.IO;

namespace Wjybxx.Dson.Tests.Codec;

/// <summary>
/// 编码时数组扩容测试
///
/// 测试的方法很简单，创建两个流，一个扩容，一个不扩容，测试最终的内容相等性
/// </summary>
public class ExpansionTest
{
    private const int MAX_CAPACITY = 2048;
    private static readonly byte[] _buffer = new byte[2048];
    private static IDsonOutput _output;
    private static DsonOutputs.ArrayOutput _growableOutput;
    
    [SetUp]
    public void SetUp() {
        Array.Clear(_buffer);
        _output = DsonOutputs.NewInstance(_buffer);

        IArrayPool<byte> bufferPool = IArrayPool<byte>.Shared;
        _growableOutput = DsonOutputs.NewInstance(bufferPool, 16, 2048);
    }

    [Test]
    public void TestNumber() {
        using (_growableOutput) {
            while (_output.Position < MAX_CAPACITY - CodedUtil.MAX_VAR_INT32_LENGTH) {
                int v = Random.Shared.Next();
                _output.WriteUInt32(v);
                _growableOutput.WriteUInt32(v);
            }
            _output.Flush();
            _growableOutput.Flush();
            Assert.AreEqual(_output.Position, _growableOutput.Position);

            Span<byte> lhs = new Span<byte>(_buffer, 0, _output.Position);
            Span<byte> rhs = new Span<byte>(_growableOutput.Buffer, 0, _growableOutput.Position);
            Assert.IsTrue(lhs.SequenceEqual(rhs));
        }
    }

    [Repeat(5)]
    [Test]
    public void TestString() {
        // 通过Random.NextBytes() 构建出来的字符串可能包含非法字符
        using (_growableOutput) {
            while (true) {
                int len = Random.Shared.Next(5, 256);
                string str = GenerateString(Random.Shared, len, true, true);
                int byteCount = Encoding.UTF8.GetByteCount(str);
                if (_output.Position + byteCount + CodedUtil.MAX_VAR_INT32_LENGTH > MAX_CAPACITY) {
                    break;
                }
                _output.WriteString(str);
                _growableOutput.WriteString(str);
            }
            _output.Flush();
            _growableOutput.Flush();
            Assert.AreEqual(_output.Position, _growableOutput.Position);

            Span<byte> lhs = new Span<byte>(_buffer, 0, _output.Position);
            Span<byte> rhs = new Span<byte>(_growableOutput.Buffer, 0, _growableOutput.Position);
            Assert.IsTrue(lhs.SequenceEqual(rhs));
        }
    }

    public static string GenerateString(Random rand, int length,
                                        bool includeSymbols = false, bool includeChinese = false) {
        var sb = new StringBuilder(length);
        for (int i = 0; i < length; i++) {
            int choice = rand.Next(0, 100);
            if (includeChinese && choice < 20) {
                // 20%概率生成汉字 -- 这概率是体育老师教的
                sb.Append((char)rand.Next(0x4E00, 0x9FA5));
            } else if (includeSymbols && choice < 40) {
                // 20%概率生成符号
                sb.Append((char)rand.Next(33, 48));
            } else if (choice < 70) {
                // 30%概率生成大写字母
                sb.Append((char)rand.Next(65, 91));
            } else {
                // 30%概率生成小写字母
                sb.Append((char)rand.Next(97, 123));
            }
        }
        return sb.ToString();
    }
}