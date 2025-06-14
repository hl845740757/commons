#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.FSM.Handler;
using Wjybxx.Dson.Codec;
using System;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class UndoStateMachineHandlerCodec<T> : AbstractDsonCodec<UndoStateMachineHandler<T>> where T : class 
{
    public override Type GetEncoderType() => typeof(UndoStateMachineHandler<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in UndoStateMachineHandler<T> inst) {
    }

    protected override UndoStateMachineHandler<T> NewInstance(IDsonObjectReader reader) {
        return UndoStateMachineHandler<T>.Inst;
    }

    protected override void ReadFields(IDsonObjectReader reader, ref UndoStateMachineHandler<T> inst) {
    }
}
}
