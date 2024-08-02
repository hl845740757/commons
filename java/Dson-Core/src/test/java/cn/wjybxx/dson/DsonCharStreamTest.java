/*
 * Copyright 2023-2024 wjybxx(845740757@qq.com)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package cn.wjybxx.dson;

import cn.wjybxx.dson.text.*;
import org.apache.commons.lang3.RandomUtils;
import org.junit.jupiter.api.Assertions;
import org.junit.jupiter.api.Test;

import java.io.StringReader;
import java.util.ArrayList;
import java.util.List;

/**
 * 验证{@link DsonCharStream}实现之间的相等性
 *
 * @author wjybxx
 * date - 2023/6/3
 */
@SuppressWarnings("deprecation")
public class DsonCharStreamTest {

    private static final String tokenString = """
            pos: {@Vector3 x: 0.5, y: 0.5, z: 0.5}
            posArray: [@{clsName:LinkedList}
              {@{V3} x: 0.1, y: 0.1, z: 0.1},
              {@{V3} x: 0.2, y: 0.2, z: 0.2}
            ]
            // 这是一行注释
            {
              k1: @i 1,
              k2: @L 987654321,
              k3: @f 1.05f,
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
            @\"""
            @| intro:
            @|   salkjlxaaslkhalkhsal,anxksjah
            @| xalsjalkjlkalhjalskhalhslahlsanlkanclxa
            @| salkhaslkanlnlkhsjlanx,nalkxanla
            @- lsaljsaljsalsaajsal
            @- saklhskalhlsajlxlsamlkjalj
            @- salkhjsaljsljldjaslna
            @\"""
            @sL 这是一行长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长长的纯文本
            """; // 换行结束与不换行是不同的

    // 删除行首以后不易拼接字符串
//    /** 根据CharStream还原tokenString，测试是否相等 */
//    @Test
//    void testCharStreamEqualsTokenString() {
//        StringBuilder sb = new StringBuilder(tokenString.length());
//        int c1;
//        try (DsonCharStream charStream = DsonCharStream.newBufferedCharStream(new StringReader(tokenString))) {
//            while ((c1 = charStream.read()) != -1) {
//                if (c1 == -2) {
//                    if (charStream.getLn() > 1) {
//                        sb.append('\n');
//                    }
//                } else {
//                    if (charStream.getPosition() - charStream.getCurLine().startPos == 2) {
//                        sb.append(' ');
//                    }
//                    sb.append((char) c1);
//                }
//            }
//            if (charStream.getCurLine().state != LineInfo.STATE_EOF) {
//                sb.append('\n');
//            }
//        }
//        Assertions.assertEquals(tokenString, sb.toString());
//    }

    /** 测试两种CharStream实现的相等性 */
    @Test
    void testCharStreamEquals() {
        int c1;
        int c2;
        boolean unread = false;
        int c3 = -1;
        try (DsonCharStream charStream = DsonCharStream.newCharStream(tokenString);
             DsonCharStream bufferedCharStream = DsonCharStream.newBufferedCharStream(new StringReader(tokenString))) {
            while ((c1 = charStream.read()) != -1) {
                c2 = bufferedCharStream.read();
                Assertions.assertEquals(c1, c2);
                if (unread) {
                    Assertions.assertEquals(c3, c1);
                }
                if (!unread && RandomUtils.nextBoolean()) {
                    c3 = c1;
                    unread = true;
                    charStream.unread();
                    bufferedCharStream.unread();
                } else {
                    c3 = -1;
                    unread = false;
                }
            }
            Assertions.assertEquals(-1, bufferedCharStream.read());
        }
    }

    @Test
    void testLineEquals() {
        List<LineInfo> stringLines = new ArrayList<>(32);
        List<LineInfo> streamLines = new ArrayList<>(32);
        try (DsonCharStream charStream = DsonCharStream.newCharStream(tokenString)) {
            pullToList(charStream, stringLines);
        }
        try (DsonCharStream charStream = DsonCharStream.newBufferedCharStream(new StringReader(tokenString))) {
            pullToList(charStream, streamLines);
        }
        Assertions.assertEquals(stringLines.size(), streamLines.size());

        int size = stringLines.size() - 1;
        for (int i = 0; i < size; i++) {
            LineInfo lineInfo1 = stringLines.get(i);
            LineInfo lineInfo2 = streamLines.get(i);
            Assertions.assertTrue(baseEquals(lineInfo1, lineInfo2), () -> {
                return lineInfo1.toString() + ", " + lineInfo2.toString();
            });
        }
    }

    public boolean baseEquals(LineInfo self, LineInfo lineInfo) {
        if (self == lineInfo) return true;

        if (self.state != lineInfo.state) return false;
        if (self.ln != lineInfo.ln) return false;
        if (self.startPos != lineInfo.startPos) return false;
        if (self.endPos != lineInfo.endPos) return false;
        return true;
    }

    private static void pullToList(DsonCharStream buffer, List<LineInfo> outList) {
        int c;
        while ((c = buffer.read()) != -1) {
            if (buffer.getPosition() == 0) {
                outList.add(buffer.getCurLine());
            } else if (c == -2) {
                outList.add(buffer.getCurLine());
            }
        }
    }

    @Test
    void testTokenEquals() {
        List<DsonToken> tokenList1 = new ArrayList<>(120);
        List<DsonToken> tokenList3 = new ArrayList<>(120);
        pullToList(Dsons.newStringScanner(tokenString), tokenList1);
        pullToList(Dsons.newStreamScanner(new StringReader(tokenString)), tokenList3);
        Assertions.assertEquals(tokenList1.size(), tokenList3.size());

        // 换行符的可能导致pos的差异
        int size = tokenList1.size();
        for (int i = 0; i < size; i++) {
            DsonToken dsonToken1 = tokenList1.get(i);
            DsonToken dsonToken3 = tokenList3.get(i);
            Assertions.assertEquals(dsonToken1, dsonToken3);
        }
    }

    private static void pullToList(DsonScanner scanner, List<DsonToken> outList) {
        while (true) {
            DsonToken nextToken = scanner.nextToken();
            if (nextToken.type == DsonTokenType.EOF) {
                break;
            }
            outList.add(nextToken);
        }
    }

}