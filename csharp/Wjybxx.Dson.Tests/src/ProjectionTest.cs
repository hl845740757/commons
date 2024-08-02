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

public class ProjectionTest
{
    private const string DsonString = """
            {@{clsName:MyClassInfo, guid :10001, flags: 0}
              name : wjybxx,
              age: 28,
              pos :{@{Vector3} x: 1, y: 2, z: 3},
              address: [
                beijing,
                chengdu,
                shanghai
              ],
              posArr: [
               {@{V3} x: 1, y: 1, z: 1},
               {@{V3} x: 2, y: 2, z: 2},
               {@{V3} x: 3, y: 3, z: 3}
              ],
              url: @sL https://www.github.com/hl845740757
            }
            """;

    private const string ProjectInfo = """
          {
            name: 1,
            age: 1,
            pos: {
              $header: 1,
              $all: 1,
              z: 0
            },
            address: {
              $slice : [1, 2] //跳过第一个元素，然后返回两个元素
            },
            posArr: {
              $header : 1, //返回数组的header
              $slice : 0,
              $elem: {  //投影数组元素的x和y
                x: 1,
                z: 1
              }
            }
          }
          """;


    [Test]
    public void Test() {
        DsonObject<string> expected = new DsonObject<string>();
        {
            DsonObject<string> dsonObject = Dsons.FromDson(DsonString).AsObject();
            Transfer(expected, dsonObject, "name");
            Transfer(expected, dsonObject, "age");
            {
                DsonObject<string> rawPos = dsonObject["pos"].AsObject();
                DsonObject<string> newPos = new DsonObject<string>();
                Transfer(newPos, rawPos, "x");
                Transfer(newPos, rawPos, "y");
                expected["pos"] = newPos;
            }
            {
                DsonArray<string> rawAddress = dsonObject["address"].AsArray();
                DsonArray<string> newAddress = rawAddress.Slice(1);
                expected["address"] = newAddress;
            }

            DsonArray<string> rawPosArr = dsonObject["posArr"].AsArray();
            DsonArray<string> newPosArr = new DsonArray<string>(3);
            foreach (DsonValue ele in rawPosArr) {
                DsonObject<string> rawPos = ele.AsObject();
                DsonObject<string> newPos = new DsonObject<string>();
                Transfer(newPos, rawPos, "x");
                Transfer(newPos, rawPos, "z");
                newPosArr.Add(newPos);
            }
            expected["posArr"] = newPosArr;
        }

        DsonObject<string> value = Dsons.Project(DsonString, ProjectInfo)!.AsObject();
        Console.WriteLine(Dsons.ToDson(value, ObjectStyle.Indent));
        Assert.That(value, Is.EqualTo(expected));
    }

    private static void Transfer(DsonObject<string> expected, DsonObject<string> dsonObject, string key) {
        expected[key] = dsonObject[key];
    }
}