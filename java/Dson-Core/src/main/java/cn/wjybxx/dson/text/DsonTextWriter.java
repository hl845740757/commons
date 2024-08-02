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

import cn.wjybxx.base.pool.ConcurrentObjectPool;
import cn.wjybxx.dson.*;
import cn.wjybxx.dson.internal.CommonsLang3;
import cn.wjybxx.dson.internal.DsonInternals;
import cn.wjybxx.dson.io.DsonChunk;
import cn.wjybxx.dson.types.*;

import java.io.Writer;
import java.util.Objects;

/**
 * 总指导：
 * 1. token字符尽量不换行，eg：'{'、'['、'@'
 * 2. token字符和内容的空格缩进尽量在行尾
 *
 * @author wjybxx
 * date - 2023/4/21
 */
public final class DsonTextWriter extends AbstractDsonWriter {

    private final char[] cBuffer = new char[16];
    private final StyleOut styleOut = new StyleOut();

    private final DsonTextWriterSettings settings;
    private DsonPrinter printer;

    public DsonTextWriter(DsonTextWriterSettings settings, Writer writer) {
        this(settings, writer, settings.autoClose);
    }

    public DsonTextWriter(DsonTextWriterSettings settings, Writer writer, boolean autoClose) {
        super(settings);
        this.settings = settings;
        this.printer = new DsonPrinter(settings, writer, autoClose);

        Context context = newContext(null, DsonContextType.TOP_LEVEL, null);
        setContext(context);
    }

    /** 用于在Object或Array上下文中自行控制换行 -- 打印得更好看 */
    public void println() {
        printer.println();
    }

    @Override
    public DsonTextWriterSettings getSettings() {
        return settings;
    }

    @Override
    protected Context getContext() {
        return (Context) super.getContext();
    }

    @Override
    public void flush() {
        printer.flush();
    }

    @Override
    public void close() {
        Context context = getContext();
        setContext(null);
        while (context != null) {
            Context parent = context.getParent();
            contextPool.release(context);
            context = parent;
        }
        if (printer != null) {
            printer.close();
            printer = null;
        }
        styleOut.reset();
        super.close();
    }

    // region state

    private void writeCurrentName(DsonPrinter printer, DsonType dsonType) {
        Context context = getContext();
        // header与外层对象无缩进，且是匿名属性 -- 如果打印多个header，将保持连续
        if (dsonType == DsonType.HEADER) {
            assert context.count == 0;
            context.headerCount++;
            return;
        }
        // 处理value之间分隔符-换行之前
        if (context.count > 0) {
            printer.print(',');
        }
        // 先处理长度超出，再处理缩进
        if (printer.getColumn() >= settings.softLineLength) {
            printer.println();
        }
        boolean newLine = printer.getColumn() == 0;
        if (context.style == ObjectStyle.INDENT) {
            if (newLine) {
                // 新的一行，只需缩进
                printer.printIndent();
            } else if (context.count == 0 || printer.getColumn() >= printer.getPrettyBodyColum()) {
                // 第一个元素，或当前行超过缩进(含逗号)，需要换行
                printer.println();
                printer.printIndent();
            } else {
                // 当前位置未达缩进位置，不换行
                printer.printSpaces(printer.getPrettyBodyColum() - printer.getColumn());
            }
        } else if (!newLine && context.hasElement()) {
            // 非缩进模式下，元素之间打印一个空格
            printer.print(' ');
        }
        if (context.contextType.isObjectLike()) {
            printString(printer, context.curName, StringStyle.AUTO_QUOTE);
            printer.fastPrint(": ");
        }
        context.count++;
    }

    private void printString(DsonPrinter printer, String value, StringStyle style) {
        final DsonTextWriterSettings settings = this.settings;
        switch (style) {
            case AUTO -> {
                if (canPrintAsUnquote(value, settings)) {
                    printer.fastPrint(value);
                } else if (canPrintAsText(value, settings)) {
                    printText(value);
                } else {
                    printEscaped(value);
                }
            }
            case AUTO_QUOTE -> {
                if (canPrintAsUnquote(value, settings)) {
                    printer.fastPrint(value);
                } else {
                    printEscaped(value);
                }
            }
            case QUOTE -> {
                printEscaped(value);
            }
            case UNQUOTE -> {
                printer.fastPrint(value);
            }
            case TEXT -> {
                if (settings.enableText) {
                    printText(value);
                } else {
                    printEscaped(value);
                }
            }
            case SIMPLE_TEXT -> {
                printSimpleText(value);
            }
            case STRING_LINE -> {
                if (value.indexOf('\n') >= 0) {
                    printEscaped(value);
                } else {
                    printStringLine(value);
                }
            }
            default -> throw new AssertionError(style);
        }
    }

    private static boolean canPrintAsUnquote(String str, DsonTextWriterSettings settings) {
        return DsonTexts.canUnquoteString(str, settings.maxLengthOfUnquoteString)
                && (!settings.unicodeChar || DsonTexts.isASCIIText(str));
    }

    private static boolean canPrintAsText(String str, DsonTextWriterSettings settings) {
        return settings.enableText && (str.length() > settings.textStringLength);
    }

    /** 打印双引号String */
    private void printEscaped(String text) {
        boolean unicodeChar = settings.unicodeChar;
        int softLineLength = settings.softLineLength;
        DsonPrinter printer = this.printer;
        printer.print('"');
        for (int i = 0, length = text.length(); i < length; i++) {
            char c = text.charAt(i);
            if (Character.isSurrogate(c)) {
                printer.printHpmCodePoint(c, text.charAt(++i));
            } else {
                printer.printEscaped(c, unicodeChar);
            }
            if (printer.getColumn() >= softLineLength && (i + 1 < length)) {
                printer.println(); // 双引号字符串换行不能缩进
            }
        }
        printer.print('"');
    }

    /** 纯文本模式打印，要执行换行符 */
    private void printText(String text) {
        int softLineLength = settings.softLineLength;
        DsonPrinter printer = this.printer;
        int headIndent;
        if (settings.textAlignLeft) {
            headIndent = printer.getPrettyBodyColum();
            printer.println();
            printer.printSpaces(headIndent);
            printer.fastPrint("@\"\"\""); // 开始符
            printer.println();
            printer.printSpaces(headIndent);
            printer.fastPrint("@- "); // 首行-避免插入换行符
        } else {
            headIndent = 0;
            printer.println();
            printer.fastPrint("@\"\"\""); // 开始符
            printer.println();
            printer.fastPrint("@- "); // 首行-避免插入换行符
        }
        for (int i = 0, length = text.length(); i < length; i++) {
            char c = text.charAt(i);
            // 要执行文本中的换行符
            if (c == '\n' || (c == '\r' && i + 1 < length && text.charAt(i + 1) == '\n')) {
                printer.println();
                printer.printSpaces(headIndent);
                printer.fastPrint("@| ");
                continue;
            }
            if (Character.isSurrogate(c)) {
                printer.printHpmCodePoint(c, text.charAt(++i));
            } else {
                printer.print(c);
            }
            if (printer.getColumn() >= softLineLength && (i + 1 < length)) {
                printer.println();
                printer.printSpaces(headIndent);
                printer.fastPrint("@- ");
            }
        }
        printer.println();
        printer.printSpaces(headIndent);
        printer.fastPrint("@\"\"\""); // 结束符
    }

    private void printSimpleText(String text) {
        DsonPrinter printer = this.printer;
        int headIndent;
        if (settings.textAlignLeft) {
            headIndent = printer.getPrettyBodyColum();
            printer.println();
            printer.printSpaces(headIndent);
            printer.fastPrint("\"\"\""); // 开始符
            printer.println();
            printer.printSpaces(headIndent);
        } else {
            headIndent = 0;
            printer.println();
            printer.fastPrint("\"\"\""); // 开始符
            printer.println();
        }
        for (int i = 0, length = text.length(); i < length; i++) {
            char c = text.charAt(i);
            // 要执行文本中的换行符
            if (c == '\n' || (c == '\r' && i + 1 < length && text.charAt(i + 1) == '\n')) {
                printer.println();
                printer.printSpaces(headIndent);
                continue;
            }
            if (Character.isSurrogate(c)) {
                printer.printHpmCodePoint(c, text.charAt(++i));
            } else {
                printer.print(c);
            }
        }
        printer.println();
        printer.printSpaces(headIndent);
        printer.fastPrint("\"\"\""); // 结束符
    }

    private void printStringLine(String text) {
        DsonPrinter printer = this.printer;
        printer.fastPrint("@sL ");
        printer.print(text);
        printer.println(); // 换行表示结束
    }

    private void printBinary(byte[] buffer, int offset, int length) {
        DsonPrinter printer = this.printer;
        if (length == 0) {
            printer.print('"');
            printer.print('"');
            return;
        }
        printer.print('"');
        int softLineLength = this.settings.softLineLength;
        // 使用小buffer多次编码代替大的buffer，一方面节省内存，一方面控制行长度
        int segment = cBuffer.length / 2;
        char[] cBuffer = this.cBuffer;
        int loop = length / segment;
        for (int i = 0; i < loop; i++) {
            checkLineLength(printer, softLineLength);
            CommonsLang3.encodeHex(buffer, offset + i * segment, segment, cBuffer, 0);
            printer.fastPrint(cBuffer, 0, cBuffer.length);
        }
        int remain = length - loop * segment;
        if (remain > 0) {
            checkLineLength(printer, softLineLength);
            CommonsLang3.encodeHex(buffer, offset + loop * segment, remain, cBuffer, 0);
            printer.fastPrint(cBuffer, 0, remain * 2);
        }
        printer.print('"');
    }

    private void checkLineLength(DsonPrinter printer, int softLineLength) {
        if (printer.getColumn() >= softLineLength) {
            printer.println();
        }
    }

    // endregion

    // region 简单值

    private void printInt32(DsonPrinter printer, int value, INumberStyle style) {
        StyleOut styleOut = this.styleOut;
        style.toString(value, styleOut.reset());
        if (styleOut.isTyped()) {
            printer.fastPrint("@i ");
        }
        printer.fastPrint(styleOut.getValue());
    }

    private void printInt64(DsonPrinter printer, long value, INumberStyle style) {
        StyleOut styleOut = this.styleOut;
        style.toString(value, styleOut.reset());
        if (styleOut.isTyped()) {
            printer.fastPrint("@L ");
        }
        printer.fastPrint(styleOut.getValue());
    }

    private void printFloat(DsonPrinter printer, float value, INumberStyle style) {
        StyleOut styleOut = this.styleOut;
        style.toString(value, styleOut.reset());
        if (styleOut.isTyped()) {
            printer.fastPrint("@f ");
        }
        printer.fastPrint(styleOut.getValue());
    }

    private void printDouble(DsonPrinter printer, double value, INumberStyle style) {
        StyleOut styleOut = this.styleOut;
        style.toString(value, styleOut.reset());
        if (styleOut.isTyped()) {
            printer.fastPrint("@d ");
        }
        printer.fastPrint(styleOut.getValue());
    }

    @Override
    protected void doWriteInt32(int value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.INT32);
        printInt32(printer, value, style);
    }

    @Override
    protected void doWriteInt64(long value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.INT64);
        printInt64(printer, value, style);
    }

    @Override
    protected void doWriteFloat(float value, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.FLOAT);
        printFloat(printer, value, style);
    }

    @Override
    protected void doWriteDouble(double value, INumberStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.DOUBLE);
        printDouble(printer, value, style);
    }

    @Override
    protected void doWriteBool(boolean value) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BOOL);
        printer.fastPrint(value ? "true" : "false");
    }

    @Override
    protected void doWriteString(String value, StringStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.STRING);
        printString(printer, value, style);
    }

    @Override
    protected void doWriteNull() {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.NULL);
        printer.fastPrint("null");
    }

    @Override
    protected void doWriteBinary(Binary binary) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.fastPrint("@bin ");
        printBinary(binary.unsafeBuffer(), 0, binary.unsafeBuffer().length);
    }

    @Override
    protected void doWriteBinary(DsonChunk chunk) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.BINARY);
        printer.fastPrint("@bin ");
        printBinary(chunk.getBuffer(), chunk.getOffset(), chunk.getLength());
    }

    @Override
    protected void doWritePtr(ObjectPtr objectPtr) {
        DsonPrinter printer = this.printer;
        int softLineLength = this.settings.softLineLength;
        writeCurrentName(printer, DsonType.POINTER);
        if (objectPtr.canBeAbbreviated()) {
            printer.fastPrint("@ptr "); // 只有localId时简写
            printString(printer, objectPtr.getLocalId(), StringStyle.AUTO_QUOTE);
            return;
        }

        printer.fastPrint("{@ptr ");
        int count = 0;
        if (objectPtr.hasNamespace()) {
            count++;
            printer.fastPrint(ObjectPtr.NAMES_NAMESPACE);
            printer.fastPrint(": ");
            printString(printer, objectPtr.getNamespace(), StringStyle.AUTO_QUOTE);
        }
        if (objectPtr.hasLocalId()) {
            if (count++ > 0) printer.fastPrint(", ");
            checkLineLength(printer, softLineLength);
            printer.fastPrint(ObjectPtr.NAMES_LOCAL_ID);
            printer.fastPrint(": ");
            printString(printer, objectPtr.getLocalId(), StringStyle.AUTO_QUOTE);
        }
        if (objectPtr.getType() != 0) {
            if (count++ > 0) printer.fastPrint(", ");
            checkLineLength(printer, softLineLength);
            printer.fastPrint(ObjectPtr.NAMES_TYPE);
            printer.fastPrint(": ");
            printer.fastPrint(Integer.toString(objectPtr.getType()));
        }
        if (objectPtr.getPolicy() != 0) {
            if (count > 0) printer.fastPrint(", ");
            checkLineLength(printer, softLineLength);
            printer.fastPrint(ObjectPtr.NAMES_POLICY);
            printer.fastPrint(": ");
            printer.fastPrint(Integer.toString(objectPtr.getPolicy()));
        }
        printer.print('}');
    }

    @Override
    protected void doWriteLitePtr(ObjectLitePtr objectLitePtr) {
        DsonPrinter printer = this.printer;
        int softLineLength = this.settings.softLineLength;
        writeCurrentName(printer, DsonType.LITE_POINTER);
        if (objectLitePtr.canBeAbbreviated()) {
            printer.fastPrint("@lptr "); // 只有localId时简写
            printer.fastPrint(Long.toString(objectLitePtr.getLocalId()));
            return;
        }

        printer.fastPrint("{@lptr ");
        int count = 0;
        if (objectLitePtr.hasNamespace()) {
            count++;
            printer.fastPrint(ObjectPtr.NAMES_NAMESPACE);
            printer.fastPrint(": ");
            printString(printer, objectLitePtr.getNamespace(), StringStyle.AUTO_QUOTE);
        }
        if (objectLitePtr.hasLocalId()) {
            if (count++ > 0) printer.fastPrint(", ");
            checkLineLength(printer, softLineLength);
            printer.fastPrint(ObjectPtr.NAMES_LOCAL_ID);
            printer.fastPrint(": ");
            printer.fastPrint(Long.toString(objectLitePtr.getLocalId()));
        }
        if (objectLitePtr.getType() != 0) {
            if (count++ > 0) printer.fastPrint(", ");
            checkLineLength(printer, softLineLength);
            printer.fastPrint(ObjectPtr.NAMES_TYPE);
            printer.fastPrint(": ");
            printer.fastPrint(Integer.toString(objectLitePtr.getType()));
        }
        if (objectLitePtr.getPolicy() != 0) {
            if (count > 0) printer.fastPrint(", ");
            checkLineLength(printer, softLineLength);
            printer.fastPrint(ObjectPtr.NAMES_POLICY);
            printer.fastPrint(": ");
            printer.fastPrint(Integer.toString(objectLitePtr.getPolicy()));
        }
        printer.print('}');
    }

    @Override
    protected void doWriteDateTime(ExtDateTime dateTime) {
        DsonPrinter printer = this.printer;
        int softLineLength = this.settings.softLineLength;
        writeCurrentName(printer, DsonType.DATETIME);
        if (dateTime.canBeAbbreviated()) {
            printer.fastPrint("@dt ");
            printer.fastPrint(ExtDateTime.formatDateTime(dateTime.getSeconds()));
            return;
        }

        printer.fastPrint("{@dt ");
        if (dateTime.hasDate()) {
            printer.fastPrint(ExtDateTime.NAMES_DATE);
            printer.fastPrint(": ");
            printer.fastPrint(ExtDateTime.formatDate(dateTime.getSeconds()));
        }
        if (dateTime.hasTime()) {
            if (dateTime.hasDate()) {
                printer.fastPrint(", ");
            }
            checkLineLength(printer, softLineLength);
            printer.fastPrint(ExtDateTime.NAMES_TIME);
            printer.fastPrint(": ");
            printer.fastPrint(ExtDateTime.formatTime(dateTime.getSeconds()));
            // nanos跟随time
            if (dateTime.getNanos() > 0) {
                printer.fastPrint(", ");
                checkLineLength(printer, softLineLength);
                if (dateTime.canConvertNanosToMillis()) {
                    printer.fastPrint(ExtDateTime.NAMES_MILLIS);
                    printer.fastPrint(": ");
                    printer.fastPrint(Integer.toString(dateTime.convertNanosToMillis()));
                } else {
                    printer.fastPrint(ExtDateTime.NAMES_NANOS);
                    printer.fastPrint(": ");
                    printer.fastPrint(Integer.toString(dateTime.getNanos()));
                }
            }
        }
        if (dateTime.hasOffset()) {
            if (dateTime.hasDate() || dateTime.hasTime()) {
                printer.fastPrint(", ");
            }
            checkLineLength(printer, softLineLength);
            printer.fastPrint(ExtDateTime.NAMES_OFFSET);
            printer.fastPrint(": ");
            printer.fastPrint(ExtDateTime.formatOffset(dateTime.getOffset()));
        }
        printer.print('}');
    }

    @Override
    protected void doWriteTimestamp(Timestamp timestamp) {
        DsonPrinter printer = this.printer;
        int softLineLength = settings.softLineLength;
        writeCurrentName(printer, DsonType.TIMESTAMP);
        if (timestamp.getNanos() == 0) { // 打印为缩写
            printer.fastPrint("@ts ");
            printer.fastPrint(Long.toString(timestamp.getSeconds()));
        } else if (timestamp.canConvertNanosToMillis()) {
            printer.fastPrint("@ts ");
            printer.fastPrint(Long.toString(timestamp.toEpochMillis()));
            printer.fastPrint("ms");
        } else {
            printer.fastPrint("{@ts ");
            printer.fastPrint(Timestamp.NAMES_SECONDS);
            printer.fastPrint(": ");
            printer.fastPrint(Long.toString(timestamp.getSeconds()));
            printer.fastPrint(", ");

            checkLineLength(printer, softLineLength);
            if (timestamp.canConvertNanosToMillis()) {
                printer.fastPrint(Timestamp.NAMES_MILLIS);
                printer.fastPrint(": ");
                printer.fastPrint(Integer.toString(timestamp.convertNanosToMillis()));
            } else {
                printer.fastPrint(Timestamp.NAMES_NANOS);
                printer.fastPrint(": ");
                printer.fastPrint(Integer.toString(timestamp.getNanos()));
            }
            printer.print('}');
        }
    }

    // endregion

    // region 容器

    @Override
    protected void doWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        DsonPrinter printer = this.printer;
        writeCurrentName(printer, dsonType);

        Context context = getContext();
        if (context.style == ObjectStyle.FLOW) {
            style = ObjectStyle.FLOW;
        }
        Context newContext = newContext(context, contextType, dsonType);
        newContext.style = style;

        printer.fastPrint(contextType.startSymbol);
        if (style == ObjectStyle.INDENT) {
            printer.indent(); // 调整缩进
        }

        setContext(newContext);
        this.recursionDepth++;
    }

    @Override
    protected void doWriteEndContainer() {
        Context context = getContext();
        DsonPrinter printer = this.printer;

        if (context.style == ObjectStyle.INDENT) {
            printer.retract(); // 恢复缩进
            // 打印了内容的情况下才换行结束
            if (context.count > 0 && printer.getColumn() > printer.getPrettyBodyColum()) {
                printer.println();
                printer.printIndent();
            }
        }
        printer.fastPrint(context.contextType.endSymbol);

        this.recursionDepth--;
        setContext(context.parent);
        returnContext(context);
    }

    // endregion

    // region 特殊接口

    @Override
    public void writeSimpleHeader(String clsName) {
        Objects.requireNonNull(clsName, "clsName");
        Context context = getContext();
        if (context.contextType == DsonContextType.OBJECT && context.state == DsonWriterState.NAME) {
            context.setState(DsonWriterState.VALUE);
        }
        autoStartTopLevel(context);
        ensureValueState(context);

        DsonPrinter printer = this.printer;
        writeCurrentName(printer, DsonType.HEADER);
        // header总是使用 @{} 包起来，提高辨识度 -- Dson2.1支持无引号
        printer.print("@{");
        printString(printer, clsName, StringStyle.UNQUOTE);
        printer.print('}');
        setNextState();
    }

    @Override
    protected void doWriteValueBytes(DsonType type, byte[] data) {
        throw new UnsupportedOperationException();
    }

    // endregion

    // region context

    private static final ConcurrentObjectPool<Context> contextPool = new ConcurrentObjectPool<>(Context::new, Context::reset,
            DsonInternals.CONTEXT_POOL_SIZE);

    private static Context newContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = contextPool.acquire();
        context.init(parent, contextType, dsonType);
        return context;
    }

    private static void returnContext(Context context) {
        contextPool.release(context);
    }

    protected static class Context extends AbstractDsonWriter.Context {

        ObjectStyle style = ObjectStyle.INDENT;
        int headerCount = 0;
        int count = 0;

        public Context() {
        }

        boolean hasElement() {
            return headerCount > 0 || count > 0;
        }

        public void reset() {
            super.reset();
            style = ObjectStyle.INDENT;
            headerCount = 0;
            count = 0;
        }

        @Override
        public Context getParent() {
            return (Context) parent;
        }

    }

    // endregion

}