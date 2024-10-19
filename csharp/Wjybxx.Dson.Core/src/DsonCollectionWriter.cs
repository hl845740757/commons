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
/// 将对象写入<see cref="DsonArray{TName}"/>
/// </summary>
/// <typeparam name="TName"></typeparam>
public sealed class DsonCollectionWriter<TName> : AbstractDsonWriter<TName> where TName : IEquatable<TName>
{
#nullable disable
    private readonly DsonArray<TName> outList;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="settings">配置</param>
    /// <param name="outList">接收编码结果</param>
    public DsonCollectionWriter(DsonWriterSettings settings, DsonArray<TName> outList)
        : base(settings) {
        this.outList = outList ?? throw new ArgumentNullException(nameof(outList));

        Context context = NewContext(null, DsonContextType.TopLevel, DsonTypes.INVALID);
        context.container = outList;
        SetContext(context);
    }


    /// <summary>
    /// 获取传入的OutList
    /// </summary>
    public DsonArray<TName> OutList => outList; // 不能通过Context查询，close后context会被清理

    private new Context GetContext() {
        return (Context)context;
    }

    public override void Flush() {
    }

    public override void Dispose() {
        Context context = GetContext();
        SetContext(null);
        while (context != null) {
            Context parent = context.Parent;
            contextPool.Release(context);
            context = parent;
        }
        base.Dispose();
    }

    #region 简单值

    protected override void DoWriteInt32(int value, WireType wireType, INumberStyle style) {
        GetContext().Add(new DsonInt32(value));
    }

    protected override void DoWriteInt64(long value, WireType wireType, INumberStyle style) {
        GetContext().Add(new DsonInt64(value));
    }

    protected override void DoWriteFloat(float value, INumberStyle style) {
        GetContext().Add(new DsonFloat(value));
    }

    protected override void DoWriteDouble(double value, INumberStyle style) {
        GetContext().Add(new DsonDouble(value));
    }

    protected override void DoWriteBool(bool value) {
        GetContext().Add(DsonBool.ValueOf(value));
    }

    protected override void DoWriteString(string value, StringStyle style) {
        GetContext().Add(new DsonString(value));
    }

    protected override void DoWriteNull() {
        GetContext().Add(DsonNull.NULL);
    }

    protected override void DoWriteBinary(Binary binary) {
        GetContext().Add(new DsonBinary(binary)); // binary默认为可共享的
    }

    protected override void DoWriteBinary(byte[] bytes, int offset, int len) {
        GetContext().Add(new DsonBinary(Binary.CopyFrom(bytes, offset, len)));
    }

    protected override void DoWritePtr(in ObjectPtr objectPtr) {
        GetContext().Add(new DsonPointer(objectPtr));
    }

    protected override void DoWriteLitePtr(in ObjectLitePtr objectLitePtr) {
        GetContext().Add(new DsonLitePointer(objectLitePtr));
    }

    protected override void DoWriteDateTime(in ExtDateTime dateTime) {
        GetContext().Add(new DsonDateTime(dateTime));
    }

    protected override void DoWriteTimestamp(in Timestamp timestamp) {
        GetContext().Add(new DsonTimestamp(timestamp));
    }

    #endregion

    #region 容器

    protected override void DoWriteStartContainer(DsonContextType contextType, DsonType dsonType, ObjectStyle style) {
        Context parent = GetContext();
        Context newContext = NewContext(parent, contextType, dsonType);
        switch (contextType) {
            case DsonContextType.Header: {
                newContext.container = parent.GetHeader();
                break;
            }
            case DsonContextType.Array: {
                newContext.container = new DsonArray<TName>();
                break;
            }
            case DsonContextType.Object: {
                newContext.container = new DsonObject<TName>();
                break;
            }
            default: throw new InvalidOperationException();
        }

        SetContext(newContext);
        this.recursionDepth++;
    }

    protected override void DoWriteEndContainer() {
        Context context = GetContext();
        if (context.contextType != DsonContextType.Header) {
            context.Parent!.Add(context.container);
        }

        this.recursionDepth--;
        SetContext(context.Parent!);
        ReturnContext(context);
    }

    #endregion

    #region 特殊

    protected override void DoWriteValueBytes(DsonType type, byte[] data) {
        throw new InvalidOperationException("Unsupported operation");
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
        protected internal DsonValue container;

        public Context() {
        }

        public new Context Parent => (Context)parent;

        public override void Reset() {
            base.Reset();
            container = null;
        }

        public DsonHeader<TName> GetHeader() {
            if (container.DsonType == DsonType.Object) {
                return container.AsObject<TName>().Header;
            } else {
                return container.AsArray<TName>().Header;
            }
        }

        public void Add(DsonValue value) {
            if (container.DsonType == DsonType.Object) {
                container.AsObject<TName>().Append(curName, value);
            } else if (container.DsonType == DsonType.Array) {
                container.AsArray<TName>().Add(value);
            } else {
                container.AsHeader<TName>().Append(curName, value);
            }
        }
    }

    #endregion
}
}