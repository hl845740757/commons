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
using System.Text;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Internal;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// Dson文本扫描器
/// </summary>
public sealed class DsonScanner : IDisposable
{
    private static readonly List<DsonTokenType> STRING_TOKEN_TYPES =
        CollectionUtil.NewList(DsonTokenType.String, DsonTokenType.UnquoteString);
    private static readonly IObjectPool<StringBuilder> STRING_BUILDER_POOL = ConcurrentObjectPool.SharedStringBuilderPool;

#nullable disable
    private IDsonCharStream _charStream;
    private StringBuilder _pooledStringBuilder;
    private readonly char[] _hexBuffer = new char[4];
#nullable enable

    public DsonScanner(string dson)
        : this(IDsonCharStream.NewCharStream(dson)) {
    }

    public DsonScanner(IDsonCharStream charStream) {
        _charStream = charStream ?? throw new ArgumentNullException(nameof(charStream));
        _pooledStringBuilder = STRING_BUILDER_POOL.Acquire();
    }

    public void Dispose() {
        if (_charStream != null) {
            _charStream.Dispose();
            _charStream = null;
        }
        if (_pooledStringBuilder != null) {
            STRING_BUILDER_POOL.Release(_pooledStringBuilder);
            _pooledStringBuilder = null;
        }
    }

    /// <summary>
    /// 扫描下一个token
    /// </summary>
    /// <param name="skipValue">是否跳过值的解析；如果为true，则仅扫描而不截取内容解析；这对于快速扫描确定位置时特别有用</param>
    /// <returns></returns>
    /// <exception cref="DsonParseException"></exception>
    public DsonToken NextToken(bool skipValue = false) {
        IDsonCharStream buffer = _charStream;
        if (buffer == null) {
            throw new DsonParseException("Scanner closed");
        }
        while (true) {
            int c = SkipWhitespace();
            if (c == -1) {
                return new DsonToken(DsonTokenType.Eof, "eof", Position);
            }
            switch (c) {
                case '{': return new DsonToken(DsonTokenType.BeginObject, "{", Position);
                case '}': return new DsonToken(DsonTokenType.EndObject, "}", Position);
                case '[': return new DsonToken(DsonTokenType.BeginArray, "[", Position);
                case ']': return new DsonToken(DsonTokenType.EndArray, "]", Position);
                case ':': return new DsonToken(DsonTokenType.Colon, ":", Position);
                case ',': return new DsonToken(DsonTokenType.Comma, ",", Position);
                case '@': return ParseTypeToken(skipValue);
                case '"': { // 普通文本段
                    int indent = buffer.Column - 1;
                    if (buffer.Read() != '"') {
                        buffer.Unread();
                    } else if (buffer.Read() != '"') {
                        buffer.Unread();
                        buffer.Unread();
                    } else {
                        if (buffer.Read() != -2) {
                            throw new DsonParseException("Illegal text block start: missing new line after opening quotes, position: " + Position);
                        }
                        return new DsonToken(DsonTokenType.String, ScanSimpleText(indent, skipValue), Position);
                    }
                    return new DsonToken(DsonTokenType.String, ScanString(skipValue), Position);
                }
                case '/': {
                    SkipComment();
                    continue;
                }
                default: {
                    return new DsonToken(DsonTokenType.UnquoteString, ScanUnquotedString((char)c, skipValue), Position);
                }
            }
        }
    }

    #region common

    private static void EnsureStringToken(DsonTokenType tokenType, int position) {
        if (tokenType != DsonTokenType.UnquoteString && tokenType != DsonTokenType.String) {
            throw InvalidTokenType(STRING_TOKEN_TYPES, tokenType, position);
        }
    }

    private static DsonParseException InvalidTokenType(List<DsonTokenType> expected, DsonTokenType tokenType, int position) {
        return new DsonParseException($"Invalid Dson Token. Position: {position}. Expected: {expected}. Found: '{tokenType}'.");
    }

    private static DsonParseException InvalidClassName(string c, int position) {
        return new DsonParseException($"Invalid className. Position: {position}. ClassName: '{c}'.");
    }

    private static DsonParseException InvalidEscapeSequence(int c, int position) {
        return new DsonParseException($"Invalid escape sequence. Position: {position}. Character: '\\{c}'.");
    }

    private static DsonParseException SpaceRequired(int position) {
        return new DsonParseException(($"Space is required. Position: {position}."));
    }

    private StringBuilder GetCachedStringBuilder() {
        _pooledStringBuilder.Length = 0;
        return _pooledStringBuilder;
    }

    private int Position => _charStream.Position;

    #endregion

    #region header

    private DsonToken ParseTypeToken(bool skipValue) {
        IDsonCharStream buffer = _charStream;
        int firstChar = buffer.Read();
        if (firstChar < 0) {
            throw InvalidClassName("@", Position);
        }
        // '@{' 对应的是header，header可能是 {k:v} 或 @{clsName} 简写形式 -- 需要判别
        if (firstChar == '{') {
            return ScanHeader();
        }
        // '@"""' 对应Dson文本块，缩进由行首确定
        if (firstChar == '"') {
            if (buffer.Read() != '"' || buffer.Read() != '"') {
                throw new DsonParseException("Illegal text block start: missing quotes, position: " + Position);
            }
            if (buffer.Read() != -2) {
                throw new DsonParseException("Illegal text block start: missing new line after opening quotes, position: " + Position);
            }
            buffer.Unread();
            return new DsonToken(DsonTokenType.String, ScanDsonText(skipValue), Position);
        }
        // '@' 对应的是内建值类型，@i @L ...
        return ScanBuiltinValue(firstChar, skipValue);
    }

    /** header不处理跳过逻辑 -- 1.header信息很重要 2.header比例较低 */
    private DsonToken ScanHeader() {
        IDsonCharStream buffer = this._charStream;
        int beginPos = buffer.Position;
        int firstChar = SkipWhitespace(); // {}下跳过空白字符
        if (firstChar < 0) {
            throw InvalidClassName("@{", Position);
        }
        string className;
        if (firstChar == '"') {
            className = ScanString(false)!;
        } else {
            // 非双引号模式下，只能由安全字符构成
            if (DsonTexts.IsUnsafeStringChar(firstChar)) {
                throw InvalidClassName(char.ToString((char)firstChar), Position);
            }
            StringBuilder sb = GetCachedStringBuilder();
            sb.Append((char)firstChar);
            if (!IsClsNameHeader(buffer, sb, beginPos)) {
                int c;
                while ((c = buffer.Read()) >= 0) {
                    if (DsonTexts.IsUnsafeStringChar(c)) {
                        break;
                    }
                    sb.Append((char)c);
                }
                if (c < 0 || DsonTexts.IsUnsafeStringChar(c)) {
                    buffer.Unread();
                }
            }
            className = sb.ToString();
        }
        // {} 模式下，下一个字符必须是 ':' 或 '}‘
        int nextChar = SkipWhitespace();
        if (nextChar == ':') { // @{k: V} Object样式，需要回退
            while (buffer.Position > beginPos) {
                buffer.Unread();
            }
            return new DsonToken(DsonTokenType.BeginHeader, "{", beginPos);
        } else if (nextChar == '}') { // @{clsName} 简单缩写形式
            return new DsonToken(DsonTokenType.SimpleHeader, className, Position);
        } else {
            throw InvalidClassName(className, Position);
        }
    }

    /** 如果在 '}' 之前没有出现':' ，我们就认为是clsName */
    private bool IsClsNameHeader(IDsonCharStream buffer, StringBuilder sb, int beginPos) {
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == ':') break; // @{k: va}结构
            if (c == '}') break; // @{clsName}缩写
            sb.Append((char)c);
        }
        buffer.Unread(); // 退出token字符
        if (c == '}') {
            // 删除尾部缩进
            int length = sb.Length;
            while (DsonTexts.IsIndentChar(sb[length - 1])) {
                length--;
            }
            sb.Length = length;
            return true;
        } else {
            // 失败回退 c == ':' or c == -1
            while (buffer.Position > beginPos) {
                buffer.Unread();
            }
            return false;
        }
    }

    /** 内建值无引号，且类型标签后必须是空格或换行缩进 */
    private DsonToken ScanBuiltinValue(int firstChar, bool skipValue) {
        Debug.Assert(firstChar != '"');
        // 非双引号模式下，只能由安全字符构成
        if (DsonTexts.IsUnsafeStringChar(firstChar)) {
            throw InvalidClassName(char.ToString((char)firstChar), Position);
        }
        IDsonCharStream buffer = this._charStream;
        StringBuilder sb = GetCachedStringBuilder();
        sb.Append((char)firstChar);
        int c;
        while ((c = buffer.Read()) >= 0) {
            if (DsonTexts.IsUnsafeStringChar(c)) {
                break;
            }
            sb.Append((char)c);
        }
        if (c == -2) {
            buffer.Unread();
        } else if (c != ' ') {
            throw SpaceRequired(Position);
        }
        string className = sb.ToString();
        if (string.IsNullOrWhiteSpace(className)) {
            throw InvalidClassName(className, Position);
        }
        return OnReadClassName(className, skipValue);
    }

    private DsonToken OnReadClassName(string className, bool skipValue) {
        int position = Position;
        switch (className) {
            case DsonTexts.LabelInt32: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Int32, null, Position);
                }
                UnionValue value = new UnionValue(DsonType.Int32)
                {
                    iValue = DsonTexts.ParseInt32(nextToken.StringValue())
                };
                return new DsonToken(DsonTokenType.Int32, in value, Position);
            }
            case DsonTexts.LabelInt64: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Int64, null, Position);
                }
                UnionValue value = new UnionValue(DsonType.Int64)
                {
                    lValue = DsonTexts.ParseInt64(nextToken.StringValue())
                };
                return new DsonToken(DsonTokenType.Int64, in value, Position);
            }
            case DsonTexts.LabelFloat: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Float, null, Position);
                }
                UnionValue value = new UnionValue(DsonType.Float)
                {
                    fValue = DsonTexts.ParseFloat(nextToken.StringValue())
                };
                return new DsonToken(DsonTokenType.Float, in value, Position);
            }
            case DsonTexts.LabelDouble: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Double, null, Position);
                }
                UnionValue value = new UnionValue(DsonType.Double)
                {
                    dValue = DsonTexts.ParseDouble(nextToken.StringValue())
                };
                return new DsonToken(DsonTokenType.Double, in value, Position);
            }
            case DsonTexts.LabelBool: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Bool, null, Position);
                }
                UnionValue value = new UnionValue(DsonType.Bool)
                {
                    bValue = DsonTexts.ParseBool(nextToken.StringValue())
                };
                return new DsonToken(DsonTokenType.Bool, in value, Position);
            }
            case DsonTexts.LabelNull: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.Null, null, Position);
                }
                DsonTexts.CheckNullString(nextToken.StringValue());
                return new DsonToken(DsonTokenType.Null, null, Position);
            }
            case DsonTexts.LabelString: {
                DsonToken nextToken = NextToken(skipValue);
                EnsureStringToken(nextToken.type, position);
                return new DsonToken(DsonTokenType.String, nextToken.StringValue(), Position);
            }
            case DsonTexts.LabelStringLine: {
                return new DsonToken(DsonTokenType.String, ScanSingleLineText(skipValue), Position);
            }
            case DsonTexts.LabelBinary: {
                return new DsonToken(DsonTokenType.Binary, ScanBinary(skipValue), Position);
            }
        }
        return new DsonToken(DsonTokenType.BuiltinStruct, className, Position);
    }

    #endregion

    #region 字符串

    /// <summary>
    /// 跳过空白字符
    /// </summary>
    /// <returns>如果跳到文件尾则返回 -1</returns>
    private int SkipWhitespace() {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                continue;
            }
            if (c == '/') {
                SkipComment();
                continue;
            }
            if (!DsonTexts.IsIndentChar(c)) {
                break;
            }
        }
        return c;
    }

    /** 跳过双斜杠'//'注释 */
    private void SkipComment() {
        IDsonCharStream buffer = this._charStream;
        int nextChar = buffer.Read();
        if (nextChar != '/') {
            throw new DsonParseException("invalid comment format: Single slash, position: " + Position);
        }
        buffer.SkipLine();
    }

    /** 扫描字节数组 */
    private byte[]? ScanBinary(bool skipValue) {
        StringBuilder sb = GetCachedStringBuilder();
        int firstChar = SkipWhitespace();
        if (firstChar != '"') {
            throw new DsonParseException("invalid binary format, position: " + Position);
        }
        ScanString(sb);
        // 为什么不使用栈上分配Span来解决？因为字节数组可能很大，栈上分配空间可能有问题
        return skipValue ? null : CommonsLang3.DecodeHex(sb);
    }

    /// <summary>
    /// 扫描无引号字符串，无引号字符串不支持切换到独立行
    /// （该方法只使用扫描元素，不适合扫描标签）
    /// </summary>
    /// <param name="firstChar">第一个非空白字符</param>
    /// <param name="skipValue">是否跳过结果</param>
    /// <returns></returns>
    private string? ScanUnquotedString(char firstChar, bool skipValue) {
        if (skipValue) {
            SkipUnquotedString();
            return null;
        }
        StringBuilder sb = GetCachedStringBuilder();
        ScanUnquotedString(firstChar, sb);
        return sb.ToString();
    }

    /** 无引号字符串应该的占比是极高的，skip值得处理 */
    private void SkipUnquotedString() {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) >= 0) {
            if (DsonTexts.IsUnsafeStringChar(c)) {
                break;
            }
        }
        buffer.Unread();
    }

    private void ScanUnquotedString(char firstChar, StringBuilder sb) {
        IDsonCharStream buffer = this._charStream;
        sb.Append(firstChar);
        int c;
        while ((c = buffer.Read()) >= 0) {
            if (DsonTexts.IsUnsafeStringChar(c)) {
                break;
            }
            sb.Append((char)c);
        }
        buffer.Unread();
    }

    /// <summary>
    /// 扫描双引号字符串
    /// </summary>
    /// <param name="skipValue">是否跳过结果</param>
    /// <returns></returns>
    private string? ScanString(bool skipValue) {
        StringBuilder sb = GetCachedStringBuilder();
        ScanString(sb);
        return skipValue ? null : sb.ToString();
    }

    private void ScanString(StringBuilder sb) {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                continue;
            }
            if (c == '"') { // 结束
                return;
            } else if (c == '\\') { // 处理转义字符
                DoEscape(buffer, sb);
            } else {
                sb.Append((char)c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }


    /** 扫描单行纯文本 */
    private string? ScanSingleLineText(bool skipValue) {
        if (skipValue) {
            _charStream.SkipLine();
            return null;
        }
        StringBuilder sb = GetCachedStringBuilder();
        ScanSingleLineText(sb);
        return sb.ToString();
    }

    private void ScanSingleLineText(StringBuilder sb) {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) >= 0) {
            sb.Append((char)c);
        }
        buffer.Unread();
    }

    private string? ScanSimpleText(int indent, bool skipValue) {
        if (skipValue) {
            SkipSimpleText(indent);
            return null;
        }
        StringBuilder sb = GetCachedStringBuilder();
        ScanSimpleText(sb, indent);
        return sb.ToString();
    }

    private void SkipSimpleText(int indent) {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) { // 空行
                continue;
            }
            // 处理缩进
            do {
                if (buffer.Column > indent) {
                    break;
                }
                if (!DsonTexts.IsIndentChar(c)) {
                    throw new DsonParseException(
                        "Line does not start with the same whitespace as the opening line of the raw string literal, position: "
                        + Position);
                }
            } while ((c = buffer.Read()) >= 0);
            // 空行
            if (c < 0) {
                continue;
            }
            // 处理结束符
            int position = buffer.Position;
            if (c == '"'
                && buffer.Read() == '"'
                && buffer.Read() == '"') {
                if (buffer.Read() == '"') { // 超过3个引号
                    throw new DsonParseException("Illegal text block end: excessive quotes, position: " + Position);
                }
                buffer.Unread();
                return; // 结束
            }
            // 回退到c对应的位置
            while (buffer.Position > position) {
                buffer.Unread();
            }
            // 跳过后续
            buffer.SkipLine();
        }
    }

    private void ScanSimpleText(StringBuilder sb, int indent) {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) { // 空行
                sb.Append('\n');
                continue;
            }
            // 处理缩进
            do {
                if (buffer.Column > indent) {
                    break;
                }
                if (!DsonTexts.IsIndentChar(c)) {
                    throw new DsonParseException(
                        "Line does not start with the same whitespace as the opening line of the raw string literal, position: "
                        + Position);
                }
            } while ((c = buffer.Read()) >= 0);
            // 空行
            if (c < 0) {
                if (c == -1) {
                    break; // eof
                }
                sb.Append('\n');
                continue;
            }
            // 处理结束符
            int position = buffer.Position;
            if (c == '"'
                && buffer.Read() == '"'
                && buffer.Read() == '"') {
                if (buffer.Read() == '"') { // 超过3个引号
                    throw new DsonParseException("Illegal text block end: excessive quotes, position: " + Position);
                }
                buffer.Unread();
                sb.Length = (sb.Length - 1); // 去除最后一个换行符
                return; // 结束
            }
            // 回退到c对应的位置
            while (buffer.Position > position) {
                buffer.Unread();
            }
            sb.Append((char)c);
            while ((c = buffer.Read()) >= 0) {
                sb.Append((char)c);
            }
            // c < 0
            if (c == -1) {
                break; // Eof
            }
            sb.Append('\n');
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    /** 扫描Dson文本段 -- @""" */
    private string? ScanDsonText(bool skipValue) {
        if (skipValue) {
            SkipDsonText();
            return null;
        }
        StringBuilder sb = GetCachedStringBuilder();
        ScanDsonText(sb);
        return sb.ToString();
    }

    private void SkipDsonText() {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2 && ReadLineHead(buffer) == LineHead.EndOfText) {
                break;
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    private void ScanDsonText(StringBuilder sb) {
        IDsonCharStream buffer = this._charStream;
        int c;
        while ((c = buffer.Read()) != -1) {
            if (c == -2) {
                LineHead lineHead = ReadLineHead(buffer);
                if (lineHead == LineHead.EndOfText) { // 读取结束
                    return;
                }
                if (lineHead == LineHead.Comment) { // 注释行
                    buffer.SkipLine();
                } else if (lineHead == LineHead.AppendLine) { // 开启新行
                    sb.Append('\n');
                } else if (lineHead == LineHead.SwitchMode) { // 进入转义模式
                    Switch2EscapeMode(buffer, sb);
                }
            } else {
                sb.Append((char)c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    /** 转义模式 - 单行有效 */
    private void Switch2EscapeMode(IDsonCharStream buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.Read()) >= 0) {
            if (c == '\\') {
                DoEscape(buffer, sb);
            } else {
                sb.Append((char)c);
            }
        }
        buffer.Unread();
    }

    private LineHead ReadLineHead(IDsonCharStream buffer) {
        int c;
        while ((c = buffer.Read()) >= 0) {
            if (DsonTexts.IsIndentChar(c)) {
                continue;
            }
            if (c == '/') { // 注释行
                SkipComment();
                return LineHead.Comment;
            }
            // 首字符必须是‘@'
            if (c != '@') {
                throw new DsonParseException("invalid text line, position: " + Position);
            }
            c = buffer.Read();
            if (c < 0) {
                throw new DsonParseException("invalid text line, position: " + Position);
            }
            // 处理结束符
            if (c == '"') {
                if (buffer.Read() != '"' || buffer.Read() != '"') {
                    throw new DsonParseException("invalid text line, position: " + Position);
                }
                if (buffer.Read() == '"') { // 超过3个引号
                    throw new DsonParseException("Illegal text block end: excessive quotes, position: " + Position);
                }
                buffer.Unread();
                return LineHead.EndOfText;
            }
            LineHead lineHead = c switch
            {
                DsonTexts.HeadAppendLine => LineHead.AppendLine,
                DsonTexts.HeadAppend => LineHead.Append,
                DsonTexts.HeadSwitchMode => LineHead.SwitchMode,
                _ => throw new DsonParseException("invalid text line, position: " + Position)
            };
            // 如果未达文件尾，必须是空格或换行
            c = buffer.Read();
            if (c < 0) {
                buffer.Unread();
            } else if (c != ' ') {
                throw SpaceRequired(Position);
            }
            return lineHead;
        }
        buffer.Unread(); // 空行
        return LineHead.Comment;
    }

    private void DoEscape(IDsonCharStream buffer, StringBuilder sb) {
        int position = Position;
        int c = ReadEscapeChar(buffer, position);
        switch (c) {
            case '"':
                sb.Append('"');
                break; // 双引号字符串下，双引号需要转义
            case '\\':
                sb.Append('\\');
                break;
            case 'b':
                sb.Append('\b');
                break;
            case 'f':
                sb.Append('\f');
                break;
            case 'n':
                sb.Append('\n');
                break;
            case 'r':
                sb.Append('\r');
                break;
            case 't':
                sb.Append('\t');
                break;
            case 'u': {
                // unicode字符，char是2字节，固定编码为4个16进制数，从高到底
                char[] hexBuffer = this._hexBuffer;
                hexBuffer[0] = (char)ReadEscapeChar(buffer, position);
                hexBuffer[1] = (char)ReadEscapeChar(buffer, position);
                hexBuffer[2] = (char)ReadEscapeChar(buffer, position);
                hexBuffer[3] = (char)ReadEscapeChar(buffer, position);
                string hex = new string(hexBuffer);
                sb.Append((char)Convert.ToInt32(hex, 16));
                break;
            }
            default: throw InvalidEscapeSequence(c, Position);
        }
    }

    /** 读取下一个要转义的字符 -- 只能换行到合并行 */
    private int ReadEscapeChar(IDsonCharStream buffer, int position) {
        int c = buffer.Read();
        if (c >= 0) {
            return c;
        }
        throw InvalidEscapeSequence('\\', position);
    }

    #endregion
}
}