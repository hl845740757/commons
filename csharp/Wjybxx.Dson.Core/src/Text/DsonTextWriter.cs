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
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson.Text
{
/// <summary>
/// 将输出写为文本的Writer
/// </summary>
public sealed class DsonTextWriter : AbstractDsonWriter<string>
{
#nullable disable
    private readonly DsonTextWriterSettings _settings;
    private DsonPrinter _printer;

    public DsonTextWriter(DsonTextWriterSettings settings, TextWriter writer, bool? autoClose = null)
        : base(settings) {
        this._settings = settings;
        this._printer = new DsonPrinter(settings, writer, autoClose ?? settings.autoClose);

        Context context = NewContext(null, DsonContextType.TopLevel, DsonTypes.INVALID);
        SetContext(context);
    }

    /** 用于在Object或Array上下文中自行控制换行 -- 打印得更好看 */
    public void Println() {
        _printer.Println();
    }

    public new DsonTextWriterSettings Settings => _settings;

    private new Context GetContext() {
        return (Context)context;
    }

    public override void Flush() {
        _printer?.Flush();
    }

    public override void Dispose() {
        Context context = GetContext();
        SetContext(null);
        while (context != null) {
            Context parent = context.Parent;
            contextPool.Release(context);
            context = parent;
        }

        if (_printer != null) {
            _printer?.Dispose();
            _printer = null!;
        }
        base.Dispose();
    }

    #region state

    private void WriteCurrentName(DsonPrinter printer, DsonType dsonType) {
        Context context = GetContext();
        // header与外层对象无缩进，且是匿名属性 -- 如果打印多个header，将保持连续
        if (dsonType == DsonType.Header) {
            Debug.Assert(context.count == 0);
            context.headerCount++;
            return;
        }
        // 处理value之间分隔符
        if (context.count > 0) {
            printer.Print(',');
        }
        // 先处理长度超出，再处理缩进
        if (printer.Column >= _settings.softLineLength) {
            printer.Println();
        }
        bool newLine = printer.Column == 0;
        if (context.style == ObjectStyle.Indent) {
            if (newLine) {
                // 新的一行，只需缩进
                printer.PrintIndent();
            } else if (context.count == 0 || printer.Column >= printer.PrettyBodyColum) {
                // 第一个元素，或当前行超过缩进(含逗号)，需要换行
                printer.Println();
                printer.PrintIndent();
            } else {
                // 当前位置未达缩进位置，不换行
                printer.PrintSpaces(printer.PrettyBodyColum - printer.Column);
            }
        } else if (!newLine && context.HasElement()) {
            // 非缩进模式下，元素之间打印一个空格
            printer.Print(' ');
        }

        if (context.contextType.IsObjectLike()) {
            PrintString(printer, context.curName, StringStyle.AutoQuote);
            printer.Print(": ");
        }
        context.count++;
    }

    private void PrintString(DsonPrinter printer, string value, StringStyle style) {
        DsonTextWriterSettings settings = this._settings;
        switch (style) {
            case StringStyle.Auto: {
                if (CanPrintAsUnquote(value, settings)) {
                    printer.FastPrint(value);
                } else if (CanPrintAsText(value, settings)) {
                    PrintText(value);
                } else {
                    PrintEscaped(value);
                }
                break;
            }
            case StringStyle.AutoQuote: {
                if (CanPrintAsUnquote(value, settings)) {
                    printer.FastPrint(value);
                } else {
                    PrintEscaped(value);
                }
                break;
            }
            case StringStyle.Quote: {
                PrintEscaped(value);
                break;
            }
            case StringStyle.Unquote: {
                printer.FastPrint(value);
                break;
            }
            case StringStyle.Text: {
                if (settings.enableText) {
                    PrintText(value);
                } else {
                    PrintEscaped(value);
                }
                break;
            }
            case StringStyle.SimpleText: {
                PrintSimpleText(value);
                break;
            }
            case StringStyle.StringLine: {
                if (value.IndexOf('\n') >= 0) {
                    PrintEscaped(value);
                } else {
                    PrintStringLine(value);
                }
                break;
            }
            default: throw new InvalidOperationException(style.ToString());
        }
    }

    private static bool CanPrintAsUnquote(string str, DsonTextWriterSettings settings) {
        return DsonTexts.CanUnquoteString(str, settings.maxLengthOfUnquoteString)
               && (!settings.unicodeChar || DsonTexts.IsAsciiText(str));
    }

    private static bool CanPrintAsText(string str, DsonTextWriterSettings settings) {
        return settings.enableText && (str.Length > settings.textStringLength);
    }

    /** 打印双引号String */
    private void PrintEscaped(string text) {
        bool unicodeChar = _settings.unicodeChar;
        int softLineLength = _settings.softLineLength;
        DsonPrinter printer = this._printer;
        printer.Print('"');
        for (int i = 0, length = text.Length; i < length; i++) {
            char c = text[i];
            if (char.IsSurrogate(c)) {
                printer.PrintHpmCodePoint(c, text[++i]);
            } else {
                printer.PrintEscaped(c, unicodeChar);
            }
            if (printer.Column >= softLineLength && (i + 1 < length)) {
                printer.Println(); // 双引号字符串换行不能缩进
            }
        }
        printer.Print('"');
    }

    /** 纯文本模式打印，要执行换行符 */
    private void PrintText(string text) {
        int softLineLength = _settings.softLineLength;
        DsonPrinter printer = this._printer;
        int headIndent;
        if (_settings.textAlignLeft) {
            headIndent = printer.PrettyBodyColum;
            printer.Println();
            printer.PrintSpaces(headIndent);
            printer.FastPrint("@\"\"\""); // 开始符
            printer.Println();
            printer.PrintSpaces(headIndent);
            printer.FastPrint("@- "); // 首行-避免插入换行符
        } else {
            headIndent = 0;
            printer.Println();
            printer.FastPrint("@\"\"\""); // 开始符
            printer.Println();
            printer.FastPrint("@- "); // 首行-避免插入换行符
        }
        for (int i = 0, length = text.Length; i < length; i++) {
            char c = text[i];
            // 要执行文本中的换行符
            if (c == '\n' || (c == '\r' && i + 1 < length && text[i + 1] == '\n')) {
                printer.Println();
                printer.PrintSpaces(headIndent);
                printer.FastPrint("@| ");
                continue;
            }
            if (char.IsSurrogate(c)) {
                printer.PrintHpmCodePoint(c, text[++i]);
            } else {
                printer.Print(c);
            }
            if (printer.Column >= softLineLength && (i + 1 < length)) {
                printer.Println();
                printer.PrintSpaces(headIndent);
                printer.FastPrint("@- ");
            }
        }
        printer.Println();
        printer.PrintSpaces(headIndent);
        printer.FastPrint("@\"\"\""); // 结束符
    }

    private void PrintSimpleText(string text) {
        DsonPrinter printer = this._printer;
        int headIndent;
        if (_settings.textAlignLeft) {
            headIndent = printer.PrettyBodyColum;
            printer.Println();
            printer.PrintSpaces(headIndent);
            printer.FastPrint("\"\"\""); // 开始符
            printer.Println();
            printer.PrintSpaces(headIndent);
        } else {
            headIndent = 0;
            printer.Println();
            printer.FastPrint("\"\"\""); // 开始符
            printer.Println();
        }
        for (int i = 0, length = text.Length; i < length; i++) {
            char c = text[i];
            // 要执行文本中的换行符
            if (c == '\n' || (c == '\r' && i + 1 < length && text[i + 1] == '\n')) {
                printer.Println();
                printer.PrintSpaces(headIndent);
                continue;
            }
            if (char.IsSurrogate(c)) {
                printer.PrintHpmCodePoint(c, text[++i]);
            } else {
                printer.Print(c);
            }
        }
        printer.Println();
        printer.PrintSpaces(headIndent);
        printer.FastPrint("\"\"\""); // 结束符
    }

    private void PrintStringLine(String text) {
        DsonPrinter printer = this._printer;
        printer.FastPrint("@sL ");
        printer.Print(text);
        printer.Println(); // 换行表示结束
    }

    private void PrintBinary(byte[] buffer, int offset, int length) {
        DsonPrinter printer = this._printer;
        if (length == 0) {
            printer.Print('"');
            printer.Print('"');
            return;
        }
        printer.Print('"');
        int softLineLength = this._settings.softLineLength;
        // 使用小buffer多次编码代替大的buffer，一方面节省内存，一方面控制行长度
        int segment = 8;
        Span<char> cBuffer = stackalloc char[segment * 2];
        int loop = length / segment;
        for (int i = 0; i < loop; i++) {
            CheckLineLength(printer, softLineLength);
            CommonsLang3.EncodeHex(buffer, offset + i * segment, segment, cBuffer);
            printer.FastPrint(cBuffer);
        }
        int remain = length - loop * segment;
        if (remain > 0) {
            CheckLineLength(printer, softLineLength);
            CommonsLang3.EncodeHex(buffer, offset + loop * segment, remain, cBuffer);
            printer.FastPrint(cBuffer.Slice(0, remain * 2));
        }
        printer.Print('"');
    }

    private void CheckLineLength(DsonPrinter printer, int softLineLength) {
        if (printer.Column >= softLineLength) {
            printer.Println();
        }
    }

    #endregion

    #region 简单值

    private void PrintInt32(DsonPrinter printer, int value, INumberStyle style) {
        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.FastPrint("@i ");
        }
        printer.FastPrint(styleOut.Value);
    }

    private void PrintInt64(DsonPrinter printer, long value, INumberStyle style) {
        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.FastPrint("@L ");
        }
        printer.FastPrint(styleOut.Value);
    }

    private void PrintFloat(DsonPrinter printer, float value, INumberStyle style) {
        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.FastPrint("@f ");
        }
        printer.FastPrint(styleOut.Value);
    }

    private void PrintDouble(DsonPrinter printer, double value, INumberStyle style) {
        StyleOut styleOut = style.ToString(value);
        if (styleOut.IsTyped) {
            printer.FastPrint("@d ");
        }
        printer.FastPrint(styleOut.Value);
    }

    protected override void DoWriteInt32(int value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Int32);
        PrintInt32(printer, value, style);
    }

    protected override void DoWriteInt64(long value, WireType wireType, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Int64);
        PrintInt64(printer, value, style);
    }

    protected override void DoWriteFloat(float value, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Float);
        PrintFloat(printer, value, style);
    }

    protected override void DoWriteDouble(double value, INumberStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Double);
        PrintDouble(printer, value, style);
    }

    protected override void DoWriteBool(bool value) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Bool);
        printer.FastPrint(value ? "true" : "false");
    }

    protected override void DoWriteString(string value, StringStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.String);
        PrintString(printer, value, style);
    }

    protected override void DoWriteNull() {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Null);
        printer.FastPrint("null");
    }

    protected override void DoWriteBinary(Binary binary) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Binary);
        printer.FastPrint("@bin ");
        PrintBinary(binary.UnsafeBuffer, 0, binary.UnsafeBuffer.Length);
    }

    protected override void DoWriteBinary(DsonChunk chunk) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Binary);
        printer.FastPrint("@bin ");
        PrintBinary(chunk.Buffer, chunk.Offset, chunk.Length);
    }

    protected override void DoWritePtr(in ObjectPtr objectPtr) {
        DsonPrinter printer = this._printer;
        int softLineLength = this._settings.softLineLength;
        WriteCurrentName(printer, DsonType.Pointer);
        if (objectPtr.CanBeAbbreviated) {
            printer.FastPrint("@ptr "); // 只有localId时简写
            PrintString(printer, objectPtr.LocalId, StringStyle.AutoQuote);
            return;
        }

        printer.FastPrint("{@ptr ");
        int count = 0;
        if (objectPtr.HasNamespace) {
            count++;
            printer.FastPrint(ObjectPtr.NamesNamespace);
            printer.FastPrint(": ");
            PrintString(printer, objectPtr.Namespace, StringStyle.AutoQuote);
        }
        if (objectPtr.HasLocalId) {
            if (count++ > 0) printer.FastPrint(", ");
            CheckLineLength(printer, softLineLength);
            printer.FastPrint(ObjectPtr.NamesLocalId);
            printer.FastPrint(": ");
            PrintString(printer, objectPtr.LocalId, StringStyle.AutoQuote);
        }
        if (objectPtr.Type != 0) {
            if (count++ > 0) printer.FastPrint(", ");
            CheckLineLength(printer, softLineLength);
            printer.FastPrint(ObjectPtr.NamesType);
            printer.FastPrint(": ");
            printer.FastPrint(objectPtr.Type.ToString());
        }
        if (objectPtr.Policy != 0) {
            if (count > 0) printer.FastPrint(", ");
            CheckLineLength(printer, softLineLength);
            printer.FastPrint(ObjectPtr.NamesPolicy);
            printer.FastPrint(": ");
            printer.FastPrint(objectPtr.Policy.ToString());
        }
        printer.Print('}');
    }

    protected override void DoWriteLitePtr(in ObjectLitePtr objectLitePtr) {
        DsonPrinter printer = this._printer;
        int softLineLength = this._settings.softLineLength;
        WriteCurrentName(printer, DsonType.LitePointer);
        if (objectLitePtr.CanBeAbbreviated) {
            printer.FastPrint("@lptr "); // 只有localId时简写
            printer.FastPrint(objectLitePtr.LocalId.ToString());
            return;
        }

        printer.FastPrint("{@lptr ");
        int count = 0;
        if (objectLitePtr.HasNamespace) {
            count++;
            printer.FastPrint(ObjectPtr.NamesNamespace);
            printer.FastPrint(": ");
            PrintString(printer, objectLitePtr.Namespace, StringStyle.AutoQuote);
        }
        if (objectLitePtr.HasLocalId) {
            if (count++ > 0) printer.FastPrint(", ");
            CheckLineLength(printer, softLineLength);
            printer.FastPrint(ObjectPtr.NamesLocalId);
            printer.FastPrint(": ");
            printer.FastPrint(objectLitePtr.LocalId.ToString());
        }
        if (objectLitePtr.Type != 0) {
            if (count++ > 0) printer.FastPrint(", ");
            CheckLineLength(printer, softLineLength);
            printer.FastPrint(ObjectPtr.NamesType);
            printer.FastPrint(": ");
            printer.FastPrint(objectLitePtr.Type.ToString());
        }
        if (objectLitePtr.Policy != 0) {
            if (count > 0) printer.FastPrint(", ");
            CheckLineLength(printer, softLineLength);
            printer.FastPrint(ObjectPtr.NamesPolicy);
            printer.FastPrint(": ");
            printer.FastPrint(objectLitePtr.Policy.ToString());
        }
        printer.Print('}');
    }

    protected override void DoWriteDateTime(in ExtDateTime dateTime) {
        DsonPrinter printer = this._printer;
        int softLineLength = this._settings.softLineLength;
        WriteCurrentName(printer, DsonType.DateTime);
        if (dateTime.CanBeAbbreviated()) {
            printer.FastPrint("@dt ");
            printer.FastPrint(ExtDateTime.FormatDateTime(dateTime.Seconds));
            return;
        }

        printer.FastPrint("{@dt ");
        if (dateTime.HasDate) {
            printer.FastPrint(ExtDateTime.NamesDate);
            printer.FastPrint(": ");
            printer.FastPrint(ExtDateTime.FormatDate(dateTime.Seconds));
        }
        if (dateTime.HasTime) {
            if (dateTime.HasDate) {
                printer.FastPrint(", ");
            }
            CheckLineLength(printer, softLineLength);
            printer.FastPrint(ExtDateTime.NamesTime);
            printer.FastPrint(": ");
            printer.FastPrint(ExtDateTime.FormatTime(dateTime.Seconds));
            // nanos跟随time
            if (dateTime.Nanos > 0) {
                printer.FastPrint(", ");
                CheckLineLength(printer, softLineLength);
                if (dateTime.CanConvertNanosToMillis()) {
                    printer.FastPrint(ExtDateTime.NamesMillis);
                    printer.FastPrint(": ");
                    printer.FastPrint(dateTime.ConvertNanosToMillis().ToString());
                } else {
                    printer.FastPrint(ExtDateTime.NamesNanos);
                    printer.FastPrint(": ");
                    printer.FastPrint(dateTime.Nanos.ToString());
                }
            }
        }
        if (dateTime.HasOffset) {
            if (dateTime.HasDate || dateTime.HasTime) {
                printer.FastPrint(", ");
            }
            CheckLineLength(printer, softLineLength);
            printer.FastPrint(ExtDateTime.NamesOffset);
            printer.FastPrint(": ");
            printer.FastPrint(ExtDateTime.FormatOffset(dateTime.Offset));
        }
        printer.Print('}');
    }

    protected override void DoWriteTimestamp(in Timestamp timestamp) {
        DsonPrinter printer = this._printer;
        int softLineLength = this._settings.softLineLength;
        WriteCurrentName(printer, DsonType.Timestamp);
        if (timestamp.Nanos == 0) { // 打印为缩写
            printer.FastPrint("@ts ");
            printer.FastPrint(timestamp.Seconds.ToString());
        } else if (timestamp.CanConvertNanosToMillis()) {
            printer.FastPrint("@ts ");
            printer.FastPrint(timestamp.ToEpochMillis().ToString());
            printer.FastPrint("ms");
        } else {
            printer.FastPrint("{@ts ");
            printer.FastPrint(Timestamp.NamesSeconds);
            printer.FastPrint(": ");
            printer.FastPrint(timestamp.Seconds.ToString());
            printer.FastPrint(", ");

            CheckLineLength(printer, softLineLength);
            if (timestamp.CanConvertNanosToMillis()) {
                printer.FastPrint(Timestamp.NamesMillis);
                printer.FastPrint(": ");
                printer.FastPrint(timestamp.ConvertNanosToMillis().ToString());
            } else {
                printer.FastPrint(Timestamp.NamesNanos);
                printer.FastPrint(": ");
                printer.FastPrint(timestamp.Nanos.ToString());
            }
            printer.Print('}');
        }
    }

    #endregion

    #region 容器

    protected override void DoWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, dsonType);

        Context context = GetContext();
        if (context.style == ObjectStyle.Flow) {
            style = ObjectStyle.Flow;
        }
        Context newContext = NewContext(context, contextType, dsonType);
        newContext.style = style;

        printer.FastPrint(contextType.GetStartSymbol()!);
        if (style == ObjectStyle.Indent) {
            printer.Indent(); // 调整缩进
        }

        SetContext(newContext);
        this.recursionDepth++;
    }

    protected override void DoWriteEndContainer() {
        Context context = GetContext();
        DsonPrinter printer = this._printer;

        if (context.style == ObjectStyle.Indent) {
            printer.Retract(); // 恢复缩进
            // 打印了内容的情况下才换行结束
            if (context.count > 0 && (printer.Column > printer.PrettyBodyColum)) {
                printer.Println();
                printer.PrintIndent();
            }
        }
        printer.FastPrint(context.contextType.GetEndSymbol()!);

        this.recursionDepth--;
        SetContext(context.Parent);
        ReturnContext(context);
    }

    #endregion

    #region 特殊

    public override void WriteSimpleHeader(string clsName) {
        if (clsName == null) throw new ArgumentNullException(nameof(clsName));
        Context context = GetContext();
        if (context.contextType == DsonContextType.Object && context.state == DsonWriterState.Name) {
            context.SetState(DsonWriterState.Value);
        }
        AutoStartTopLevel(context);
        EnsureValueState(context);

        DsonPrinter printer = this._printer;
        WriteCurrentName(printer, DsonType.Header);
        // header总是使用 @{} 包起来，提高辨识度 -- Dson2.1支持无引号
        printer.Print("@{");
        PrintString(printer, clsName, StringStyle.Unquote);
        printer.Print('}');
        SetNextState();
    }

    protected override void DoWriteValueBytes(DsonType type, byte[] data) {
        throw new InvalidOperationException("UnsupportedOperation");
    }

    #endregion

    #region context

    private static readonly ConcurrentObjectPool<Context> contextPool = new ConcurrentObjectPool<Context>(
        () => new Context(), context => context.Reset(),
        DsonInternals.CONTEXT_POOL_SIZE);

    private static Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = contextPool.Acquire();
        context.Init(parent, contextType, dsonType);
        return context;
    }

    private static void ReturnContext(Context context) {
        contextPool.Release(context);
    }

#pragma warning disable CS0628
    protected new class Context : AbstractDsonWriter<string>.Context
    {
        internal ObjectStyle style = ObjectStyle.Indent;
        internal int headerCount = 0;
        internal int count = 0;

        public Context() {
        }

        internal bool HasElement() {
            return headerCount > 0 || count > 0;
        }

        public new Context Parent => (Context)parent;

        public override void Reset() {
            base.Reset();
            style = ObjectStyle.Indent;
            headerCount = 0;
            count = 0;
        }
    }

    #endregion
}
}