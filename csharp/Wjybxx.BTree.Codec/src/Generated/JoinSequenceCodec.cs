#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Branch.Join;
using Wjybxx.Dson.Codec;
using System;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class JoinSequenceCodec<T> : AbstractDsonCodec<JoinSequence<T>> where T : class 
{
    public override Type GetEncoderType() => typeof(JoinSequence<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in JoinSequence<T> inst) {
    }

    protected override JoinSequence<T> NewInstance(IDsonObjectReader reader) {
        return JoinSequence<T>.GetInstance();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref JoinSequence<T> inst) {
    }
}
}
