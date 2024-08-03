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
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
/// <summary>
/// Dson二进制Reader
/// </summary>
/// <typeparam name="TName"></typeparam>
public sealed class DsonBinaryReader<TName> : AbstractDsonReader<TName> where TName : IEquatable<TName>
{
#nullable disable
    private IDsonInput _input;
    private readonly bool _autoClose;
    private readonly AbstractDsonReader<string> _textReader;
    private readonly AbstractDsonReader<FieldNumber> _binReader;

    public DsonBinaryReader(DsonReaderSettings settings, IDsonInput input, bool? autoClose = null)
        : base(settings) {
        if (DsonInternals.IsStringKey<TName>()) {
            this._textReader = this as AbstractDsonReader<string>;
            this._binReader = null;
        } else {
            this._textReader = null;
            this._binReader = this as AbstractDsonReader<FieldNumber>;
        }

        this._input = input ?? throw new ArgumentNullException(nameof(input));
        this._autoClose = autoClose ?? settings.autoClose;

        Context context = NewContext(null, DsonContextType.TopLevel, DsonTypes.INVALID);
        SetContext(context);
    }

    private new Context GetContext() {
        return (Context)context;
    }

    public override void Dispose() {
        Context context = GetContext();
        SetContext(null);
        while (context != null) {
            Context parent = context.Parent;
            contextPool.Release(context);
            context = parent;
        }
        if (_input != null) {
            if (_autoClose) {
                _input.Dispose();
            }
            _input = null;
        }
        base.Dispose();
    }

    #region state

    public override DsonType ReadDsonType() {
        Context context = GetContext();
        CheckReadDsonTypeState(context);

        int fullType = _input.IsAtEnd() ? 0 : _input.ReadRawByte();
        int wreTypeBits = Dsons.WireTypeOfFullType(fullType);
        DsonType dsonType = DsonTypes.ForNumber(Dsons.DsonTypeOfFullType(fullType));
        WireType wireType = dsonType.HasWireType() ? WireTypes.ForNumber(wreTypeBits) : WireType.VarInt;
        this.currentDsonType = dsonType;
        this.currentWireType = wireType;
        this.currentWireTypeBits = wreTypeBits;
        this.currentName = default!;

        OnReadDsonType(context, dsonType);
        return dsonType;
    }

    public override DsonType PeekDsonType() {
        Context context = GetContext();
        CheckReadDsonTypeState(context);

        int fullType = _input.IsAtEnd() ? 0 : _input.GetByte(_input.Position);
        return DsonTypes.ForNumber(Dsons.DsonTypeOfFullType(fullType));
    }

    protected override void DoReadName() {
        if (_textReader != null) {
            string filedName = _input.ReadString();
            if (settings.enableFieldIntern) {
                filedName = Dsons.InternField(filedName);
            }
            _textReader.currentName = filedName;
        } else {
            _binReader!.currentName = FieldNumber.OfFullNumber(_input.ReadUint32());
        }
    }

    #endregion

    #region 简单值

    protected override int DoReadInt32() {
        return DsonReaderUtils.ReadInt32(_input, currentWireType);
    }

    protected override long DoReadInt64() {
        return DsonReaderUtils.ReadInt64(_input, currentWireType);
    }

    protected override float DoReadFloat() {
        return DsonReaderUtils.ReadFloat(_input, currentWireTypeBits);
    }

    protected override double DoReadDouble() {
        return DsonReaderUtils.ReadDouble(_input, currentWireTypeBits);
    }

    protected override bool DoReadBool() {
        return DsonReaderUtils.ReadBool(_input, currentWireTypeBits);
    }

    protected override string DoReadString() {
        return _input.ReadString();
    }

    protected override void DoReadNull() {
    }

    protected override Binary DoReadBinary() {
        return DsonReaderUtils.ReadBinary(_input);
    }

    protected override ObjectPtr DoReadPtr() {
        return DsonReaderUtils.ReadPtr(_input, currentWireTypeBits);
    }

    protected override ObjectLitePtr DoReadLitePtr() {
        return DsonReaderUtils.ReadLitePtr(_input, currentWireTypeBits);
    }

    protected override ExtDateTime DoReadDateTime() {
        return DsonReaderUtils.ReadDateTime(_input, currentWireTypeBits);
    }

    protected override Timestamp DoReadTimestamp() {
        return DsonReaderUtils.ReadTimestamp(_input);
    }

    #endregion

    #region 容器

    protected override void DoReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        int length;
        if (contextType == DsonContextType.Header) {
            length = _input.ReadFixed16();
        } else {
            length = _input.ReadFixed32();
        }
        newContext.oldLimit = _input.PushLimit(length);
        newContext.name = currentName;

        this.recursionDepth++;
        SetContext(newContext);
    }

    protected override void DoReadEndContainer() {
        if (!_input.IsAtEnd()) {
            throw DsonIOException.BytesRemain(_input.GetBytesUntilLimit());
        }
        Context context = GetContext();
        _input.PopLimit(context.oldLimit);

        // 恢复上下文
        RecoverDsonType(context);
        this.recursionDepth--;
        SetContext(context.parent!);
        ReturnContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoSkipName() {
        if (_textReader != null) {
            // 避免构建字符串
            int size = _input.ReadUint32();
            if (size > 0) {
                _input.SkipRawBytes(size);
            }
        } else {
            _input.ReadUint32();
        }
    }

    protected override void DoSkipValue() {
        DsonReaderUtils.SkipValue(_input, ContextType, currentDsonType, currentWireType, currentWireTypeBits);
    }

    protected override void DoSkipToEndOfObject() {
        DsonReaderUtils.SkipToEndOfObject(_input);
    }

    protected override byte[] DoReadValueAsBytes() {
        return DsonReaderUtils.ReadValueAsBytes(_input, currentDsonType);
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
    protected new class Context : AbstractDsonReader<TName>.Context
    {
        protected internal int oldLimit = -1;

        public Context() {
        }

        public new Context Parent => (Context)parent;

        public override void Reset() {
            base.Reset();
            oldLimit = -1;
        }
    }

    #endregion
}
}