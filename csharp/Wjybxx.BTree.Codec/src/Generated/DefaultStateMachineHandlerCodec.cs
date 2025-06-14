#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.FSM.Handler;
using Wjybxx.Dson.Codec;
using System;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class DefaultStateMachineHandlerCodec<T> : AbstractDsonCodec<DefaultStateMachineHandler<T>> where T : class 
{
    public override Type GetEncoderType() => typeof(DefaultStateMachineHandler<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in DefaultStateMachineHandler<T> inst) {
    }

    protected override DefaultStateMachineHandler<T> NewInstance(IDsonObjectReader reader) {
        return DefaultStateMachineHandler<T>.Inst;
    }

    protected override void ReadFields(IDsonObjectReader reader, ref DefaultStateMachineHandler<T> inst) {
    }
}
}
