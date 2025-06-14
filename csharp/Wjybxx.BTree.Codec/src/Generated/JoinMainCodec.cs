#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Branch.Join;
using Wjybxx.Dson.Codec;
using System;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class JoinMainCodec<T> : AbstractDsonCodec<JoinMain<T>> where T : class 
{
    public override Type GetEncoderType() => typeof(JoinMain<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in JoinMain<T> inst) {
    }

    protected override JoinMain<T> NewInstance(IDsonObjectReader reader) {
        return JoinMain<T>.GetInstance();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref JoinMain<T> inst) {
    }
}
}
