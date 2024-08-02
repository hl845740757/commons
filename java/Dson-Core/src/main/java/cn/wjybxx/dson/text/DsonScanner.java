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

import cn.wjybxx.base.ObjectUtils;
import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.base.pool.ObjectPool;

import java.util.HexFormat;
import java.util.List;
import java.util.Objects;

/**
 * @author wjybxx
 * date - 2023/6/2
 */
public final class DsonScanner implements AutoCloseable {

    private static final List<DsonTokenType> STRING_TOKEN_TYPES = List.of(DsonTokenType.STRING, DsonTokenType.UNQUOTE_STRING);
    private static final ObjectPool<StringBuilder> STRING_BUILDER_POOL = ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL;

    private DsonCharStream charStream;
    private StringBuilder pooledStringBuilder;
    private final CharBuffer hexBuffer = new CharBuffer(4);

    public DsonScanner(CharSequence dson) {
        this(new StringCharStream(dson));
    }

    public DsonScanner(DsonCharStream charStream) {
        this.charStream = Objects.requireNonNull(charStream);
        this.pooledStringBuilder = STRING_BUILDER_POOL.acquire();
    }

    @Override
    public void close() {
        if (charStream != null) {
            charStream.close();
            charStream = null;
        }
        if (pooledStringBuilder != null) {
            STRING_BUILDER_POOL.release(pooledStringBuilder);
            pooledStringBuilder = null;
        }
    }

    public DsonToken nextToken() {
        return nextToken(false);
    }

    /**
     * @param skipValue 是否跳过值解析；如果为true，则仅扫描而不截取内容解析；这对于快速扫描确定位置时特别有用
     */
    public DsonToken nextToken(boolean skipValue) {
        DsonCharStream buffer = charStream;
        if (buffer == null) {
            throw new DsonParseException("Scanner closed");
        }
        while (true) {
            int c = skipWhitespace();
            if (c == -1) {
                return new DsonToken(DsonTokenType.EOF, "eof", getPosition());
            }
            switch (c) {
                case '{':
                    return new DsonToken(DsonTokenType.BEGIN_OBJECT, "{", getPosition());
                case '}':
                    return new DsonToken(DsonTokenType.END_OBJECT, "}", getPosition());
                case '[':
                    return new DsonToken(DsonTokenType.BEGIN_ARRAY, "[", getPosition());
                case ']':
                    return new DsonToken(DsonTokenType.END_ARRAY, "]", getPosition());
                case ':':
                    return new DsonToken(DsonTokenType.COLON, ":", getPosition());
                case ',':
                    return new DsonToken(DsonTokenType.COMMA, ",", getPosition());
                case '@':
                    return parseTypeToken(skipValue);
                case '"': { // 普通文本段
                    int indent = buffer.getColumn() - 1;
                    if (buffer.read() != '"') {
                        buffer.unread();
                    } else if (buffer.read() != '"') {
                        buffer.unread();
                        buffer.unread();
                    } else {
                        if (buffer.read() != -2) {
                            throw new DsonParseException("Illegal text block start: missing new line after opening quotes, position: " + getPosition());
                        }
                        return new DsonToken(DsonTokenType.STRING, scanSimpleText(indent, skipValue), getPosition());
                    }
                    return new DsonToken(DsonTokenType.STRING, scanString(skipValue), getPosition());
                }
                case '/': {
                    skipComment();
                    continue;
                }
                default: {
                    return new DsonToken(DsonTokenType.UNQUOTE_STRING, scanUnquotedString((char) c, skipValue), getPosition());
                }
            }
        }
    }

    // region common

    private static void ensureStringToken(DsonTokenType tokenType, int position) {
        if (tokenType != DsonTokenType.UNQUOTE_STRING && tokenType != DsonTokenType.STRING) {
            throw invalidTokenType(STRING_TOKEN_TYPES, tokenType, position);
        }
    }

    private static DsonParseException invalidTokenType(List<DsonTokenType> expected, DsonTokenType tokenType, int position) {
        return new DsonParseException(String.format("Invalid Dson Token. Position: %d. Expected: %s. Found: '%s'.",
                position, expected, tokenType));
    }

    private static DsonParseException invalidClassName(String c, int position) {
        return new DsonParseException(String.format("Invalid className. Position: %d. ClassName: '%s'.", position, c));
    }

    private static DsonParseException invalidEscapeSequence(int c, int position) {
        return new DsonParseException(String.format("Invalid escape sequence. Position: %d. Character: '\\%c'.", position, c));
    }

    private static DsonParseException spaceRequired(int position) {
        return new DsonParseException(String.format("Space is required. Position: %d.", position));
    }

    private StringBuilder getCachedStringBuilder() {
        pooledStringBuilder.setLength(0);
        return pooledStringBuilder;
    }

    private int getPosition() {
        return charStream.getPosition();
    }

    // endregion

    // region header

    private DsonToken parseTypeToken(boolean skipValue) {
        DsonCharStream buffer = charStream;
        int firstChar = buffer.read();
        if (firstChar < 0) {
            throw invalidClassName("@", getPosition());
        }
        // '@{' 对应的是header，header可能是 {k:v} 或 @{clsName} 简写形式 -- 需要判别
        if (firstChar == '{') {
            return scanHeader();
        }
        // '@"""' 对应Dson文本块，缩进由行首确定
        if (firstChar == '"') {
            if (buffer.read() != '"' || buffer.read() != '"') {
                throw new DsonParseException("Illegal text block start: missing quotes, position: " + getPosition());
            }
            if (buffer.read() != -2) {
                throw new DsonParseException("Illegal text block start: missing new line after opening quotes, position: " + getPosition());
            }
            buffer.unread();
            return new DsonToken(DsonTokenType.STRING, scanDsonText(skipValue), getPosition());
        }
        // '@' 对应的是内建值类型，@i @L ...
        return scanBuiltinValue(firstChar, skipValue);
    }

    /** header不处理跳过逻辑 -- 1.header信息很重要 2.header比例较低 */
    private DsonToken scanHeader() {
        DsonCharStream buffer = this.charStream;
        final int beginPos = buffer.getPosition();
        int firstChar = skipWhitespace(); // {}下跳过空白字符
        if (firstChar < 0) {
            throw invalidClassName("@{", getPosition());
        }
        String className;
        if (firstChar == '"') {
            className = scanString(false);
        } else {
            // 非双引号模式下，只能由安全字符构成
            if (DsonTexts.isUnsafeStringChar(firstChar)) {
                throw invalidClassName(Character.toString((char) firstChar), getPosition());
            }
            StringBuilder sb = getCachedStringBuilder();
            sb.append((char) firstChar);
            if (!isClsNameHeader(buffer, sb, beginPos)) {
                int c;
                while ((c = buffer.read()) >= 0) {
                    if (DsonTexts.isUnsafeStringChar(c)) {
                        break;
                    }
                    sb.append((char) c);
                }
                if (c < 0 || DsonTexts.isUnsafeStringChar(c)) {
                    buffer.unread();
                }
            }
            className = sb.toString();
        }
        // {} 模式下，下一个字符必须是 ':' 或 '}‘
        int nextChar = skipWhitespace();
        if (nextChar == ':') { // @{k: V} Object样式，需要回退
            while (buffer.getPosition() > beginPos) {
                buffer.unread();
            }
            return new DsonToken(DsonTokenType.BEGIN_HEADER, "{", beginPos);
        } else if (nextChar == '}') { // @{clsName} 简单缩写形式
            return new DsonToken(DsonTokenType.SIMPLE_HEADER, className, getPosition());
        } else {
            throw invalidClassName(className, getPosition());
        }
    }

    /** 如果在 '}' 之前没有出现':' ，我们就认为是clsName */
    private boolean isClsNameHeader(DsonCharStream buffer, StringBuilder sb, int beginPos) {
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == ':') break; // @{k: va}结构
            if (c == '}') break; // @{clsName}缩写
            sb.append((char) c);
        }
        buffer.unread(); // 退出token字符
        if (c == '}') {
            // 删除尾部缩进
            int length = sb.length();
            while (DsonTexts.isIndentChar(sb.charAt(length - 1))) {
                length--;
            }
            sb.setLength(length);
            return true;
        } else {
            // 失败回退 c == ':' or c == -1
            while (buffer.getPosition() > beginPos) {
                buffer.unread();
            }
            return false;
        }
    }

    /** 内建值无引号，且类型标签后必须是空格或换行缩进 */
    private DsonToken scanBuiltinValue(int firstChar, boolean skipValue) {
        assert firstChar != '"';
        // 非双引号模式下，只能由安全字符构成
        if (DsonTexts.isUnsafeStringChar(firstChar)) {
            throw invalidClassName(Character.toString((char) firstChar), getPosition());
        }
        DsonCharStream buffer = this.charStream;
        StringBuilder sb = getCachedStringBuilder();
        sb.append((char) firstChar);
        int c;
        while ((c = buffer.read()) >= 0) {
            if (DsonTexts.isUnsafeStringChar(c)) {
                break;
            }
            sb.append((char) c);
        }
        if (c == -2) {
            buffer.unread();
        } else if (c != ' ') {
            throw spaceRequired(getPosition());
        }
        String className = sb.toString();
        if (ObjectUtils.isBlank(className)) {
            throw invalidClassName(className, getPosition());
        }
        return onReadClassName(className, skipValue);
    }

    private DsonToken onReadClassName(String className, boolean skipValue) {
        final int position = getPosition();
        switch (className) {
            case DsonTexts.LABEL_INT32 -> {
                DsonToken nextToken = nextToken(skipValue);
                ensureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.INT32, null, getPosition());
                }
                return new DsonToken(DsonTokenType.INT32, DsonTexts.parseInt32(nextToken.stringValue()), getPosition());
            }
            case DsonTexts.LABEL_INT64 -> {
                DsonToken nextToken = nextToken(skipValue);
                ensureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.INT64, null, getPosition());
                }
                return new DsonToken(DsonTokenType.INT64, DsonTexts.parseInt64(nextToken.stringValue()), getPosition());
            }
            case DsonTexts.LABEL_FLOAT -> {
                DsonToken nextToken = nextToken(skipValue);
                ensureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.FLOAT, null, getPosition());
                }
                return new DsonToken(DsonTokenType.FLOAT, DsonTexts.parseFloat(nextToken.stringValue()), getPosition());
            }
            case DsonTexts.LABEL_DOUBLE -> {
                DsonToken nextToken = nextToken(skipValue);
                ensureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.DOUBLE, null, getPosition());
                }
                return new DsonToken(DsonTokenType.DOUBLE, DsonTexts.parseDouble(nextToken.stringValue()), getPosition());
            }
            case DsonTexts.LABEL_BOOL -> {
                DsonToken nextToken = nextToken(skipValue);
                ensureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.BOOL, null, getPosition());
                }
                return new DsonToken(DsonTokenType.BOOL, DsonTexts.parseBool(nextToken.stringValue()), getPosition());
            }
            case DsonTexts.LABEL_NULL -> {
                DsonToken nextToken = nextToken(skipValue);
                ensureStringToken(nextToken.type, position);
                if (skipValue) {
                    return new DsonToken(DsonTokenType.NULL, null, getPosition());
                }
                DsonTexts.checkNullString(nextToken.stringValue());
                return new DsonToken(DsonTokenType.NULL, null, getPosition());
            }
            case DsonTexts.LABEL_STRING -> {
                DsonToken nextToken = nextToken(skipValue);
                ensureStringToken(nextToken.type, position);
                return new DsonToken(DsonTokenType.STRING, nextToken.stringValue(), getPosition());
            }
            case DsonTexts.LABEL_STRING_LINE -> {
                return new DsonToken(DsonTokenType.STRING, scanSingleLineText(skipValue), getPosition());
            }
            case DsonTexts.LABEL_BINARY -> {
                return new DsonToken(DsonTokenType.BINARY, scanBinary(skipValue), getPosition());
            }
        }
        return new DsonToken(DsonTokenType.BUILTIN_STRUCT, className, getPosition());
    }

    // endregion

    // region 字符串

    /** @return 如果跳到文件尾则返回 -1 */
    private int skipWhitespace() {
        DsonCharStream buffer = this.charStream;
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) {
                continue;
            }
            if (c == '/') {
                skipComment();
                continue;
            }
            if (!DsonTexts.isIndentChar(c)) {
                break;
            }
        }
        return c;
    }

    /** 跳过双斜杠'//'注释 */
    private void skipComment() {
        DsonCharStream buffer = this.charStream;
        int nextChar = buffer.read();
        if (nextChar != '/') {
            throw new DsonParseException("invalid comment format: Single slash, position: " + getPosition());
        }
        buffer.skipLine();
    }

    /** 扫描字节数组 */
    private byte[] scanBinary(boolean skipValue) {
        StringBuilder sb = getCachedStringBuilder();
        final int firstChar = skipWhitespace();
        if (firstChar != '"') {
            throw new DsonParseException("invalid binary format, position: " + getPosition());
        }
        scanString(sb);
        // 可直接根据StringBuilder解析字节数组 -- 避免额外字符串构建
        return skipValue ? null : HexFormat.of().parseHex(sb);
    }

    /**
     * 扫描无引号字符串，无引号字符串不支持切换到独立行
     * （该方法只使用扫描元素，不适合扫描标签）
     *
     * @param firstChar 第一个非空白字符
     * @param skipValue 是否跳过值解析
     */
    private String scanUnquotedString(final char firstChar, boolean skipValue) {
        if (skipValue) {
            skipUnquotedString();
            return null;
        }
        StringBuilder sb = getCachedStringBuilder();
        scanUnquotedString(firstChar, sb);
        return sb.toString();
    }

    /** 无引号字符串应该的占比是极高的，skip值得处理 */
    private void skipUnquotedString() {
        DsonCharStream buffer = this.charStream;
        int c;
        while ((c = buffer.read()) >= 0) {
            if (DsonTexts.isUnsafeStringChar(c)) {
                break;
            }
        }
        buffer.unread();
    }

    private void scanUnquotedString(char firstChar, StringBuilder sb) {
        DsonCharStream buffer = this.charStream;
        sb.append(firstChar);
        int c;
        while ((c = buffer.read()) >= 0) {
            if (DsonTexts.isUnsafeStringChar(c)) {
                break;
            }
            sb.append((char) c);
        }
        buffer.unread();
    }

    /**
     * 扫描双引号字符串
     */
    private String scanString(boolean skipValue) {
        StringBuilder sb = getCachedStringBuilder();
        scanString(sb);
        return skipValue ? null : sb.toString();
    }

    private void scanString(StringBuilder sb) {
        DsonCharStream buffer = this.charStream;
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) {
                continue;
            }
            if (c == '"') { // 结束
                return;
            } else if (c == '\\') { // 处理转义字符
                doEscape(buffer, sb);
            } else {
                sb.append((char) c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    /** 扫描单行纯文本 */
    private String scanSingleLineText(boolean skipValue) {
        if (skipValue) {
            charStream.skipLine();
            return null;
        }
        StringBuilder sb = getCachedStringBuilder();
        scanSingleLineText(sb);
        return sb.toString();
    }

    private void scanSingleLineText(StringBuilder sb) {
        DsonCharStream buffer = this.charStream;
        int c;
        while ((c = buffer.read()) >= 0) {
            sb.append((char) c);
        }
        buffer.unread();
    }

    /**
     * 扫描文本段 -- """
     *
     * @param indent 缩进数量
     */
    private String scanSimpleText(int indent, boolean skipValue) {
        if (skipValue) {
            skipSimpleText(indent);
            return null;
        }
        StringBuilder sb = getCachedStringBuilder();
        scanSimpleText(sb, indent);
        return sb.toString();
    }

    private void skipSimpleText(int indent) {
        DsonCharStream buffer = this.charStream;
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) { // 空行
                continue;
            }
            // 处理缩进
            do {
                if (buffer.getColumn() > indent) {
                    break;
                }
                if (!DsonTexts.isIndentChar(c)) {
                    throw new DsonParseException("Line does not start with the same whitespace as the opening line of the raw string literal, position: " + getPosition());
                }
            } while ((c = buffer.read()) >= 0);
            // 空行
            if (c < 0) {
                continue;
            }
            // 处理结束符
            int position = buffer.getPosition();
            if (c == '"'
                    && buffer.read() == '"'
                    && buffer.read() == '"') {
                if (buffer.read() == '"') { // 超过3个引号
                    throw new DsonParseException("Illegal text block end: excessive quotes, position: " + getPosition());
                }
                buffer.unread();
                return; // 结束
            }
            // 回退到c对应的位置
            while (buffer.getPosition() > position) {
                buffer.unread();
            }
            // 跳过后续
            buffer.skipLine();
        }
    }

    private void scanSimpleText(StringBuilder sb, int indent) {
        DsonCharStream buffer = this.charStream;
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) { // 空行
                sb.append('\n');
                continue;
            }
            // 处理缩进
            do {
                if (buffer.getColumn() > indent) {
                    break;
                }
                if (!DsonTexts.isIndentChar(c)) {
                    throw new DsonParseException("Line does not start with the same whitespace as the opening line of the raw string literal, position: " + getPosition());
                }
            } while ((c = buffer.read()) >= 0);
            // 空行
            if (c < 0) {
                if (c == -1) {
                    break; // eof
                }
                sb.append('\n');
                continue;
            }
            // 处理结束符
            int position = buffer.getPosition();
            if (c == '"'
                    && buffer.read() == '"'
                    && buffer.read() == '"') {
                if (buffer.read() == '"') { // 超过3个引号
                    throw new DsonParseException("Illegal text block end: excessive quotes, position: " + getPosition());
                }
                buffer.unread();
                sb.setLength(sb.length() - 1); // 去除最后一个换行符
                return; // 结束
            }
            // 回退到c对应的位置
            while (buffer.getPosition() > position) {
                buffer.unread();
            }
            sb.append((char) c);
            while ((c = buffer.read()) >= 0) {
                sb.append((char) c);
            }
            // c < 0
            if (c == -1) {
                break; // Eof
            }
            sb.append('\n');
        }
        throw new DsonParseException("End of file in Dson string.");
    }


    /** 扫描Dson文本段 -- @""" */
    private String scanDsonText(boolean skipValue) {
        if (skipValue) {
            skipDsonText();
            return null;
        }
        StringBuilder sb = getCachedStringBuilder();
        scanDsonText(sb);
        return sb.toString();
    }

    private void skipDsonText() {
        DsonCharStream buffer = this.charStream;
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2 && readLineHead(buffer) == LineHead.END_OF_TEXT) {
                break;
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    private void scanDsonText(StringBuilder sb) {
        DsonCharStream buffer = this.charStream;
        int c;
        while ((c = buffer.read()) != -1) {
            if (c == -2) {
                LineHead lineHead = readLineHead(buffer);
                if (lineHead == LineHead.END_OF_TEXT) { // 读取结束
                    return;
                }
                if (lineHead == LineHead.COMMENT) { // 注释行
                    buffer.skipLine();
                } else if (lineHead == LineHead.APPEND_LINE) { // 开启新行
                    sb.append('\n');
                } else if (lineHead == LineHead.SWITCH_MODE) { // 进入转义模式
                    switch2EscapeMode(buffer, sb);
                }
            } else {
                sb.append((char) c);
            }
        }
        throw new DsonParseException("End of file in Dson string.");
    }

    /** 转义模式 - 单行有效 */
    private void switch2EscapeMode(DsonCharStream buffer, StringBuilder sb) {
        int c;
        while ((c = buffer.read()) >= 0) {
            if (c == '\\') {
                doEscape(buffer, sb);
            } else {
                sb.append((char) c);
            }
        }
        buffer.unread();
    }

    private LineHead readLineHead(DsonCharStream buffer) {
        int c;
        while ((c = buffer.read()) >= 0) {
            if (DsonTexts.isIndentChar(c)) {
                continue;
            }
            if (c == '/') { // 注释行
                skipComment();
                return LineHead.COMMENT;
            }
            // 文本行必须'@'标记开始
            if (c != '@') {
                throw new DsonParseException("invalid text line, position: " + getPosition());
            }
            c = buffer.read();
            if (c < 0) {
                throw new DsonParseException("invalid text line, position: " + getPosition());
            }
            // 处理结束符
            if (c == '"') {
                if (buffer.read() != '"' || buffer.read() != '"') {
                    throw new DsonParseException("invalid text line, position: " + getPosition());
                }
                if (buffer.read() == '"') { // 超过3个引号
                    throw new DsonParseException("Illegal text block end: excessive quotes, position: " + getPosition());
                }
                buffer.unread();
                return LineHead.END_OF_TEXT;
            }
            LineHead lineHead = switch (c) {
                case DsonTexts.HEAD_APPEND_LINE -> LineHead.APPEND_LINE;
                case DsonTexts.HEAD_APPEND -> LineHead.APPEND;
                case DsonTexts.HEAD_SWITCH_MODE -> LineHead.SWITCH_MODE;
                default -> throw new DsonParseException("invalid text line, position: " + getPosition());
            };
            // 如果未达文件尾，必须是空格或换行
            c = buffer.read();
            if (c < 0) {
                buffer.unread();
            } else if (c != ' ') {
                throw spaceRequired(getPosition());
            }
            return lineHead;
        }
        buffer.unread(); // 空行
        return LineHead.COMMENT;
    }

    private void doEscape(DsonCharStream buffer, StringBuilder sb) {
        final int position = getPosition();
        final int c = readEscapeChar(buffer, position);
        switch (c) {
            case '"' -> sb.append('"'); // 双引号字符串下，双引号需要转义
            case '\\' -> sb.append('\\');
            case 'b' -> sb.append('\b');
            case 'f' -> sb.append('\f');
            case 'n' -> sb.append('\n');
            case 'r' -> sb.append('\r');
            case 't' -> sb.append('\t');
            case 'u' -> {
                // unicode字符，char是2字节，固定编码为4个16进制数，从高到底
                CharBuffer hexBuffer = this.hexBuffer;
                hexBuffer.clear();
                hexBuffer.write((char) readEscapeChar(buffer, position));
                hexBuffer.write((char) readEscapeChar(buffer, position));
                hexBuffer.write((char) readEscapeChar(buffer, position));
                hexBuffer.write((char) readEscapeChar(buffer, position));
                sb.append((char) Integer.parseInt(hexBuffer, 0, 4, 16));
            }
            default -> throw invalidEscapeSequence(c, position);
        }
    }

    /** 读取下一个要转义的字符 */
    private static int readEscapeChar(DsonCharStream buffer, int position) {
        int c = buffer.read();
        if (c >= 0) {
            return c;
        }
        throw invalidEscapeSequence('\\', position);
    }

    // endregion

}