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
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

public class FormatTest
{
    internal static readonly string DsonString = """"
        // 以下是一个简单的DsonObject示例
        {@{clsName:MyClassInfo, guid :10001, flags: 0}
          name : wjybxx,
          age: 28,
          pos :{@{Vector3} x: 0, y: 0, z: 0},
          address: [
            beijing,
            chengdu
          ],
          intro: 
          """
            我是wjybxx，是一个游戏开发者，Dson是我设计的文档型数据表达法，你可以通过github联系到我。
            thanks
          """
          , url: @sL https://www.github.com/hl845740757
          , time: {@dt date: 2023-06-17, time: 18:37:00, millis: 100, offset: +08:00}
        }
        """";


    [Test]
    public void FormatTest0() {
        DsonObject<string> dsonObject = Dsons.FromDson(DsonString).AsObject();
        // 标准模式纯文本左对齐
        {
            DsonTextWriterSettings.Builder builder = new DsonTextWriterSettings.Builder()
            {
                SoftLineLength = 50,
                TextStringLength = 50,
                TextAlignLeft = true
            };
            string dsonString2 = dsonObject.ToDson(ObjectStyle.Indent, builder.Build());
            Console.WriteLine("IndentStyle, alignLeft: true");
            Console.WriteLine(dsonString2);

            DsonValue dsonObject2 = Dsons.FromDson(dsonString2);
            Assert.That(dsonObject2, Is.EqualTo(dsonObject));
        }
        Console.WriteLine();

        // 标准模式，不对齐
        {
            DsonTextWriterSettings.Builder builder = new DsonTextWriterSettings.Builder()
            {
                SoftLineLength = 60,
                TextStringLength = 50,
                TextAlignLeft = false
            };
            string dsonString2 = dsonObject.ToDson(ObjectStyle.Indent, builder.Build());
            Console.WriteLine("IndentStyle, alignLeft: false");
            Console.WriteLine(dsonString2);

            DsonValue dsonObject2 = Dsons.FromDson(dsonString2);
            Assert.That(dsonObject2, Is.EqualTo(dsonObject));
        }
    }
}