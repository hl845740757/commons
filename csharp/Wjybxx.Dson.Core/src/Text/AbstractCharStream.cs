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
using System.Collections.Generic;
using System.Diagnostics;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// CharStram抽象类实现
/// </summary>
public abstract class AbstractCharStream : IDsonCharStream
{
    private readonly List<LineInfo> _lines = new List<LineInfo>();
    private LineInfo? _curLine;
    private bool _readingContent = false;
    private int _position = -1;
    private bool _eof = false;

    internal AbstractCharStream() {
    }

    /** 应该只在初始化时使用 */
    protected void InitPosition(int position) {
        _position = position;
    }

    protected void AddLine(LineInfo lineInfo) {
        if (lineInfo == null) throw new ArgumentNullException(nameof(lineInfo));
        _lines.Add(lineInfo);
    }

    public int Read() {
        if (IsClosed()) throw new DsonParseException("Trying to read after closed");
        if (_eof) throw new DsonParseException("Trying to read past eof");

        LineInfo curLine = this._curLine;
        if (curLine == null) {
            if (_lines.Count == 0 && !ScanNextLine()) {
                _eof = true;
                return -1;
            }
            curLine = _lines[0];
            OnReadNextLine(curLine);
            return -2;
        }
        // 到达当前扫描部分的尾部，扫描更多的字符 - 不测试readingContent也没问题
        if (_position == curLine.endPos && !curLine.IsScanCompleted()) {
            ScanMoreChars(curLine); // 要么读取到一个输入，要么行扫描完毕
            Debug.Assert(_position < curLine.endPos || curLine.IsScanCompleted());
        }
        if (curLine.IsScanCompleted()) {
            if (_readingContent) {
                if (_position >= curLine.LastReadablePosition()) { // 读完或已在行尾(unread)
                    return OnReadEndOfLine(curLine);
                } else {
                    _position++;
                }
            } else if (curLine.HasContent()) {
                _readingContent = true;
            } else {
                return OnReadEndOfLine(curLine);
            }
        } else {
            if (_readingContent) {
                _position++;
            } else {
                _readingContent = true;
            }
        }
        return CharAt(curLine, _position);
    }

    private int OnReadEndOfLine(LineInfo curLine) {
        // 这里不可以修改position，否则unread可能出错
        if (curLine.state == LineInfo.StateEof) {
            _eof = true;
            return -1;
        }
        int index = IndexOfCurLine(_lines, curLine);
        if (index + 1 == _lines.Count && !ScanNextLine()) {
            _eof = true;
            return -1;
        }
        curLine = _lines[index + 1];
        OnReadNextLine(curLine);
        return -2;
    }

    private void OnReadNextLine(LineInfo nextLine) {
        Debug.Assert(nextLine.IsScanCompleted() || nextLine.HasContent());
        this._curLine = nextLine;
        this._readingContent = false;
        this._position = nextLine.startPos;
        DiscardReadLines(_lines, nextLine); // 清除部分缓存
    }

    private void OnBackToPreLine(LineInfo preLine) {
        Debug.Assert(preLine.IsScanCompleted());
        this._curLine = preLine;
        if (preLine.HasContent()) {
            // 有内容的情况下，需要回退到上一行最后一个字符的位置，否则继续unread会出错
            this._position = preLine.LastReadablePosition();
            this._readingContent = true;
        } else {
            // 无内容的情况下回退到startPos，和read保持一致
            this._position = preLine.startPos;
            this._readingContent = false;
        }
    }

    public int Unread() {
        if (_eof) {
            _eof = false;
            return -1;
        }
        LineInfo curLine = this._curLine;
        if (curLine == null) {
            throw new InvalidOperationException("read must be called before unread.");
        }
        // 当前行回退 -- 需要检测是否回退到bufferStartPos之前
        if (_readingContent) {
            if (_position > curLine.startPos) {
                CheckUnreadOverFlow(_position - 1);
                _position--;
            } else {
                _readingContent = false;
            }
            return 0;
        }
        // 尝试回退到上一行，需要检测上一行的最后一个可读字符是否溢出
        int index = IndexOfCurLine(_lines, curLine);
        if (index > 0) {
            LineInfo preLine = _lines[index - 1];
            if (preLine.HasContent()) {
                CheckUnreadOverFlow(preLine.LastReadablePosition());
            } else {
                CheckUnreadOverFlow(preLine.startPos);
            }
            OnBackToPreLine(preLine);
            return -2;
        } else {
            if (curLine.ln != FirstLn) {
                throw BufferOverFlow(_position);
            }
            // 回退到初始状态
            this._curLine = null;
            this._readingContent = false;
            this._position = -1;
            return 0;
        }
    }

    public void SkipLine() {
        LineInfo curLine = this._curLine;
        if (curLine == null) throw new InvalidOperationException();
        while (!curLine.IsScanCompleted()) {
            _position = curLine.endPos;
            ScanMoreChars(curLine);
        }
        if (curLine.HasContent()) {
            _readingContent = true;
            _position = curLine.LastReadablePosition();
        }
    }

    public int Position => _position;

    public LineInfo? CurLine => _curLine;

    //

    protected static int IndexOfCurLine(List<LineInfo> lines, LineInfo curLine) {
        return curLine.ln - lines[0].ln;
    }

    protected static DsonParseException BufferOverFlow(int position) {
        return new DsonParseException("BufferOverFlow, caused by unread, pos: " + position);
    }

    protected bool IsReadingContent() {
        return _readingContent;
    }

    protected bool IsEof() {
        return _eof;
    }

    /** 获取首行行号，基于Reader时可能不是第一行开始 */
    protected virtual int FirstLn => 1;

    /** 丢弃部分已读的行，减少内存占用 */
    protected void DiscardReadLines(List<LineInfo> lines, LineInfo? curLine) {
        if (curLine == null) {
            return;
        }
        int idx = IndexOfCurLine(lines, curLine);
        if (idx >= 10) {
            lines.RemoveRange(0, 5);
        }
    }

    /// <summary>
    /// 当前流是否已处于关闭状态
    /// </summary>
    /// <returns>如果已关闭则返回true</returns>
    protected abstract bool IsClosed();

    /// <summary>
    /// 获取Line在全局位置的字符
    /// </summary>
    /// <param name="curLine">当前读取的行</param>
    /// <param name="position">全局位置</param>
    /// <returns></returns>
    protected abstract int CharAt(LineInfo curLine, int position);

    /// <summary>
    /// 检测是否可以回退到指定位置
    /// </summary>
    /// <param name="position"></param>
    /// <exception cref="DsonParseException">如果不可回退到指定位置</exception>
    protected abstract void CheckUnreadOverFlow(int position);

    /// <summary>
    /// 丢弃指定位置以前已读的字节
    /// </summary>
    /// <param name="position"></param>
    public virtual void DiscardReadChars(int position) {
    }

    /// <summary>
    /// 扫描更多的字符
    /// 注意：要么读取到一个输入，要么行扫描完毕
    /// </summary>
    /// <param name="line">要扫描的行，可能是当前行，也可能是下一行</param>
    /// <exception cref="DsonParseException">如果缓冲区已满</exception>
    protected abstract void ScanMoreChars(LineInfo line);

    /// <summary>
    /// 尝试扫描下一行（可以扫描多行）
    /// </summary>
    /// <returns>如果扫描到新的一行则返回true</returns>
    protected abstract bool ScanNextLine();

    public abstract void Dispose();
}
}