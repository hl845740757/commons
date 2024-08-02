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

import cn.wjybxx.base.io.ByteBufferUtils;
import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.base.io.StringBuilderWriter;
import cn.wjybxx.dson.io.DsonIOException;

import java.io.Writer;
import java.util.Arrays;

/**
 * 该接口与{@link DsonScanner}对应
 * Printer和Scanner一样是非结构化的，由外层来实现结构化；
 *
 * @author wjybxx
 * date - 2023/6/5
 */
@SuppressWarnings("unused")
public final class DsonPrinter implements AutoCloseable {

    /** 默认共享的缩进缓存 -- 4空格 */
    private static final char[] sharedIndentionArray = "    ".toCharArray();

    private final DsonTextWriterSettings settings;
    private final Writer writer;
    private final boolean autoClose;

    /** 行缓冲，减少同步写操作 */
    private StringBuilder builder;
    /** 是否是Writer内部的builder */
    private final boolean backingBuilder;

    /** 缩进字符缓存，减少字符串构建 */
    private char[] indentionArray = sharedIndentionArray;
    /** 结构体缩进 -- 默认的body缩进 */
    private int structIndent = 0;

    /** 当前行号 - 1开始 */
    private int ln;
    /** 当前列号 */
    private int column;

    public DsonPrinter(DsonTextWriterSettings settings, Writer writer) {
        this(settings, writer, settings.autoClose);
    }

    public DsonPrinter(DsonTextWriterSettings settings, Writer writer, boolean autoClose) {
        this.settings = settings;
        this.writer = writer;
        this.autoClose = autoClose;

        // 初始化
        if (settings.accessBackingBuilder && writer instanceof StringBuilderWriter sbWriter) {
            builder = sbWriter.getBuilder();
            backingBuilder = true;
        } else {
            builder = ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL.acquire();
            backingBuilder = false;
        }
    }

    // region 属性

    public Writer getWriter() {
        return writer;
    }

    /** 当前行号 - 初始1 */
    public int getLn() {
        return ln;
    }

    /** 当前列数 - 初始0 */
    public int getColumn() {
        return column;
    }

    /** 获取结构化输出body的最佳开始位置 */
    public int getPrettyBodyColum() {
        return structIndent;
    }

    // endregion

    // region 普通打印

    /**
     * tab增加的列不是固定的...
     * 所以其它打印字符串的方法都必须调用该方法，一定程度上降低了性能，不能批量拷贝
     */
    public void print(char c) {
        if (c == '\n') {
            throw new IllegalArgumentException("println is required");
        }
        builder.append(c);
        if (c == '\t') {
            column--;
            column += (4 - (column % 4)); // -1 % 4 => -1
        } else {
            column += 1;
        }
    }

    /** 打印高平面码点 */
    public void printHpmCodePoint(char high, char low) {
        builder.append(high);
        builder.append(low);
        column += 1;
    }

    public void print(char[] cBuffer) {
        for (char c : cBuffer) {
            print(c);
        }
    }

    public void print(char[] cBuffer, int offset, int len) {
        ByteBufferUtils.checkBuffer(cBuffer.length, offset, len);
        for (int idx = offset, end = offset + len; idx < end; idx++) {
            print(cBuffer[idx]);
        }
    }

    public void print(CharSequence text) {
        for (int idx = 0, end = text.length(); idx < end; idx++) {
            print(text.charAt(idx));
        }
    }

    /** c必须是安全字符，不可以包含tab和换行符 */
    public void fastPrint(char c) {
        builder.append(c);
        column++;
    }

    /** @param cBuffer 内容中无tab字符 */
    public void fastPrint(char[] cBuffer) {
        builder.append(cBuffer);
        column += cBuffer.length;
    }

    /** @param cBuffer 内容中无tab字符 */
    public void fastPrint(char[] cBuffer, int offset, int len) {
        builder.append(cBuffer, offset, len);
        column += len;
    }

    /** @param text 内容中无tab字符 */
    public void fastPrint(CharSequence text) {
        builder.append(text);
        column += text.length();
    }

    /** @param text 内容中无tab字符 */
    public void fastPrintRange(CharSequence text, int start, int end) {
        builder.append(text, start, end);
        column += (end - start);
    }

    // endregion

    // region dson

    public void printBeginObject() {
        builder.append('{');
        column += 1;
    }

    public void printEndObject() {
        builder.append('}');
        column += 1;
    }

    public void printBeginArray() {
        builder.append('[');
        column += 1;
    }

    public void printEndArray() {
        builder.append(']');
        column += 1;
    }

    public void printBeginHeader() {
        builder.append("@{");
        column += 2;
    }

    /** 打印冒号 */
    public void printColon() {
        builder.append(':');
        column += 1;
    }

    /** 打印逗号 */
    public void printComma() {
        builder.append(',');
        column += 1;
    }

    /** 打印可能需要转义的字符 */
    public void printEscaped(char c, boolean unicodeChar) {
        StringBuilder sb = builder;
        switch (c) {
            case '\"' -> {
                sb.append('\\');
                sb.append('"');
                column += 2;
            }
            case '\\' -> {
                sb.append('\\');
                sb.append('\\');
                column += 2;
            }
            case '\b' -> {
                sb.append('\\');
                sb.append('b');
                column += 2;
            }
            case '\f' -> {
                sb.append('\\');
                sb.append('f');
                column += 2;
            }
            case '\n' -> {
                sb.append('\\');
                sb.append('n');
                column += 2;
            }
            case '\r' -> {
                sb.append('\\');
                sb.append('r');
                column += 2;
            }
            case '\t' -> {
                sb.append('\\');
                sb.append('t');
                column += 2;
            }
            default -> {
                if (unicodeChar && (c < 32 || c > 126)) {
                    sb.append('\\');
                    sb.append('u');
                    sb.append(Integer.toHexString(0x10000 + (int) c), 1, 5);
                    column += 6;
                } else {
                    sb.append(c);
                    column += 1;
                }
            }
        }
    }

    // endregion

    // region 缩进

    /** 换行 */
    public void println() {
        builder.append(settings.lineSeparator);
        if (builder.length() >= 4096) {
            flush(); // 如果每一行都flush，在数量大的情况下会产生大量的io操作，降低性能
        }
        ln++;
        column = 0;
    }

    /** 打印全部缩进 */
    public void printIndent() {
        printSpaces(structIndent);
    }

    /** 打印一个空格 */
    public void printSpace() {
        builder.append(' ');
        column += 1;
    }

    /** 打印指定数量的空格 -- char可以静默转int，改名安全些 */
    public void printSpaces(int count) {
        if (count < 0) throw new IllegalArgumentException();
        if (count == 0) return;
        if (count <= indentionArray.length) {
            builder.append(indentionArray, 0, count);
        } else {
            char[] chars = new char[count];
            Arrays.fill(chars, ' ');
            builder.append(chars);
            // 尝试缓存下来
            if (count - indentionArray.length <= 8) {
                indentionArray = chars;
            }
        }
        column += count;
    }

    public void indent() {
        structIndent += 2;
        updateIndent();
    }

    public void retract() {
        if (structIndent < 2) {
            throw new IllegalStateException("indent must be called before retract");
        }
        structIndent -= 2;
        updateIndent();
    }

    private void updateIndent() {
        if (structIndent > indentionArray.length) {
            indentionArray = new char[structIndent];
            Arrays.fill(indentionArray, ' ');
        }
    }
    // endregion

    // region io

    public void flush() {
        if (backingBuilder) {
            return;
        }
        try {
            StringBuilder builder = this.builder;
            if (builder.length() > 0) {
                // 显式转cBuffer，避免toString的额外开销
                char[] cBuffer = new char[builder.length()];
                builder.getChars(0, cBuffer.length, cBuffer, 0);

                writer.write(cBuffer, 0, cBuffer.length);
                builder.setLength(0);
            }
            writer.flush();
        } catch (Exception e) {
            throw DsonIOException.wrap(e);
        }
    }

    @Override
    public void close() {
        if (builder == null) {
            return;
        }
        try {
            flush();
            if (!backingBuilder) {
                ConcurrentObjectPool.SHARED_STRING_BUILDER_POOL.release(builder);
            }
            builder = null;
            if (autoClose) {
                writer.close();
            }
        } catch (Exception e) {
            throw DsonIOException.wrap(e);
        }
    }

    // endregion
}