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
using System.Runtime.CompilerServices;
using Wjybxx.Commons.Collections;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
public abstract class AbstractDsonReader<TName> : IDsonReader<TName> where TName : IEquatable<TName>
{
#nullable disable
    protected readonly DsonReaderSettings settings;
    protected Context context;
    protected int recursionDepth; // 这些值放外面，不需要上下文隔离，但需要能恢复
    protected DsonType currentDsonType = DsonTypes.INVALID;
    protected WireType currentWireType;
    protected int currentWireTypeBits;
    protected internal TName currentName;
    protected Context waitStartContext; // 暂时只支持单次回滚，在ReadStart或SkipValue时都应该清理

    protected AbstractDsonReader(DsonReaderSettings settings) {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public DsonReaderSettings Settings => settings;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected Context GetContext() {
        return context;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SetContext(Context context) {
        this.context = context;
    }

    public virtual void Dispose() {
        context = null;
        recursionDepth = 0;
        currentDsonType = DsonTypes.INVALID;
        currentWireType = WireType.VarInt;
        currentWireTypeBits = 0;
        currentName = default;
        waitStartContext = null;
    }

    #region state

    public DsonContextType ContextType => context.contextType;

    public DsonType CurrentDsonType {
        get {
            if (currentDsonType == DsonTypes.INVALID) {
                Debug.Assert(context.contextType == DsonContextType.TopLevel);
                throw InvalidState(CollectionUtil.NewList(DsonReaderState.Name, DsonReaderState.Value));
            }
            return currentDsonType;
        }
    }

    public TName CurrentName {
        get {
            if (context.state != DsonReaderState.Value) {
                throw InvalidState(CollectionUtil.NewList(DsonReaderState.Value));
            }
            return currentName;
        }
    }

    public bool IsAtType {
        get {
            if (context.state == DsonReaderState.Type) {
                return true;
            }
            return context.contextType == DsonContextType.TopLevel
                   && context.state == DsonReaderState.Initial;
        }
    }

    public bool IsAtName => context.state == DsonReaderState.Name;

    public bool IsAtValue => context.state == DsonReaderState.Value;

    public TName ReadName() {
        if (context.state != DsonReaderState.Name) {
            throw InvalidState(CollectionUtil.NewList(DsonReaderState.Name));
        }
        DoReadName();
        context.SetState(DsonReaderState.Value);
        return currentName;
    }

    public void ReadName(TName expected) {
        // 不直接使用方法返回值比较，避免装箱
        ReadName();
        if (!expected.Equals(currentName)) {
            throw DsonIOException.UnexpectedName(expected, currentName);
        }
    }

    public abstract DsonType ReadDsonType();

    public abstract DsonType PeekDsonType();

    /** 不直接返回值，而是存储在变量上可避免泛型问题 */
    protected abstract void DoReadName();

    /** 检查是否可以执行{@link #readDsonType()} */
    protected void CheckReadDsonTypeState(Context context) {
        if (context.contextType == DsonContextType.TopLevel) {
            if (context.state != DsonReaderState.Initial && context.state != DsonReaderState.Type) {
                throw InvalidState(CollectionUtil.NewList(DsonReaderState.Initial, DsonReaderState.Type));
            }
        } else if (context.state != DsonReaderState.Type) {
            throw InvalidState(CollectionUtil.NewList(DsonReaderState.Type));
        }
    }

    /** 处理读取dsonType后的状态切换 */
    protected void OnReadDsonType(Context context, DsonType dsonType) {
        if (dsonType == DsonType.EndOfObject) {
            // readEndXXX都是子上下文中执行的，因此正常情况下topLevel不会读取到 endOfObject 标记
            // 顶层读取到 END_OF_OBJECT 表示到达文件尾
            if (context.contextType == DsonContextType.TopLevel) {
                context.SetState(DsonReaderState.EndOfFile);
            } else {
                context.SetState(DsonReaderState.WaitEndObject);
            }
        } else {
            // topLevel只可是容器对象
            if (context.contextType == DsonContextType.TopLevel && !dsonType.IsContainerOrHeader()) {
                throw DsonIOException.InvalidDsonType(context.contextType, dsonType);
            }
            if (context.contextType == DsonContextType.Object) {
                // 如果是header则直接进入VALUE状态 - header是匿名属性
                if (dsonType == DsonType.Header) {
                    context.SetState(DsonReaderState.Value);
                } else {
                    context.SetState(DsonReaderState.Name);
                }
            } else if (context.contextType == DsonContextType.Header) {
                context.SetState(DsonReaderState.Name);
            } else {
                context.SetState(DsonReaderState.Value);
            }
        }
    }

    /** 前进到读值状态 */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void AdvanceToValueState(TName name, DsonType requiredType) {
        Context context = this.context;
        if (context.state != DsonReaderState.Value) {
            if (context.state == DsonReaderState.Type) {
                ReadDsonType();
            }
            if (context.state == DsonReaderState.Name) {
                ReadName(name);
            }
            if (context.state != DsonReaderState.Value) {
                throw InvalidState(CollectionUtil.NewList(DsonReaderState.Value));
            }
        }
        if (requiredType != DsonTypes.INVALID && currentDsonType != requiredType) {
            throw DsonIOException.DsonTypeMismatch(requiredType, currentDsonType);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureValueState(Context context, DsonType requiredType) {
        if (context.state != DsonReaderState.Value) {
            throw InvalidState(CollectionUtil.NewList(DsonReaderState.Value));
        }
        if (currentDsonType != requiredType) {
            throw DsonIOException.DsonTypeMismatch(requiredType, currentDsonType);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void SetNextState() {
        context.SetState(DsonReaderState.Type);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected DsonIOException InvalidState(List<DsonReaderState> expected) {
        return DsonIOException.InvalidState(context.contextType, expected, context.state);
    }

    #endregion

    #region 简单值

    public int ReadInt32(TName name) {
        AdvanceToValueState(name, DsonType.Int32);
        int value = DoReadInt32();
        SetNextState();
        return value;
    }

    public long ReadInt64(TName name) {
        AdvanceToValueState(name, DsonType.Int64);
        long value = DoReadInt64();
        SetNextState();
        return value;
    }

    public float ReadFloat(TName name) {
        AdvanceToValueState(name, DsonType.Float);
        float value = DoReadFloat();
        SetNextState();
        return value;
    }

    public double ReadDouble(TName name) {
        AdvanceToValueState(name, DsonType.Double);
        double value = DoReadDouble();
        SetNextState();
        return value;
    }

    public bool ReadBool(TName name) {
        AdvanceToValueState(name, DsonType.Bool);
        bool value = DoReadBool();
        SetNextState();
        return value;
    }

    public string ReadString(TName name) {
        AdvanceToValueState(name, DsonType.String);
        string value = DoReadString();
        SetNextState();
        return value;
    }

    public void ReadNull(TName name) {
        AdvanceToValueState(name, DsonType.Null);
        DoReadNull();
        SetNextState();
    }

    public Binary ReadBinary(TName name) {
        AdvanceToValueState(name, DsonType.Binary);
        Binary value = DoReadBinary();
        SetNextState();
        return value;
    }

    public ObjectPtr ReadPtr(TName name) {
        AdvanceToValueState(name, DsonType.Pointer);
        ObjectPtr value = DoReadPtr();
        SetNextState();
        return value;
    }

    public ObjectLitePtr ReadLitePtr(TName name) {
        AdvanceToValueState(name, DsonType.LitePointer);
        ObjectLitePtr value = DoReadLitePtr();
        SetNextState();
        return value;
    }

    public ExtDateTime ReadDateTime(TName name) {
        AdvanceToValueState(name, DsonType.DateTime);
        ExtDateTime value = DoReadDateTime();
        SetNextState();
        return value;
    }

    public Timestamp ReadTimestamp(TName name) {
        AdvanceToValueState(name, DsonType.Timestamp);
        Timestamp value = DoReadTimestamp();
        SetNextState();
        return value;
    }

    protected abstract int DoReadInt32();

    protected abstract long DoReadInt64();

    protected abstract float DoReadFloat();

    protected abstract double DoReadDouble();

    protected abstract bool DoReadBool();

    protected abstract string DoReadString();

    protected abstract void DoReadNull();

    protected abstract Binary DoReadBinary();

    protected abstract ObjectPtr DoReadPtr();

    protected abstract ObjectLitePtr DoReadLitePtr();

    protected abstract ExtDateTime DoReadDateTime();

    protected abstract Timestamp DoReadTimestamp();

    #endregion

    #region 容器

    public void ReadStartArray() {
        ReadStartContainer(DsonContextType.Array, DsonType.Array);
    }

    public void ReadEndArray() {
        ReadEndContainer(DsonContextType.Array);
    }

    public void ReadStartObject() {
        ReadStartContainer(DsonContextType.Object, DsonType.Object);
    }

    public void ReadEndObject() {
        ReadEndContainer(DsonContextType.Object);
    }

    public void ReadStartHeader() {
        ReadStartContainer(DsonContextType.Header, DsonType.Header);
    }

    public void ReadEndHeader() {
        ReadEndContainer(DsonContextType.Header);
    }

    public bool HasWaitingStartContext() {
        return waitStartContext != null;
    }

    public void BackToWaitStart() {
        Context context = this.context;
        if (context.contextType == DsonContextType.TopLevel) {
            throw DsonIOException.ContextErrorTopLevel();
        }
        if (context.state != DsonReaderState.Type) {
            throw InvalidState(CollectionUtil.NewList(DsonReaderState.Type));
        }
        waitStartContext = context;
        // 模拟ReadEnd
        RecoverDsonType(this.context);
        recursionDepth--;
        SetContext(context.parent);
        context.parent.SetState(DsonReaderState.Value); // 设置读Value状态
    }

    private void ReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context waitStartContext = this.waitStartContext;
        if (waitStartContext != null) {
            this.waitStartContext = null;
            // 模拟ReadStart
            recursionDepth++;
            SetContext(waitStartContext);
            SetNextState(); // 设置新上下文状态
            return;
        }

        if (recursionDepth >= settings.recursionLimit) {
            throw DsonIOException.RecursionLimitExceeded();
        }
        Context context = this.context;
        AutoStartTopLevel(context);
        EnsureValueState(context, dsonType);
        DoReadStartContainer(contextType, dsonType);
        SetNextState(); // 设置新上下文状态
    }

    private void ReadEndContainer(DsonContextType contextType) {
        Context context = this.context;
        CheckEndContext(context, contextType);
        DoReadEndContainer();
        SetNextState(); // parent前进一个状态
    }

    private void AutoStartTopLevel(Context context) {
        if (context.contextType == DsonContextType.TopLevel
            && (context.state == DsonReaderState.Initial || context.state == DsonReaderState.Type)) {
            ReadDsonType();
        }
    }

    private void CheckEndContext(Context context, DsonContextType contextType) {
        if (context.contextType != contextType) {
            throw DsonIOException.ContextError(contextType, context.contextType);
        }
        if (context.state != DsonReaderState.WaitEndObject) {
            throw InvalidState(CollectionUtil.NewList(DsonReaderState.WaitEndObject));
        }
    }

    /** 限用于读取容器后恢复上下文 */
    protected void RecoverDsonType(Context context) {
        this.currentDsonType = context.dsonType;
        this.currentWireType = WireType.VarInt;
        this.currentWireTypeBits = 0;
        this.currentName = context.name;
    }

    /** 创建新的context，保存信息，压入上下文 */
    protected abstract void DoReadStartContainer(DsonContextType contextType, DsonType dsonType);

    /** 恢复到旧的上下文，恢复{@link #currentDsonType}，弹出上下文 */
    protected abstract void DoReadEndContainer();

    #endregion

    #region 特殊

    public void SkipName() {
        Context context = GetContext();
        if (context.state == DsonReaderState.Value) {
            return;
        }
        if (context.state != DsonReaderState.Name) {
            throw InvalidState(CollectionUtil.NewList(DsonReaderState.Value, DsonReaderState.Name));
        }
        DoSkipName();
        currentName = default;
        context.SetState(DsonReaderState.Value);
    }

    public void SkipValue() {
        if (context.state != DsonReaderState.Value) {
            throw InvalidState(CollectionUtil.NewList(DsonReaderState.Value));
        }
        waitStartContext = null;
        DoSkipValue();
        SetNextState();
    }

    public void SkipToEndOfObject() {
        Context context = GetContext();
        if (context.contextType == DsonContextType.TopLevel) {
            throw DsonIOException.ContextErrorTopLevel();
        }
        if (currentDsonType == DsonType.EndOfObject) {
            Debug.Assert(context.state == DsonReaderState.WaitEndObject);
            return;
        }
        waitStartContext = null;
        DoSkipToEndOfObject();
        SetNextState();
        ReadDsonType(); // end of object
        Debug.Assert(currentDsonType == DsonType.EndOfObject);
    }

    public byte[] ReadValueAsBytes(TName name) {
        AdvanceToValueState(name, DsonTypes.INVALID);
        DsonReaderUtils.CheckReadValueAsBytes(currentDsonType);
        byte[] data = DoReadValueAsBytes();
        SetNextState();
        return data;
    }

    public object Attach(object userData) {
        return context.Attach(userData);
    }

    public object Attachment() {
        return context.userData;
    }

    public DsonReaderGuide WhatShouldIDo() {
        return DsonReaderUtils.WhatShouldIDo(context.contextType, context.state);
    }

    protected abstract void DoSkipName();

    protected abstract void DoSkipValue();

    protected abstract void DoSkipToEndOfObject();

    protected abstract byte[] DoReadValueAsBytes();

    #endregion

    #region context

    protected internal abstract class Context
    {
        protected internal Context parent;
        protected internal DsonContextType contextType;
        protected internal DsonType dsonType = DsonTypes.INVALID;
        protected internal DsonReaderState state = DsonReaderState.Initial;
        protected internal TName name;
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
            name = default;
            userData = null;
        }

        public object Attach(object userData) {
            var r = this.userData;
            this.userData = userData;
            return r;
        }

        /** 方便查看赋值的调用 */
        public void SetState(DsonReaderState state) {
            this.state = state;
        }

        public Context Parent => parent;
    }

    #endregion
}
}