#region LICENSE

//  Copyright 2023-2024 wjybxx(845740757@qq.com)
// 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

#endregion

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Wjybxx.Commons;
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
    private readonly DsonBinaryReader<string> _textReader;
    private readonly DsonBinaryReader<int> _binReader;

    private TName _name0;
    private readonly byte[] _nameBuffer;

    public DsonBinaryReader(DsonReaderSettings settings, IDsonInput input, bool? autoClose = null)
        : base(settings) {
        if (DsonInternals.IsStringKey<TName>()) {
            this._textReader = this as DsonBinaryReader<string>;
            this._binReader = null;
            this._nameBuffer = new byte[7];
        } else {
            this._textReader = null;
            this._binReader = this as DsonBinaryReader<int>;
            this._nameBuffer = Array.Empty<byte>();
        }

        this._input = input ?? throw new ArgumentNullException(nameof(input));
        this._autoClose = autoClose ?? settings.autoClose;

        Context context = NewContext(null, DsonContextType.TopLevel, DsonTypes.INVALID);
        SetContext(context);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        DsonType dsonType = (DsonType)Dsons.DsonTypeOfFullType(fullType);
        WireType wireType = dsonType.HasWireType() ? WireTypes.ForNumber(wreTypeBits) : WireType.Uint;
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
        return (DsonType)Dsons.DsonTypeOfFullType(fullType);
    }

    public override bool PeekClassName(TName name, out string? clsName) {
        int position = _input.Position;
        int length = _input.ReadFixed32(); // array/object的长度字段
        int oldLimit = _input.PushLimit(length);
        try {
            int fullType = _input.IsAtEnd() ? 0 : _input.ReadRawByte();
            DsonType dsonType = (DsonType)Dsons.DsonTypeOfFullType(fullType);
            if (dsonType != DsonType.Header) {
                clsName = null;
                return false;
            }

            length = _input.ReadFixed16(); // header长度
            _input.PushLimit(length);

            _name0 = name;
            bool r = _textReader != null
                ? ScanClassName0(out clsName)
                : ScanClassName1(out clsName);
            return r;
        }
        finally {
            _input.Position = position;
            _input.PopLimit(oldLimit);
        }
    }

    private bool ScanClassName0(out string clsName) {
        byte[] targetName = DsonHeader.Bytes_ClassName;
        byte[] buffer = _nameBuffer;
        Array.Clear(buffer, 0, buffer.Length);
        //
        while (!_input.IsAtEnd()) {
            int fullType = _input.ReadRawByte();
            int wreTypeBits = Dsons.WireTypeOfFullType(fullType);
            DsonType dsonType = (DsonType)Dsons.DsonTypeOfFullType(fullType);
            int size = _input.ReadUInt32(); // name长度
            if (dsonType == DsonType.String && size == targetName.Length) {
                _input.ReadRawBytes(buffer, 0, size);
                if (ArrayUtil.Equals(targetName, buffer)) {
                    clsName = _input.ReadString();
                    return true;
                }
            } else {
                _input.SkipRawBytes(size); // skipName
            }
            DsonReaderUtils.SkipValue(_input, DsonContextType.Header, dsonType, wreTypeBits);
        }
        clsName = null;
        return false;
    }

    private bool ScanClassName1(out string clsName) {
        int targetName = _binReader._name0;
        while (!_input.IsAtEnd()) {
            int fullType = _input.ReadRawByte();
            int wreTypeBits = Dsons.WireTypeOfFullType(fullType);
            DsonType dsonType = (DsonType)Dsons.DsonTypeOfFullType(fullType);
            if (dsonType != DsonType.String) {
                // 跳过Name + Value
                _input.ReadUInt32();
                DsonReaderUtils.SkipValue(_input, DsonContextType.Header, dsonType, wreTypeBits);
            } else {
                // 测试name
                int name = _input.ReadUInt32();
                if (name == targetName) {
                    clsName = _input.ReadString();
                    return true;
                }
                // 跳过Value(string)
                int size = _input.ReadUInt32();
                _input.SkipRawBytes(size);
            }
        }
        clsName = null;
        return false;
    }

    protected override void DoReadName() {
        if (_textReader != null) {
            string filedName = _input.ReadString();
            if (context.enableNameIntern) {
                filedName = Dsons.InternField(filedName);
            }
            _textReader.currentName = filedName;
        } else {
            _binReader!.currentName = _input.ReadUInt32();
        }
    }

    #endregion

    #region 简单值

    protected override int DoReadInt32() {
        return currentWireType.ReadInt32(_input);
    }

    protected override long DoReadInt64() {
        return currentWireType.ReadInt64(_input);
    }

    protected override float DoReadFloat() {
        return currentWireType.ReadFloat(_input);
    }

    protected override double DoReadDouble() {
        return currentWireType.ReadDouble(_input);
    }

    protected override Fxp64 DoReadFxp64() {
        return new Fxp64(currentWireType.ReadInt64(_input));
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

    protected override ExtDateTime DoReadDateTime() {
        return DsonReaderUtils.ReadDateTime(_input, currentWireTypeBits);
    }

    protected override Timestamp DoReadTimestamp() {
        return DsonReaderUtils.ReadTimestamp(_input);
    }

    protected override Double4 DoReadDouble4() {
        return DsonReaderUtils.ReadDouble4(_input, currentWireTypeBits);
    }

    protected override Long4 DoReadLong4() {
        return DsonReaderUtils.ReadLong4(_input, currentWireTypeBits);
    }

    protected override Fxp4 DoReadFxp4() {
        return DsonReaderUtils.ReadFxp4(_input, currentWireTypeBits);
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
            int size = _input.ReadUInt32();
            if (size > 0) {
                _input.SkipRawBytes(size);
            }
        } else {
            _input.ReadUInt32();
        }
    }

    protected override void DoSkipValue() {
        ClearWaitStartContext();
        DsonReaderUtils.SkipValue(_input, ContextType, currentDsonType, currentWireTypeBits);
    }

    protected override void DoSkipToEndOfObject() {
        ClearWaitStartContext();
        DsonReaderUtils.SkipToEndOfObject(_input);
    }

    protected override byte[] DoReadValueAsBytes() {
        ClearWaitStartContext();
        return DsonReaderUtils.ReadValueAsBytes(_input, currentDsonType);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearWaitStartContext() {
    }

    #endregion

    #region context

    private static readonly ConcurrentObjectPool<Context> contextPool = new(
        () => new Context(), context => context.Reset(), DsonInternals.CONTEXT_POOL_SIZE);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Context NewContext(Context parent, DsonContextType contextType, DsonType dsonType) {
        Context context = contextPool.Acquire();
        context.Init(parent, contextType, dsonType);
        return context;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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