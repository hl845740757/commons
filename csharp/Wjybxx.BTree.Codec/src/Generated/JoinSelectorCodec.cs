#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Branch.Join;
using Wjybxx.Dson.Codec;
using System;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class JoinSelectorCodec<T> : AbstractDsonCodec<JoinSelector<T>> where T : class 
{
    public override Type GetEncoderType() => typeof(JoinSelector<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in JoinSelector<T> inst) {
    }

    protected override JoinSelector<T> NewInstance(IDsonObjectReader reader) {
        return JoinSelector<T>.GetInstance();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref JoinSelector<T> inst) {
    }
}
}
