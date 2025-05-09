/*
 * Copyright 2023-2025 wjybxx(845740757@qq.com)
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

import java.util.List;

/**
 * 已就绪的
 *
 * @author wjybxx
 * date - 2025/5/9
 */
public class PreparedCharStream extends AbstractCharStream {

    private List<LineInfo> rawLines;

    public PreparedCharStream(List<LineInfo> rawLines) {
        this.rawLines = rawLines;
        addLines(rawLines);
    }

    @Override
    protected boolean isClosed() {
        return rawLines == null;
    }

    @Override
    protected int charAt(LineInfo curLine, int position) {
        int idx = position - curLine.startPos;
        return curLine.rawLine.charAt(idx);
    }

    @Override
    protected void checkUnreadOverFlow(int position) {

    }

    @Override
    protected void scanMoreChars(LineInfo line) {
        throw new AssertionError();
    }

    @Override
    protected boolean scanNextLine() {
        return false;
    }

    @Override
    public void close() {
        rawLines = null;
    }
}