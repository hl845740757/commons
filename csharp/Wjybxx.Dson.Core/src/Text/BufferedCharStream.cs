#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to iBn writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Wjybxx.Commons.Pool;

namespace Wjybxx.Dson.Text
{
class BufferedCharStream : AbstractCharStream
{
    private const int MinBufferSize = 32;
    /** 行首前面超过空白字符太多是有问题的 */
    private const int MaxBufferSize = 4096;
    /** 方便以后调整 */
    private static readonly IArrayPool<char> charArrayPool = IArrayPool<char>.Shared;

#nullable disable
    private TextReader _reader;
    private readonly bool _autoClose;

    private CharBuffer _buffer;
    /** reader批量读取数据到该buffer，然后再读取到当前buffer -- 缓冲的缓冲，减少io操作 */
    private CharBuffer _nextBuffer;
#nullable enable

    /** buffer全局开始位置 */
    private int _bufferStartPos;
    /** reader是否已到达文件尾部 -- 部分reader在到达文件尾部的时候不可继续读 */
    private bool _readerEof;

    public BufferedCharStream(TextReader reader, bool autoClose = true) {
        this._reader = reader ?? throw new ArgumentNullException(nameof(reader));
        this._buffer = new CharBuffer(charArrayPool.Acquire(1024));
        this._nextBuffer = new CharBuffer(charArrayPool.Acquire(1024));
        this._autoClose = autoClose;
    }

    private void ReturnBuffers() {
        if (_buffer != null) {
            charArrayPool.Release(_buffer.array);
            _buffer = null;
        }
        if (_nextBuffer != null) {
            charArrayPool.Release(_nextBuffer.array);
            _nextBuffer = null;
        }
    }

    public override void Dispose() {
        ReturnBuffers();
        if (_reader != null && _autoClose) {
            _reader.Dispose();
            _reader = null;
        }
    }

    protected override bool IsClosed() {
        return _reader == null;
    }

    protected override int CharAt(LineInfo curLine, int position) {
        int ridx = position - _bufferStartPos;
        return _buffer.CharAt(ridx);
    }

    protected override void CheckUnreadOverFlow(int position) {
        int ridx = position - _bufferStartPos;
        if (ridx < 0 || ridx >= _buffer.widx) {
            throw BufferOverFlow(position);
        }
    }

    public override void DiscardReadChars(int position) {
        if (position <= 0 || position >= Position) {
            return;
        }
        int shiftCount = position - _bufferStartPos - 1;
        if (shiftCount > 0) {
            _buffer.Shift(shiftCount);
            _bufferStartPos += shiftCount;
        }
    }

    private void DiscardReadChars() {
        CharBuffer buffer = this._buffer;
        // 已读部分达75%时丢弃50%(保留最近的25%)；这里不根据具体的数字来进行丢弃，减少不必要的数组拷贝
        if (Position - _bufferStartPos >= buffer.Capacity * 0.75f) {
            DiscardReadChars((int)(_bufferStartPos + buffer.Capacity * 0.25d));
        }
        // 如果可写空间不足，则尝试扩容
        if (buffer.WritableChars <= 4
            && buffer.Capacity < MaxBufferSize) {
            GrowUp(buffer);
        }
    }

    private void GrowUp(CharBuffer charBuffer) {
        int capacity = Math.Min(MaxBufferSize, charBuffer.Capacity * 2);
        char[] newBuffer = charArrayPool.Acquire(capacity);
        char[] oldBuffer = charBuffer.Grow(newBuffer);
        charArrayPool.Release(oldBuffer);
    }

    /** 该方法一直读到指定行读取完毕，或缓冲区满(不一定扩容) */
    protected override void ScanMoreChars(LineInfo line) {
        DiscardReadChars();
        CharBuffer buffer = this._buffer;
        CharBuffer nextBuffer = this._nextBuffer;
        try {
            // >= 2 是为了处理\r\n换行符，避免读入单个\r不知如何处理
            while (!line.IsScanCompleted() && buffer.WritableChars >= 2) {
                ReadToBuffer(nextBuffer);
                while (nextBuffer.IsReadable && buffer.WritableChars >= 2) {
                    char c = nextBuffer.Read();
                    line.endPos++;
                    buffer.Write(c);

                    if (c == '\n') { // LF
                        line.state = LineInfo.StateLf;
                        return;
                    }
                    if (c == '\r') {
                        // 再读取一次，以判断\r\n
                        if (!nextBuffer.IsReadable) {
                            ReadToBuffer(nextBuffer);
                        }
                        if (!nextBuffer.IsReadable) {
                            Debug.Assert(_readerEof);
                            line.state = LineInfo.StateEof;
                            return;
                        }
                        c = nextBuffer.Read();
                        line.endPos++;
                        buffer.Write(c);
                        if (c == '\n') { // CRLF
                            line.state = LineInfo.StateCrlf;
                            return;
                        }
                    }
                }
                if (_readerEof && !nextBuffer.IsReadable) {
                    line.state = LineInfo.StateEof;
                    return;
                }
            }
        }
        catch (ThreadInterruptedException) {
            Thread.CurrentThread.Interrupt(); // 恢复中断
        }
    }

    private void ReadToBuffer(CharBuffer nextBuffer) {
        if (!_readerEof) {
            if (nextBuffer.ridx >= nextBuffer.Capacity / 2) {
                nextBuffer.Shift(nextBuffer.ridx);
            }
            int len = nextBuffer.WritableChars;
            if (len <= 0) {
                return;
            }
            int n = _reader.Read(nextBuffer.array, nextBuffer.widx, len);
            if (n <= 0) { // C# 在Eof的情况下不会返回-1...返回0时已读完
                _readerEof = true;
            } else {
                nextBuffer.AddWidx(n);
            }
        }
    }

    protected override bool ScanNextLine() {
        if (_readerEof && !_nextBuffer.IsReadable) {
            return false;
        }
        LineInfo? curLine = CurLine;
        int ln;
        int startPos;
        if (curLine == null) {
            ln = FirstLn;
            startPos = 0;
        } else {
            ln = curLine.ln + 1;
            startPos = curLine.endPos + 1;
        }

        // startPos指向的是下一个位置，而endPos是在scan的时候增加，因此endPos需要回退一个位置
        LineInfo tempLine = new LineInfo(ln, startPos, startPos - 1);
        ScanMoreChars(tempLine);
        if (tempLine.startPos > tempLine.endPos) { // 无效行，没有输入
            return false;
        }
        AddLine(tempLine);
        return true;
    }
}
}