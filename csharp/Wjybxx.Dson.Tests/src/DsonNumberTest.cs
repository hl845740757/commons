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

using System.Text;
using NUnit.Framework;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 一测试就发现C#默认不支持Infinity....
/// 果然测试用例不够是不行的...
/// </summary>
public class DsonNumberTest
{
    private static readonly string NumberString = """
                                                  {
                                                    value1: 10001,
                                                    value2: 1.05,
                                                    value3: @i 0xFF,
                                                    value4: @i 0b10010001,
                                                    value5: @i 100_000_000,
                                                    value6: @d 1.05E-15,
                                                    value7: @d Infinity,
                                                    value8: @d NaN,
                                                    value9: @i -0xFF,
                                                    value10: @i -0b10010001,
                                                    value11: @d -1.05E-15,
                                                    value12: @d -1.123456789,
                                                    value13: @i "0x01 | 0x02 | 0x10 | 0x20"
                                                  }
                                                  """;

    [Test]
    public void TestNumber() {
        DsonObject<string> dsonObject = Dsons.FromDson(NumberString).AsObject();
        // 必须带类型，否则无法精确反序列化，断言会失败
        List<NumberStyle> styleList = new List<NumberStyle>
        {
            NumberStyle.Typed, NumberStyle.Unsigned,
            NumberStyle.Hex, NumberStyle.Hex | NumberStyle.Unsigned, NumberStyle.Hex | NumberStyle.Fixed,
            NumberStyle.Binary, NumberStyle.Binary | NumberStyle.Unsigned, NumberStyle.Binary | NumberStyle.Fixed,
            NumberStyle.NoExponent3, NumberStyle.NoExponent7 // 浮点数
        };
        foreach (NumberStyle style in styleList) {
            bool supportFloat = IsSupportFloat(style);
            StringWriter stringWriter = new StringWriter(new StringBuilder(120));
            using DsonTextWriter writer = new DsonTextWriter(DsonTextWriterSettings.Default, stringWriter);
            writer.WriteStartObject(ObjectStyle.Indent);
            for (int i = 1; i <= dsonObject.Count; i++) {
                string name = "value" + i;
                if (!dsonObject.TryGetValue(name, out DsonValue dsonValue)) {
                    break;
                }
                DsonNumber dsonNumber = dsonValue.AsNumber();
                switch (dsonNumber.DsonType) {
                    case DsonType.Int32: {
                        writer.WriteInt32(name, dsonNumber.IntValue, style);
                        break;
                    }
                    case DsonType.Int64: {
                        writer.WriteInt64(name, dsonNumber.LongValue, style);
                        break;
                    }
                    case DsonType.Float: {
                        writer.WriteFloat(name, dsonNumber.FloatValue, supportFloat ? style : NumberStyle.Typed);
                        break;
                    }
                    case DsonType.Double: {
                        writer.WriteDouble(name, dsonNumber.DoubleValue, supportFloat ? style : NumberStyle.Simple);
                        break;
                    }
                }
            }
            writer.WriteEndObject();
            writer.Flush();

            string dsonString2 = stringWriter.ToString();
            Console.WriteLine(style.ToString());
            Console.WriteLine(dsonString2);
            // 数据截断以后无法保证相等性
            if ((style & NumberStyle.NoExponent3) != 0 
                || (style & NumberStyle.NoExponent7) != 0) {
                continue;
            }
            DsonObject<string> dsonObject2 = Dsons.FromDson(dsonString2).AsObject();
            Assert.That(dsonObject, Is.EqualTo(dsonObject2));
        }
    }

    /** 是否支持浮点数 -- float和double */
    private static bool IsSupportFloat(NumberStyle style) {
        return style == NumberStyle.Simple
               || style == NumberStyle.Typed
               || style == NumberStyle.NoExponent3
               || style == NumberStyle.NoExponent7;
    }
}