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

import cn.wjybxx.base.ThreadUtils;
import cn.wjybxx.base.pool.ArrayPool;
import cn.wjybxx.base.pool.ConcurrentArrayPool;
import cn.wjybxx.dson.io.DsonIOException;

import java.io.IOException;
import java.io.Reader;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/5
 */
final class BufferedCharStream extends AbstractCharStream {

    private static final int MIN_BUFFER_SIZE = 32;
    /** 行首前面超过空白字符太多是有问题的 */
    private static final int MAX_BUFFER_SIZE = 4096;
    /** 方便以后调整 */
    private static final ArrayPool<char[]> charArrayPool = ConcurrentArrayPool.SHARED_CHAR_ARRAY_POOL;

    private Reader reader;
    private final boolean autoClose;

    private CharBuffer buffer;
    /** reader批量读取数据到该buffer，然后再读取到当前buffer -- 缓冲的缓冲，减少io操作 */
    private CharBuffer nextBuffer;

    /** buffer全局开始位置 */
    private int bufferStartPos;
    /** reader是否已到达文件尾部 -- 部分reader在到达文件尾部的时候不可继续读 */
    private boolean readerEof;

    BufferedCharStream(Reader reader) {
        this(reader, true);
    }

    BufferedCharStream(Reader reader, boolean autoClose) {
        Objects.requireNonNull(reader);
        this.reader = reader;
        this.autoClose = autoClose;
        this.buffer = new CharBuffer(charArrayPool.acquire(1024));
        this.nextBuffer = new CharBuffer(charArrayPool.acquire(1024));
    }

    private void returnBuffers() {
        if (buffer != null) {
            charArrayPool.release(buffer.array);
            buffer = null;
        }
        if (nextBuffer != null) {
            charArrayPool.release(nextBuffer.array);
            nextBuffer = null;
        }
    }

    @Override
    public void close() {
        returnBuffers();
        try {
            if (reader != null && autoClose) {
                reader.close();
                reader = null;
            }
        } catch (IOException e) {
            throw DsonIOException.wrap(e);
        }
    }

    @Override
    protected boolean isClosed() {
        return reader == null;
    }

    @Override
    protected int charAt(LineInfo curLine, int position) {
        int ridx = position - bufferStartPos;
        return buffer.charAt(ridx);
    }

    @Override
    protected void checkUnreadOverFlow(int position) {
        int ridx = position - bufferStartPos;
        if (ridx < 0 || ridx >= buffer.widx) {
            throw bufferOverFlow(position);
        }
    }

    @Override
    public void discardReadChars(int position) {
        if (position <= 0 || position >= getPosition()) {
            return;
        }
        int shiftCount = position - bufferStartPos - 1;
        if (shiftCount > 0) {
            buffer.shift(shiftCount);
            bufferStartPos += shiftCount;
        }
    }

    private void discardReadChars() {
        CharBuffer buffer = this.buffer;
        // 已读部分达75%时丢弃50%(保留最近的25%)；这里不根据具体的数字来进行丢弃，减少不必要的数组拷贝
        if (getPosition() - bufferStartPos >= buffer.capacity() * 0.75f) {
            discardReadChars((int) (bufferStartPos + buffer.capacity() * 0.25d));
        }
        // 如果可写空间不足，则尝试扩容
        if (buffer.writableChars() <= 4
                && buffer.capacity() < MAX_BUFFER_SIZE) {
            growUp(buffer);
        }
    }

    private void growUp(CharBuffer charBuffer) {
        int capacity = Math.min(MAX_BUFFER_SIZE, charBuffer.capacity() * 2);
        char[] newBuffer = charArrayPool.acquire(capacity);
        char[] oldBuffer = charBuffer.grow(newBuffer);
        charArrayPool.release(oldBuffer);
    }

    /** 该方法一直读到指定行读取完毕，或缓冲区满(不一定扩容) */
    @Override
    protected void scanMoreChars(LineInfo line) {
        discardReadChars();
        CharBuffer buffer = this.buffer;
        CharBuffer nextBuffer = this.nextBuffer;
        try {
            // >= 2 是为了处理\r\n换行符，避免读入单个\r不知如何处理
            while (!line.isScanCompleted() && buffer.writableChars() >= 2) {
                readToBuffer(nextBuffer);
                while (nextBuffer.isReadable() && buffer.writableChars() >= 2) {
                    char c = nextBuffer.read();
                    line.endPos++;
                    buffer.write(c);

                    if (c == '\n') { // LF
                        line.state = LineInfo.STATE_LF;
                        return;
                    }
                    if (c == '\r') {
                        // 再读取一次，以判断\r\n
                        if (!nextBuffer.isReadable()) {
                            readToBuffer(nextBuffer);
                        }
                        if (!nextBuffer.isReadable()) {
                            assert readerEof;
                            line.state = LineInfo.STATE_EOF;
                            return;
                        }
                        c = nextBuffer.read();
                        line.endPos++;
                        buffer.write(c);
                        if (c == '\n') { // CRLF
                            line.state = LineInfo.STATE_CRLF;
                            return;
                        }
                    }
                }
                if (readerEof && !nextBuffer.isReadable()) {
                    line.state = LineInfo.STATE_EOF;
                    return;
                }
            }
        } catch (IOException e) {
            ThreadUtils.recoveryInterrupted(e.getCause());
            throw DsonIOException.wrap(e);
        }
    }

    private void readToBuffer(CharBuffer nextBuffer) throws IOException {
        if (!readerEof) {
            if (nextBuffer.ridx >= nextBuffer.capacity() / 2) {
                nextBuffer.shift(nextBuffer.ridx);
            }
            int len = nextBuffer.writableChars();
            if (len <= 0) {
                return;
            }
            int n = reader.read(nextBuffer.array, nextBuffer.widx, len);
            if (n <= 0) { // Java会在Eof的情况下返回-1，0其实也结束了
                readerEof = true;
            } else {
                nextBuffer.addWidx(n);
            }
        }
    }

    @Override
    protected boolean scanNextLine() {
        if (readerEof && !nextBuffer.isReadable()) {
            return false;
        }
        LineInfo curLine = getCurLine();
        final int ln;
        final int startPos;
        if (curLine == null) {
            ln = getFirstLn();
            startPos = 0;
        } else {
            ln = curLine.ln + 1;
            startPos = curLine.endPos + 1;
        }

        // startPos指向的是下一个位置，而endPos是在scan的时候增加，因此endPos需要回退一个位置
        LineInfo tempLine = new LineInfo(ln, startPos, startPos - 1);
        scanMoreChars(tempLine);
        if (tempLine.startPos > tempLine.endPos) { // 无效行，没有输入
            return false;
        }
        addLine(tempLine);
        return true;
    }

}