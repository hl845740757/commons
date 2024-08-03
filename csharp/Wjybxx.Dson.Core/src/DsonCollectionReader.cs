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
using System.Collections;
using System.Collections.Generic;
using Wjybxx.Commons.Collections;
using Wjybxx.Commons.Pool;
using Wjybxx.Dson.Ext;
using Wjybxx.Dson.Internal;
using Wjybxx.Dson.IO;
using Wjybxx.Dson.Types;

namespace Wjybxx.Dson
{
/// <summary>
/// 从<see cref="DsonArray{TK}"/>中读取输入
/// </summary>
/// <typeparam name="TName"></typeparam>
public sealed class DsonCollectionReader<TName> : AbstractDsonReader<TName> where TName : IEquatable<TName>
{
#nullable disable
    private TName _nextName;
    private DsonValue _nextValue;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="dsonArray">输入</param>
    public DsonCollectionReader(DsonReaderSettings settings, DsonArray<TName> dsonArray)
        : base(settings) {
        if (dsonArray == null) throw new ArgumentNullException(nameof(dsonArray));

        Context context = NewContext(null, DsonContextType.TopLevel, DsonTypes.INVALID);
        context.header = dsonArray.Header.Count > 0 ? dsonArray.Header : null;
        context.arrayIterator = NewArrayIterator(dsonArray.GetEnumerator());
        SetContext(context);
    }

    /// <summary>
    /// 设置key的迭代顺序。
    /// 注意：这期间不能触发<see cref="PeekDsonType"/>等可能导致mark的操作，
    ///  mark操作会导致key缓存到本地，从而使得外部的keyItr无效。
    /// </summary>
    /// <param name="keyItr">Key的迭代器</param>
    /// <param name="defValue">key不存在时的返回值</param>
    public void SetKeyItr(ISequentialEnumerator<TName> keyItr, DsonValue defValue) {
        if (keyItr == null) throw new ArgumentNullException(nameof(keyItr));
        if (defValue == null) throw new ArgumentNullException(nameof(defValue));
        Context context = GetContext();
        if (context.dsonObject == null) {
            throw DsonIOException.ContextError(CollectionUtil.NewList(DsonContextType.Object, DsonContextType.Header), context.contextType);
        }
        context.SetKeyItr(keyItr, defValue);
    }

    /// <summary>
    /// 获取当前对象的所有key。
    /// 注意：不可修改返回的集合。
    /// </summary>
    /// <returns></returns>
    /// <exception cref="DsonIOException"></exception>
    public ICollection<TName> Keys() {
        Context context = GetContext();
        if (context.dsonObject == null) {
            throw DsonIOException.ContextError(CollectionUtil.NewList(DsonContextType.Object, DsonContextType.Header), context.contextType);
        }
        return context.dsonObject.Keys;
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
        _nextName = default;
        _nextValue = null;
        base.Dispose();
    }

    #region state

    private void PushNextValue(DsonValue nextValue) {
        if (nextValue == null) throw new ArgumentNullException(nameof(nextValue));
        this._nextValue = nextValue;
    }

    private DsonValue PopNextValue() {
        DsonValue r = this._nextValue;
        this._nextValue = null;
        return r!;
    }

    private void PushNextName(TName nextName) {
        this._nextName = nextName;
    }

    private TName PopNextName() {
        TName r = this._nextName;
        this._nextName = default;
        return r;
    }

    public override DsonType ReadDsonType() {
        Context context = GetContext();
        CheckReadDsonTypeState(context);

        PopNextName();
        PopNextValue();

        DsonType dsonType;
        if (context.header != null) { // 需要先读取header
            dsonType = DsonType.Header;
            PushNextValue(context.header);
            context.header = null;
        } else if (context.contextType.IsArrayLike()) {
            DsonValue nextValue = context.NextValue();
            if (nextValue == null) {
                dsonType = DsonType.EndOfObject;
            } else {
                PushNextValue(nextValue);
                dsonType = nextValue.DsonType;
            }
        } else {
            KeyValuePair<TName, DsonValue>? nextElement = context.NextElement();
            if (!nextElement.HasValue) {
                dsonType = DsonType.EndOfObject;
            } else {
                PushNextName(nextElement.Value.Key);
                PushNextValue(nextElement.Value.Value);
                dsonType = nextElement.Value.Value.DsonType;
            }
        }

        this.currentDsonType = dsonType;
        this.currentWireType = WireType.VarInt;
        this.currentName = default;

        OnReadDsonType(context, dsonType);
        return dsonType;
    }

    public override DsonType PeekDsonType() {
        Context context = this.GetContext();
        CheckReadDsonTypeState(context);

        if (context.header != null) {
            return DsonType.Header;
        }
        if (!context.HasNext()) {
            return DsonType.EndOfObject;
        }
        if (context.contextType.IsArrayLike()) {
            context.MarkItr();
            DsonValue nextValue = context.NextValue();
            context.ResetItr();
            return nextValue!.DsonType;
        } else {
            context.MarkItr();
            KeyValuePair<TName, DsonValue>? nextElement = context.NextElement();
            context.ResetItr();
            return nextElement!.Value.Value.DsonType;
        }
    }

    protected override void DoReadName() {
        currentName = PopNextName();
    }

    #endregion

    #region 简单值

    protected override int DoReadInt32() {
        return PopNextValue().AsInt32();
    }

    protected override long DoReadInt64() {
        return PopNextValue().AsInt64();
    }

    protected override float DoReadFloat() {
        return PopNextValue().AsFloat();
    }

    protected override double DoReadDouble() {
        return PopNextValue().AsDouble();
    }

    protected override bool DoReadBool() {
        return PopNextValue().AsBool();
    }

    protected override string DoReadString() {
        return PopNextValue().AsString();
    }

    protected override void DoReadNull() {
        PopNextValue();
    }

    protected override Binary DoReadBinary() {
        return PopNextValue().AsBinary().DeepCopy(); // 需要拷贝
    }

    protected override ObjectPtr DoReadPtr() {
        return PopNextValue().AsPointer();
    }

    protected override ObjectLitePtr DoReadLitePtr() {
        return PopNextValue().AsLitePointer();
    }

    protected override ExtDateTime DoReadDateTime() {
        return PopNextValue().AsDateTime();
    }

    protected override Timestamp DoReadTimestamp() {
        return PopNextValue().AsTimestamp();
    }

    #endregion

    #region 容器

    protected override void DoReadStartContainer(DsonContextType contextType, DsonType dsonType) {
        Context newContext = NewContext(GetContext(), contextType, dsonType);
        DsonValue dsonValue = PopNextValue();
        if (dsonValue.DsonType == DsonType.Object) {
            DsonObject<TName> dsonObject = dsonValue.AsObject<TName>();
            newContext.header = dsonObject.Header.Count > 0 ? dsonObject.Header : null;
            newContext.dsonObject = dsonObject;
            newContext.objectIterator = NewObjectIterator(dsonObject.GetEnumerator());
        } else if (dsonValue.DsonType == DsonType.Array) {
            DsonArray<TName> dsonArray = dsonValue.AsArray<TName>();
            newContext.header = dsonArray.Header.Count > 0 ? dsonArray.Header : null;
            newContext.arrayIterator = NewArrayIterator(dsonArray.GetEnumerator());
        } else {
            // 其它内置结构体
            newContext.dsonObject = dsonValue.AsHeader<TName>();
            newContext.objectIterator = NewObjectIterator(dsonValue.AsHeader<TName>().GetEnumerator());
        }
        newContext.name = currentName;

        this.recursionDepth++;
        SetContext(newContext);
    }

    protected override void DoReadEndContainer() {
        Context context = GetContext();

        // 恢复上下文
        RecoverDsonType(context);
        this.recursionDepth--;
        SetContext(context.parent!);
        ReturnContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoSkipName() {
        PopNextName();
    }

    protected override void DoSkipValue() {
        PopNextValue();
    }

    protected override void DoSkipToEndOfObject() {
        Context context = GetContext();
        context.header = null;
        if (context.arrayIterator != null) {
            context.arrayIterator.ForEachRemaining(_ => { });
        } else {
            context.objectIterator!.ForEachRemaining(_ => { });
        }
    }

    protected override byte[] DoReadValueAsBytes() {
        throw new InvalidOperationException("Unsupported operation");
    }

    #endregion

    #region itrpool

    private static readonly ConcurrentObjectPool<MarkableIterator<KeyValuePair<TName, DsonValue>>> objectItrPool =
        new(() => new MarkableIterator<KeyValuePair<TName, DsonValue>>(), itr => itr.Dispose(),
            DsonInternals.CONTEXT_POOL_SIZE);
    private static ConcurrentObjectPool<MarkableIterator<DsonValue>> arrayItrPool => DsonInternals.arrayItrPool;

    private static MarkableIterator<DsonValue> NewArrayIterator(IEnumerator<DsonValue> baseIterator) {
        var markableIterator = arrayItrPool.Acquire();
        markableIterator.Init(baseIterator);
        return markableIterator;
    }

    private static MarkableIterator<KeyValuePair<TName, DsonValue>> NewObjectIterator(IEnumerator<KeyValuePair<TName, DsonValue>> baseIterator) {
        var markableIterator = objectItrPool.Acquire();
        markableIterator.Init(baseIterator);
        return markableIterator;
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
        /** 如果不为null，则表示需要先读取header */
        protected internal DsonHeader<TName> header;
        protected internal AbstractDsonObject<TName> dsonObject;
        protected internal MarkableIterator<KeyValuePair<TName, DsonValue>> objectIterator;
        protected internal MarkableIterator<DsonValue> arrayIterator;

        public Context() {
        }

        public new Context Parent => (Context)parent;

        public override void Reset() {
            base.Reset();
            header = null;
            dsonObject = null;
            if (objectIterator != null) {
                objectItrPool.Release(objectIterator);
                objectIterator = null;
            }
            if (arrayIterator != null) {
                arrayItrPool.Release(arrayIterator);
                arrayIterator = null;
            }
        }

        public void SetKeyItr(ISequentialEnumerator<TName> keyItr, DsonValue defValue) {
            if (dsonObject == null) throw new InvalidOperationException();
            if (objectIterator!.IsMarking) throw new InvalidOperationException("reader is in marking state");
            objectIterator = NewObjectIterator(new KeyIterator(dsonObject, keyItr, defValue));
        }

        public bool HasNext() {
            if (objectIterator != null) {
                return objectIterator.HasNext();
            } else {
                return arrayIterator!.HasNext();
            }
        }

        public void MarkItr() {
            if (objectIterator != null) {
                objectIterator.Mark();
            } else {
                arrayIterator!.Mark();
            }
        }

        public void ResetItr() {
            if (objectIterator != null) {
                objectIterator.Reset();
            } else {
                arrayIterator!.Reset();
            }
        }

        public DsonValue NextValue() {
            return arrayIterator!.HasNext() ? arrayIterator.Next() : null;
        }

        public KeyValuePair<TName, DsonValue>? NextElement() {
            return objectIterator!.HasNext() ? objectIterator.Next() : null;
        }
    }

    private class KeyIterator : ISequentialEnumerator<KeyValuePair<TName, DsonValue>>
    {
        private readonly AbstractDsonObject<TName> _dsonObject;
        private readonly ISequentialEnumerator<TName> _keyItr;
        private readonly DsonValue _defValue;

        public KeyIterator(AbstractDsonObject<TName> dsonObject, ISequentialEnumerator<TName> keyItr, DsonValue defValue) {
            this._dsonObject = dsonObject;
            this._keyItr = keyItr;
            this._defValue = defValue;
        }

        public bool HasNext() {
            return _keyItr.HasNext();
        }

        public bool MoveNext() {
            return _keyItr.MoveNext();
        }

        public KeyValuePair<TName, DsonValue> Current {
            get {
                TName key = _keyItr.Current;
                if (_dsonObject.TryGetValue(key!, out DsonValue dsonValue)) {
                    return new KeyValuePair<TName, DsonValue>(key, dsonValue);
                } else {
                    return new KeyValuePair<TName, DsonValue>(key, _defValue);
                }
            }
        }

        public void Reset() {
            _keyItr.Reset();
        }

        object IEnumerator.Current => Current;

        public void Dispose() {
        }
    }

    #endregion
}
}