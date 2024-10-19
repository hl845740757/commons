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
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.IO;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Text;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
public abstract class AbstractDsonWriter<TName> : IDsonWriter<TName> where TName : IEquatable<TName>
{
#nullable disable
    protected readonly DsonWriterSettings settings;
    protected internal Context context;
    protected int recursionDepth; // 当前递归深度

    /// <summary>
    /// </summary>
    /// <param name="settings">设置</param>
    /// <exception cref="ArgumentNullException"></exception>
    protected AbstractDsonWriter(DsonWriterSettings settings) {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public DsonWriterSettings Settings => settings;

    protected virtual Context GetContext() {
        return context;
    }

    protected void SetContext(Context context) {
        this.context = context;
    }

    public abstract void Flush();

    public virtual void Dispose() {
        context = null;
        recursionDepth = 0;
    }

    #region state

    public DsonContextType ContextType => context.contextType;

    public TName CurrentName {
        get {
            Context context = this.context;
            if (context.state != DsonWriterState.Value) {
                throw InvalidState(CollectionUtil.NewList(DsonWriterState.Value), context.state);
            }
            return context.curName;
        }
    }

    public bool IsAtName => context.state == DsonWriterState.Name;

    public void WriteName(TName name) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        Context context = this.context;
        if (context.state != DsonWriterState.Name) {
            throw InvalidState(CollectionUtil.NewList(DsonWriterState.Name), context.state);
        }
        context.curName = name;
        context.state = DsonWriterState.Value;
        DoWriteName(name);
    }

    /** 执行{@link #WriteName(String)}时调用 */
    protected void DoWriteName(TName name) {
    }

    protected void AdvanceToValueState(TName name) {
        Context context = this.context;
        if (context.state == DsonWriterState.Name) {
            WriteName(name);
        }
        if (context.state != DsonWriterState.Value) {
            throw InvalidState(CollectionUtil.NewList(DsonWriterState.Value), context.state);
        }
    }

    protected void EnsureValueState(Context context) {
        if (context.state != DsonWriterState.Value) {
            throw InvalidState(CollectionUtil.NewList(DsonWriterState.Value), context.state);
        }
    }

    protected void SetNextState() {
        switch (context.contextType) {
            case DsonContextType.Object:
            case DsonContextType.Header: {
                context.SetState(DsonWriterState.Name);
                break;
            }
            case DsonContextType.TopLevel:
            case DsonContextType.Array: {
                context.SetState(DsonWriterState.Value);
                break;
            }
            default: throw new InvalidOperationException();
        }
    }

    private DsonIOException InvalidState(List<DsonWriterState> expected, DsonWriterState state) {
        return DsonIOException.InvalidState(context.contextType, expected, state);
    }

    #endregion

    #region 简单值

    public void WriteInt32(TName name, int value, WireType wireType, INumberStyle style) {
        AdvanceToValueState(name);
        DoWriteInt32(value, wireType, style);
        SetNextState();
    }

    public void WriteInt64(TName name, long value, WireType wireType, INumberStyle style) {
        AdvanceToValueState(name);
        DoWriteInt64(value, wireType, style);
        SetNextState();
    }

    public void WriteFloat(TName name, float value, INumberStyle style) {
        AdvanceToValueState(name);
        DoWriteFloat(value, style);
        SetNextState();
    }

    public void WriteDouble(TName name, double value, INumberStyle style) {
        AdvanceToValueState(name);
        DoWriteDouble(value, style);
        SetNextState();
    }

    public void WriteBool(TName name, bool value) {
        AdvanceToValueState(name);
        DoWriteBool(value);
        SetNextState();
    }

    public void WriteString(TName name, string value, StringStyle style = StringStyle.Auto) {
        if (value == null) throw new ArgumentNullException(nameof(value));
        AdvanceToValueState(name);
        DoWriteString(value, style);
        SetNextState();
    }

    public void WriteNull(TName name) {
        AdvanceToValueState(name);
        DoWriteNull();
        SetNextState();
    }

    public void WriteBinary(TName name, Binary binary) {
        if (binary.IsNull) {
            throw new ArgumentNullException(nameof(binary));
        }
        AdvanceToValueState(name);
        DoWriteBinary(binary);
        SetNextState();
    }

    public void WriteBinary(TName name, byte[] bytes, int offset, int len) {
        ByteBufferUtil.CheckBuffer(bytes, offset, len);
        AdvanceToValueState(name);
        DoWriteBinary(bytes, offset, len);
        SetNextState();
    }

    public void WritePtr(TName name, in ObjectPtr objectPtr) {
        AdvanceToValueState(name);
        DoWritePtr(in objectPtr);
        SetNextState();
    }

    public void WriteLitePtr(TName name, in ObjectLitePtr objectLitePtr) {
        AdvanceToValueState(name);
        DoWriteLitePtr(in objectLitePtr);
        SetNextState();
    }

    public void WriteDateTime(TName name, in ExtDateTime dateTime) {
        AdvanceToValueState(name);
        DoWriteDateTime(in dateTime);
        SetNextState();
    }

    public void WriteTimestamp(TName name, in Timestamp timestamp) {
        AdvanceToValueState(name);
        DoWriteTimestamp(in timestamp);
        SetNextState();
    }

    protected abstract void DoWriteInt32(int value, WireType wireType, INumberStyle style);

    protected abstract void DoWriteInt64(long value, WireType wireType, INumberStyle style);

    protected abstract void DoWriteFloat(float value, INumberStyle style);

    protected abstract void DoWriteDouble(double value, INumberStyle style);

    protected abstract void DoWriteBool(bool value);

    protected abstract void DoWriteString(string value, StringStyle style);

    protected abstract void DoWriteNull();

    protected abstract void DoWriteBinary(Binary binary);

    protected abstract void DoWriteBinary(byte[] bytes, int offset, int len);

    protected abstract void DoWritePtr(in ObjectPtr objectPtr);

    protected abstract void DoWriteLitePtr(in ObjectLitePtr objectLitePtr);

    protected abstract void DoWriteDateTime(in ExtDateTime dateTime);

    protected abstract void DoWriteTimestamp(in Timestamp timestamp);

    #endregion

    #region 容器

    public void WriteStartArray(ObjectStyle style) {
        WriteStartContainer(DsonContextType.Array, DsonType.Array, style);
    }

    public void WriteEndArray() {
        WriteEndContainer(DsonContextType.Array, DsonWriterState.Value);
    }

    public void WriteStartObject(ObjectStyle style) {
        WriteStartContainer(DsonContextType.Object, DsonType.Object, style);
    }

    public void WriteEndObject() {
        WriteEndContainer(DsonContextType.Object, DsonWriterState.Name);
    }

    public void WriteStartHeader(ObjectStyle style) {
        // object下默认是name状态
        Context context = this.context;
        if (context.contextType == DsonContextType.Object && context.state == DsonWriterState.Name) {
            context.SetState(DsonWriterState.Value);
        }
        WriteStartContainer(DsonContextType.Header, DsonType.Header, style);
    }

    public void WriteEndHeader() {
        WriteEndContainer(DsonContextType.Header, DsonWriterState.Name);
    }

    private void WriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        if (recursionDepth >= settings.recursionLimit) {
            throw DsonIOException.RecursionLimitExceeded();
        }
        Context context = this.context;
        AutoStartTopLevel(context);
        EnsureValueState(context);
        DoWriteStartContainer(contextType, dsonType, style);
        SetNextState(); // 设置新上下文状态
    }

    private void WriteEndContainer(DsonContextType contextType, DsonWriterState expectedState) {
        Context context = this.context;
        CheckEndContext(context, contextType, expectedState);
        DoWriteEndContainer();
        SetNextState(); // parent前进一个状态
    }

    protected void AutoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TopLevel
            && context.state == DsonWriterState.Initial) {
            context.SetState(DsonWriterState.Value);
        }
    }

    protected void CheckEndContext(Context context, DsonContextType contextType, DsonWriterState state) {
        if (context.contextType != contextType) {
            throw DsonIOException.ContextError(contextType, context.contextType);
        }
        if (context.state != state) {
            throw InvalidState(CollectionUtil.NewList(state), context.state);
        }
    }

    /** 写入类型信息，创建新上下文，压入上下文 */
    protected abstract void DoWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style);

    /** 弹出上下文 */
    protected abstract void DoWriteEndContainer();

    #endregion

    #region 特殊

    public virtual void WriteSimpleHeader(string clsName) {
        if (clsName == null) throw new ArgumentNullException(nameof(clsName));
        IDsonWriter<string> textWrite = (IDsonWriter<string>)this;
        textWrite.WriteStartHeader();
        textWrite.WriteString(DsonHeaders.Names_ClassName, clsName, StringStyle.AutoQuote);
        textWrite.WriteEndHeader();
    }

    public void WriteValueBytes(TName name, DsonType type, byte[] data) {
        DsonReaderUtils.CheckWriteValueAsBytes(type);
        AdvanceToValueState(name);
        DoWriteValueBytes(type, data);
        SetNextState();
    }

    public object Attach(object userData) {
        return context.Attach(userData);
    }

    public object Attachment() {
        return context.userData;
    }

    protected abstract void DoWriteValueBytes(DsonType type, byte[] data);

    #endregion

    #region contxt

    protected internal abstract class Context
    {
        protected internal Context parent;
        protected internal DsonContextType contextType;
        protected internal DsonType dsonType; // 用于在Object/Array模式下写入内置数据结构
        protected internal DsonWriterState state = DsonWriterState.Initial;
        protected internal TName curName;
        protected internal object userData;

        public Context() {
        }

        public Context Init(Context parent, DsonContextType contextType, DsonType dsonType) {
            this.parent = parent;
            this.contextType = contextType;
            this.dsonType = dsonType;
            return this;
        }

        public virtual void Reset() {
            parent = null;
            contextType = default;
            dsonType = DsonTypes.INVALID;
            state = default;
            curName = default;
            userData = null;
        }

        public object Attach(object userData) {
            object r = this.userData;
            this.userData = userData;
            return r;
        }

        /** 方便查看赋值的调用 */
        public void SetState(DsonWriterState state) {
            this.state = state;
        }

        /** 子类可隐藏 */
        public Context Parent => parent;
    }

    #endregion
}
}