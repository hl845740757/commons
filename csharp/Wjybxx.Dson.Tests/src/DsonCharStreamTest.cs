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
using NUnit.Framework.Internal;
using Wjybxx.Commons;
using Wjybxx.Dson.Text;

namespace Wjybxx.Dson.Tests;

/// <summary>
/// 字符流相等性测试
/// </summary>
public class DsonCharStreamTest
{
    private const string tokenString = """"
            pos: {@Vector3 x: 0.5, y: 0.5, z: 0.5}
            posArray: [@{clsName:LinkedList}
              {@{V3} x: 0.1, y: 0.1, z: 0.1},
              {@{V3} x: 0.2, y: 0.2, z: 0.2}
            ]
            // 这是一行注释
            {
              k1: @i 1,
              k2: @L 987654321,
              k3: @f 1.05,
              k4: 1.0000001,
              k5: @b true,
              k6: @b 1,
              k7: @N null,
              k8: null,
              k9: wjybxx,
              k10: "\\u4F60\\u597D",
              K11: @dt 2023-06-17T18:37:00,
              K12: @ts 1715659200
            }
            @bin "FFFA"
            @bin ""
            // 这是一个文本段落
            @"""
            @| intro:
            @|   salkjlxaaslkhalkhsal,anxksjah
            @| xalsjalkjlkalhjalskhalhslahlsanlkanclxa
            @| salkhaslkanlnlkhsjlanx,nalkxanla
            @- lsaljsaljsalsaajsal
            @- saklhskalhlsajlxlsamlkjalj
            @- salkhjsaljsljldjaslna
            @"""
            @sL 这是一行长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长的纯文本
            """"; // 换行结束与不换行是不同的

    [Test]
    public void testLineEquals() {
        List<LineInfo> lines1 = new(32);
        List<LineInfo> lines2 = new(32);
        List<LineInfo> lines3 = new(32);
        using (IDsonCharStream charStream = IDsonCharStream.NewCharStream(tokenString)) {
            pullToList(charStream, lines1);
        }
        using (IDsonCharStream charStream = IDsonCharStream.NewBufferedCharStream(new StringReader(tokenString))) {
            pullToList(charStream, lines2);
        }
        using (IDsonCharStream charStream = IDsonCharStream.NewPreparedCharStream(toPreparedLines())) {
            pullToList(charStream, lines3);
        }
        Assert.AreEqual(lines1.Count, lines2.Count);
        Assert.AreEqual(lines1.Count, lines3.Count);

        int size = lines1.Count - 1;
        for (int i = 0; i < size; i++) {
            LineInfo lineInfo1 = lines1[i];
            LineInfo lineInfo2 = lines2[i];
            LineInfo lineInfo3 = lines3[i];
            Assert.IsTrue(baseEquals(lineInfo1, lineInfo2), "{0}, {1}", lineInfo1, lineInfo2);
            Assert.IsTrue(baseEquals(lineInfo1, lineInfo3), "{0}, {1}", lineInfo1, lineInfo3);
        }
    }

    public bool baseEquals(LineInfo self, LineInfo lineInfo) {
        if (self == lineInfo) return true;

        if (self.state != lineInfo.state) return false;
        if (self.ln != lineInfo.ln) return false;
        if (self.startPos != lineInfo.startPos) return false;
        if (self.endPos != lineInfo.endPos) return false;
        return true;
    }

    private static void pullToList(IDsonCharStream buffer, List<LineInfo> outList) {
        int c;
        while ((c = buffer.Read()) != -1) {
            if (buffer.Position == 0) {
                outList.Add(buffer.CurLine);
            } else if (c == -2) {
                outList.Add(buffer.CurLine);
            }
        }
    }

    [Test]
    public void testTokenEquals() {
        List<DsonToken> tokenList1 = new(120);
        List<DsonToken> tokenList2 = new(120);
        List<DsonToken> tokenList3 = new(120);
        pullToList(Dsons.NewStringScanner(tokenString), tokenList1);
        pullToList(Dsons.NewStreamScanner(new StringReader(tokenString)), tokenList2);
        pullToList(Dsons.NewLinesScanner(toPreparedLines()), tokenList3);
        Assert.AreEqual(tokenList1.Count, tokenList2.Count);

        // 换行符的可能导致pos的差异
        int size = tokenList1.Count;
        for (int i = 0; i < size; i++) {
            DsonToken dsonToken1 = tokenList1[i];
            DsonToken dsonToken2 = tokenList2[i];
            DsonToken dsonToken3 = tokenList3[i];
            Assert.AreEqual(dsonToken1, dsonToken2);
            Assert.AreEqual(dsonToken1, dsonToken3);
        }
    }

    private static List<LineInfo> toPreparedLines() {
        List<string> tokenLines = ObjectUtil.Lines(tokenString);
        List<LineInfo> result = new(tokenLines.Count);
        // java换行符默认是LF，C#是CRLF...
        int state = LineInfo.StateCrLf;
        for (int idx = 0; idx < tokenLines.Count; idx++) {
            String line = tokenLines[idx];
            LineInfo lineInfo;
            if (idx == 0) {
                lineInfo = new LineInfo(1, 0, line.Length + 1); // length就是换行符位置
            } else {
                LineInfo preLine = result[idx - 1];
                int startPos = preLine.endPos + 1; // 换行符的下一个位置
                int endPos = startPos + line.Length + 1; // length就是换行符位置
                lineInfo = new LineInfo(idx + 1, startPos, endPos);
            }
            lineInfo.rawLine = line;
            lineInfo.state = state;
            result.Add(lineInfo);
        }
        return result;
    }

    private static void pullToList(DsonScanner scanner, List<DsonToken> outList) {
        while (true) {
            DsonToken nextToken = scanner.NextToken();
            if (nextToken.type == DsonTokenType.Eof) {
                break;
            }
            outList.Add(nextToken);
        }
    }
}