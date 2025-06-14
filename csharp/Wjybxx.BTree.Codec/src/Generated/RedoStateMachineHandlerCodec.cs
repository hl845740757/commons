#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.FSM.Handler;
using Wjybxx.Dson.Codec;
using System;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class RedoStateMachineHandlerCodec<T> : AbstractDsonCodec<RedoStateMachineHandler<T>> where T : class 
{
    public override Type GetEncoderType() => typeof(RedoStateMachineHandler<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in RedoStateMachineHandler<T> inst) {
    }

    protected override RedoStateMachineHandler<T> NewInstance(IDsonObjectReader reader) {
        return RedoStateMachineHandler<T>.Inst;
    }

    protected override void ReadFields(IDsonObjectReader reader, ref RedoStateMachineHandler<T> inst) {
    }
}
}
