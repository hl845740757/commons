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

using System;
using System.IO;
using System.Text;

namespace Wjybxx.Commons.Apt;

/// <summary>
/// 处理行长度切换
/// </summary>
internal class LineWrapper
{
    public readonly StringBuilder codeOut;
    /// <summary>
    /// 似乎不写入内容，仅仅用于外部解析引用
    /// </summary>
    public bool nullWriter;

    private readonly string indent;
    private readonly int columnLimit;

    /// <summary>
    /// 当前行缓冲
    /// </summary>
    private readonly StringBuilder buffer = new StringBuilder();
    /// <summary>
    /// 当前行的字符数，包含已写入<see cref="codeOut"/>的字符。
    /// </summary>
    private int column = 0;
    /// <summary>
    /// -1表示缓冲区为空，否则表示在换行后需要写入的缩进数
    /// </summary>
    private int indentLevel = -1;
    /// <summary>
    /// null表示缓存区位空，否则表示下一次flush时的参数（也表示下一次Append需要Flush）。
    /// </summary>
    private FlushType? nextFlush;

    public LineWrapper(StringBuilder codeOut, string indent, int columnLimit) {
        this.codeOut = codeOut ?? throw new ArgumentNullException(nameof(codeOut));
        this.indent = indent ?? throw new ArgumentNullException(nameof(indent));
        this.columnLimit = columnLimit;
    }

    public void Reset() {
        buffer.Clear();
        column = 0;
        indentLevel = -1;
        nextFlush = null;
    }

    /// <summary>
    /// 最后一个写入的字符串
    /// </summary>
    public char LastChar => codeOut.Length == 0 ? (char)0 : codeOut[codeOut.Length - 1];

    /// <summary>
    /// 追加文本内容
    /// </summary>
    /// <param name="s"></param>
    /// <exception cref="IllegalStateException"></exception>
    public void Append(string s) {
        if (nullWriter) {
            return;
        }
        if (nextFlush != null) {
            int nextNewline = s.IndexOf('\n');
            // 不包含换行符，且当前行能写入
            if (nextNewline == -1 && column + s.Length <= columnLimit) {
                buffer.Append(s);
                column += s.Length;
                return;
            }
            // 如果直接追加文本可能超过行长度，则进行Wrap；否则使用nextFlush清空缓存
            bool wrap = nextNewline == -1 || column + s.Length > columnLimit;
            Flush(wrap ? FlushType.Wrap : nextFlush.Value);
        }

        codeOut.Append(s);
        int lastNewline = s.LastIndexOf('\n');
        column = lastNewline != -1
            ? s.Length - lastNewline - 1
            : column + s.Length;
    }

    /// <summary>
    /// 写入一个空格或换行符
    /// </summary>
    /// <param name="indentLevel"></param>
    /// <exception cref="IllegalStateException"></exception>
    public void WrappingSpace(int indentLevel) {
        if (this.nextFlush != null) Flush(this.nextFlush.Value); // 先Flush之前的内容
        this.column++; // 即使将空格延迟到下一次调用flush()，也要增加列
        this.nextFlush = FlushType.Space;
        this.indentLevel = indentLevel;
    }

    /// <summary>
    /// 如果行将超过其限制，则发出换行符，否则什么都不做
    /// </summary>
    public void ZeroWidthSpace(int indentLevel) {
        if (column == 0) return;
        if (this.nextFlush != null) Flush(this.nextFlush.Value); // 先Flush之前的内容
        this.nextFlush = FlushType.Empty;
        this.indentLevel = indentLevel;
    }

    /// <summary>
    /// 刷新缓冲区
    /// </summary>
    public void Flush() {
        if (nextFlush != null) Flush(nextFlush.Value);
    }

    private void Flush(FlushType flushType) {
        if (nullWriter) {
            return;
        }
        switch (flushType) {
            case FlushType.Wrap: {
                codeOut.Append('\n');
                for (int i = 0; i < indentLevel; i++) {
                    codeOut.Append(indent);
                }
                column = indentLevel <= 0 ? 0 : indentLevel * indent.Length;
                column += buffer.Length;
                break;
            }
            case FlushType.Space: {
                codeOut.Append(' ');
                break;
            }
            case FlushType.Empty: {
                break;
            }
            default: throw new AssertionError();
        }
        codeOut.Append(buffer);
        buffer.Length = 0;
        indentLevel = -1;
        nextFlush = null;
    }

    private enum FlushType
    {
        /// <summary>
        /// Flush时进行换行缩进(比如方法参数换行对齐)
        /// </summary>
        Wrap,

        /// <summary>
        /// Flush时追加空格
        /// </summary>
        Space,

        /// <summary>
        /// Flush时无操作
        /// </summary>
        Empty
    }
}