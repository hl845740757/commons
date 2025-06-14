#pragma warning disable CS1591

using Wjybxx.Commons.Attributes;
using Wjybxx.BTree.Branch.Join;
using Wjybxx.Dson.Codec;
using System;

namespace Wjybxx.BTree.Codecs
{
[Generated("Wjybxx.Dson.Apt2.CodecProcessor")]
public sealed class JoinWaitAllCodec<T> : AbstractDsonCodec<JoinWaitAll<T>> where T : class 
{
    public override Type GetEncoderType() => typeof(JoinWaitAll<T>);

    protected override void WriteFields(IDsonObjectWriter writer, in JoinWaitAll<T> inst) {
    }

    protected override JoinWaitAll<T> NewInstance(IDsonObjectReader reader) {
        return JoinWaitAll<T>.GetInstance();
    }

    protected override void ReadFields(IDsonObjectReader reader, ref JoinWaitAll<T> inst) {
    }
}
}
