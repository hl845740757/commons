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
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson二进制Writer
/// </summary>
/// <typeparam name="TName"></typeparam>
public sealed class DsonBinaryWriter<TName> : AbstractDsonWriter<TName> where TName : IEquatable<TName>
{
#nullable disable
    private IDsonOutput _output;
    private readonly bool _autoClose;
    private readonly AbstractDsonWriter<string> _textWriter;
    private readonly AbstractDsonWriter<FieldNumber> _binWriter;

    public DsonBinaryWriter(DsonWriterSettings settings, IDsonOutput output, bool? autoClose = null)
        : base(settings) {
        if (DsonInternals.IsStringKey<TName>()) {
            _textWriter = this as AbstractDsonWriter<string>;
            _binWriter = null;
        } else {
            _textWriter = null;
            _binWriter = this as AbstractDsonWriter<FieldNumber>;
        }
        this._output = output ?? throw new ArgumentNullException(nameof(output));
        this._autoClose = autoClose ?? settings.autoClose;

        Context context = NewContext(null, DsonContextType.TopLevel, DsonTypes.INVALID);
        SetContext(context);
    }

    private new Context GetContext() {
        return (Context)context;
    }

    public override void Flush() {
        _output?.Flush();
    }

    public override void Dispose() {
        Context context = GetContext();
        SetContext(null);
        while (context != null) {
            Context parent = context.Parent;
            contextPool.Release(context);
            context = parent;
        }
        if (_output != null) {
            _output.Flush();
            if (_autoClose) {
                _output.Dispose();
            }
            _output = null!;
        }
        base.Dispose();
    }

    #region state

    private void WriteFullTypeAndCurrentName(IDsonOutput output, DsonType dsonType, int wireType) {
        output.WriteRawByte((byte)Dsons.MakeFullType((int)dsonType, wireType));
        if (dsonType == DsonType.Header) { // header是匿名属性
            return;
        }
        DsonContextType contextType = this.ContextType;
        if (contextType == DsonContextType.Object || contextType == DsonContextType.Header) {
            if (_textWriter != null) { // 避免装箱
                output.WriteString(_textWriter.context.curName);
            } else {
                output.WriteUint32(_binWriter!.context.curName.FullNumber);
            }
        }
    }

    #endregion

    #region 简单值

    protected override void DoWriteInt32(int value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Int32, (int)wireType);
        DsonReaderUtils.WriteInt32(output, value, wireType);
    }

    protected override void DoWriteInt64(long value, WireType wireType, INumberStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Int64, (int)wireType);
        DsonReaderUtils.WriteInt64(output, value, wireType);
    }

    protected override void DoWriteFloat(float value, INumberStyle style) {
        int wireType = DsonReaderUtils.WireTypeOfFloat(value);
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Float, wireType);
        DsonReaderUtils.WriteFloat(output, value, wireType);
    }

    protected override void DoWriteDouble(double value, INumberStyle style) {
        int wireType = DsonReaderUtils.WireTypeOfDouble(value);
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Double, wireType);
        DsonReaderUtils.WriteDouble(output, value, wireType);
    }

    protected override void DoWriteBool(bool value) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Bool, value ? 1 : 0); // 内联到wireType
    }

    protected override void DoWriteString(string value, StringStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.String, 0);
        output.WriteString(value);
    }

    protected override void DoWriteNull() {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Null, 0);
    }

    protected override void DoWriteBinary(Binary binary) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Binary, 0);
        DsonReaderUtils.WriteBinary(output, binary);
    }

    protected override void DoWriteBinary(byte[] bytes, int offset, int len) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Binary, 0);
        DsonReaderUtils.WriteBinary(output, bytes, offset, len);
    }

    protected override void DoWritePtr(in ObjectPtr objectPtr) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Pointer, DsonReaderUtils.WireTypeOfPtr(in objectPtr));
        DsonReaderUtils.WritePtr(output, in objectPtr);
    }

    protected override void DoWriteLitePtr(in ObjectLitePtr objectLitePtr) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.LitePointer, DsonReaderUtils.WireTypeOfLitePtr(in objectLitePtr));
        DsonReaderUtils.WriteLitePtr(output, in objectLitePtr);
    }

    protected override void DoWriteDateTime(in ExtDateTime dateTime) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.DateTime, dateTime.Enables);
        DsonReaderUtils.WriteDateTime(output, dateTime);
    }

    protected override void DoWriteTimestamp(in Timestamp timestamp) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, DsonType.Timestamp, 0);
        DsonReaderUtils.WriteTimestamp(output, timestamp);
    }

    #endregion

    #region 容器

    protected override void DoWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, dsonType, 0);

        Context newContext = NewContext(GetContext(), contextType, dsonType);
        newContext.preWritten = output.Position;
        if (contextType == DsonContextType.Header) {
            output.WriteFixed16(0);
        } else {
            output.WriteFixed32(0);
        }

        SetContext(newContext);
        this.recursionDepth++;
    }

    protected override void DoWriteEndContainer() {
        // 记录preWritten在写length之前，最后的size要减4
        Context context = GetContext();
        int preWritten = context.preWritten;

        int len;
        if (context.contextType == DsonContextType.Header) {
            len = _output.Position - preWritten - 2;
            if (len > 65535) throw new DsonIOException("header is too large");
            _output.SetFixed16(preWritten, len);
        } else {
            len = _output.Position - preWritten - 4;
            _output.SetFixed32(preWritten, len);
        }

        this.recursionDepth--;
        SetContext(context.Parent);
        ReturnContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoWriteValueBytes(DsonType type, byte[] data) {
        IDsonOutput output = this._output;
        WriteFullTypeAndCurrentName(output, type, 0);
        DsonReaderUtils.WriteValueBytes(output, type, data);
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
    protected new class Context : AbstractDsonWriter<TName>.Context
    {
        protected internal int preWritten;

        public Context() {
        }

        public new Context Parent => (Context)parent;

        public override void Reset() {
            base.Reset();
            preWritten = 0;
        }
    }

    #endregion
}
}