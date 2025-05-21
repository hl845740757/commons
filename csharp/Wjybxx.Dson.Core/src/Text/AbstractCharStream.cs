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
using System.Runtime.CompilerServices;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// CharStream抽象类实现
/// </summary>
public abstract class AbstractCharStream : IDsonCharStream
{
    private const int MAX_LINES = 20;
    private readonly List<LineInfo> _lines = new(MAX_LINES);
    /// <summary>
    /// 在Line为值类型情况下，该字段的作用：避免每次从lines中取值时产生拷贝。
    /// 不过，由于curLine是副本，因此每次调用<see cref="ScanMoreChars"/>后，都应当写回到lines。
    /// </summary>
    private LineInfo _curLine;
    private int _position = -1;
    private bool _readingContent = false;
    private bool _eof = false;

    internal AbstractCharStream() {
    }

    /** 应该只在初始化时使用 */
    protected void InitPosition(int position) {
        _position = position;
    }

    protected void AddLines(IList<LineInfo> newLines) {
        // unity下可能无该方法
#if NET6_0_OR_GREATER
        _lines.EnsureCapacity(_lines.Count + newLines.Count);
#endif
        foreach (LineInfo newLine in newLines) {
            if (newLine.IsNull) throw new NullReferenceException("newLine");
            _lines.Add(newLine);
        }
    }

    protected void AddLine(LineInfo newLine) {
        if (newLine.IsNull) throw new ArgumentNullException(nameof(newLine));
        _lines.Add(newLine);
    }

    public int Read() {
        if (IsClosed()) throw new DsonParseException("Trying to read after closed");
        if (_eof) throw new DsonParseException("Trying to read past eof");

        ref LineInfo curLine = ref _curLine;
        if (curLine.IsNull) {
            if (_lines.Count == 0 && !ScanNextLine(in curLine)) {
                _eof = true;
                return -1;
            }
            LineInfo nextLine = _lines[0];
            OnReadNextLine(nextLine);
            return -2;
        }
        // 到达当前扫描部分的尾部，扫描更多的字符 - 不测试readingContent也没问题
        if (_position == curLine.endPos && !curLine.IsScanCompleted()) {
            ScanMoreChars(ref curLine); // 要么读取到一个输入，要么行扫描完毕
            WriteBack(in curLine); // 写回到Lines
            Debug.Assert(_position < curLine.endPos || curLine.IsScanCompleted());
        }
        if (curLine.IsScanCompleted()) {
            if (_readingContent) {
                if (_position >= curLine.LastReadablePosition()) { // 读完或已在行尾(unread)
                    return OnReadEndOfLine(ref curLine);
                } else {
                    _position++;
                }
            } else if (curLine.HasContent()) {
                _readingContent = true;
            } else {
                return OnReadEndOfLine(ref curLine);
            }
        } else {
            if (_readingContent) {
                _position++;
            } else {
                _readingContent = true;
            }
        }
        return CharAt(ref curLine, _position);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteBack(in LineInfo curLine) {
        int idx = IndexOfCurLine(_lines, in curLine);
        _lines[idx] = curLine;
    }

    /** 当前行的数据读取完毕 */
    private int OnReadEndOfLine(ref LineInfo curLine) {
        // 这里不可以修改position，否则unread可能出错
        if (curLine.state == LineInfo.StateEof) {
            _eof = true;
            return -1;
        }
        int index = IndexOfCurLine(_lines, in curLine);
        if (index + 1 == _lines.Count && !ScanNextLine(in curLine)) {
            _eof = true;
            return -1;
        }
        LineInfo nextLine = _lines[index + 1];
        OnReadNextLine(nextLine);
        return -2;
    }

    private void OnReadNextLine(LineInfo nextLine) {
        Debug.Assert(nextLine.IsScanCompleted() || nextLine.HasContent());
        this._curLine = nextLine;
        this._readingContent = false;
        this._position = nextLine.startPos;
        DiscardReadLines(_lines, in nextLine); // 清除部分缓存
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
        ref LineInfo curLine = ref _curLine;
        if (curLine.IsNull) {
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
        int index = IndexOfCurLine(_lines, in curLine);
        if (index > 0) {
            LineInfo preLine = _lines[index - 1];
            CheckUnreadOverFlow(preLine.endPos);
            OnBackToPreLine(preLine);
            return -2;
        } else {
            if (curLine.ln != FirstLn) {
                throw BufferOverFlow(_position);
            }
            // 回退到初始状态
            this._curLine = default;
            this._readingContent = false;
            this._position = -1;
            return 0;
        }
    }

    public void SkipLine() {
        ref LineInfo curLine = ref _curLine;
        if (curLine.IsNull) throw new InvalidOperationException();
        while (!curLine.IsScanCompleted()) {
            _position = curLine.endPos;
            ScanMoreChars(ref curLine);
        }
        WriteBack(in curLine);

        if (curLine.HasContent()) {
            _readingContent = true;
            _position = curLine.LastReadablePosition();
        }
    }

    public int Position => _position;
    public int Ln => _curLine.ln;
    public int Column => _curLine.IsNull ? 0 : (_position - _curLine.startPos + 1);

    //

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int IndexOfCurLine(List<LineInfo> lines, in LineInfo curLine) {
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

    /** 丢弃部分已读的行，减少内存占用 -- 如果注释行很多，这可能有问题 */
    protected void DiscardReadLines(List<LineInfo> lines, in LineInfo curLine) {
        if (curLine.IsNull) {
            return;
        }
        int idx = IndexOfCurLine(lines, in curLine);
        if (idx >= MAX_LINES) {
            lines.RemoveRange(0, MAX_LINES / 2);
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
    protected abstract int CharAt(ref LineInfo curLine, int position);

    /// <summary>
    /// 检测是否可以回退到指定位置（目标位置数据是否还在缓存中）
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
    /// 注意：要么读取到一个输入，要么行扫描完毕。
    /// </summary>
    /// <param name="curLine">要扫描的行，可能是当前行，也可能是下一行</param>
    /// <exception cref="DsonParseException">如果缓冲区已满</exception>
    protected abstract void ScanMoreChars(ref LineInfo curLine);

    /// <summary>
    /// 尝试扫描下一行（可以扫描多行）
    /// </summary>
    /// <param name="curLine"></param>
    /// <returns>如果扫描到新的一行则返回true</returns>
    protected abstract bool ScanNextLine(in LineInfo curLine);

    public abstract void Dispose();
}
}