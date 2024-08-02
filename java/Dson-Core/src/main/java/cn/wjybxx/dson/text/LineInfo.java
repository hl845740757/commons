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

package cn.wjybxx.dson.text;

import javax.annotation.Nonnull;

/**
 * 缓存一定的行信息可以有效避免unread时反向扫描整行
 * 行的基础信息 ln、startPos、endPos等由{@link DsonCharStream}维护。
 * 行的业务信息 contentStartPos、contentEndPos、lineHead由{@link DsonScanner}维护。
 *
 * @author wjybxx
 * date - 2023/6/3
 */
public final class LineInfo {

    public static final int STATE_SCAN = 0;
    public static final int STATE_LF = 1;
    public static final int STATE_CRLF = 2;
    public static final int STATE_EOF = 3;

    /** 行号 */
    public final int ln;
    /** 行全局起始位置， 0-based */
    public final int startPos;
    /**
     * 行结束位置（全局），0-based
     * 1.如果换行符是\r\n，则是\n的位置；
     * 2.如果换行符是\n，则是\n的位置；
     * 3.eof的情况下，是最后一个字符的位置 --换行结束的情况下，eof出现在读取下一行的时候
     * 4.start和end相等时表示空行；start大于end时表示无效行。
     */
    public int endPos;
    /** 行在字符流中的状态 -- endPos是否到达行尾 */
    public int state = STATE_SCAN;

    public LineInfo(int ln, int startPos, int endPos) {
        this.ln = ln;
        this.startPos = startPos;
        this.endPos = endPos;
    }

    /** 当前行是否已扫描完成 */
    public boolean isScanCompleted() {
        return state != STATE_SCAN;
    }

    /** 最后一个可读取的位置 */
    public int lastReadablePosition() {
        return lastReadablePosition(state, endPos);
    }

    /** 最后一个可读取的位置 -- 不包含换行符；可能小于startPos */
    public static int lastReadablePosition(int state, int endPos) {
        if (state == STATE_LF) {
            return endPos - 1;
        }
        if (state == STATE_CRLF) {
            return endPos - 2;
        }
        return endPos;
    }

    /** 当前行是否有内容 */
    public boolean hasContent() {
        if (state == STATE_LF) {
            return startPos + 1 <= endPos; // startPos有字符
        }
        if (state == STATE_CRLF) {
            return startPos + 2 <= endPos;  // startPos有字符
        }
        return startPos <= endPos; // 适用eof
    }

    /** 当前已扫描部分长度 */
    public int lineLength() {
        if (endPos < startPos) {
            return 0;
        }
        return endPos - startPos + 1;
    }

    @Override
    public final boolean equals(Object o) {
        return this == o;
    }

    @Override
    public final int hashCode() {
        return ln;
    }

    @Nonnull
    @Override
    public final String toString() {
        return new StringBuilder(64)
                .append("LineInfo{")
                .append("ln=").append(ln)
                .append(", startPos=").append(startPos)
                .append(", endPos=").append(endPos)
                .append(", state=").append(state)
                .append('}').toString();
    }

}