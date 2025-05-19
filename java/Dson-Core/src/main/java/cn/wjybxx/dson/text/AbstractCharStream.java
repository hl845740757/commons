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

import javax.annotation.Nullable;
import java.util.ArrayList;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/5
 */
public abstract class AbstractCharStream implements DsonCharStream {

    private final ArrayList<LineInfo> lines = new ArrayList<>();
    private LineInfo curLine;
    private boolean readingContent = false;
    private int position = -1;
    private boolean eof = false;

    public AbstractCharStream() {
    }

    /** 应该只在初始化时使用 */
    protected final void initPosition(int position) {
        this.position = position;
    }

    protected final void addLines(List<LineInfo> newLines) {
        lines.ensureCapacity(lines.size() + newLines.size());
        for (LineInfo lineInfo : newLines) {
            if (lineInfo == null) throw new NullPointerException("lineInfo");
            lines.add(lineInfo);
        }
    }

    protected final void addLine(LineInfo newLine) {
        Objects.requireNonNull(newLine);
        lines.add(newLine);
    }

    @Override
    public int read() {
        if (isClosed()) throw new DsonParseException("Trying to read after closed");
        if (eof) throw new DsonParseException("Trying to read past eof");

        final LineInfo curLine = this.curLine;
        if (curLine == null) {
            if (lines.isEmpty() && !scanNextLine(null)) {
                eof = true;
                return -1;
            }
            LineInfo nextLine = lines.get(0);
            onReadNextLine(nextLine);
            return -2;
        }
        // 到达当前扫描部分的尾部，扫描更多的字符 - 不测试readingContent也没问题
        if (position == curLine.endPos && !curLine.isScanCompleted()) {
            scanMoreChars(curLine); // 要么读取到一个输入，要么行扫描完毕
            assert position < curLine.endPos || curLine.isScanCompleted();
        }
        if (curLine.isScanCompleted()) {
            if (readingContent) {
                if (position >= curLine.lastReadablePosition()) { // 读完或已在行尾(unread)
                    return onReadEndOfLine(curLine);
                } else {
                    position++;
                }
            } else if (curLine.hasContent()) {
                readingContent = true;
            } else {
                return onReadEndOfLine(curLine);
            }
        } else {
            if (readingContent) {
                position++;
            } else {
                readingContent = true;
            }
        }
        return charAt(curLine, position);
    }

    private int onReadEndOfLine(final LineInfo curLine) {
        // 这里不可以修改position，否则unread可能出错
        if (curLine.state == LineInfo.STATE_EOF) {
            eof = true;
            return -1;
        }
        int index = indexOfCurLine(lines, curLine);
        if (index + 1 == lines.size() && !scanNextLine(curLine)) {
            eof = true;
            return -1;
        }
        LineInfo nextLine = lines.get(index + 1);
        onReadNextLine(nextLine);
        return -2;
    }

    private void onReadNextLine(final LineInfo nextLine) {
        assert nextLine.isScanCompleted() || nextLine.hasContent();
        this.curLine = nextLine;
        this.readingContent = false;
        this.position = nextLine.startPos;
        discardReadLines(lines, nextLine); // 清除部分缓存
    }

    private void onBackToPreLine(LineInfo preLine) {
        assert preLine.isScanCompleted();
        this.curLine = preLine;
        if (preLine.hasContent()) {
            // 有内容的情况下，需要回退到上一行最后一个字符的位置，否则继续unread会出错
            this.position = preLine.lastReadablePosition();
            this.readingContent = true;
        } else {
            // 无内容的情况下回退到startPos，和read保持一致
            this.position = preLine.startPos;
            this.readingContent = false;
        }
    }

    @Override
    public int unread() {
        if (eof) {
            eof = false;
            return -1;
        }
        LineInfo curLine = this.curLine;
        if (curLine == null) {
            throw new IllegalStateException("read must be called before unread.");
        }
        // 当前行回退 -- 需要检测是否回退到bufferStartPos之前
        if (readingContent) {
            if (position > curLine.startPos) {
                checkUnreadOverFlow(position - 1);
                position--;
            } else {
                readingContent = false;
            }
            return 0;
        }
        // 尝试回退到上一行，需要检测上一行的最后一个可读字符是否溢出
        int index = indexOfCurLine(lines, curLine);
        if (index > 0) {
            LineInfo preLine = lines.get(index - 1);
            checkUnreadOverFlow(preLine.endPos);
            onBackToPreLine(preLine);
            return -2;
        } else {
            if (curLine.ln != getFirstLn()) {
                throw bufferOverFlow(position);
            }
            // 回退到初始状态
            this.curLine = null;
            this.readingContent = false;
            this.position = -1;
            return 0;
        }
    }

    @Override
    public void skipLine() {
        LineInfo curLine = this.curLine;
        if (curLine == null) throw new IllegalStateException();
        while (!curLine.isScanCompleted()) {
            position = curLine.endPos;
            scanMoreChars(curLine);
        }
        if (curLine.hasContent()) {
            readingContent = true;
            position = curLine.lastReadablePosition();
        }
    }

    @Override
    public int getPosition() {
        return position;
    }

    @Override
    public int getLn() {
        return curLine == null ? 0 : curLine.ln;
    }

    @Override
    public int getColumn() {
        return curLine == null ? 0 : (position - curLine.startPos + 1);
    }

    //

    protected static int indexOfCurLine(List<LineInfo> lines, LineInfo curLine) {
        return curLine.ln - lines.get(0).ln;
    }

    protected static DsonParseException bufferOverFlow(int position) {
        return new DsonParseException("BufferOverFlow, caused by unread, pos: " + position);
    }

    protected boolean isReadingContent() {
        return readingContent;
    }

    protected boolean isEof() {
        return eof;
    }

    /** 获取首行行号，基于Reader时可能不是第一行开始 */
    protected int getFirstLn() {
        return 1;
    }

    /** 丢弃部分已读的行，减少内存占用 -- 如果注释行很多，这可能有问题 */
    protected void discardReadLines(List<LineInfo> lines, LineInfo curLine) {
        if (curLine == null) {
            return;
        }
        int idx = indexOfCurLine(lines, curLine);
        if (idx >= 20) {
            lines.subList(0, 10).clear();
        }
    }

    /** 当前流是否已处于关闭状态 */
    protected abstract boolean isClosed();

    /** 获取指定全局位置的字符 */
    protected abstract int charAt(LineInfo curLine, int position);

    /**
     * 检测是否可以回退到指定位置(指定位置是否还在缓存中)
     *
     * @throws DsonParseException 如果不可回退到指定位置
     */
    protected abstract void checkUnreadOverFlow(int position);

    /**
     * @param curLine 要扫描的行(指向最新位置)
     * @throws DsonParseException 如果缓冲区已满
     * @apiNote 要么读取到一个输入，要么行扫描完毕
     */
    protected abstract void scanMoreChars(LineInfo curLine);

    /** @return 如果扫描到新的一行则返回true */
    protected abstract boolean scanNextLine(@Nullable LineInfo curLine);

}